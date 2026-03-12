import XCTest
@testable import Docxtor

final class AppRunRequestTests: XCTestCase {
    func testBuildUsesOutputAndDerivedReportPaths() {
        let first = URL(fileURLWithPath: "/tmp/a/alpha.docx")
        let second = URL(fileURLWithPath: "/tmp/a/beta.docx")
        let output = URL(fileURLWithPath: "/tmp/out/final.docx")

        let request = AppRunRequestBuilder.build(
            inputs: [first, second, first],
            outputURL: output
        )

        XCTAssertEqual(
            request,
            AppRunRequest(
                inputs: [first.path, second.path, first.path],
                outputPath: output.path,
                reportPath: "/tmp/out/final.merge-report.json",
                templatePath: nil,
                insertSourceFileTitles: false
            )
        )
    }

    func testBuildCanEnableSourceTitles() {
        let input = URL(fileURLWithPath: "/tmp/a/alpha.docx")
        let output = URL(fileURLWithPath: "/tmp/out/final.docx")

        let request = AppRunRequestBuilder.build(
            inputs: [input],
            outputURL: output,
            insertSourceFileTitles: true
        )

        XCTAssertTrue(request.insertSourceFileTitles)
    }

    func testDefaultOutputFollowsFirstInputDirectory() {
        let inputs = [
            URL(fileURLWithPath: "/tmp/docs/one.docx"),
            URL(fileURLWithPath: "/tmp/elsewhere/two.docx")
        ]

        XCTAssertEqual(
            AppRunRequestBuilder.defaultOutputURL(for: inputs),
            URL(fileURLWithPath: "/tmp/docs/main.docx")
        )
    }
}
