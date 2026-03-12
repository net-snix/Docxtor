import SwiftUI

struct ReportActivityFeedView: View {
    let activities: [MergeActivityItem]
    let phase: MergeViewModel.Phase
    let minHeight: CGFloat

    var body: some View {
        ScrollView {
            Group {
                if activities.isEmpty {
                    placeholder
                } else {
                    LazyVStack(alignment: .leading, spacing: 10) {
                        ForEach(activities) { activity in
                            ReportActivityRow(activity: activity)
                        }
                    }
                    .padding(.leading, 14)
                    .padding(.vertical, 2)
                }
            }
            .frame(maxWidth: .infinity, alignment: .topLeading)
        }
        .frame(maxWidth: .infinity, minHeight: minHeight, alignment: .topLeading)
        .overlay(alignment: .leading) {
            Rectangle()
                .fill(feedAccent.opacity(0.28))
                .frame(width: 2)
        }
    }

    private var placeholder: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Waiting")
                .font(StudioType.strong(11))
                .tracking(1.2)
                .foregroundStyle(StudioPalette.rule.opacity(0.60))

            Text("Live merge status shows here.")
                .font(StudioType.copy(13))
                .foregroundStyle(StudioPalette.slate)
        }
        .padding(.leading, 14)
        .padding(.vertical, 2)
    }

    private var feedAccent: Color {
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
}

private struct ReportActivityRow: View {
    let activity: MergeActivityItem

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            indicator

            VStack(alignment: .leading, spacing: 4) {
                Text(activity.title)
                    .font(StudioType.copy(13))
                    .foregroundStyle(StudioPalette.ink.opacity(0.92))
                    .fixedSize(horizontal: false, vertical: true)

                if let detail = activity.detail, !detail.isEmpty {
                    Text(detail)
                        .font(StudioType.copy(11))
                        .foregroundStyle(StudioPalette.slate)
                        .fixedSize(horizontal: false, vertical: true)
                }
            }

            Spacer(minLength: 12)

            Text(activity.state.label.uppercased())
                .font(StudioType.strong(10))
                .tracking(1.2)
                .foregroundStyle(activity.state.tint)
                .padding(.horizontal, 10)
                .padding(.vertical, 6)
                .background(activity.state.tint.opacity(0.12), in: Capsule())
        }
        .padding(12)
        .background(
            RoundedRectangle(cornerRadius: 18, style: .continuous)
                .fill(Color.white.opacity(0.12))
        )
    }

    @ViewBuilder
    private var indicator: some View {
        if activity.state == .active {
            ZStack {
                Circle()
                    .fill(activity.state.tint.opacity(0.12))
                    .frame(width: 28, height: 28)

                ProgressView()
                    .controlSize(.small)
                    .tint(activity.state.tint)
            }
        } else {
            ZStack {
                Circle()
                    .fill(activity.state.tint.opacity(0.12))
                    .frame(width: 28, height: 28)

                Image(systemName: activity.state.symbol)
                    .font(.system(size: 11, weight: .bold))
                    .foregroundStyle(activity.state.tint)
            }
        }
    }
}

private extension MergeActivityItem.State {
    var label: String {
        switch self {
        case .active:
            return "Doing"
        case .complete:
            return "Done"
        case .success:
            return "Success"
        case .failed:
            return "Failed"
        case .cancelled:
            return "Stopped"
        }
    }

    var symbol: String {
        switch self {
        case .active:
            return "hourglass"
        case .complete:
            return "checkmark"
        case .success:
            return "checkmark.circle.fill"
        case .failed:
            return "exclamationmark"
        case .cancelled:
            return "pause.fill"
        }
    }

    var tint: Color {
        switch self {
        case .active:
            return StudioPalette.brass
        case .complete, .success:
            return StudioPalette.moss
        case .failed:
            return StudioPalette.ember
        case .cancelled:
            return StudioPalette.slate
        }
    }
}
