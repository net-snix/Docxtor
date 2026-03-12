import XCTest
@testable import Docxtor

final class ProcessMergeRunnerTests: XCTestCase {
    func testCreateRequestFilesUsesOwnerOnlyPermissions() throws {
        let runner = ProcessMergeRunner(environment: [:], fileManager: .default)
        let request = AppRunRequest(
            inputs: ["/tmp/input.docx"],
            outputPath: "/tmp/output.docx",
            reportPath: "/tmp/output.merge-report.json",
            templatePath: nil,
            insertSourceFileTitles: false
        )

        let requestFiles = try runner.createRequestFiles(request: request)
        defer {
            try? FileManager.default.removeItem(at: requestFiles.directoryURL)
        }

        let directoryAttributes = try FileManager.default.attributesOfItem(atPath: requestFiles.directoryURL.path)
        let fileAttributes = try FileManager.default.attributesOfItem(atPath: requestFiles.requestURL.path)

        let directoryPermissions = (directoryAttributes[.posixPermissions] as? NSNumber)?.intValue ?? 0
        let filePermissions = (fileAttributes[.posixPermissions] as? NSNumber)?.intValue ?? 0

        XCTAssertEqual(directoryPermissions & 0o777, 0o700)
        XCTAssertEqual(filePermissions & 0o777, 0o600)
    }
}
