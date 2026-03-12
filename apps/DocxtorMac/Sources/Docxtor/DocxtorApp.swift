import SwiftUI

@main
struct DocxtorApp: App {
    var body: some Scene {
        WindowGroup("Docxtor") {
            AppView(viewModel: MergeViewModel())
        }
        .defaultSize(width: 1240, height: 900)
        .windowResizability(.automatic)
        .windowStyle(.hiddenTitleBar)
    }
}
