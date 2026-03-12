import AppKit
import SwiftUI
import UniformTypeIdentifiers

struct AppView: View {
    @StateObject private var viewModel: MergeViewModel
    @State private var isDropTargeted = false

    init(viewModel: MergeViewModel) {
        _viewModel = StateObject(wrappedValue: viewModel)
    }

    var body: some View {
        GeometryReader { proxy in
            let size = proxy.size
            let contentWidth = size.width - 56
            let compactWidth = contentWidth < 980
            let topPadding: CGFloat = compactWidth ? 4 : 8

            ZStack {
                StudioBackdrop()
                WindowChromeView()
                    .allowsHitTesting(false)

                ScrollView(.vertical, showsIndicators: false) {
                    VStack(alignment: .leading, spacing: 20) {
                        HeaderBarView(
                            phase: viewModel.phase,
                            phaseTitle: phaseTitle,
                            statusText: viewModel.statusText,
                            isCompact: compactWidth,
                            canMerge: viewModel.canMerge,
                            canCancel: viewModel.canCancel,
                            onMerge: mergeButtonTapped
                        )

                        WorkspaceSurfaceView(
                            availableSize: size,
                            inputItems: viewModel.inputItems,
                            selectedIDs: viewModel.selectedInputIDs,
                            isDropTargeted: isDropTargeted,
                            insertSourceFileTitles: viewModel.insertSourceFileTitles,
                            mergedPath: viewModel.resolvedOutputURL?.path ?? "Select docs to get the default main.docx",
                            reportPath: viewModel.resolvedReportURL?.path ?? "Report path follows the merged output.",
                            hasOutputOverride: viewModel.outputOverrideURL != nil,
                            phase: viewModel.phase,
                            phaseTitle: phaseTitle,
                            progressValue: viewModel.progressValue,
                            showsProgress: viewModel.showsProgress,
                            statusText: viewModel.statusText,
                            reportActivities: viewModel.reportActivities,
                            canChooseOutput: !viewModel.inputItems.isEmpty,
                            canRemoveSelected: viewModel.canRemoveSelected,
                            canMoveSelectionUp: viewModel.canMoveSelectionUp,
                            canMoveSelectionDown: viewModel.canMoveSelectionDown,
                            onToggleSelection: toggleSelection,
                            onAddDocuments: openInputPanel,
                            onRemove: { viewModel.removeInput(id: $0) },
                            onRemoveSelected: viewModel.removeSelectedInputs,
                            onClear: viewModel.clearInputs,
                            onMoveUp: viewModel.moveSelectionUp,
                            onMoveDown: viewModel.moveSelectionDown,
                            onChooseOutput: openOutputPanel,
                            onUseSuggested: viewModel.resetToSuggestedOutput,
                            onSetInsertSourceFileTitles: { viewModel.insertSourceFileTitles = $0 },
                            onOpen: viewModel.openOutput,
                            onReveal: viewModel.revealOutput,
                            onShowReport: viewModel.showReport,
                            onDrop: handleDrop,
                            onDropTargetChange: { isDropTargeted = $0 }
                        )
                    }
                    .padding(.horizontal, compactWidth ? 20 : 28)
                    .padding(.bottom, 24)
                    .padding(.top, topPadding)
                    .frame(maxWidth: .infinity, alignment: .topLeading)
                }
            }
        }
        .frame(minWidth: 900, minHeight: 620)
        .animation(.spring(response: 0.42, dampingFraction: 0.84), value: viewModel.inputItems.count)
        .animation(.spring(response: 0.42, dampingFraction: 0.86), value: viewModel.phase)
        .animation(.easeInOut(duration: 0.22), value: viewModel.progressValue)
    }
}

private extension AppView {
    var phaseTitle: String {
        switch viewModel.phase {
        case .idle:
            return "Ready"
        case .running:
            return "Merging"
        case .succeeded:
            return "Done"
        case .failed:
            return "Failed"
        case .cancelled:
            return "Stopped"
        }
    }

    func mergeButtonTapped() {
        if viewModel.canCancel {
            viewModel.cancelMerge()
        } else {
            viewModel.startMerge()
        }
    }

    func toggleSelection(for id: InputDocument.ID) {
        viewModel.selectInput(
            id: id,
            additive: NSEvent.modifierFlags.contains(.command)
        )
    }

    func handleDrop(_ items: [URL]) -> Bool {
        viewModel.addInputURLs(items)
        return !items.isEmpty
    }

    func openInputPanel() {
        let panel = NSOpenPanel()
        panel.allowedContentTypes = [UTType.docx]
        panel.allowsMultipleSelection = true
        panel.canChooseDirectories = false
        panel.canChooseFiles = true
        panel.directoryURL = viewModel.preferredInputDirectory

        guard panel.runModal() == .OK else {
            return
        }

        viewModel.addInputURLs(panel.urls)
    }

    func openOutputPanel() {
        let panel = NSSavePanel()
        panel.allowedContentTypes = [UTType.docx]
        panel.directoryURL = viewModel.preferredOutputDirectory
        panel.canCreateDirectories = true
        panel.nameFieldStringValue = viewModel.resolvedOutputURL?.lastPathComponent ?? "main.docx"

        guard panel.runModal() == .OK, let url = panel.url else {
            return
        }

        viewModel.setOutputURL(url.normalizedDocxURL)
    }
}

private extension UTType {
    static let docx = UTType(filenameExtension: "docx") ?? .data
}

private extension URL {
    var normalizedDocxURL: URL {
        if pathExtension.lowercased() == "docx" {
            return self
        }

        return deletingPathExtension().appendingPathExtension("docx")
    }
}
