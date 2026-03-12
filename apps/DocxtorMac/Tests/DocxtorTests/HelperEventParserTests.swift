import XCTest
@testable import Docxtor

final class HelperEventParserTests: XCTestCase {
    func testParseStageEventWithInputProgress() throws {
        let line = #"{"type":"stage","stage":"merging-input","currentInputIndex":2,"totalInputs":5,"inputDisplayName":"chapter-2.docx","message":"Merging second input"}"#

        let event = try HelperEventParser.parse(line: line)

        XCTAssertEqual(
            event,
            .stage(
                MergeStageEvent(
                    stage: .mergingInput,
                    message: "Merging second input",
                    currentInputIndex: 2,
                    totalInputs: 5,
                    inputDisplayName: "chapter-2.docx"
                )
            )
        )
    }

    func testParseFailureEvent() throws {
        let line = #"{"type":"failed","message":"Tracked changes are not supported.","reportPath":"/tmp/report.json"}"#

        let event = try HelperEventParser.parse(line: line)

        XCTAssertEqual(
            event,
            .failed(
                message: "Tracked changes are not supported.",
                reportPath: "/tmp/report.json"
            )
        )
    }
}
