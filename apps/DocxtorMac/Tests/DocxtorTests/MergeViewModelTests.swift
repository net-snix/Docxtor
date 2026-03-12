import XCTest
@testable import Docxtor

@MainActor
final class MergeViewModelTests: XCTestCase {
    private struct ActivitySnapshot: Equatable {
        let title: String
        let detail: String?
        let state: MergeActivityItem.State
    }

    func testIdleStateStartsWithoutPromptCopy() {
        let viewModel = MergeViewModel(runner: TestMergeRunner(), preferences: TestPreferences())

        XCTAssertEqual(viewModel.phase, .idle)
        XCTAssertEqual(viewModel.statusText, "")
    }

    func testClearInputsRestoresEmptyIdleStatus() {
        let viewModel = MergeViewModel(runner: TestMergeRunner(), preferences: TestPreferences())

        viewModel.addInputURLs([URL(fileURLWithPath: "/tmp/docs/one.docx")])
        XCTAssertEqual(viewModel.statusText, "1 document ready.")

        viewModel.clearInputs()

        XCTAssertEqual(viewModel.phase, .idle)
        XCTAssertEqual(viewModel.statusText, "")
    }

    func testShowsCompletedArtifactsOnlyForSuccess() {
        XCTAssertFalse(MergeViewModel.Phase.idle.showsCompletedArtifacts)
        XCTAssertFalse(MergeViewModel.Phase.running.showsCompletedArtifacts)
        XCTAssertFalse(MergeViewModel.Phase.failed(message: "Nope").showsCompletedArtifacts)
        XCTAssertFalse(MergeViewModel.Phase.cancelled.showsCompletedArtifacts)
        XCTAssertTrue(
            MergeViewModel.Phase.succeeded(
                outputURL: URL(fileURLWithPath: "/tmp/main.docx"),
                reportURL: URL(fileURLWithPath: "/tmp/main.merge-report.json")
            ).showsCompletedArtifacts
        )
    }

    func testStartMergeTransitionsToSuccess() async throws {
        let runner = TestMergeRunner()
        let preferences = TestPreferences()
        let viewModel = MergeViewModel(runner: runner, preferences: preferences)
        let reportURL = try makeTempFile(named: "report.json", contents: #"{"status":"ok"}"#)

        viewModel.addInputURLs([
            URL(fileURLWithPath: "/tmp/docs/one.docx"),
            URL(fileURLWithPath: "/tmp/docs/two.docx")
        ])

        viewModel.startMerge()
        XCTAssertEqual(viewModel.phase, .running)
        XCTAssertTrue(viewModel.showsProgress)
        XCTAssertEqual(runner.requests.count, 1)
        XCTAssertEqual(runner.requests[0].outputPath, "/tmp/docs/main.docx")
        XCTAssertFalse(runner.requests[0].insertSourceFileTitles)
        XCTAssertEqual(
            activitySnapshot(viewModel),
            [
                ActivitySnapshot(
                    title: "Setup",
                    detail: "Preparing merge...",
                    state: .active
                )
            ]
        )

        runner.emit(.started(message: "Started"))
        await settle()
        XCTAssertEqual(viewModel.statusText, "Started")
        XCTAssertEqual(
            activitySnapshot(viewModel),
            [
                ActivitySnapshot(
                    title: "Setup",
                    detail: "Started",
                    state: .active
                )
            ]
        )

        runner.emit(
            .stage(
                MergeStageEvent(
                    stage: .mergingInput,
                    message: nil,
                    currentInputIndex: 2,
                    totalInputs: 2,
                    inputDisplayName: "two.docx"
                )
            )
        )
        await settle()
        XCTAssertTrue(viewModel.progressValue > 0.7)
        XCTAssertEqual(
            activitySnapshot(viewModel),
            [
                ActivitySnapshot(
                    title: "Setup",
                    detail: "Started",
                    state: .complete
                ),
                ActivitySnapshot(
                    title: "Merge",
                    detail: "Merging 2 of 2 two.docx",
                    state: .active
                )
            ]
        )

        runner.emit(.completed(outputPath: "/tmp/docs/main.docx", reportPath: reportURL.path))
        await settle()

        XCTAssertEqual(
            viewModel.phase,
            .succeeded(
                outputURL: URL(fileURLWithPath: "/tmp/docs/main.docx"),
                reportURL: reportURL
            )
        )
        XCTAssertFalse(viewModel.showsProgress)
        XCTAssertEqual(viewModel.reportPreview, #"{"status":"ok"}"#)
        XCTAssertEqual(
            activitySnapshot(viewModel),
            [
                ActivitySnapshot(
                    title: "Setup",
                    detail: "Started",
                    state: .complete
                ),
                ActivitySnapshot(
                    title: "Merge",
                    detail: "Merging 2 of 2 two.docx",
                    state: .complete
                ),
                ActivitySnapshot(
                    title: "Done",
                    detail: "Merge successful.",
                    state: .success
                )
            ]
        )
        XCTAssertEqual(viewModel.statusText, "Merge successful.")
    }

    func testFailureLoadsReportPreview() async throws {
        let runner = TestMergeRunner()
        let preferences = TestPreferences()
        let viewModel = MergeViewModel(runner: runner, preferences: preferences)
        let reportURL = try makeTempFile(named: "failure.json", contents: #"{"error":"tracked changes"}"#)

        viewModel.addInputURLs([URL(fileURLWithPath: "/tmp/docs/one.docx")])
        viewModel.startMerge()
        runner.emit(
            .stage(
                MergeStageEvent(
                    stage: .preflight,
                    message: nil,
                    currentInputIndex: nil,
                    totalInputs: nil,
                    inputDisplayName: nil
                )
            )
        )
        await settle()

        runner.emit(.failed(message: "Tracked changes are not supported.", reportPath: reportURL.path))
        await settle()

        XCTAssertEqual(viewModel.phase, .failed(message: "Tracked changes are not supported."))
        XCTAssertEqual(viewModel.reportPreview, #"{"error":"tracked changes"}"#)
        XCTAssertFalse(viewModel.showsProgress)
        XCTAssertEqual(
            activitySnapshot(viewModel),
            [
                ActivitySnapshot(
                    title: "Setup",
                    detail: "Preparing merge...",
                    state: .complete
                ),
                ActivitySnapshot(
                    title: "Preflight",
                    detail: "Tracked changes are not supported.",
                    state: .failed
                )
            ]
        )
    }

    func testCancelTransitionsToCancelled() async {
        let runner = TestMergeRunner()
        let preferences = TestPreferences()
        let viewModel = MergeViewModel(runner: runner, preferences: preferences)

        viewModel.addInputURLs([URL(fileURLWithPath: "/tmp/docs/one.docx")])
        viewModel.startMerge()
        viewModel.cancelMerge()
        await settle()

        XCTAssertEqual(viewModel.phase, .cancelled)
        XCTAssertEqual(
            activitySnapshot(viewModel),
            [
                ActivitySnapshot(
                    title: "Setup",
                    detail: "Merge cancelled.",
                    state: .cancelled
                )
            ]
        )
        XCTAssertTrue(runner.control.cancelCalled)
        XCTAssertFalse(viewModel.showsProgress)
    }

    func testSourceTitlesPreferenceIsLoadedPersistedAndSentToHelper() {
        let runner = TestMergeRunner()
        let preferences = TestPreferences()
        preferences.insertSourceFileTitles = true
        let viewModel = MergeViewModel(runner: runner, preferences: preferences)

        XCTAssertTrue(viewModel.insertSourceFileTitles)

        viewModel.addInputURLs([URL(fileURLWithPath: "/tmp/docs/one.docx")])
        viewModel.startMerge()

        XCTAssertEqual(runner.requests.count, 1)
        XCTAssertTrue(runner.requests[0].insertSourceFileTitles)

        viewModel.insertSourceFileTitles = false
        XCTAssertFalse(preferences.insertSourceFileTitles)
    }

    func testClearInputsResetsToIdleWithoutPromptCopy() {
        let runner = TestMergeRunner()
        let preferences = TestPreferences()
        let viewModel = MergeViewModel(runner: runner, preferences: preferences)

        viewModel.addInputURLs([URL(fileURLWithPath: "/tmp/docs/one.docx")])
        XCTAssertEqual(viewModel.statusText, "1 document ready.")

        viewModel.clearInputs()

        XCTAssertEqual(viewModel.phase, .idle)
        XCTAssertEqual(viewModel.statusText, "")
        XCTAssertFalse(viewModel.showsProgress)
    }

    private func settle() async {
        await Task.yield()
        await Task.yield()
    }

    private func activitySnapshot(
        _ viewModel: MergeViewModel
    ) -> [ActivitySnapshot] {
        viewModel.reportActivities.map { activity in
            ActivitySnapshot(
                title: activity.title,
                detail: activity.detail,
                state: activity.state
            )
        }
    }

    private func makeTempFile(named name: String, contents: String) throws -> URL {
        let directory = FileManager.default.temporaryDirectory.appending(path: UUID().uuidString, directoryHint: .isDirectory)
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        let fileURL = directory.appending(path: name, directoryHint: .notDirectory)
        try contents.data(using: .utf8)?.write(to: fileURL)
        return fileURL
    }
}

private final class TestMergeRunner: MergeRunner {
    var requests: [AppRunRequest] = []
    let control = TestMergeControl()
    private var handler: ((Result<HelperEvent, Error>) -> Void)?

    func start(
        request: AppRunRequest,
        onEvent: @escaping @Sendable (Result<HelperEvent, Error>) -> Void
    ) throws -> MergeRunControlling {
        requests.append(request)
        handler = onEvent
        return control
    }

    func emit(_ event: HelperEvent) {
        handler?(.success(event))
    }
}

private final class TestMergeControl: MergeRunControlling {
    private(set) var cancelCalled = false

    func cancel() {
        cancelCalled = true
    }
}

private final class TestPreferences: AppPreferencesStore {
    var lastInputDirectory: URL?
    var lastOutputDirectory: URL?
    var insertSourceFileTitles = false

    func setLastInputDirectory(_ url: URL?) {
        lastInputDirectory = url
    }

    func setLastOutputDirectory(_ url: URL?) {
        lastOutputDirectory = url
    }

    func setInsertSourceFileTitles(_ enabled: Bool) {
        insertSourceFileTitles = enabled
    }
}
