import SwiftUI

struct HeaderBarView: View {
    let phase: MergeViewModel.Phase
    let phaseTitle: String
    let statusText: String
    let isCompact: Bool
    let canMerge: Bool
    let canCancel: Bool
    let onMerge: () -> Void

    var body: some View {
        Group {
            if isCompact {
                VStack(alignment: .leading, spacing: 14) {
                    titleBlock

                    HStack(alignment: .center, spacing: 14) {
                        if showsStatusText {
                            HStack(spacing: 12) {
                                PhaseTag(title: phaseTitle, tint: phaseAccent)

                                Text(statusText)
                                    .font(StudioType.copy(12))
                                    .foregroundStyle(StudioPalette.slate)
                                    .lineLimit(2)
                            }
                        }

                        Spacer(minLength: 12)

                        actionButtons
                    }
                }
            } else {
                HStack(alignment: .center, spacing: 20) {
                    titleBlock

                    Spacer(minLength: 18)

                    if showsStatusText {
                        HStack(spacing: 16) {
                            PhaseTag(title: phaseTitle, tint: phaseAccent)

                            Text(statusText)
                                .font(StudioType.copy(12))
                                .foregroundStyle(StudioPalette.slate)
                                .multilineTextAlignment(.trailing)
                                .lineLimit(2)
                                .frame(maxWidth: 210, alignment: .trailing)
                        }
                    }

                    actionButtons
                }
            }
        }
    }

    private var titleBlock: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text("Docxtor")
                .font(StudioType.hero(34))
                .foregroundStyle(StudioPalette.ink)

            Text("Order in. One file out.")
                .font(StudioType.copy(13))
                .foregroundStyle(StudioPalette.slate)
        }
    }

    private var actionButtons: some View {
        HStack(spacing: 10) {
            StudioActionButton(
                canCancel ? "Cancel" : "Merge",
                systemImage: canCancel ? "stop.fill" : "play.fill",
                tone: canCancel ? .warning : .accent,
                isDisabled: !canCancel && !canMerge,
                action: onMerge
            )
        }
    }

    private var phaseAccent: Color {
        switch phase {
        case .idle:
            return StudioPalette.brass
        case .running, .succeeded:
            return StudioPalette.moss
        case .failed:
            return StudioPalette.ember
        case .cancelled:
            return StudioPalette.brass
        }
    }

    private var showsStatusText: Bool {
        !statusText.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty
    }
}

struct WorkspaceSurfaceView: View {
    let availableSize: CGSize
    let inputCount: Int
    let deckRows: [DeckRowEntry]
    let selectedIDs: Set<InputDocument.ID>
    let isDropTargeted: Bool
    let insertSourceFileTitles: Bool
    let mergedPath: String
    let reportPath: String
    let hasOutputOverride: Bool
    let phase: MergeViewModel.Phase
    let phaseTitle: String
    let progressValue: Double
    let showsProgress: Bool
    let statusText: String
    let reportActivities: [MergeActivityItem]
    let canChooseOutput: Bool
    let canRemoveSelected: Bool
    let canMoveSelectionUp: Bool
    let canMoveSelectionDown: Bool
    let onToggleSelection: (InputDocument.ID) -> Void
    let onAddDocuments: () -> Void
    let onRemove: (InputDocument.ID) -> Void
    let onRemoveSelected: () -> Void
    let onClear: () -> Void
    let onMoveUp: () -> Void
    let onMoveDown: () -> Void
    let onChooseOutput: () -> Void
    let onUseSuggested: () -> Void
    let onSetInsertSourceFileTitles: (Bool) -> Void
    let onOpen: () -> Void
    let onReveal: () -> Void
    let onShowReport: () -> Void
    let onDrop: ([URL]) -> Bool
    let onDropTargetChange: (Bool) -> Void

    var body: some View {
        Group {
            if isCompactWidth {
                VStack(alignment: .leading, spacing: 22) {
                    deckArea
                    HorizontalDividerLine()
                    sidebar
                }
            } else {
                HStack(alignment: .top, spacing: 30) {
                    deckArea
                    VerticalDividerLine()
                        .padding(.vertical, 6)
                    sidebar
                        .frame(width: sidebarWidth)
                }
            }
        }
    }

    private var deckArea: some View {
        DeckAreaView(
            inputCount: inputCount,
            rows: deckRows,
            selectedIDs: selectedIDs,
            isDropTargeted: isDropTargeted,
            canRemoveSelected: canRemoveSelected,
            canMoveSelectionUp: canMoveSelectionUp,
            canMoveSelectionDown: canMoveSelectionDown,
            contentMinHeight: deckMinHeight,
            onToggleSelection: onToggleSelection,
            onAddDocuments: onAddDocuments,
            onRemove: onRemove,
            onRemoveSelected: onRemoveSelected,
            onClear: onClear,
            onMoveUp: onMoveUp,
            onMoveDown: onMoveDown,
            onDrop: onDrop,
            onDropTargetChange: onDropTargetChange
        )
        .equatable()
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .top)
    }

    private var sidebar: some View {
        SidebarView(
            insertSourceFileTitles: insertSourceFileTitles,
            mergedPath: mergedPath,
            reportPath: reportPath,
            hasOutputOverride: hasOutputOverride,
            canChooseOutput: canChooseOutput,
            phase: phase,
            phaseTitle: phaseTitle,
            progressValue: progressValue,
            showsProgress: showsProgress,
            statusText: statusText,
            reportActivities: reportActivities,
            isCompactWidth: isCompactWidth,
            availableHeight: availableSize.height,
            onChooseOutput: onChooseOutput,
            onUseSuggested: onUseSuggested,
            onSetInsertSourceFileTitles: onSetInsertSourceFileTitles,
            onOpen: onOpen,
            onReveal: onReveal,
            onShowReport: onShowReport
        )
        .equatable()
    }

    private var isCompactWidth: Bool {
        availableSize.width < 980
    }

    private var sidebarWidth: CGFloat {
        min(310, max(270, availableSize.width * 0.25))
    }

    private var deckMinHeight: CGFloat {
        let reservedHeight: CGFloat = isCompactWidth ? 520 : 360
        let maxHeight: CGFloat = isCompactWidth ? 340 : 420
        return min(maxHeight, max(280, availableSize.height - reservedHeight))
    }
}

struct DeckRowEntry: Identifiable, Equatable {
    let id: InputDocument.ID
    let position: Int
    let positionLabel: String
    let item: InputDocument
    let displayName: String
    let directoryPath: String
    let showsDivider: Bool

    static func makeEntries(for inputItems: [InputDocument]) -> [DeckRowEntry] {
        let lastIndex = inputItems.count - 1

        return inputItems.enumerated().map { index, item in
            DeckRowEntry(
                id: item.id,
                position: index + 1,
                positionLabel: String(format: "%02d", index + 1),
                item: item,
                displayName: item.url.lastPathComponent,
                directoryPath: item.url.deletingLastPathComponent().path,
                showsDivider: index < lastIndex
            )
        }
    }
}

private struct DeckAreaView: View, @MainActor Equatable {
    let inputCount: Int
    let rows: [DeckRowEntry]
    let selectedIDs: Set<InputDocument.ID>
    let isDropTargeted: Bool
    let canRemoveSelected: Bool
    let canMoveSelectionUp: Bool
    let canMoveSelectionDown: Bool
    let contentMinHeight: CGFloat
    let onToggleSelection: (InputDocument.ID) -> Void
    let onAddDocuments: () -> Void
    let onRemove: (InputDocument.ID) -> Void
    let onRemoveSelected: () -> Void
    let onClear: () -> Void
    let onMoveUp: () -> Void
    let onMoveDown: () -> Void
    let onDrop: ([URL]) -> Bool
    let onDropTargetChange: (Bool) -> Void

    static func == (lhs: DeckAreaView, rhs: DeckAreaView) -> Bool {
        lhs.inputCount == rhs.inputCount &&
        lhs.rows == rhs.rows &&
        lhs.selectedIDs == rhs.selectedIDs &&
        lhs.isDropTargeted == rhs.isDropTargeted &&
        lhs.canRemoveSelected == rhs.canRemoveSelected &&
        lhs.canMoveSelectionUp == rhs.canMoveSelectionUp &&
        lhs.canMoveSelectionDown == rhs.canMoveSelectionDown &&
        lhs.contentMinHeight == rhs.contentMinHeight
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            HStack(alignment: .firstTextBaseline) {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Files")
                        .font(StudioType.section(30))
                        .foregroundStyle(StudioPalette.ink)

                    Text(rows.isEmpty ? "Drop files or click anywhere." : "Select rows. Move them to set merge order.")
                        .font(StudioType.copy(13))
                        .foregroundStyle(StudioPalette.slate)
                }

                Spacer()

                Text("\(inputCount)")
                    .font(StudioType.strong(15))
                    .foregroundStyle(StudioPalette.ink)
                    .padding(.horizontal, 12)
                    .padding(.vertical, 6)
                    .background(StudioPalette.brass.opacity(0.10), in: Capsule())
            }

            ZStack {
                RoundedRectangle(cornerRadius: 26, style: .continuous)
                    .fill(
                        LinearGradient(
                            colors: [
                                Color.white.opacity(rows.isEmpty ? 0.28 : 0.18),
                                Color.white.opacity(0.08)
                            ],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        )
                    )

                RoundedRectangle(cornerRadius: 26, style: .continuous)
                    .strokeBorder(
                        isDropTargeted ? StudioPalette.moss.opacity(0.40) : Color.clear,
                        lineWidth: 1.6
                    )

                Rectangle()
                    .fill(isDropTargeted ? StudioPalette.moss : StudioPalette.cloud.opacity(0.30))
                    .frame(width: isDropTargeted ? 4 : 1)
                    .frame(maxHeight: .infinity, alignment: .leading)
                    .padding(.vertical, 18)
                    .padding(.leading, 18)
                    .frame(maxWidth: .infinity, alignment: .leading)

                Group {
                    if rows.isEmpty {
                        EmptyDeckView(isDropTargeted: isDropTargeted)
                            .frame(maxWidth: .infinity, maxHeight: .infinity)
                    } else {
                        ScrollView {
                            LazyVStack(spacing: 0) {
                                ForEach(rows) { row in
                                    DeckRow(
                                        row: row,
                                        isSelected: selectedIDs.contains(row.id),
                                        onToggleSelection: { onToggleSelection(row.id) },
                                        onRemove: { onRemove(row.id) }
                                    )
                                }
                            }
                            .padding(.horizontal, 18)
                            .padding(.vertical, 14)
                        }
                    }
                }
            }
            .frame(maxWidth: .infinity, minHeight: contentMinHeight, maxHeight: .infinity)
            .clipShape(RoundedRectangle(cornerRadius: 26, style: .continuous))
            .contentShape(RoundedRectangle(cornerRadius: 26, style: .continuous))
            .onTapGesture {
                guard rows.isEmpty else {
                    return
                }

                onAddDocuments()
            }
            .dropDestination(for: URL.self) { items, _ in
                onDrop(items)
            } isTargeted: { targeted in
                onDropTargetChange(targeted)
            }

            HStack(spacing: 10) {
                StudioActionButton(
                    "Remove",
                    systemImage: "minus",
                    tone: .neutral,
                    isDisabled: !canRemoveSelected,
                    action: onRemoveSelected
                )

                StudioActionButton(
                    "Clear",
                    systemImage: "xmark",
                    tone: .quiet,
                    isDisabled: rows.isEmpty,
                    action: onClear
                )

                Spacer()

                StudioActionButton(
                    "Up",
                    systemImage: "arrow.up",
                    tone: .neutral,
                    isDisabled: !canMoveSelectionUp,
                    action: onMoveUp
                )

                StudioActionButton(
                    "Down",
                    systemImage: "arrow.down",
                    tone: .neutral,
                    isDisabled: !canMoveSelectionDown,
                    action: onMoveDown
                )
            }
        }
    }
}

private struct SidebarView: View, @MainActor Equatable {
    let insertSourceFileTitles: Bool
    let mergedPath: String
    let reportPath: String
    let hasOutputOverride: Bool
    let canChooseOutput: Bool
    let phase: MergeViewModel.Phase
    let phaseTitle: String
    let progressValue: Double
    let showsProgress: Bool
    let statusText: String
    let reportActivities: [MergeActivityItem]
    let isCompactWidth: Bool
    let availableHeight: CGFloat
    let onChooseOutput: () -> Void
    let onUseSuggested: () -> Void
    let onSetInsertSourceFileTitles: (Bool) -> Void
    let onOpen: () -> Void
    let onReveal: () -> Void
    let onShowReport: () -> Void

    static func == (lhs: SidebarView, rhs: SidebarView) -> Bool {
        lhs.insertSourceFileTitles == rhs.insertSourceFileTitles &&
        lhs.mergedPath == rhs.mergedPath &&
        lhs.reportPath == rhs.reportPath &&
        lhs.hasOutputOverride == rhs.hasOutputOverride &&
        lhs.canChooseOutput == rhs.canChooseOutput &&
        lhs.phase == rhs.phase &&
        lhs.phaseTitle == rhs.phaseTitle &&
        lhs.progressValue == rhs.progressValue &&
        lhs.showsProgress == rhs.showsProgress &&
        lhs.statusText == rhs.statusText &&
        lhs.reportActivities == rhs.reportActivities &&
        lhs.isCompactWidth == rhs.isCompactWidth &&
        lhs.availableHeight == rhs.availableHeight
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 22) {
            outputSection

            if showsStatusText {
                HorizontalDividerLine()
                statusSection
            }

            if case .succeeded = phase {
                successSection
            }

            HorizontalDividerLine()
            reportSection
        }
    }

    private var outputSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            SidebarHeader(title: "Output")

            if phase.showsCompletedArtifacts {
                PathPlate(title: "Merged file", value: mergedPath)
                PathPlate(title: "Report", value: reportPath)
            }

            HStack(spacing: 10) {
                StudioActionButton(
                    "Choose",
                    systemImage: "folder.badge.plus",
                    tone: .accent,
                    isDisabled: !canChooseOutput,
                    action: onChooseOutput
                )

                if hasOutputOverride {
                    StudioActionButton(
                        "Reset",
                        systemImage: "arrow.uturn.backward",
                        tone: .neutral,
                        action: onUseSuggested
                    )
                }
            }

            SourceTitleToggle(
                isOn: Binding(
                    get: { insertSourceFileTitles },
                    set: { value in
                        onSetInsertSourceFileTitles(value)
                    }
                )
            )
        }
    }

    private var statusSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack(alignment: .firstTextBaseline) {
                SidebarHeader(title: phaseTitle)
                Spacer()
                CompactStatusDot(tint: phaseAccent)
            }

            ProgressRail(
                progress: showsProgress ? progressValue : 0.03,
                tint: phaseAccent
            )

            if showsStatusText {
                Text(statusText)
                    .font(StudioType.copy(13))
                    .foregroundStyle(StudioPalette.slate)
                    .fixedSize(horizontal: false, vertical: true)
            }
        }
    }

    private var successSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Done")
                .font(StudioType.strong(11))
                .tracking(1.2)
                .foregroundStyle(StudioPalette.rule.opacity(0.60))

            ViewThatFits(in: .horizontal) {
                HStack(spacing: 10) {
                    StudioActionButton("Open", systemImage: "arrow.up.right.square", tone: .accent, action: onOpen)
                    StudioActionButton("Reveal", systemImage: "folder", tone: .neutral, action: onReveal)
                    StudioActionButton("Report", systemImage: "doc.text", tone: .neutral, action: onShowReport)
                }

                VStack(alignment: .leading, spacing: 10) {
                    StudioActionButton("Open", systemImage: "arrow.up.right.square", tone: .accent, action: onOpen)
                    StudioActionButton("Reveal", systemImage: "folder", tone: .neutral, action: onReveal)
                    StudioActionButton("Report", systemImage: "doc.text", tone: .neutral, action: onShowReport)
                }
            }
        }
    }

    private var reportSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            SidebarHeader(title: "Report")
            ReportActivityFeedView(
                activities: reportActivities,
                phase: phase,
                minHeight: reportHeight
            )
        }
    }

    private var phaseAccent: Color {
        switch phase {
        case .idle:
            return StudioPalette.brass
        case .running, .succeeded:
            return StudioPalette.moss
        case .failed:
            return StudioPalette.ember
        case .cancelled:
            return StudioPalette.brass
        }
    }

    private var reportHeight: CGFloat {
        let targetHeight: CGFloat = switch phase {
        case .idle:
            96
        case .running:
            120
        case .succeeded, .failed:
            220
        case .cancelled:
            110
        }

        let cap = max(76, availableHeight * (isCompactWidth ? 0.14 : 0.20))
        return min(targetHeight, cap)
    }

    private var showsStatusText: Bool {
        !statusText.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty
    }
}
