//
//  CsvFileManager.swift
//  KaiminApp
//

import Foundation

class CsvFileManager: NSObject {

    // プロフィール情報構造体
    struct ProfileData {
        var name: String
        var sex: String
        var birthday: String
        var tall: String
        var weight: String
        var sleepStartTime: String
        var sleepEndTime: String
    }

    // ファームウェア情報構造体
    struct FirmwareData {
        var deviceId: String
        var g1dVersion: String
    }

    // 睡眠記録開始時間情報構造体
    struct RecordingTimeData {
        var directoryName: String
        var fileName: String
        var date: String
        var weekDay: String
        var time: String
        var snoreDetectionCount: String
        var apneaDetectionCount: String
        var snoreTime: String
        var apneaTime: String
        var maxApneaTime: String
        var sleepMode: String
        var vibrationStrength: String
        var snoreSensitivity: String
        var apneaSensitivity: String
        var suppressionStartTime: String
        var dataLength: String
    }

    // 睡眠データ構造体
    struct SleepData {
        var date: String
        var weekDay: String
        var time: String
        var breathingState1: String
        var breathingState2: String
        var breathingState3: String
        var sleepStage: String
        var snoreVolume1: String
        var snoreVolume2: String
        var snoreVolume3: String
        var neckDirection1: String
        var neckDirection2: String
        var neckDirection3: String
        var photoSensor1: String
        var photoSensor2: String
        var photoSensor3: String
    }

    // 戻り値　ファイル情報
    struct CsvFileInfo {
        var tempPath: String
        var fileName: String
    }

    // TEMPファイルのカウント
    static var csvCount: Int = 0
    private static let TEMP_FILE_NAME = "tmp"

    /**
     * CSV書き込み
     *
     * @param ProfileData profileData プロフィル情報構造体
     * @param FirmwareData firmwareData ファームウェア情報構造体
     * @param RecordingTimeData recordingTimeData 睡眠記録開始時間情報構造体
     * @return String? CSVまでのパス 失敗時はnil
     */
    static func writeCsv(profileData: ProfileData,
                         firmwareData: FirmwareData,
                         recordingTimeData: RecordingTimeData,
                         sleepData: [SleepData]) throws -> CsvFileInfo? {

        guard let documentPath = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first else {
            return nil
        }

        let retPath = firmwareData.deviceId + "/" + recordingTimeData.directoryName
        let directoryPath = documentPath.appendingPathComponent(retPath)
        // Documents内にディレクトリを作成
        try FileManager.default.createDirectory(at: directoryPath, withIntermediateDirectories: true, attributes: nil)

        // カウントをあげる1始まりにしておく
        csvCount = csvCount + 1
        // CSVファイル名
        let csvName = String(format: "%@%02d%@", TEMP_FILE_NAME, csvCount, ".csv")
        // ファイルパス
        let csvPath = directoryPath.appendingPathComponent(csvName)
        // 書き込みデータ
        // プロフィール情報
        let profileText = [profileData.name,
                           profileData.sex,
                           profileData.birthday,
                           profileData.tall,
                           profileData.weight,
                           profileData.sleepStartTime,
                           profileData.sleepEndTime]
        // ファームウェア情報
        let firmwareData = [firmwareData.g1dVersion]
        // 睡眠記録開始時間
        let recordingTimeDataArray :[String] = [recordingTimeData.date,
                                 recordingTimeData.weekDay,
                                 recordingTimeData.time,
                                 recordingTimeData.snoreDetectionCount,
                                 recordingTimeData.apneaDetectionCount,
                                 recordingTimeData.snoreTime,
                                 recordingTimeData.apneaTime,
                                 recordingTimeData.maxApneaTime,
                                 recordingTimeData.sleepMode,
                                 recordingTimeData.vibrationStrength,
                                 recordingTimeData.snoreSensitivity,
                                 recordingTimeData.apneaSensitivity,
                                 recordingTimeData.suppressionStartTime,
                                 String(format: "%d", sleepData.count)]

        var sleepDataStr = ""
        // 睡眠データ
        for data in sleepData {
            // 書き込みデータ
            let textData: [String] = [data.date,
                                      data.weekDay,
                                      data.time,
                                      data.breathingState1,
                                      data.breathingState2,
                                      data.breathingState3,
                                      data.sleepStage,
                                      data.snoreVolume1,
                                      data.snoreVolume2,
                                      data.snoreVolume3,
                                      data.neckDirection1,
                                      data.neckDirection2,
                                      data.neckDirection3,
                                      data.photoSensor1,
                                      data.photoSensor2,
                                      data.photoSensor3]
            sleepDataStr.append(textData.joined(separator: ",") + "\r\n")
        }
        // 文字列 BOM付きにする 改行はCRLF
        var str = String.init(format: "\u{FEFF}%@\r\n%@\r\n%@\r\n",
                              profileText.joined(separator: ","),
                              firmwareData.joined(separator: ","),
                              recordingTimeDataArray.joined(separator: ","))

        if !sleepDataStr.isEmpty {
            str.append(sleepDataStr)
        }

        // utf8で書き込み　エラーは呼び出し元に任せる
        try str.write(to: csvPath, atomically: true, encoding: .utf8)

        return CsvFileInfo(tempPath: String(format: "/%@/%@", retPath, csvName), fileName: recordingTimeData.fileName)
    }
}
