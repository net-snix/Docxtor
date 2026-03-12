import SwiftUI

enum StudioPalette {
    static let ink = Color(red: 0.13, green: 0.11, blue: 0.09)
    static let paper = Color(red: 0.95, green: 0.91, blue: 0.84)
    static let cream = Color(red: 0.98, green: 0.95, blue: 0.91)
    static let brass = Color(red: 0.77, green: 0.55, blue: 0.24)
    static let ember = Color(red: 0.78, green: 0.31, blue: 0.20)
    static let moss = Color(red: 0.21, green: 0.49, blue: 0.42)
    static let slate = Color(red: 0.34, green: 0.39, blue: 0.42)
    static let cloud = Color(red: 0.82, green: 0.78, blue: 0.71)
    static let rule = Color(red: 0.33, green: 0.30, blue: 0.25)
}

enum StudioType {
    static func hero(_ size: CGFloat) -> Font {
        .custom("AvenirNextCondensed-Heavy", size: size, relativeTo: .largeTitle)
    }

    static func section(_ size: CGFloat) -> Font {
        .custom("AvenirNextCondensed-DemiBold", size: size, relativeTo: .title2)
    }

    static func copy(_ size: CGFloat) -> Font {
        .custom("AvenirNext-Medium", size: size, relativeTo: .body)
    }

    static func strong(_ size: CGFloat) -> Font {
        .custom("AvenirNext-DemiBold", size: size, relativeTo: .headline)
    }

    static func mono(_ size: CGFloat) -> Font {
        .custom("Menlo", size: size, relativeTo: .caption)
    }
}

struct StudioBackdrop: View {
    var body: some View {
        ZStack {
            LinearGradient(
                colors: [
                    StudioPalette.paper,
                    Color(red: 0.92, green: 0.88, blue: 0.81),
                    Color(red: 0.86, green: 0.84, blue: 0.80)
                ],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )

            RadialGradient(
                colors: [
                    StudioPalette.brass.opacity(0.34),
                    .clear
                ],
                center: .topTrailing,
                startRadius: 40,
                endRadius: 520
            )

            RadialGradient(
                colors: [
                    StudioPalette.moss.opacity(0.18),
                    .clear
                ],
                center: .bottomLeading,
                startRadius: 20,
                endRadius: 460
            )

            DraftingGrid()
                .opacity(0.18)
        }
        .ignoresSafeArea()
    }
}

struct PathPlate: View {
    let title: String
    let value: String

    var body: some View {
        ViewThatFits(in: .horizontal) {
            HStack(alignment: .firstTextBaseline, spacing: 10) {
                titleText

                Text(value)
                    .font(StudioType.mono(12))
                    .foregroundStyle(StudioPalette.ink.opacity(0.88))
                    .textSelection(.enabled)
                    .lineLimit(3)
                    .frame(maxWidth: .infinity, alignment: .leading)
            }

            VStack(alignment: .leading, spacing: 4) {
                titleText
                Text(value)
                    .font(StudioType.mono(12))
                    .foregroundStyle(StudioPalette.ink.opacity(0.88))
                    .textSelection(.enabled)
                    .lineLimit(4)
                    .frame(maxWidth: .infinity, alignment: .leading)
            }
        }
        .padding(.vertical, 4)
    }

    private var titleText: some View {
        Text(title.uppercased())
            .font(StudioType.strong(10))
            .tracking(1.4)
            .foregroundStyle(StudioPalette.rule.opacity(0.60))
            .frame(width: 88, alignment: .leading)
    }
}

struct ProgressRail: View {
    let progress: Double
    let tint: Color

    var body: some View {
        GeometryReader { proxy in
            let width = max(18, proxy.size.width * max(0.02, min(progress, 1)))

            ZStack(alignment: .leading) {
                Capsule()
                    .fill(StudioPalette.cloud.opacity(0.35))

                Capsule()
                    .fill(
                        LinearGradient(
                            colors: [
                                tint.opacity(0.70),
                                tint
                            ],
                            startPoint: .leading,
                            endPoint: .trailing
                        )
                    )
                    .frame(width: width)
                    .overlay(alignment: .trailing) {
                        Circle()
                            .fill(Color.white.opacity(0.92))
                            .frame(width: 10, height: 10)
                            .shadow(color: tint.opacity(0.35), radius: 6, x: 0, y: 0)
                    }
            }
        }
        .frame(height: 16)
    }
}

struct StudioActionButton: View {
    enum Tone {
        case accent
        case neutral
        case warning
        case quiet
    }

    let title: String
    let systemImage: String?
    let tone: Tone
    let isDisabled: Bool
    let action: () -> Void

    init(
        _ title: String,
        systemImage: String? = nil,
        tone: Tone = .neutral,
        isDisabled: Bool = false,
        action: @escaping () -> Void
    ) {
        self.title = title
        self.systemImage = systemImage
        self.tone = tone
        self.isDisabled = isDisabled
        self.action = action
    }

    var body: some View {
        Button(action: action) {
            HStack(spacing: 8) {
                if let systemImage {
                    Image(systemName: systemImage)
                        .font(.system(size: 13, weight: .semibold))
                }

                Text(title)
                    .font(StudioType.strong(13))
                    .lineLimit(1)
            }
            .foregroundStyle(foregroundColor)
            .padding(.horizontal, 13)
            .padding(.vertical, 9)
            .background(backgroundStyle, in: Capsule())
            .overlay(
                Capsule()
                    .strokeBorder(borderColor, lineWidth: 1)
            )
        }
        .buttonStyle(.plain)
        .opacity(isDisabled ? 0.45 : 1)
        .disabled(isDisabled)
    }

    private var foregroundColor: Color {
        switch tone {
        case .accent, .warning:
            return .white
        case .neutral, .quiet:
            return StudioPalette.ink
        }
    }

    private var backgroundStyle: AnyShapeStyle {
        switch tone {
        case .accent:
            return AnyShapeStyle(LinearGradient(colors: [StudioPalette.moss, StudioPalette.moss.opacity(0.82)], startPoint: .topLeading, endPoint: .bottomTrailing))
        case .warning:
            return AnyShapeStyle(LinearGradient(colors: [StudioPalette.ember, StudioPalette.ember.opacity(0.80)], startPoint: .topLeading, endPoint: .bottomTrailing))
        case .neutral:
            return AnyShapeStyle(Color.white.opacity(0.42))
        case .quiet:
            return AnyShapeStyle(StudioPalette.paper.opacity(0.66))
        }
    }

    private var borderColor: Color {
        switch tone {
        case .accent:
            return StudioPalette.moss.opacity(0.40)
        case .warning:
            return StudioPalette.ember.opacity(0.34)
        case .neutral:
            return StudioPalette.cloud.opacity(0.52)
        case .quiet:
            return StudioPalette.cloud.opacity(0.32)
        }
    }
}

struct PaperStackIllustration: View {
    let tint: Color

    var body: some View {
        ZStack {
            RoundedRectangle(cornerRadius: 20, style: .continuous)
                .fill(Color.white.opacity(0.55))
                .frame(width: 120, height: 148)
                .rotationEffect(.degrees(-8))
                .offset(x: -18, y: -6)

            RoundedRectangle(cornerRadius: 20, style: .continuous)
                .fill(Color.white.opacity(0.72))
                .frame(width: 128, height: 156)
                .rotationEffect(.degrees(4))
                .offset(x: 14, y: 4)

            RoundedRectangle(cornerRadius: 22, style: .continuous)
                .fill(Color.white.opacity(0.92))
                .frame(width: 132, height: 164)
                .overlay(alignment: .bottomLeading) {
                    Circle()
                        .fill(tint)
                        .frame(width: 34, height: 34)
                        .overlay(
                            Image(systemName: "plus")
                                .font(.system(size: 15, weight: .bold))
                                .foregroundStyle(.white)
                        )
                        .offset(x: -12, y: 12)
                }

            Image(systemName: "doc.on.doc")
                .font(.system(size: 34, weight: .semibold))
                .foregroundStyle(tint)
        }
        .frame(width: 180, height: 190)
    }
}

private struct DraftingGrid: View {
    var body: some View {
        GeometryReader { geometry in
            Canvas { context, size in
                var path = Path()
                let step: CGFloat = 34

                stride(from: 0 as CGFloat, through: size.width, by: step).forEach { x in
                    path.move(to: CGPoint(x: x, y: 0))
                    path.addLine(to: CGPoint(x: x, y: size.height))
                }

                stride(from: 0 as CGFloat, through: size.height, by: step).forEach { y in
                    path.move(to: CGPoint(x: 0, y: y))
                    path.addLine(to: CGPoint(x: size.width, y: y))
                }

                context.stroke(path, with: .color(StudioPalette.rule.opacity(0.08)), lineWidth: 1)
            }
            .frame(width: geometry.size.width, height: geometry.size.height)
        }
    }
}
