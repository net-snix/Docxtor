import Foundation

enum MergeStage: String, Codable, Equatable {
    case starting
    case preflight
    case mergingInput = "merging-input"
    case validation
    case writingReport = "writing-report"
    case completed
}

struct MergeStageEvent: Codable, Equatable {
    let stage: MergeStage
    let message: String?
    let currentInputIndex: Int?
    let totalInputs: Int?
    let inputDisplayName: String?
}

enum HelperEvent: Equatable {
    case started(message: String?)
    case stage(MergeStageEvent)
    case completed(outputPath: String, reportPath: String)
    case failed(message: String, reportPath: String?)
}

struct AppRunRequest: Codable, Equatable {
    let inputs: [String]
    let outputPath: String
    let reportPath: String
    let templatePath: String?
    let insertSourceFileTitles: Bool
}

enum AppRunRequestBuilder {
    static func build(
        inputs: [URL],
        outputURL: URL,
        templateURL: URL? = nil,
        insertSourceFileTitles: Bool = false
    ) -> AppRunRequest {
        AppRunRequest(
            inputs: inputs.map(\.path),
            outputPath: outputURL.path,
            reportPath: defaultReportURL(for: outputURL).path,
            templatePath: templateURL?.path,
            insertSourceFileTitles: insertSourceFileTitles
        )
    }

    static func defaultOutputURL(for inputs: [URL]) -> URL? {
        guard let first = inputs.first else {
            return nil
        }

        return first
            .deletingLastPathComponent()
            .appending(path: "main.docx", directoryHint: .notDirectory)
    }

    static func defaultReportURL(for outputURL: URL) -> URL {
        let directory = outputURL.deletingLastPathComponent()
        let baseName = outputURL.deletingPathExtension().lastPathComponent
        return directory.appending(path: "\(baseName).merge-report.json", directoryHint: .notDirectory)
    }
}

enum HelperBridgeError: LocalizedError, Equatable {
    case helperMissing
    case badEventLine(String)
    case exited(status: Int32, stderr: String)

    var errorDescription: String? {
        switch self {
        case .helperMissing:
            return "Docxtor helper missing. Package the app with DOCXTOR_HELPER_PATH or set DOCXTOR_HELPER_PATH before launch."
        case .badEventLine(let line):
            return "Unexpected helper output: \(line)"
        case .exited(let status, let stderr):
            if stderr.isEmpty {
                return "Docxtor helper exited with status \(status)."
            }

            return "Docxtor helper exited with status \(status): \(stderr)"
        }
    }
}

enum HelperEventParser {
    private static let decoder = JSONDecoder()

    static func parse(line: String) throws -> HelperEvent {
        let data = Data(line.utf8)
        let envelope = try decoder.decode(EventEnvelope.self, from: data)

        switch envelope.type {
        case "started":
            return .started(message: envelope.message)
        case "stage":
            return .stage(
                MergeStageEvent(
                    stage: envelope.stage ?? .starting,
                    message: envelope.message,
                    currentInputIndex: envelope.currentInputIndex,
                    totalInputs: envelope.totalInputs,
                    inputDisplayName: envelope.inputDisplayName
                )
            )
        case "completed":
            guard
                let outputPath = envelope.outputPath, !outputPath.isEmpty,
                let reportPath = envelope.reportPath, !reportPath.isEmpty
            else {
                throw HelperBridgeError.badEventLine(line)
            }

            return .completed(
                outputPath: outputPath,
                reportPath: reportPath
            )
        case "failed":
            return .failed(
                message: envelope.message ?? "Merge failed.",
                reportPath: envelope.reportPath
            )
        default:
            throw HelperBridgeError.badEventLine(line)
        }
    }

    private struct EventEnvelope: Decodable {
        let type: String
        let stage: MergeStage?
        let message: String?
        let currentInputIndex: Int?
        let totalInputs: Int?
        let inputDisplayName: String?
        let outputPath: String?
        let reportPath: String?
    }
}

protocol MergeRunControlling: AnyObject {
    func cancel()
}

protocol MergeRunner {
    @discardableResult
    func start(
        request: AppRunRequest,
        onEvent: @escaping @Sendable (Result<HelperEvent, Error>) -> Void
    ) throws -> MergeRunControlling
}

final class ProcessMergeRunner: MergeRunner {
    private static let ownerOnlyDirectoryPermissions = NSNumber(value: Int16(0o700))
    private static let ownerOnlyFilePermissions = NSNumber(value: Int16(0o600))

    private let bundle: Bundle
    private let environment: [String: String]
    private let fileManager: FileManager

    init(
        bundle: Bundle = .main,
        environment: [String: String] = ProcessInfo.processInfo.environment,
        fileManager: FileManager = .default
    ) {
        self.bundle = bundle
        self.environment = environment
        self.fileManager = fileManager
    }

    func start(
        request: AppRunRequest,
        onEvent: @escaping @Sendable (Result<HelperEvent, Error>) -> Void
    ) throws -> MergeRunControlling {
        let helperURL = try resolveHelperURL()
        let requestFiles = try createRequestFiles(request: request)

        let process = Process()
        process.executableURL = helperURL
        process.arguments = ["app-run", "--request", requestFiles.requestURL.path]
        process.environment = environment

        let stdoutPipe = Pipe()
        let stderrPipe = Pipe()
        process.standardOutput = stdoutPipe
        process.standardError = stderrPipe

        let control = ProcessMergeRunControl(
            process: process,
            cleanupURL: requestFiles.directoryURL,
            fileManager: fileManager
        )

        control.connectOutput(
            stdoutHandle: stdoutPipe.fileHandleForReading,
            stderrHandle: stderrPipe.fileHandleForReading,
            onEvent: onEvent
        )

        process.terminationHandler = { _ in
            control.handleTermination(onEvent: onEvent)
        }

        do {
            try process.run()
        } catch {
            control.cancel()
            throw error
        }

        return control
    }

    private func resolveHelperURL() throws -> URL {
        if let override = environment["DOCXTOR_HELPER_PATH"], !override.isEmpty {
            return URL(fileURLWithPath: override)
        }

        if let bundled = bundle.resourceURL?.appending(path: "DocxtorHelper/Docxtor.Cli", directoryHint: .notDirectory),
           fileManager.isExecutableFile(atPath: bundled.path) {
            return bundled
        }

        throw HelperBridgeError.helperMissing
    }

    func createRequestFiles(request: AppRunRequest) throws -> (directoryURL: URL, requestURL: URL) {
        let requestDirectory = fileManager.temporaryDirectory.appending(path: "DocxtorMac-\(UUID().uuidString)", directoryHint: .isDirectory)
        try fileManager.createDirectory(
            at: requestDirectory,
            withIntermediateDirectories: true,
            attributes: [.posixPermissions: Self.ownerOnlyDirectoryPermissions]
        )

        let requestURL = requestDirectory.appending(path: "request.json", directoryHint: .notDirectory)
        let requestData = try JSONEncoder().encode(request)
        guard fileManager.createFile(
            atPath: requestURL.path,
            contents: requestData,
            attributes: [.posixPermissions: Self.ownerOnlyFilePermissions]
        ) else {
            throw CocoaError(.fileWriteUnknown)
        }

        return (requestDirectory, requestURL)
    }
}

private final class ProcessMergeRunControl: MergeRunControlling, @unchecked Sendable {
    private let process: Process
    private let cleanupURL: URL
    private let fileManager: FileManager
    private let lock = NSLock()

    private var wasCancelled = false
    private var sawTerminalEvent = false
    private var capturedStderr = ""
    private var stdoutTask: Task<Void, Never>?
    private var stderrTask: Task<Void, Never>?

    init(process: Process, cleanupURL: URL, fileManager: FileManager) {
        self.process = process
        self.cleanupURL = cleanupURL
        self.fileManager = fileManager
    }

    func connectOutput(
        stdoutHandle: FileHandle,
        stderrHandle: FileHandle,
        onEvent: @escaping @Sendable (Result<HelperEvent, Error>) -> Void
    ) {
        stdoutTask = Task.detached { [weak self] in
            do {
                for try await line in stdoutHandle.bytes.lines {
                    guard !line.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty else {
                        continue
                    }

                    let event = try HelperEventParser.parse(line: line)
                    self?.mark(event: event)
                    onEvent(.success(event))
                }
            } catch {
                onEvent(.failure(error))
            }
        }

        stderrTask = Task.detached { [weak self] in
            do {
                let data = try stderrHandle.readToEnd() ?? Data()
                self?.append(stderr: String(decoding: data, as: UTF8.self).trimmingCharacters(in: .whitespacesAndNewlines))
            } catch {
                self?.append(stderr: error.localizedDescription)
            }
        }
    }

    func cancel() {
        lock.lock()
        wasCancelled = true
        lock.unlock()

        if process.isRunning {
            process.terminate()
        }

        cleanUp()
    }

    func handleTermination(
        onEvent: @escaping @Sendable (Result<HelperEvent, Error>) -> Void
    ) {
        Task.detached { [weak self] in
            guard let self else {
                return
            }

            _ = await self.stdoutTask?.result
            _ = await self.stderrTask?.result

            let snapshot = self.snapshot()
            self.cleanUp()

            guard !snapshot.wasCancelled else {
                return
            }

            guard !snapshot.sawTerminalEvent else {
                return
            }

            onEvent(.failure(HelperBridgeError.exited(status: self.process.terminationStatus, stderr: snapshot.stderr)))
        }
    }

    private func mark(event: HelperEvent) {
        guard event.isTerminal else {
            return
        }

        lock.lock()
        sawTerminalEvent = true
        lock.unlock()
    }

    private func append(stderr: String) {
        guard !stderr.isEmpty else {
            return
        }

        lock.lock()
        defer { lock.unlock() }

        if capturedStderr.isEmpty {
            capturedStderr = stderr
        } else {
            capturedStderr += "\n\(stderr)"
        }
    }

    private func snapshot() -> (wasCancelled: Bool, sawTerminalEvent: Bool, stderr: String) {
        lock.lock()
        defer { lock.unlock() }
        return (wasCancelled, sawTerminalEvent, capturedStderr)
    }

    private func cleanUp() {
        try? fileManager.removeItem(at: cleanupURL)
    }
}

private extension HelperEvent {
    var isTerminal: Bool {
        switch self {
        case .completed, .failed:
            return true
        case .started, .stage:
            return false
        }
    }
}
