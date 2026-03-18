import SwiftUI

struct EmptyDeckView: View {
    let isDropTargeted: Bool

    var body: some View {
        VStack(spacing: 14) {
            PaperStackIllustration(tint: isDropTargeted ? StudioPalette.moss : StudioPalette.brass)

            Text(isDropTargeted ? "Release to add" : "Drop .docx files")
                .font(StudioType.section(26))
                .foregroundStyle(StudioPalette.ink)

            Text(isDropTargeted ? "Drop to add them now" : "or click anywhere")
                .font(StudioType.copy(12))
                .foregroundStyle(StudioPalette.slate)
        }
        .padding(30)
    }
}

struct DeckRow: View {
    let row: DeckRowEntry
    let isSelected: Bool
    let onToggleSelection: () -> Void
    let onRemove: () -> Void

    var body: some View {
        VStack(spacing: 0) {
            HStack(spacing: 14) {
                Text(row.positionLabel)
                    .font(StudioType.strong(13))
                    .foregroundStyle(StudioPalette.ink)
                    .frame(width: 32)

                VStack(alignment: .leading, spacing: 3) {
                    Text(row.displayName)
                        .font(StudioType.strong(14))
                        .foregroundStyle(StudioPalette.ink)
                        .lineLimit(1)

                    Text(row.directoryPath)
                        .font(StudioType.mono(11))
                        .foregroundStyle(StudioPalette.slate)
                        .lineLimit(1)
                }

                Spacer(minLength: 12)

                Button(action: onRemove) {
                    Image(systemName: "xmark")
                        .font(.system(size: 12, weight: .bold))
                        .foregroundStyle(StudioPalette.ember)
                        .frame(width: 24, height: 24)
                }
                .buttonStyle(.plain)
            }
            .padding(.horizontal, 8)
            .padding(.vertical, 14)
            .background(isSelected ? StudioPalette.moss.opacity(0.08) : Color.clear)
            .overlay(alignment: .leading) {
                if isSelected {
                    Capsule()
                        .fill(StudioPalette.moss)
                        .frame(width: 3, height: 26)
                        .padding(.leading, 2)
                }
            }

            if row.showsDivider {
                Rectangle()
                    .fill(StudioPalette.cloud.opacity(0.32))
                    .frame(height: 1)
                    .padding(.leading, 54)
            }
        }
        .contentShape(Rectangle())
        .onTapGesture(perform: onToggleSelection)
    }
}

struct PhaseTag: View {
    let title: String
    let tint: Color

    var body: some View {
        HStack(spacing: 8) {
            Circle()
                .fill(tint)
                .frame(width: 8, height: 8)

            Text(title.uppercased())
                .font(StudioType.strong(10))
                .tracking(1.6)
                .foregroundStyle(StudioPalette.rule.opacity(0.68))
        }
    }
}

struct SidebarHeader: View {
    let title: String

    var body: some View {
        Text(title)
            .font(StudioType.section(24))
            .foregroundStyle(StudioPalette.ink)
    }
}

struct SourceTitleToggle: View {
    @Binding var isOn: Bool

    var body: some View {
        Toggle(isOn: $isOn) {
            VStack(alignment: .leading, spacing: 2) {
                Text("Source titles")
                    .font(StudioType.strong(13))
                    .foregroundStyle(StudioPalette.ink)

                Text("Add each file name before its content.")
                    .font(StudioType.copy(11))
                    .foregroundStyle(StudioPalette.slate)
            }
        }
        .toggleStyle(.switch)
    }
}

struct CompactStatusDot: View {
    let tint: Color

    var body: some View {
        Circle()
            .fill(tint)
            .frame(width: 10, height: 10)
            .shadow(color: tint.opacity(0.3), radius: 5, x: 0, y: 0)
    }
}

struct VerticalDividerLine: View {
    var body: some View {
        Rectangle()
            .fill(StudioPalette.cloud.opacity(0.28))
            .frame(width: 1)
            .frame(maxHeight: .infinity)
    }
}

struct HorizontalDividerLine: View {
    var body: some View {
        Rectangle()
            .fill(StudioPalette.cloud.opacity(0.28))
            .frame(height: 1)
            .frame(maxWidth: .infinity)
    }
}
