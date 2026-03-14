import AppKit
import Combine
import Foundation

struct InputDocument: Identifiable, Equatable {
    let id: UUID
    let url: URL

    init(id: UUID = UUID(), url: URL) {
        self.id = id
        self.url = url
    }

    var displayName: String {
        url.lastPathComponent
    }

    var directoryPath: String {
        url.deletingLastPathComponent().path
    }
}

struct MergeActivityItem: Identifiable, Equatable {
    enum State: Equatable {
        case active
        case complete
        case success
        case failed
        case cancelled
    }

    let id: UUID
    var title: String
    var detail: String?
    var state: State

    init(
        id: UUID = UUID(),
        title: String,
        detail: String? = nil,
        state: State
    ) {
        self.id = id
        self.title = title
        self.detail = detail
        self.state = state
    }
}

extension MergeViewModel.Phase {
    var showsCompletedArtifacts: Bool {
        if case .succeeded = self {
            return true
        }

        return false
    }
}

@MainActor
final class MergeViewModel: ObservableObject {
    enum Phase: Equatable {
        case idle
        case running
        case succeeded(outputURL: URL, reportURL: URL)
        case failed(message: String)
        case cancelled
    }

    @Published private(set) var inputItems: [InputDocument] = []
    private(set) var deckRows: [DeckRowEntry] = []
    @Published var selectedInputIDs: Set<InputDocument.ID> = []
    @Published private(set) var phase: Phase = .idle
    @Published private(set) var statusText = ""
    @Published private(set) var progressValue = 0.0
    @Published private(set) var showsProgress = false
    @Published private(set) var reportPreview = ""
    @Published private(set) var reportActivities: [MergeActivityItem] = []
    @Published private(set) var resolvedOutputURL: URL?
    @Published private(set) var resolvedReportURL: URL?
    @Published private(set) var outputOverrideURL: URL?
    @Published var insertSourceFileTitles: Bool {
        didSet {
            preferences.setInsertSourceFileTitles(insertSourceFileTitles)
        }
    }

    private let runner: MergeRunner
    private let preferences: AppPreferencesStore

    private var currentRun: MergeRunControlling?
    private var activeRunToken: UUID?

    init(
        runner: MergeRunner = ProcessMergeRunner(),
        preferences: AppPreferencesStore = AppPreferences()
    ) {
        self.runner = runner
        self.preferences = preferences
        self.insertSourceFileTitles = preferences.insertSourceFileTitles
        refreshResolvedPaths()
    }

    var preferredInputDirectory: URL? {
        preferences.lastInputDirectory
    }

    var preferredOutputDirectory: URL? {
        if let outputOverrideURL {
            return outputOverrideURL.deletingLastPathComponent()
        }

        if let resolvedOutputURL {
            return resolvedOutputURL.deletingLastPathComponent()
        }

        return preferences.lastOutputDirectory
    }

    var canMerge: Bool {
        !inputItems.isEmpty && resolvedOutputURL != nil && currentRun == nil
    }

    var canCancel: Bool {
        currentRun != nil
    }

    var canRemoveSelected: Bool {
        !selectedInputIDs.isEmpty
    }

    var canMoveSelectionUp: Bool {
        selectedIndices.contains { $0 > 0 }
    }

    var canMoveSelectionDown: Bool {
        selectedIndices.contains { $0 < inputItems.count - 1 }
    }

    var successOutputURL: URL? {
        if case .succeeded(let outputURL, _) = phase {
            return outputURL
        }

        return nil
    }

    var successReportURL: URL? {
        if case .succeeded(_, let reportURL) = phase {
            return reportURL
        }

        return nil
    }

    func isInputSelected(_ id: InputDocument.ID) -> Bool {
        selectedInputIDs.contains(id)
    }

    func selectInput(id: InputDocument.ID, additive: Bool = false) {
        if additive {
            if selectedInputIDs.contains(id) {
                selectedInputIDs.remove(id)
            } else {
                selectedInputIDs.insert(id)
            }

            return
        }

        if selectedInputIDs == [id] {
            selectedInputIDs.removeAll()
        } else {
            selectedInputIDs = [id]
        }
    }

    func addInputURLs(_ urls: [URL]) {
        let filtered = urls
            .filter { $0.pathExtension.lowercased() == "docx" }
            .map { InputDocument(url: $0) }

        guard !filtered.isEmpty else {
            return
        }

        setInputItems(inputItems + filtered)
        refreshResolvedPaths()
        preferences.setLastInputDirectory(filtered[0].url.deletingLastPathComponent())

        if outputOverrideURL == nil, let outputURL = resolvedOutputURL {
            preferences.setLastOutputDirectory(outputURL.deletingLastPathComponent())
        }

        resetOutcomeIfNeeded()
        statusText = "\(inputItems.count) document\(inputItems.count == 1 ? "" : "s") ready."
    }

    func removeSelectedInputs() {
        setInputItems(inputItems.filter { !selectedInputIDs.contains($0.id) })
        selectedInputIDs.removeAll()
        resetAfterInputMutation()
    }

    func removeInput(id: InputDocument.ID) {
        setInputItems(inputItems.filter { $0.id != id })
        selectedInputIDs.remove(id)
        resetAfterInputMutation()
    }

    func clearInputs() {
        setInputItems([])
        selectedInputIDs.removeAll()
        outputOverrideURL = nil
        refreshResolvedPaths()
        phase = .idle
        statusText = ""
        showsProgress = false
        progressValue = 0
        reportPreview = ""
        reportActivities = []
    }

    func moveSelectionUp() {
        moveSelected(delta: -1)
    }

    func moveSelectionDown() {
        moveSelected(delta: 1)
    }

    func setOutputURL(_ url: URL?) {
        outputOverrideURL = url
        refreshResolvedPaths()

        if let url {
            preferences.setLastOutputDirectory(url.deletingLastPathComponent())
        }
    }

    func resetToSuggestedOutput() {
        outputOverrideURL = nil
        refreshResolvedPaths()
    }

    func startMerge() {
        guard currentRun == nil, let outputURL = resolvedOutputURL else {
            return
        }

        let request = AppRunRequestBuilder.build(
            inputs: inputItems.map(\.url),
            outputURL: outputURL,
            insertSourceFileTitles: insertSourceFileTitles
        )

        let runToken = UUID()
        activeRunToken = runToken
        phase = .running
        statusText = "Preparing merge..."
        progressValue = 0.02
        showsProgress = true
        reportPreview = ""
        reportActivities = []
        beginActivity("Setup", detail: "Preparing merge...")
        preferences.setLastOutputDirectory(outputURL.deletingLastPathComponent())

        do {
            currentRun = try runner.start(request: request) { [weak self] result in
                Task { @MainActor in
                    self?.handle(result, for: runToken)
                }
            }
        } catch {
            applyFailure(message: error.localizedDescription, reportURL: nil)
        }
    }

    func cancelMerge() {
        activeRunToken = nil
        currentRun?.cancel()
        currentRun = nil
        phase = .cancelled
        statusText = "Merge cancelled."
        showsProgress = false
        cancelCurrentActivity(with: "Merge cancelled.")
    }

    func openOutput() {
        guard let url = successOutputURL else {
            return
        }

        NSWorkspace.shared.open(url)
    }

    func revealOutput() {
        guard let url = successOutputURL else {
            return
        }

        NSWorkspace.shared.activateFileViewerSelecting([url])
    }

    func showReport() {
        guard let url = successReportURL ?? resolvedReportURL else {
            return
        }

        NSWorkspace.shared.open(url)
    }

    private var selectedIndices: [Int] {
        inputItems.enumerated()
            .compactMap { index, item in
                selectedInputIDs.contains(item.id) ? index : nil
            }
            .sorted()
    }

    private func moveSelected(delta: Int) {
        let indices = selectedIndices
        guard !indices.isEmpty else {
            return
        }

        var updated = inputItems
        let isMovingUp = delta < 0
        let orderedIndices = isMovingUp ? indices : indices.reversed()

        for index in orderedIndices {
            let destination = index + delta
            guard updated.indices.contains(destination) else {
                continue
            }

            updated.swapAt(index, destination)
        }

        setInputItems(updated)
        resetOutcomeIfNeeded()
    }

    private func setInputItems(_ items: [InputDocument]) {
        inputItems = items
        deckRows = DeckRowEntry.makeEntries(for: items)
    }

    private func resetAfterInputMutation() {
        refreshResolvedPaths()

        if inputItems.isEmpty {
            phase = .idle
            statusText = ""
            showsProgress = false
            progressValue = 0
            reportPreview = ""
            reportActivities = []
        } else {
            resetOutcomeIfNeeded()
            statusText = "\(inputItems.count) document\(inputItems.count == 1 ? "" : "s") ready."
        }
    }

    private func resetOutcomeIfNeeded() {
        if case .running = phase {
            return
        }

        phase = .idle
        showsProgress = false
        progressValue = 0
        reportPreview = ""
        reportActivities = []
    }

    private func refreshResolvedPaths() {
        let outputURL = outputOverrideURL ?? AppRunRequestBuilder.defaultOutputURL(for: inputItems.map(\.url))
        resolvedOutputURL = outputURL
        resolvedReportURL = outputURL.map(AppRunRequestBuilder.defaultReportURL(for:))
    }

    private func handle(_ result: Result<HelperEvent, Error>, for runToken: UUID) {
        guard activeRunToken == runToken else {
            return
        }

        switch result {
        case .success(let event):
            handle(event, for: runToken)
        case .failure(let error):
            applyFailure(message: error.localizedDescription, reportURL: nil)
        }
    }

    private func handle(_ event: HelperEvent, for runToken: UUID) {
        guard activeRunToken == runToken else {
            return
        }

        switch event {
        case .started(let message):
            statusText = message ?? "Merge started."
            progressValue = 0.05
            updateCurrentActivityDetail(message ?? "Merge started.")
        case .stage(let stageEvent):
            let stageMessage = stageText(for: stageEvent)
            statusText = stageMessage
            progressValue = progress(for: stageEvent)
            beginActivity(stageActivityTitle(for: stageEvent.stage), detail: stageMessage)
        case .completed(let outputPath, let reportPath):
            currentRun = nil
            activeRunToken = nil
            showsProgress = false
            progressValue = 1

            let outputURL = URL(fileURLWithPath: outputPath)
            let reportURL = URL(fileURLWithPath: reportPath)
            phase = .succeeded(outputURL: outputURL, reportURL: reportURL)
            statusText = "Merge successful."
            reportPreview = loadReportPreview(from: reportURL)
            completeCurrentActivity()
            appendTerminalActivity("Done", detail: "Merge successful.", state: .success)
        case .failed(let message, let reportPath):
            applyFailure(
                message: message,
                reportURL: reportPath.map(URL.init(fileURLWithPath:))
            )
        }
    }

    private func applyFailure(message: String, reportURL: URL?) {
        currentRun = nil
        activeRunToken = nil
        phase = .failed(message: message)
        statusText = message
        showsProgress = false
        reportPreview = reportURL.map(loadReportPreview(from:)) ?? ""
        failCurrentActivity(with: message)
    }

    private func loadReportPreview(from url: URL) -> String {
        (try? String(contentsOf: url, encoding: .utf8)) ?? "Report saved to \(url.path)"
    }

    private func stageText(for event: MergeStageEvent) -> String {
        if let message = event.message, !message.isEmpty {
            return message
        }

        switch event.stage {
        case .starting:
            return "Preparing merge..."
        case .preflight:
            return "Running preflight checks..."
        case .mergingInput:
            if let current = event.currentInputIndex, let total = event.totalInputs {
                let label = event.inputDisplayName.map { " \($0)" } ?? ""
                return "Merging \(current) of \(total)\(label)"
            }

            return "Merging documents..."
        case .validation:
            return "Validating output..."
        case .writingReport:
            return "Writing merge report..."
        case .completed:
            return "Finishing merge..."
        }
    }

    private func beginActivity(_ title: String, detail: String? = nil) {
        let normalizedTitle = normalizedActivityTitle(title)
        guard !normalizedTitle.isEmpty else {
            return
        }

        if let lastActivity = reportActivities.last,
           lastActivity.state == .active,
           normalizedActivityTitle(lastActivity.title) == normalizedTitle {
            return
        }

        completeCurrentActivity()
        reportActivities.append(
            MergeActivityItem(
                title: title,
                detail: detail,
                state: .active
            )
        )
    }

    private func completeCurrentActivity() {
        guard let lastIndex = reportActivities.lastIndex(where: { $0.state == .active }) else {
            return
        }

        reportActivities[lastIndex].state = .complete
    }

    private func appendTerminalActivity(
        _ title: String,
        detail: String? = nil,
        state: MergeActivityItem.State
    ) {
        reportActivities.append(
            MergeActivityItem(
                title: title,
                detail: detail,
                state: state
            )
        )
    }

    private func failCurrentActivity(with message: String) {
        if let lastIndex = reportActivities.lastIndex(where: { $0.state == .active }) {
            reportActivities[lastIndex].state = .failed
            reportActivities[lastIndex].detail = message
            return
        }

        appendTerminalActivity(message, state: .failed)
    }

    private func cancelCurrentActivity(with message: String) {
        if let lastIndex = reportActivities.lastIndex(where: { $0.state == .active }) {
            reportActivities[lastIndex].state = .cancelled
            reportActivities[lastIndex].detail = message
            return
        }

        appendTerminalActivity("Stopped", detail: message, state: .cancelled)
    }

    private func normalizedActivityTitle(_ title: String) -> String {
        title.trimmingCharacters(in: .whitespacesAndNewlines)
    }

    private func updateCurrentActivityDetail(_ detail: String) {
        guard let lastIndex = reportActivities.lastIndex(where: { $0.state == .active }) else {
            return
        }

        reportActivities[lastIndex].detail = detail
    }

    private func stageActivityTitle(for stage: MergeStage) -> String {
        switch stage {
        case .starting:
            return "Setup"
        case .preflight:
            return "Preflight"
        case .mergingInput:
            return "Merge"
        case .validation:
            return "Validation"
        case .writingReport:
            return "Report"
        case .completed:
            return "Finish"
        }
    }

    private func progress(for event: MergeStageEvent) -> Double {
        switch event.stage {
        case .starting:
            return 0.08
        case .preflight:
            return 0.16
        case .mergingInput:
            guard let current = event.currentInputIndex, let total = event.totalInputs, total > 0 else {
                return 0.45
            }

            let normalized = Double(current) / Double(total)
            return 0.2 + (normalized * 0.55)
        case .validation:
            return 0.82
        case .writingReport:
            return 0.92
        case .completed:
            return 1
        }
    }
}
