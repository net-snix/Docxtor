import XCTest
@testable import Docxtor

@MainActor
final class ReportActivityFeedViewTests: XCTestCase {
    func testEqualViewsMatchOnVisibleInputsOnly() {
        let activities = [
            MergeActivityItem(title: "Setup", detail: "Preparing merge...", state: .active),
            MergeActivityItem(title: "Merge", detail: nil, state: .complete)
        ]

        let lhs = ReportActivityFeedView(
            activities: activities,
            phase: .running,
            minHeight: 120
        )
        let rhs = ReportActivityFeedView(
            activities: activities,
            phase: .running,
            minHeight: 120
        )

        XCTAssertEqual(lhs, rhs)
    }

    func testDifferentFeedInputsDoNotCompareEqual() {
        let activities = [MergeActivityItem(title: "Setup", detail: nil, state: .active)]

        let running = ReportActivityFeedView(
            activities: activities,
            phase: .running,
            minHeight: 120
        )
        let failed = ReportActivityFeedView(
            activities: activities,
            phase: .failed(message: "Nope"),
            minHeight: 120
        )

        XCTAssertNotEqual(running, failed)
    }
}
