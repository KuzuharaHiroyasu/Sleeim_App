//
//  DataAanalysis.swift
//  KaiminApp
//

import UIKit

class DataManager: NSObject {

    static let shared = DataManager()

    struct VersionData {
        var g1dAppVerMajor: Int
        var g1dAppVerMinor: Int
        var g1dAppVerRevision: Int
        var g1dAppVerBuild: Int
    }

    /**
     * 枠情報(日時等)のデータ
     */
    struct FrameData {
        var year: Int
        var month: Int
        var weekDay: Int
        var day: Int
        var hour: Int
        var minute: Int
        var second: Int
        var snoreDetectionCount: Int
        var apneaDetectionCount: Int
        var snoreTime: Int
        var apneaTime: Int
        var maxApneaTime: Int
        var sleepMode: Int
        var vibrationStrength: Int
        var snoreSensitivity: Int
        var apneaSensitivity: Int
        var suppressionStartTime: Int
    }

    /**
     * 睡眠データ(機器データコマンドから取得)
     */
    struct SleepData {
        // いびきの大きさ
        var snoreVolume1: Int
        var snoreVolume2: Int
        var snoreVolume3: Int
        // 呼吸状態
        var breathingState1: Int
        var breathingState2: Int
        var breathingState3: Int
        // 睡眠ステージ
        var sleepStage: Int
        // 首の向き
        var neckDirection1: Int
        var neckDirection2: Int
        var neckDirection3: Int
        // フォトセンサー
        var photoSensor1: Int
        var photoSensor2: Int
        var photoSensor3: Int
    }

    struct AlarmNotifiData {
        var state: Int
        var isAlarm: Bool
    }

    struct DeviceStatusData {
        var address: String
        var dataCount: Int
        var year: Int
        var month: Int
        var day: Int
        var hour: Int
        var minute: Int
        var second: Int
        var weekDay: Int
    }

    struct H1DUpdateDoneData {
        var state: Int
        var verMajor: Int
        var verMinor: Int
        var verRevision: Int
        var verBuild: Int
    }

    /// コマンドコード
    enum CommandCode: UInt8 {
        case deviceSettingChange = 0xC6
    }

    /// BLEコマンドID
    enum CommandType: Int {
        // コマンドなし
        case none = 0
        // 状態状態変更
        case stateChange = 1
        // 待機状態
        case stateChangeWait = 2
        // GET状態
        case stateChangeGet = 3
        // G1Dプログラム状態変更
        case stateChangeG1dUpdate = 5
        // 日時設定
        case timeSetting = 6
        // 電池残量情報取得
        case getBattery = 7
        // バージョン取得
        case getVersion = 8
        // データ取得NEXT
        case dataNext = 9
        // データ取得END
        case dataEnd = 10
        // 枠情報
        case dataTime = 11
        // 機器データ
        case dataSleep = 12
        // H1D プログラムデータ転送
        case h1dTransferData = 13
        // H1D プログラム転送結果
        case h1dTransferDataResult = 14
        // H1D プログラム更新完了確認
        case h1dTransferDataDone = 15
        // アラーム設定変更
        case alarmSetting = 16
        // アラーム通知
        case alarmNotification = 17
        // デバイス状況取得
        case getDevaiceStatus = 18
        // 取得完了
        case getEnd = 19
    }
    // コマンドのデータ定数
    private let STATE_CHANGE_COMMAND: UInt8 = 0xB0
    private let TIME_SETTING_COMMAND: UInt8 = 0xC1
    private let GET_BATTERY_COMMAND: UInt8 = 0xC2
    private let GET_VERSION_COMMAND: UInt8 = 0xC3
    private let DATA_NEXT_COMMAND: UInt8 = 0xE0
    private let DATA_END_COMMAND: UInt8 = 0xE1
    private let DATA_TIME_COMMAND: UInt8 = 0xE2
    private let DATA_SLEEP_COMMAND: UInt8 = 0xE3
    private let H1D_TRANSFER_DATA_RESULT: UInt8 = 0xD1
    private let H1D_TRANSFER_DATA_DONE: UInt8 = 0xD3
    private let ALARM_SETTING_COMMAND: UInt8 = 0xC0
    private let ALARM_NOTIFICATION_COMMAND: UInt8 = 0xC4
    private let DEVICE_GETSTATUS_COMMAND: UInt8 = 0xC5
    // H1Dデータ転送バイト数
    private let H1D_TRANSFER_DATA_LENGTH: NSInteger = 20
    //
    private let H1D_TRANSFER_SUM_DATA_LENGTH: NSInteger = 4
    // OKNGのデータ解析用
    private let DATA_BOOL_LENGTH: NSInteger = 2
    private let DATA_BOOL_INDEX: NSInteger = 1
    private let DATA_BOOL_OK: UInt8 = 0x00
    // 電池情報データ解析用
    private let DATA_BATTERY_LENGTH: NSInteger = 3
    private let DATA_BATTERY_INDEX: NSInteger = 2
    // バージョン情報データ解析用
    private let DATA_VERSION_LENGTH: NSInteger = 6
    private let DATA_VERSION_G1D_APP_MAJOR: NSInteger = 2
    private let DATA_VERSION_G1D_APP_MIONOR: NSInteger = 3
    private let DATA_VERSION_G1D_APP_REVISION: NSInteger = 4
    private let DATA_VERSION_G1D_APP_BUILD: NSInteger = 5
    // 枠情報のデータ解析用
    private let DATA_FRAME_YEAR: NSInteger = 1
    private let DATA_FRAME_MONTH: NSInteger = 2
    private let DATA_FRAME_WEEKDAY: NSInteger = 3
    private let DATA_FRAME_DAY: NSInteger = 4
    private let DATA_FRAME_HOUR: NSInteger = 5
    private let DATA_FRAME_MINUTE: NSInteger = 6
    private let DATA_FRAME_SECOND: NSInteger = 7
    private let DATA_FRAME_SNORE_DETECTION_COUNT1: NSInteger = 8
    private let DATA_FRAME_SNORE_DETECTION_COUNT2: NSInteger = 9
    private let DATA_FRAME_APNEA_DETECTION_COUNT1: NSInteger = 10
    private let DATA_FRAME_APNEA_DETECTION_COUNT2: NSInteger = 11
    private let DATA_SNORE_TIME_1: NSInteger = 12
    private let DATA_SNORE_TIME_2: NSInteger = 13
    private let DATA_APNEA_TIME_1: NSInteger = 14
    private let DATA_APNEA_TIME_2: NSInteger = 15
    private let DATA_FRAME_MAX_APHEA_TIME1: NSInteger = 16
    private let DATA_FRAME_MAX_APHEA_TIME2: NSInteger = 17
    private let DATA_FRAME_DEVICE_SETTING: NSInteger = 18
    private let DATA_FRAME_SUPPRESSION_START_TIME: NSInteger = 19
    private let DATA_FRAME_LENGTH: NSInteger = 20
    // 機器データ解析用
    private let DATA_SLEEP_SNORE_VOLUME1: NSInteger = 1
    private let DATA_SLEEP_SNORE_VOLUME2: NSInteger = 2
    private let DATA_SLEEP_SNORE_VOLUME3: NSInteger = 3
    private let DATA_SLEEP_STATE: NSInteger = 4
    private let DATA_SLEEP_NECK_DIRECTION: NSInteger = 5
    private let DATA_PHOTO_SENSOR1 : NSInteger = 6
    private let DATA_PHOTO_SENSOR2 : NSInteger = 7
    private let DATA_PHOTO_SENSOR3 : NSInteger = 8
    private let DATA_SLEEP_LENGTH: NSInteger = 9
    // プログラム転送結果データ解析用
    private let DATA_H1D_TRANSFER_SUM_LENGTH: NSInteger = 2
    private let DATA_H1D_TRANSFER_SUM_INDEX: NSInteger = 1
    // プログラム更新完了データ解析用
    private let DATA_H1D_TRANSFER_DONE_LENGTH: NSInteger = 6
    private let DATA_H1D_TRANSFER_DONE_STATE: NSInteger = 1
    private let DATA_H1D_TRANSFER_DONE_MAJOR: NSInteger = 2
    private let DATA_H1D_TRANSFER_DONE_MIONOR: NSInteger = 3
    private let DATA_H1D_TRANSFER_DONE_REVISION: NSInteger = 4
    private let DATA_H1D_TRANSFER_DONE_BUILD: NSInteger = 5
    // アラーム通知解析用
    private let DATA_ALARM_LENGTH: NSInteger = 3
    private let DATA_ALARM_STATE: NSInteger = 1
    private let DATA_ALARM_ISONOFF: NSInteger = 2
    // デバイス状況取得のデータ解析用
    private let DATA_DEVICE_LENGTH: NSInteger = 16
    private let DATA_DEVICE_ADDRESS1: NSInteger = 2
    private let DATA_DEVICE_ADDRESS2: NSInteger = 3
    private let DATA_DEVICE_ADDRESS3: NSInteger = 4
    private let DATA_DEVICE_ADDRESS4: NSInteger = 5
    private let DATA_DEVICE_ADDRESS5: NSInteger = 6
    private let DATA_DEVICE_ADDRESS6: NSInteger = 7
    private let DATA_DEVICE_DATA_COUNT: NSInteger = 8
    private let DATA_DEVICE_YEAR: NSInteger = 9
    private let DATA_DEVICE_MONTH: NSInteger = 10
    private let DATA_DEVICE_WEEKDAY: NSInteger = 11
    private let DATA_DEVICE_DAY: NSInteger = 12
    private let DATA_DEVICE_HOUR: NSInteger = 13
    private let DATA_DEVICE_MINUTE: NSInteger = 14
    private let DATA_DEVICE_SECOND: NSInteger = 15
    private let commandList: [CommandType : UInt8]

    private override init() {
        // 機器から送られてきたデータ解析ようにコマンドリストを作成
        self.commandList = [.stateChange : STATE_CHANGE_COMMAND,
                            .timeSetting : TIME_SETTING_COMMAND,
                            .getBattery : GET_BATTERY_COMMAND,
                            .getVersion : GET_VERSION_COMMAND,
                            .dataNext : DATA_NEXT_COMMAND,
                            .dataEnd : DATA_END_COMMAND,
                            .dataTime : DATA_TIME_COMMAND,
                            .dataSleep : DATA_SLEEP_COMMAND,
                            .h1dTransferDataResult : H1D_TRANSFER_DATA_RESULT,
                            .h1dTransferDataDone : H1D_TRANSFER_DATA_DONE,
                            .alarmSetting : ALARM_SETTING_COMMAND,
                            .alarmNotification : ALARM_NOTIFICATION_COMMAND,
                            .getDevaiceStatus : DEVICE_GETSTATUS_COMMAND]
    }

    //MARK:- Command要求用
    /**
     * 引数なしデータ生成
     *
     * @param CommandType commandType
     * @return Data? コマンドデータ
     */
    func createCommandData(_ commandType: CommandType) -> Data? {

        var retData: Data?

        switch commandType {
        case .stateChangeWait:
            let value: [UInt8] = [0xB0, 0x00]
            retData = Data(bytes: value)

        case .stateChangeGet:
            let value: [UInt8] = [0xB0, 0x03]
            retData = Data(bytes: value)

        case .stateChangeG1dUpdate:
            let value: [UInt8] = [0xB0, 0x05]
            retData = Data(bytes: value)

        case .getBattery:
            let value: [UInt8] = [0xC2]
            retData = Data(bytes: value)

        case .getVersion:
            let value: [UInt8] = [0xC3]
            retData = Data(bytes: value)

        case .h1dTransferDataDone:
            let value: [UInt8] = [0xD3]
            retData = Data(bytes: value)

        case .getDevaiceStatus:
            let value: [UInt8] = [0xC5]
            retData = Data(bytes: value)

        default:
            break
        }

        return retData
    }

    /**
     * End送信コマンドデータ作成
     *
     * @param Bool isOK データ取得OKかどうか
     */
    func createGetEndData(_ isOK: Bool) -> Data? {

        var value: [UInt8] = [0xE4]

        if isOK {
            value.append(0x00)
        } else {
            value.append(0x01)
        }
        return Data(bytes: value)
    }

    /**
     * 日時設定のコマンドデータ作成
     *
     * @param String date
     * @return Data? コマンドデータ
     */
    func createTimeSettingData(_ dateStr: String) -> Data? {

        let dateFormater = DateFormatter()
        dateFormater.locale = Locale.init(identifier: "ja_JP")
        dateFormater.dateFormat = "yyyy/MM/dd HH:mm:ss"

        if let date = dateFormater.date(from: dateStr) {

            let components = Calendar(identifier: .gregorian).dateComponents([.year, .month, .day, .hour, .minute, .second, .weekday], from: date)
            // 下２桁を取得
            let year = components.year! % 100
            let value: [UInt8] = [0xC1,
                                  UInt8(year),
                                  UInt8(components.month!),
                                  UInt8(components.weekday! - 1),
                                  UInt8(components.day!),
                                  UInt8(components.hour!),
                                  UInt8(components.minute!),
                                  UInt8(components.second!)]

            return Data(bytes: value)
        }

        return nil
    }

    /**
     * H1Dデータ転送コマンドデータ作成
     *
     * @param [UInt8] byteArray
     * @return Data? コマンドデータ
     */
    func createH1dTransferData(_ data: Data) -> Data? {

        if data.count < H1D_TRANSFER_DATA_LENGTH {
            return nil
        }

        return data
    }

    /**
     * H1Dデータ転送結果コマンドデータ作成
     *
     * @param Int sum
     * @return Data? コマンドデータ
     */
    func createH1dTransferSumData(_ sum: Data) -> Data? {

        if sum.count < H1D_TRANSFER_SUM_DATA_LENGTH {
            return nil
        }

        let value: [UInt8] = [0xD1, sum[0], sum[1], sum[2], sum[3]]

        return Data(bytes: value)
    }

    /**
     * アラーム設定コマンドデータ作成
     *
     * @param Int alarm アラーム有効
     * @param Int snoreAlarm いびきアラーム
     * @param Int snoreSensitivity いびきアラーム感度
     * @param Int apneaAlarm 低呼吸アラーム
     * @param Int alarmDelay アラーム遅延
     * @param Int bodyMoveStop 体動停止
     * @param Int alramTime アラーム時間
     */
    func crateAlarmSettingData(_ alarm: Int, snoreAlarm: Int,
                               snoreSensitivity: Int, apneaAlarm: Int,
                               alarmDelay: Int, bodyMoveStop: Int, alramTime: Int) -> Data? {

        let alarmUInt8 = UInt8(alarm)
        let snoreAlarmUInt8 = UInt8(snoreAlarm)
        let snoreSensitivityUInt8 = UInt8(snoreSensitivity)
        let apneaAlarmUInt8 = UInt8(apneaAlarm)
        let alarmDelayUInt8 = UInt8(alarmDelay)
        let bodyMoveStopUInt8 = UInt8(bodyMoveStop)
        let alramTimeUInt8 = UInt8(alramTime)

        let value: [UInt8] = [0xC0, alarmUInt8, snoreAlarmUInt8, snoreSensitivityUInt8, apneaAlarmUInt8,
                              alarmDelayUInt8, bodyMoveStopUInt8, alramTimeUInt8]
        return Data(bytes: value)
    }

    /**
     * コマンドタイプ取得
     *
     * @param Data data
     * @return CommandType コマンドタイプ
     */
    func checkCommandType(_ data: Data) -> CommandType {

        var ret: CommandType = .none

        if data.count < 1 {
            return ret
        }

        let commandValue = data[0]
        let item = commandList.first(where: { $0.value == commandValue})
        if item != nil {
            ret = item!.key
        }

        return ret
    }

    //MARK:- Command応答用
    /**
     * OKNGのデータ解析
     *
     * @param Data data
     * @return Bool 成否
     */
    func dataAnalysisBool(_ data: Data) -> Bool? {

        var ret = false
        // データがないのは失敗にする
        if data.count < DATA_BOOL_LENGTH {
            return nil
        }
        // OKの時だけ成功にする
        if data[DATA_BOOL_INDEX] == DATA_BOOL_OK {
            ret = true
        }

        return ret
    }

    /**
     * 電池情報解析
     *
     * @param Data data
     * @return Int 電池残量
     */
    func dataAnalysisBattery(_ data: Data,
                             isOK: UnsafeMutablePointer<ObjCBool>?) -> Int? {

        if data.count < DATA_BATTERY_LENGTH {
            return nil
        }

        isOK?.pointee = false
        // OKの時だけ成功にする
        if data[DATA_BOOL_INDEX] == DATA_BOOL_OK {
            isOK?.pointee = true
        }

        return self.convertUInt8ToInt(data: data[DATA_BATTERY_INDEX])
    }

    /**
     * バージョン情報解析
     *
     * @param Data data
     * @return VersionData バージョン情報構造体
     */
    func dataAnalysisVersion(_ data: Data,
                             isOK: UnsafeMutablePointer<ObjCBool>?) -> VersionData? {

        if data.count < DATA_VERSION_LENGTH {
            return nil
        }

        isOK?.pointee = false
        // OKの時だけ成功にする
        if data[DATA_BOOL_INDEX] == DATA_BOOL_OK {
            isOK?.pointee = true
        }

        let g1dAppVerMajor = self.convertUInt8ToInt(data: data[DATA_VERSION_G1D_APP_MAJOR])
        let g1dAppVerMinor = self.convertUInt8ToInt(data: data[DATA_VERSION_G1D_APP_MIONOR])
        let g1dAppVerRevision = self.convertUInt8ToInt(data: data[DATA_VERSION_G1D_APP_REVISION])
        let g1dAppVerBuild = self.convertUInt8ToInt(data: data[DATA_VERSION_G1D_APP_BUILD])

        return VersionData(g1dAppVerMajor: g1dAppVerMajor,
                           g1dAppVerMinor: g1dAppVerMinor,
                           g1dAppVerRevision: g1dAppVerRevision,
                           g1dAppVerBuild: g1dAppVerBuild)
    }

    /**
     * 枠情報解析
     *
     * @param Data data
     * @return FrameData 枠情報構造体
     */
    func dataAnalysisFrame(_ data: Data) -> FrameData? {

        if data.count < DATA_FRAME_LENGTH {
            return nil
        }

        let year = self.convertUInt8ToInt(data: data[DATA_FRAME_YEAR])
        let month = self.convertUInt8ToInt(data: data[DATA_FRAME_MONTH])
        let weekDay = self.convertUInt8ToInt(data: data[DATA_FRAME_WEEKDAY])
        let day = self.convertUInt8ToInt(data: data[DATA_FRAME_DAY])
        let hour = self.convertUInt8ToInt(data: data[DATA_FRAME_HOUR])
        let minute = self.convertUInt8ToInt(data: data[DATA_FRAME_MINUTE])
        let second = self.convertUInt8ToInt(data: data[DATA_FRAME_SECOND])
        let snoreDetectionCount = self.convertUInt8ArrayToInt(
            data1: data[DATA_FRAME_SNORE_DETECTION_COUNT1],
            data2: data[DATA_FRAME_SNORE_DETECTION_COUNT2])
        let apneaDetectionCount = self.convertUInt8ArrayToInt(
            data1: data[DATA_FRAME_APNEA_DETECTION_COUNT1],
            data2: data[DATA_FRAME_APNEA_DETECTION_COUNT2])
        let snoreTime = self.convertUInt8ArrayToInt(
            data1: data[DATA_SNORE_TIME_1],
            data2: data[DATA_SNORE_TIME_2])
        let apneaTime = self.convertUInt8ArrayToInt(
            data1: data[DATA_APNEA_TIME_1],
            data2: data[DATA_APNEA_TIME_2])
        let maxApneaTime = self.convertUInt8ArrayToInt(
            data1: data[DATA_FRAME_MAX_APHEA_TIME1],
            data2: data[DATA_FRAME_MAX_APHEA_TIME2])

        let sleepMode = Int(data[DATA_FRAME_DEVICE_SETTING] & 0b00000011); //0x03
        let vibrationStrength = Int((data[DATA_FRAME_DEVICE_SETTING] & 0b00001100) >> 2); //0x0C
        let snoreSensitivity = Int((data[DATA_FRAME_DEVICE_SETTING] & 0b00110000) >> 4);  //0x30
        let apneaSensitivity = Int((data[DATA_FRAME_DEVICE_SETTING] & 0b11000000) >> 6);  //0xC0
        let suppressionStartTime = Int(data[DATA_FRAME_SUPPRESSION_START_TIME] & 0b11111111); //0xff

        return FrameData(year: year,
                         month: month,
                         weekDay: weekDay,
                         day: day,
                         hour: hour,
                         minute: minute,
                         second: second,
                         snoreDetectionCount: snoreDetectionCount,
                         apneaDetectionCount: apneaDetectionCount,
                         snoreTime: snoreTime,
                         apneaTime: apneaTime,
                         maxApneaTime: maxApneaTime,
                         sleepMode: sleepMode,
                         vibrationStrength: vibrationStrength,
                         snoreSensitivity: snoreSensitivity,
                         apneaSensitivity: apneaSensitivity,
                         suppressionStartTime: suppressionStartTime
                         )
    }

    /**
     * 機器情報解析
     *
     * @param Data data
     * @return SleepData 機器情報構造体
     */
    func dataAnalysisSleepData(_ data: Data) -> SleepData? {

        if data.count < DATA_SLEEP_LENGTH {
            return nil
        }

        // いびきの大きさ
        let snoreVol1 = Int(data[DATA_SLEEP_SNORE_VOLUME1])
        let snoreVol2 = Int(data[DATA_SLEEP_SNORE_VOLUME2])
        let snoreVol3 = Int(data[DATA_SLEEP_SNORE_VOLUME3])
        // 呼吸状態
        let breathingState1 = Int((data[DATA_SLEEP_STATE] & 0b00000011))
        let breathingState2 = Int((data[DATA_SLEEP_STATE] & 0b00001100) >> 2)
        let breathingState3 = Int((data[DATA_SLEEP_STATE] & 0b00110000) >> 4)
        // 睡眠ステージ
        let sleepStage = Int((data[DATA_SLEEP_STATE] & 0b11000000) >> 6)
        // 首の向き
        let neckDirection1 = Int((data[DATA_SLEEP_NECK_DIRECTION] & 0b00000011))
        let neckDirection2 = Int((data[DATA_SLEEP_NECK_DIRECTION] & 0b00001100) >> 2)
        let neckDirection3 = Int((data[DATA_SLEEP_NECK_DIRECTION] & 0b00110000) >> 4)
        // フォトセンサー
        let photoSensor1 = Int(data[DATA_PHOTO_SENSOR1])
        let photoSensor2 = Int(data[DATA_PHOTO_SENSOR2])
        let photoSensor3 = Int(data[DATA_PHOTO_SENSOR3])

        return SleepData(
            snoreVolume1: snoreVol1,
            snoreVolume2: snoreVol2,
            snoreVolume3: snoreVol3,
            breathingState1: breathingState1,
            breathingState2: breathingState2,
            breathingState3: breathingState3,
            sleepStage: sleepStage,
            neckDirection1: neckDirection1,
            neckDirection2: neckDirection2,
            neckDirection3: neckDirection3,
            photoSensor1: photoSensor1,
            photoSensor2: photoSensor2,
            photoSensor3: photoSensor3
        )
    }

    /**
     * H1Dプログラム転送結果解析
     *
     * @param Data data
     * @return Int 結果
     */
    func dataAnalysisH1dUpdateSum(_ data: Data) -> Int? {

        if data.count < DATA_H1D_TRANSFER_SUM_LENGTH {
            return nil
        }

        return self.convertUInt8ToInt(data: data[DATA_H1D_TRANSFER_SUM_INDEX])
    }

    /**
     * H1Dプログラム完了確認解析
     *
     * @param Data data
     * @return H1DUpdateDoneData 更新情報構造体
     */
    func dataAnalysisH1dUpdateDone(_ data: Data) -> H1DUpdateDoneData? {

        if data.count < DATA_H1D_TRANSFER_DONE_LENGTH {
            return nil
        }
        // 完了状態
        let state = self.convertUInt8ToInt(data: data[DATA_H1D_TRANSFER_DONE_STATE])
        // verメジャー情報
        let major = self.convertUInt8ToInt(data: data[DATA_H1D_TRANSFER_DONE_MAJOR])
        // verマイナー情報
        let mionor = self.convertUInt8ToInt(data: data[DATA_H1D_TRANSFER_DONE_MIONOR])
        // verリビジョン情報
        let revision = self.convertUInt8ToInt(data: data[DATA_H1D_TRANSFER_DONE_REVISION])
        // verビルド情報
        let build = self.convertUInt8ToInt(data: data[DATA_H1D_TRANSFER_DONE_BUILD])

        return H1DUpdateDoneData(state: state,
                                 verMajor: major,
                                 verMinor: mionor,
                                 verRevision: revision,
                                 verBuild: build)
    }

    /**
     * アラーム通知解析
     *
     * @param Data data
     * @return AlarmNotifiData アラーム通知情報構造体
     */
    func dataAnalysisAlarm(_ data: Data) -> AlarmNotifiData? {

        if data.count < DATA_ALARM_LENGTH {
            return nil
        }

        let state = self.convertUInt8ToInt(data: data[DATA_ALARM_STATE])
        let isOnOff = self.convertUInt8ToInt(data: data[DATA_ALARM_ISONOFF])
        return AlarmNotifiData(state: state, isAlarm: (isOnOff == 1))
    }

    func dataAnalysisDeviceStatus(_ data: Data,
                                  isOK: UnsafeMutablePointer<ObjCBool>?) -> DeviceStatusData? {

        if data.count < DATA_DEVICE_LENGTH {
            return nil
        }

        isOK?.pointee = false
        // OKの時だけ成功にする
        if data[DATA_BOOL_INDEX] == DATA_BOOL_OK {
            isOK?.pointee = true
        }

        let address1 = self.convertUInt8ToInt(data: data[DATA_DEVICE_ADDRESS1])
        let address2 = self.convertUInt8ToInt(data: data[DATA_DEVICE_ADDRESS2])
        let address3 = self.convertUInt8ToInt(data: data[DATA_DEVICE_ADDRESS3])
        let address4 = self.convertUInt8ToInt(data: data[DATA_DEVICE_ADDRESS4])
        let address5 = self.convertUInt8ToInt(data: data[DATA_DEVICE_ADDRESS5])
        let address6 = self.convertUInt8ToInt(data: data[DATA_DEVICE_ADDRESS6])
        let address = "\(String(format:"%02X", address6))\(String(format:"%02X", address5))\(String(format:"%02X", address4))\(String(format:"%02X", address3))\(String(format:"%02X", address2))\(String(format:"%02X", address1))"

        let dataCount = self.convertUInt8ToInt(data: data[DATA_DEVICE_DATA_COUNT])

        let year = self.convertUInt8ToInt(data: data[DATA_DEVICE_YEAR])
        let month = self.convertUInt8ToInt(data: data[DATA_DEVICE_MONTH])
        let day = self.convertUInt8ToInt(data: data[DATA_DEVICE_DAY])
        let hour = self.convertUInt8ToInt(data: data[DATA_DEVICE_HOUR])
        let minute = self.convertUInt8ToInt(data: data[DATA_DEVICE_MINUTE])
        let second = self.convertUInt8ToInt(data: data[DATA_DEVICE_SECOND])
        let weekDay = self.convertUInt8ToInt(data: data[DATA_DEVICE_WEEKDAY])

        return DeviceStatusData(address: address,
                                dataCount: dataCount,
                                year: year,
                                month: month,
                                day: day,
                                hour: hour,
                                minute: minute,
                                second: second,
                                weekDay: weekDay)
    }

    /**
     * UInt8からIntに変換
     *
     * @param UInt8 data
     * @return Int 変換後の数字
     */
    private func convertUInt8ToInt(data: UInt8) -> Int {

        let array : [UInt8] = [data, 0, 0, 0]
        let datas = Data(bytes: array)
        let value = Int(datas.withUnsafeBytes{ (p: UnsafePointer<UInt32>) in p.pointee})
        return value
    }

    /**
     * unsind shortからIntに変換
     *
     * @param UInt8 data1
     * @param UInt8 data2
     * @return Int 変換後の数字
     */
    private func convertUInt8ArrayToInt(data1: UInt8, data2: UInt8) -> Int {

        let array : [UInt8] = [data1, data2, 0, 0]
        let datas = Data(bytes: array)
        let value = Int(datas.withUnsafeBytes{ (p: UnsafePointer<UInt32>) in p.pointee})
        return value
    }
}
