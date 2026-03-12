// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "DocxtorMac",
    platforms: [
        .macOS(.v14)
    ],
    products: [
        .executable(
            name: "Docxtor",
            targets: ["Docxtor"]
        )
    ],
    targets: [
        .executableTarget(
            name: "Docxtor",
            path: "Sources/Docxtor"
        ),
        .testTarget(
            name: "DocxtorTests",
            dependencies: ["Docxtor"],
            path: "Tests/DocxtorTests"
        )
    ]
)
