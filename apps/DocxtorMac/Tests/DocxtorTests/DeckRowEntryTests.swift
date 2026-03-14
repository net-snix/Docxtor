import XCTest
@testable import Docxtor

final class DeckRowEntryTests: XCTestCase {
    func testMakeEntriesUsesStableDocumentIDsInsteadOfIndexIdentity() {
        let first = InputDocument(
            id: UUID(uuidString: "11111111-1111-1111-1111-111111111111")!,
            url: URL(fileURLWithPath: "/tmp/docs/one.docx")
        )
        let second = InputDocument(
            id: UUID(uuidString: "22222222-2222-2222-2222-222222222222")!,
            url: URL(fileURLWithPath: "/tmp/docs/two.docx")
        )

        let initial = DeckRowEntry.makeEntries(for: [first, second])
        let reordered = DeckRowEntry.makeEntries(for: [second, first])

        XCTAssertEqual(initial.map(\.id), [first.id, second.id])
        XCTAssertEqual(reordered.map(\.id), [second.id, first.id])
        XCTAssertEqual(reordered.map(\.position), [1, 2])
    }

    func testMakeEntriesOnlyShowsDividerBeforeLastRow() {
        let entries = DeckRowEntry.makeEntries(
            for: [
                InputDocument(url: URL(fileURLWithPath: "/tmp/docs/one.docx")),
                InputDocument(url: URL(fileURLWithPath: "/tmp/docs/two.docx")),
                InputDocument(url: URL(fileURLWithPath: "/tmp/docs/three.docx"))
            ]
        )

        XCTAssertEqual(entries.map(\.showsDivider), [true, true, false])
    }
}
