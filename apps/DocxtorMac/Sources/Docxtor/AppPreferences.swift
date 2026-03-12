import Foundation

protocol AppPreferencesStore: AnyObject {
    var lastInputDirectory: URL? { get }
    var lastOutputDirectory: URL? { get }
    var insertSourceFileTitles: Bool { get }

    func setLastInputDirectory(_ url: URL?)
    func setLastOutputDirectory(_ url: URL?)
    func setInsertSourceFileTitles(_ enabled: Bool)
}

final class AppPreferences: AppPreferencesStore {
    private enum Keys {
        static let lastInputDirectory = "docxtor.lastInputDirectory"
        static let lastOutputDirectory = "docxtor.lastOutputDirectory"
        static let insertSourceFileTitles = "docxtor.insertSourceFileTitles"
    }

    private let userDefaults: UserDefaults

    init(userDefaults: UserDefaults = .standard) {
        self.userDefaults = userDefaults
    }

    var lastInputDirectory: URL? {
        url(forKey: Keys.lastInputDirectory)
    }

    var lastOutputDirectory: URL? {
        url(forKey: Keys.lastOutputDirectory)
    }

    var insertSourceFileTitles: Bool {
        userDefaults.bool(forKey: Keys.insertSourceFileTitles)
    }

    func setLastInputDirectory(_ url: URL?) {
        set(url, forKey: Keys.lastInputDirectory)
    }

    func setLastOutputDirectory(_ url: URL?) {
        set(url, forKey: Keys.lastOutputDirectory)
    }

    func setInsertSourceFileTitles(_ enabled: Bool) {
        userDefaults.set(enabled, forKey: Keys.insertSourceFileTitles)
    }

    private func url(forKey key: String) -> URL? {
        guard let value = userDefaults.string(forKey: key), !value.isEmpty else {
            return nil
        }

        return URL(fileURLWithPath: value)
    }

    private func set(_ url: URL?, forKey key: String) {
        if let url {
            userDefaults.set(url.path, forKey: key)
        } else {
            userDefaults.removeObject(forKey: key)
        }
    }
}
