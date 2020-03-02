//
//  BluetoothSwift.swift
//  KaiminApp
//

import UIKit
import CoreBluetooth
import UserNotifications

class BluetoothSwift: NSObject {
    struct DateObj {
        var date: String!
        var time: String!
        var weekday: String!
    }
    static let shared = BluetoothSwift()
    var isBluetoothPoweredOn = false
    var isBluetoothSupported = true

    enum BleState: Int {
        case unsupported = 0
        case poweredOff = 1
        case poweredOn = 2
    }

    private let DEVICE_NAME = "Sleeim"
    // ペリフェラルの識別子
    private let PERIPHERAL_IDENTIFIER_KEY_NAME = "peripheralIdentifier"
    // UARサービス
    private let VUART_HDL_SVC_UUID = "D68C0001-A21B-11E5-8CB8-0002A5D5C51B"
    // サーバーからクライアントへの文字列送信用
    private let VUART_HDL_INDICATION_CHAR_UUID = "D68C0002-A21B-11E5-8CB8-0002A5D5C51B"
    // クライアントからサーバーへの文字列送信用
    private let VUART_HDL_WRITE_UUID = "D68C0003-A21B-11E5-8CB8-0002A5D5C51B"
    // ファームウェアアップデート用サービスUUID
    private let VUART_FWUP_SVC_UUID = "01010000-0000-0000-0000-000000000080"
    // ファームウェア更新制御コマンド通信用UUID
    private let VUARE_FWUP_CONTROL_CHAR_UUID = "02010000-0000-0000-0000-000000000080"
    // ファームウェア更新データ通信用UUID
    private let VUARE_FWUP_DATA_CHAR_UUID = "03010000-0000-0000-0000-000000000080"
    // セントラルマネージャの復元識別子
    private let OPTION_RESTORE_IDENTIFIER_KEY = "KaiminCentralManagerIdentifier"

    private let TIME_OUT_1SEC: Int = 1
    private let TIME_OUT_30SEC: Int = 30

    //サービスUUID
    private var serviceUUID :String

    // ファーム更新時のキャラクタリスティックUUID
    private var firmwareUpdateCharacteristicUUID :String

    private var centralManager : CBCentralManager!
    private var peripheral: CBPeripheral!
    private var indicationCharacteristic: CBCharacteristic!
    private var writeCharacteristic: CBCharacteristic!
    private var FWUPControlCharacteristic: CBCharacteristic!
    private var FWUPDataCharacteristic: CBCharacteristic!
    private var isFirstConnect = false
    private var isScanPeripherals = false
    private var isdeInitialize = false
    private var workItem: DispatchWorkItem!
    private var searchPeripheralArray: [CBPeripheral] = []
    private var restorationPeripheralArray: [CBPeripheral] = []
    private var profileData: CsvFileManager.ProfileData?
    private var firmwareData:CsvFileManager.FirmwareData?
    private var recordingTimeData:CsvFileManager.RecordingTimeData? {
        didSet {
            if let data = recordingTimeData {
                let dateText = "\(data.date) \(data.time)"
                let dateFormater = DateFormatter()
                dateFormater.locale = Locale.init(identifier: "ja_JP")
                dateFormater.dateFormat = "yyyy/MM/dd HH:mm:ss"
                recordingTimeDate = dateFormater.date(from: dateText)
            } else {
                recordingTimeDate = nil
            }
        }
    }
    private var sleepDataArray:[CsvFileManager.SleepData] = []
    private var recordingTimeDate:Date?

    // 接続しようとしているペリフェラル
    private var willConnectPeripheral: CBPeripheral?
    // 現在のコマンド
    private var currentCommand = DataManager.CommandType.none

    override init() {
        // UUID設定
        serviceUUID = VUART_HDL_SVC_UUID
        firmwareUpdateCharacteristicUUID = VUARE_FWUP_CONTROL_CHAR_UUID

        super.init()
        // iCloudのバックアップ除外
        let documentPath = NSSearchPathForDirectoriesInDomains(.documentDirectory, .userDomainMask, true)[0]
        let documentsURL = NSURL(fileURLWithPath: documentPath)
        do {
            try documentsURL.setResourceValue(NSNumber(value: true), forKey: .isExcludedFromBackupKey)
        } catch {

        }
    }
    //MARK:- API
    /**
     * イニシャライズ
     * アプリケーション起動時に呼ばれる想定
     */
    func initialize() {
        let options = [CBCentralManagerOptionRestoreIdentifierKey : OPTION_RESTORE_IDENTIFIER_KEY]
        self.centralManager = CBCentralManager(delegate: self,
                                               queue: nil,
                                               options: options)
        AlarmManager.shared.requestAuthorization()
    }

    /**
     * ファイナライズ
     * アプリケーション終了時に呼ばれる想定
     */
    func deInitialize() {
        self.isdeInitialize = true
        self.removePeripheral()
    }

    /**
     * 設定画面を開く（BLE用）
     */
    func openBLESetting() {
        // アプリ固有の設定画面を開く（細かい設定画面の指定はreject対象）
        UIApplication.shared.open(URL(string: UIApplicationOpenSettingsURLString)!,
                                  options: [:],
                                  completionHandler: nil)
    }

    /**
     * 接続中断or切断処理
     */
    func disConnectPeripheral() {
        if let willConnectPeripheral = willConnectPeripheral {
            // 接続要求中の場合
            timeoutCancel()
            self.centralManager.cancelPeripheralConnection(willConnectPeripheral)
        }
        removePeripheral()
    }

    /**
     * CSVのヘッダ情報設定（プロフィール情報、ファームウェア情報）
     *
     * @param String deviceId 機器から取得したID
     * @param String nickname ニックネーム
     * @param String sex 性別
     * @param String birthday 誕生日
     * @param String tall 身長
     * @param String weight 体重
     * @param String sleepStartTime 睡眠開始時間
     * @param String sleepEndTime 睡眠終了時間
     * @param String g1dVersion G1Dファームウェアバージョン
     */
    func setCsvHeaderInfo(_ deviceId: String, nickname: String, sex: String,
                          birthday: String, tall: String,
                          weight: String, sleepStartTime: String,
                          sleepEndTime: String, g1dVersion: String) {
        self.profileData = CsvFileManager.ProfileData(name: nickname, sex: sex,
                                                      birthday: birthday, tall: tall,
                                                      weight: weight, sleepStartTime: sleepStartTime,
                                                      sleepEndTime: sleepEndTime)

        self.firmwareData = CsvFileManager.FirmwareData(deviceId: deviceId,
                                                        g1dVersion: g1dVersion)
    }

    /**
     * スキャン開始
     */
    func scanStart() {
        // BluetoothがONじゃない時は何もしない
        if !isBluetoothPoweredOn {
            // エラー通知(コマンドIDないので0)
            BluetoothPlugin.shared.callBackError(0, errorCode: .timeOut)
            return
        }
        if centralManager.isScanning {
            timeoutCancel()
        }
        // 初期化しておく
        self.searchPeripheralArray = []
        self.workItem = DispatchWorkItem {
            self.scanTimeout()
        }

        let dispatchWallTime = DispatchWallTime.now() + .seconds(TIME_OUT_30SEC)
        // タイムアウト処理登録
        DispatchQueue.global(qos: .default).asyncAfter(wallDeadline: dispatchWallTime,
                                                       execute: self.workItem)

        // サービスのUUIDを作成して検索
        let uuidArray = [CBUUID(string: self.serviceUUID)]
        self.centralManager.scanForPeripherals(withServices: nil,
                                               options: [CBCentralManagerScanOptionAllowDuplicatesKey : true])// 同一機器を何度も検知するよう修正
    }

    /**
     * スキャン停止
     */
    func scanStop() {
        if centralManager.isScanning {
            timeoutCancel()
            self.centralManager.stopScan()
        }
    }

    /**
     * ペリフェアル接続
     */
    func connectionPeripheral(_ index: Int) {
        // スキャン停止
        scanStop()
        // BluetoothがONじゃない時は何もしない
        if !isBluetoothPoweredOn {
            // エラー通知(コマンドIDないので0)
            BluetoothPlugin.shared.callBackError(0, errorCode: .connection)
            return
        }
        // 選択されたindexがない時はエラー
        if (self.searchPeripheralArray.count - 1) < index || index < 0 {
            // エラー通知(コマンドIDないので0)
            BluetoothPlugin.shared.callBackError(0, errorCode: .connection)
            return
        }

        let peripheral = self.searchPeripheralArray[index]
        connectPeripheral(peripheral)
    }

    private func connectPeripheral(_ peripheral: CBPeripheral) {
        // スキャン停止
        scanStop()

        if self.peripheral != nil &&
            self.peripheral.state == .connected {
            // 接続済の場合
            if self.peripheral == peripheral {
                // 同一端末の場合
                BluetoothPlugin.shared.callBackConnectionPeripheral(self.peripheral.identifier.description,
                                                                    deviceName: DEVICE_NAME,
                                                                    address: "")
                return
            } else {
                // 機器を切断する
                disConnectPeripheral()
            }
        }

        if let willConnectPeripheral = willConnectPeripheral {
            // 接続要求中の機器がある場合
            if self.peripheral == willConnectPeripheral {
                // 同一端末の場合
                return
            } else {
                // 機器を切断する
                disConnectPeripheral()
            }
        }
        // キャンセル用に保持
        willConnectPeripheral = peripheral

        // タイムアウト設定
        self.workItem = DispatchWorkItem {
            self.disConnectPeripheral()
            BluetoothPlugin.shared.callBackError(0, errorCode: .connection)
        }

        let dispatchWallTime = DispatchWallTime.now() + .seconds(TIME_OUT_30SEC)
        // タイムアウト処理登録
        DispatchQueue.global(qos: .default).asyncAfter(wallDeadline: dispatchWallTime,
                                                       execute: self.workItem)
        // 接続する
        self.centralManager.connect(peripheral, options: nil)
    }

    /**
     * ペリフェアル再接続
     *
     * @param UUID identifier ペリフェアル識別子
     */
    func reConnectionPeripheral(_ identifier: UUID) {
        // スキャン停止
        scanStop()
        // 復帰して接続していたペリフェラルがあるならそれと接続
        if self.restorationPeripheralArray.count > 0 {
            if let restorationPeripheral = self.restorationPeripheralArray.filter({ $0.identifier == identifier }).first {
                // 接続する
                connectPeripheral(restorationPeripheral)
                return
            }
        }

        // 既知のペリフェラルのリストを取得する
        let identifiers = self.centralManager.retrievePeripherals(withIdentifiers: [identifier])
        if identifiers.count > 0 {
            // 接続する
            connectPeripheral(identifiers[0])
            return
        }

        let connectedPeripherals = self.centralManager.retrieveConnectedPeripherals(withServices: [CBUUID(string: self.serviceUUID)])
        if let connectedPeripheral = connectedPeripherals.filter({ $0.identifier == identifier }).first {
            // 接続する
            connectPeripheral(connectedPeripheral)
            return
        }

        // 何もないのでエラーにする(コマンドIDないので0)
        BluetoothPlugin.shared.callBackError(0, errorCode: .peripheralCaseRemoved)
    }

    /**
     * コマンド送信
     *
     * @param Int commandId コマンド
     */
    func sendCommand(_ commandId: Int) {
        // タイムアウト処理はキャンセルしておく
        self.timeoutCancel()
        guard let commandType = DataManager.CommandType(rawValue: commandId) else {
            return
        }
        if isCheckBLEError(commandId: commandId) {
            // エラー通知
            return
        }
        currentCommand = commandType

        // 非同期でコマンド送る
        DispatchQueue.global(qos: .default).async {
            if let data = DataManager.shared.createCommandData(commandType) {
                if commandType == .stateChangeGet {
                    // get取得の時はCSVのTEMP名を初期化
                    CsvFileManager.csvCount = 0
                }
                if( self.serviceUUID == self.VUART_HDL_SVC_UUID){
                    self.peripheral.writeValue(data,
                                               for: self.writeCharacteristic,
                                               type: .withResponse)

                }
                else{
                    switch self.firmwareUpdateCharacteristicUUID {
                    case self.VUARE_FWUP_CONTROL_CHAR_UUID:
                        self.peripheral.writeValue(data,
                                                   for: self.FWUPControlCharacteristic,
                                                   type: .withResponse)
                    case self.VUARE_FWUP_DATA_CHAR_UUID:
                        self.peripheral.writeValue(data,
                                                   for: self.FWUPDataCharacteristic,
                                                   type: .withoutResponse)
                        // 書き込み成功
                        BluetoothPlugin.shared.callBackWrite(commandType.rawValue, isOK: true)
                    default:
                        break;
                    }       
                }
            } else {
                // エラー通知
                var cId = commandType.rawValue
                if cId <= DataManager.CommandType.stateChangeG1dUpdate.rawValue {
                    // 1系のエラーの場合
                    cId = DataManager.CommandType.stateChange.rawValue
                }
                BluetoothPlugin.shared.callBackWrite(commandType.rawValue, isOK: false)
            }
        }
    }
    /**
     * End送信
     *
     * @param Bool isOK データ取得OKかどうか
     */
    func sendGetEnd(_ isOK: Bool) {
        // タイムアウト処理はキャンセルしておく
        self.timeoutCancel()
        if isCheckBLEError(commandId: DataManager.CommandType.getEnd.rawValue) {
            // エラー通知
            return
        }

        currentCommand = .getEnd
        // 非同期でコマンド送る
        DispatchQueue.global(qos: .default).async {
            if let data = DataManager.shared.createGetEndData(isOK) {
                self.peripheral.writeValue(data, for: self.writeCharacteristic,
                                           type: .withResponse)
            } else {
                // エラー通知
                BluetoothPlugin.shared.callBackWrite(DataManager.CommandType.getEnd.rawValue,
                                                     isOK: false)
            }
        }
    }

    /**
     * 日時設定
     * フォーマット yyyy/mm/dd hh:mm:ss
     *
     * @param String date 日付
     */
    func sendDateSetting(_ date: String) {
        // タイムアウト処理はキャンセルしておく
        self.timeoutCancel()
        if isCheckBLEError(commandId: DataManager.CommandType.timeSetting.rawValue) {
            // エラー通知
            return
        }

        currentCommand = .timeSetting
        // 非同期でコマンド送る
        DispatchQueue.global(qos: .default).async {
            if let data = DataManager.shared.createTimeSettingData(date) {
                self.peripheral.writeValue(data,
                                           for: self.writeCharacteristic,
                                           type: .withResponse)
            } else {
                // エラー通知
                BluetoothPlugin.shared.callBackWrite(DataManager.CommandType.timeSetting.rawValue,
                                                     isOK: false)
            }
        }
    }

    /**
     * H1Dプログラムデータ転送
     *
     * @param Data data 転送データ
     */
    func sendH1DDate(_ h1dData: Data) {
        // タイムアウト処理はキャンセルしておく
        self.timeoutCancel()
        if isCheckBLEError(commandId: DataManager.CommandType.h1dTransferData.rawValue) {
            // エラー通知
            return
        }

        currentCommand = .h1dTransferData
        // 非同期でコマンド送る
        DispatchQueue.global(qos: .default).async {
            if let data = DataManager.shared.createH1dTransferData(h1dData) {
                self.peripheral.writeValue(data,
                                           for: self.writeCharacteristic,
                                           type: .withResponse)
            } else {
                // エラー通知
                BluetoothPlugin.shared.callBackWrite(DataManager.CommandType.h1dTransferData.rawValue,
                                                     isOK: false)
            }
        }
    }

    /**
     * H1Dプログラム転送結果
     *
     * @param Data sum アドレス
     */
    func sendH1DCheckSUM(_ sum: Data) {
        // タイムアウト処理はキャンセルしておく
        self.timeoutCancel()
        if isCheckBLEError(commandId: DataManager.CommandType.h1dTransferDataResult.rawValue) {
            // エラー通知
            return
        }

        currentCommand = .h1dTransferDataResult

        // 非同期でコマンド送る
        DispatchQueue.global(qos: .default).async {
            if let data = DataManager.shared.createH1dTransferSumData(sum) {
                self.peripheral.writeValue(data,
                                           for: self.writeCharacteristic,
                                           type: .withResponse)
            } else {
                // エラー通知
                BluetoothPlugin.shared.callBackWrite(DataManager.CommandType.h1dTransferDataResult.rawValue,
                                                     isOK: false)
            }
        }
    }

    /**
     * アラーム設定
     *
     * @param Int alarm アラーム有効
     * @param Int snoreAlarm いびきアラーム
     * @param Int snoreSensitivity いびきアラーム感度
     * @param Int apneaAlarm 低呼吸アラーム
     * @param Int alarmDelay アラーム遅延
     * @param Int bodyMoveStop 体動停止
     * @param Int alramTime アラーム時間
     */
    func sendAlarmSetting(_ alarm: Int, snoreAlarm: Int, snoreSensitivity: Int,
                          apneaAlarm: Int, alarmDelay: Int, bodyMoveStop: Int, alramTime: Int) {
        // タイムアウト処理はキャンセルしておく
        self.timeoutCancel()
        if isCheckBLEError(commandId: DataManager.CommandType.alarmSetting.rawValue) {
            // エラー通知
            return
        }

        currentCommand = .alarmSetting

        // 非同期でコマンド送る
        DispatchQueue.global(qos: .default).async {
            if let data = DataManager.shared.crateAlarmSettingData(alarm, snoreAlarm: snoreAlarm,
                                                                   snoreSensitivity: snoreSensitivity, apneaAlarm: apneaAlarm,
                                                                   alarmDelay: alarmDelay, bodyMoveStop: bodyMoveStop,
                                                                   alramTime: alramTime) {
                self.peripheral.writeValue(data,
                                           for: self.writeCharacteristic,
                                           type: .withResponse)
            } else {
                // エラー通知
                BluetoothPlugin.shared.callBackWrite(DataManager.CommandType.alarmSetting.rawValue,
                                                     isOK: false)
            }
        }
    }


    /// BLEコマンドを送信する.
    ///
    /// - Parameters:
    ///   - command: コマンド
    func sendBleCommand(_ commandType: Int, command: Data) {
        self.timeoutCancel()
        let id = commandType
        if self.peripheral == nil {
            BluetoothPlugin.shared.callBackWrite(id, isOK: false)
            return
        }

        DispatchQueue.global(qos: .default).async {
            switch self.firmwareUpdateCharacteristicUUID {
            case self.VUARE_FWUP_CONTROL_CHAR_UUID:
                if (self.FWUPControlCharacteristic != nil) {
                    self.peripheral.writeValue(command,
                                            for: self.FWUPControlCharacteristic,
                                            type: .withResponse)
                } else if (self.writeCharacteristic != nil) {
                    self.peripheral.writeValue(command,
                                           for: self.writeCharacteristic,
                                           type: .withResponse)
                }
            case self.VUARE_FWUP_DATA_CHAR_UUID:
                self.peripheral.writeValue(command,
                                           for: self.FWUPDataCharacteristic,
                                           type: .withoutResponse)
                // 書き込み成功
                BluetoothPlugin.shared.callBackWrite(commandType, isOK: true)
            default :
                self.peripheral.writeValue(command,
                                           for: self.writeCharacteristic,
                                           type: .withResponse)
            }
        }
    }

    /// サービスUUIDをファーム汎用通信用に変更する
    func changeServiceUUIDToNormal() {
        self.serviceUUID = VUART_HDL_SVC_UUID
    }

    /// サービスUUIDをファーム更新用に変更する
    func changeServiceUUIDToFirmwareUpdate() {
        self.serviceUUID = VUART_FWUP_SVC_UUID
    }

    // ファーム更新制御コマンド通信用キャラクタリスティックに変更する
    func changeCharacteristicUUIDToFirmwareUpdateControl() {
        self.firmwareUpdateCharacteristicUUID = VUARE_FWUP_CONTROL_CHAR_UUID
    }

    // ファーム更新データ通信用キャラクタリスティックに変更する
    func changeCharacteristicUUIDToFirmwareUpdateData() {
        self.firmwareUpdateCharacteristicUUID = VUARE_FWUP_DATA_CHAR_UUID
    }

    //MARK:-

    private func isCheckBLEError(commandId: Int) -> Bool {
//        if !isBluetoothPoweredOn {
//            // エラー通知
//            BluetoothPlugin.shared.callBackError(commandId,
//                                                 errorCode: .bluetoothOff)
//            return true
//        }
//
        if self.peripheral == nil {
            // エラー通知
//            BluetoothPlugin.shared.callBackError(commandId,
//                                                 errorCode: .sendBluetooth)
            BluetoothPlugin.shared.callBackWrite(commandId, isOK: false)
            return true
        }

        //プログラム更新完了確認待ち時はチェック不要
        if commandId != 15 {
            if peripheral.state != .connected {
                disConnectPeripheral()
                if peripheral.state != .connecting {
                    // connectedと.connectingの場合しか切断処理そをしないのでその他はここで通知する
                    BluetoothPlugin.shared.callBackError(commandId, errorCode: .disconnected)
                }
                return true
            }
        }

        return false
    }

    /**
     * 接続解除
     */
    private func removePeripheral() {
        // ペリフェラルが何もなければ接続されていないので何もしない
        guard let _peripheral = self.peripheral else {
            return
        }

        // 通知の申し込みを取り消す
        if self.indicationCharacteristic != nil {
            _peripheral.setNotifyValue(false, for: self.indicationCharacteristic)
        }

        if _peripheral.state == .connected ||
            _peripheral.state == .connecting {
            // 接続を解除する
            self.centralManager.cancelPeripheralConnection(_peripheral)
        }

        self.peripheral = nil
        self.indicationCharacteristic = nil
        self.writeCharacteristic = nil
        self.FWUPControlCharacteristic = nil
        self.FWUPControlCharacteristic = nil
    }

    /**
     * CSV書き込み
     *
     * @return String? CSVまでのパス 失敗時はnil
     */
    private func writeScv() -> CsvFileManager.CsvFileInfo? {
        guard let _profileData = self.profileData,
            let _firmwareData = self.firmwareData,
            let _recordingTimeData = self.recordingTimeData else {
                return nil
        }
        var info: CsvFileManager.CsvFileInfo?
        do {
            info = try CsvFileManager.writeCsv(profileData: _profileData,
                                               firmwareData: _firmwareData,
                                               recordingTimeData: _recordingTimeData,
                                               sleepData: self.sleepDataArray)
        } catch CocoaError.fileWriteOutOfSpace {
            //TODO:コマンドID必要？
            BluetoothPlugin.shared.callBackError(0, errorCode: .fileWriteOutOfSpace)
        } catch {
            //TODO:コマンドID必要？
            BluetoothPlugin.shared.callBackError(0, errorCode: .fileWrite)
        }
        // 初期化
        self.sleepDataArray = []
        self.recordingTimeData = nil

        return info
    }

    /**
     * コマンド結果を返す
     *
     * @param Data data 機器から取得したデータ
     */
    private func resultCommandData(data: Data) {
        let commandType = DataManager.shared.checkCommandType(data)

        // commandTypeは、仕様として定義されているコマンドコードとは別の値であり、
        // 開発者が定義した値である。
        // この値に引きずられると、保守しにくいコードになる可能性があるので、
        // 使わないほうがよい。

        // NOTE:新コード
        // 仕様で定められたコマンドコードの値を利用して処理を分ける
        let code = data[0]
        switch code {
            case DataManager.CommandCode.deviceSettingChange.rawValue:
                if let ret = DataManager.shared.dataAnalysisBool(data) {
                    BluetoothPlugin.shared.callBackBool(Int(code), isOK: ret)
                } else {
                    BluetoothPlugin.shared.callBackError(Int(code), errorCode: .timeOut)
                }

            default:
                break
        }

        // NOTE:旧コード
        // TODO:commandTypeで識別せずに、仕様として定められているコマンドコードで処理を分けるべき
        switch commandType {
        case .stateChange:
            // 状態変更
            if let ret = DataManager.shared.dataAnalysisBool(data) {
                BluetoothPlugin.shared.callBackBool(commandType.rawValue,
                                                    isOK: ret)
            } else {
                // エラー
                BluetoothPlugin.shared.callBackError(commandType.rawValue,
                                                     errorCode: .timeOut)
            }

        case .timeSetting:
            // 日時設定
            if let ret = DataManager.shared.dataAnalysisBool(data) {
                BluetoothPlugin.shared.callBackBool(commandType.rawValue,
                                                    isOK: ret)
            } else {
                // エラー
                BluetoothPlugin.shared.callBackError(commandType.rawValue,
                                                     errorCode: .timeOut)
            }

        case .getBattery:
            // バッテリ情報
            var isOK : ObjCBool = false
            if let ret = DataManager.shared.dataAnalysisBattery(data, isOK: &isOK) {
                if isOK.boolValue {
                    BluetoothPlugin.shared.callBackBattery(ret)
                } else {
                    BluetoothPlugin.shared.callBackBool(commandType.rawValue,
                                                        isOK: false)
                }
            } else {
                // エラー
                BluetoothPlugin.shared.callBackError(commandType.rawValue,
                                                     errorCode: .timeOut)
            }

        case .getVersion:
            // バージョン情報
            var isOK : ObjCBool = false
            if let ret = DataManager.shared.dataAnalysisVersion(data, isOK: &isOK) {
                if isOK.boolValue {
                    BluetoothPlugin.shared.callBackGetVersion(ret.g1dAppVerMajor,
                                                              g1dAppVerMinor: ret.g1dAppVerMinor,
                                                              g1dAppVerRevision: ret.g1dAppVerRevision,
                                                              g1dAppVerBuild: ret.g1dAppVerBuild)
                } else {
                    BluetoothPlugin.shared.callBackBool(commandType.rawValue,
                                                        isOK: false)
                }
            } else {
                // エラー
                BluetoothPlugin.shared.callBackError(commandType.rawValue,
                                                     errorCode: .timeOut)
            }

        case .dataNext:
            // CSV書き込み
            let info = self.writeScv()
            BluetoothPlugin.shared.callBackGetData(CsvFileManager.csvCount,
                                                   isNext: true,
                                                   isEnd: false,
                                                   tempPath: info?.tempPath,
                                                   fileName: info?.fileName)
            // タイムアウト処理を登録
            self.startTimeoutTimer(.stateChangeGet)

        case .dataEnd:
            // CSV書き込み
            let info = self.writeScv()
            BluetoothPlugin.shared.callBackGetData(CsvFileManager.csvCount,
                                                   isNext: false,
                                                   isEnd: true,
                                                   tempPath: info?.tempPath,
                                                   fileName: info?.fileName)

        case .dataTime:
            // 枠情報
            if let ret = DataManager.shared.dataAnalysisFrame(data) {
                // ファイル名作成
                // 現在日時
                let nowDate = Date()
                // 年取得
                let calendar = Calendar(identifier: .gregorian)
                let strDate = String(format: "%04d", calendar.component(.year, from: nowDate))
                // 年の上位２桁取得
                var yy = ""
                for (index, value) in strDate.enumerated() {
                    if index < 2 {
                        yy.append(value)
                    } else {
                        break
                    }
                }

                let directoryName = String(format: "%@%02d%02d",
                                           yy, ret.year, ret.month)
                let fileName = String(format: "%@%02d%02d%02d%02d%02d%02d.csv",
                                      yy, ret.year, ret.month, ret.day,
                                      ret.hour, ret.minute, ret.second)

                // 睡眠記録開始時間
                let date = String(format: "%@%02d/%d/%d",
                                  yy, ret.year, ret.month, ret.day)
                let weekStr = String(format: "%d", ret.weekDay)
                let time = String(format: "%02d:%02d:%02d",
                                  ret.hour, ret.minute, ret.second)
                let snoreDetectionCount = String(format: "%d", ret.snoreDetectionCount)
                let apneaDetectionCount = String(format: "%d", ret.apneaDetectionCount)
                let snoreTime = String(format: "%d", ret.snoreTime)
                let apneaTime = String(format: "%d", ret.apneaTime)
                let maxApneaTime = String(format: "%d", ret.maxApneaTime)

                let sleepMode = String(format: "%d", ret.sleepMode)
                let vibrationStrength = String(format: "%d", ret.vibrationStrength)
                let snoreSensitivity = String(format: "%d", ret.snoreSensitivity)
                let apneaSensitivity = String(format: "%d", ret.apneaSensitivity)
                let suppressionStartTime = String(format: "%d", ret.suppressionStartTime)
                
                self.recordingTimeData
                    = CsvFileManager.RecordingTimeData(directoryName: directoryName,
                                                       fileName: fileName,
                                                       date: date,
                                                       weekDay: weekStr,
                                                       time: time,
                                                       snoreDetectionCount: snoreDetectionCount,
                                                       apneaDetectionCount: apneaDetectionCount,
                                                       snoreTime: snoreTime,
                                                       apneaTime: apneaTime,
                                                       maxApneaTime: maxApneaTime,
                                                       dataLength: "0")
            }

            // タイムアウト処理を登録
            self.startTimeoutTimer(.stateChangeGet)

        case .dataSleep:
            // 機器データ
            if let ret = DataManager.shared.dataAnalysisSleepData(data) {
                // 睡眠データ作成
                let date = getSleepDataDate(dataCount: sleepDataArray.count + 1)
                // いびきの大きさとフォトセンサー下位２ビットがマイコン側で欠落するため2ビットシフトして補う
                let sleepData = CsvFileManager.SleepData(date: date.date,
                                                         weekDay: date.weekday,
                                                         time: date.time,
                                                         breathingState1: String(format: "%d", ret.breathingState1),
                                                         breathingState2: String(format: "%d", ret.breathingState2),
                                                         breathingState3: String(format: "%d", ret.breathingState3),
                                                         sleepStage: String(format: "%d", ret.sleepStage),
                                                         snoreVolume1: String(format: "%d", ret.snoreVolume1 * 4),
                                                         snoreVolume2: String(format: "%d", ret.snoreVolume2 * 4),
                                                         snoreVolume3: String(format: "%d", ret.snoreVolume3 * 4),
                                                         neckDirection1: String(format: "%d", ret.neckDirection1),
                                                         neckDirection2: String(format: "%d", ret.neckDirection2),
                                                         neckDirection3: String(format: "%d", ret.neckDirection3),
                                                         photoSensor1: String(format: "%d", ret.photoSensor1 * 4),
                                                         photoSensor2: String(format: "%d", ret.photoSensor2 * 4),
                                                         photoSensor3: String(format: "%d", ret.photoSensor3 * 4))

                self.sleepDataArray.append(sleepData)
            }
            // タイムアウト処理を登録
            self.startTimeoutTimer(.stateChangeGet)

        case .h1dTransferDataResult:
            // H1Dプログラム転送結果
            if let ret = DataManager.shared.dataAnalysisH1dUpdateSum(data) {
                BluetoothPlugin.shared.callBackH1dTransferDataResult(ret)
            } else {
                // エラー
                BluetoothPlugin.shared.callBackError(commandType.rawValue,
                                                     errorCode: .timeOut)
            }

        case .h1dTransferDataDone:
            // H1dプログラム完了確認
            if let ret = DataManager.shared.dataAnalysisH1dUpdateDone(data) {
                BluetoothPlugin.shared.callBackH1dTransferDataDone(ret.state,
                                                                   verMajor: ret.verMajor,
                                                                   verMinor: ret.verMinor,
                                                                   verRevision: ret.verRevision,
                                                                   verBuild: ret.verBuild)
            } else {
                // エラー
                BluetoothPlugin.shared.callBackError(commandType.rawValue,
                                                     errorCode: .timeOut)
            }
        case .alarmSetting:
            // アラーム設定
            if let ret = DataManager.shared.dataAnalysisBool(data) {
                BluetoothPlugin.shared.callBackBool(commandType.rawValue, isOK: ret)
            } else {
                // エラー
                BluetoothPlugin.shared.callBackError(commandType.rawValue,
                                                     errorCode: .timeOut)
            }

        case .alarmNotification:
            // アラーム通知
            if let ret = DataManager.shared.dataAnalysisAlarm(data) {
                if AlarmManager.shared.isAlarmPlaying() &&
                    !ret.isAlarm {
                    // アラームがなっているかつ発生の場合通知しない
                    return
                }
                BluetoothPlugin.shared.callBackAlarm(ret.state,
                                                     isOn: ret.isAlarm)
                if ret.isAlarm {
                    // 解除の場合
                    AlarmManager.shared.stopAlarm()
                    return
                }
                // 通知表示
                if let type = AlarmManager.Alarm(rawValue: ret.state) {
                    AlarmManager.shared.showNotification(type: type)
                }
            } else {
                // エラー
                BluetoothPlugin.shared.callBackError(commandType.rawValue,
                                                     errorCode: .timeOut)
            }
        case .getDevaiceStatus:
            // デバイス状況取得
            var isOK : ObjCBool = false
            if let ret = DataManager.shared.dataAnalysisDeviceStatus(data, isOK: &isOK) {
                if isOK.boolValue {
                    BluetoothPlugin.shared.callBackDeviceStatus(ret.address,
                                                                dataCount: ret.dataCount,
                                                                year: ret.year,
                                                                month: ret.month,
                                                                day: ret.day,
                                                                hour: ret.hour,
                                                                minute: ret.minute,
                                                                second: ret.second,
                                                                weekDay: ret.weekDay)
                } else {
                    BluetoothPlugin.shared.callBackBool(commandType.rawValue,
                                                        isOK: false)
                }
            } else {
                BluetoothPlugin.shared.callBackError(commandType.rawValue,
                                                     errorCode: .timeOut)
            }
        default:
            break
        }
    }

    private func getSleepDataDate(dataCount: Int) -> DateObj {
        guard let date = recordingTimeDate else {
            return DateObj(date: "", time: "", weekday: "")
        }
        let currentDate = date.addingTimeInterval(TimeInterval(30*dataCount))
        let calendar = Calendar(identifier: .gregorian)
        let components = calendar.dateComponents([.year, .month, .day, .hour, .minute, .second, .weekday],
                                                 from: currentDate)
        guard let year = components.year,
            let month = components.month,
            let day = components.day,
            let hour = components.hour,
            let minute = components.minute,
            let second = components.second,
            let weekday = components.weekday else {
                return DateObj(date: "", time: "", weekday: "")
        }

        return DateObj(date: String(format: "%d/%d/%d", year, month, day),
                       time: String(format: "%02d:%02d:%02d", hour, minute, second),
                       weekday: String(format: "%d", weekday-1))
    }

    //MARK:- timeout
    /**
     * タイムアウト時間取得
     *
     * @param DataManager.CommandType commandType コマンド種類
     */
    private func getTimeout(_ commandType: DataManager.CommandType) -> Int {
        var ret: Int = 0
        if commandType != .h1dTransferDataDone ||
            commandType != .getEnd ||
            commandType != .h1dTransferData{
            ret = TIME_OUT_1SEC
        }

        return ret
    }

    /**
     * タイムアウト開始
     *
     * @param DataManager.CommandType commandType コマンド種類
     */
    private func startTimeoutTimer(_ commandType: DataManager.CommandType) {
        currentCommand = commandType
        // キャンセル
        self.timeoutCancel()
        // 更新完了確認は呼び出し側でタイムアウト処理をする
        if commandType == .h1dTransferDataDone ||
            commandType == .getEnd ||
            commandType == .h1dTransferData {
            return
        }

        self.workItem = DispatchWorkItem {
            self.timeout(commandType)
        }

        let time = self.getTimeout(commandType)
        let dispatchWallTime = DispatchWallTime.now() + .seconds(time)
        // タイムアウト処理登録
        DispatchQueue.global(qos: .default).asyncAfter(wallDeadline: dispatchWallTime,
                                                       execute: self.workItem)
    }

    /**
     * タイムアウト処理
     */
    private func timeout(_ commandType: DataManager.CommandType) {
        // エラー通知
        var cId = commandType.rawValue
        if cId <= DataManager.CommandType.stateChangeG1dUpdate.rawValue {
            // 1系のエラーの場合
            cId = DataManager.CommandType.stateChange.rawValue
        }
        BluetoothPlugin.shared.callBackError(cId,
                                             errorCode: .timeOut)
    }

    /**
     * タイムアウトキャンセル処理
     */
    private func timeoutCancel() {
        // キャンセルされていなかったらキャンセルしておく
        if self.workItem != nil && !self.workItem.isCancelled {
            self.workItem.cancel()
        }
    }

    /**
     * スキャンのタイムアウト
     */
    private func scanTimeout() {
        // 検索停止
        self.scanStop()
        // エラー通知
        BluetoothPlugin.shared.callBackError(0,
                                             errorCode: .timeOut)
    }
}

extension BluetoothSwift: CBCentralManagerDelegate {
    /**
     * 復元機能をオプトインしたアプリケーションの場合最初に呼ばれる
     */
    func centralManager(_ central: CBCentralManager, willRestoreState dict: [String : Any]) {
        // セントラルマネージャが接続し、または接続を試みていたペリフェラルリスト
        if let peripherals = dict[CBCentralManagerRestoredStatePeripheralsKey] {
            // 復元するペリフェアルのリストを保持しておく
            self.restorationPeripheralArray = peripherals as! [CBPeripheral]
        }
    }

    /**
     * 端末の状態が変更されたときに呼ばれる
     */
    func centralManagerDidUpdateState(_ central: CBCentralManager) {
        switch central.state {
        case .poweredOn:
            // ON
            self.isBluetoothPoweredOn = true
            BluetoothPlugin.shared.callBackBluetoothState(BleState.poweredOn.rawValue)
        case .poweredOff, .unauthorized:
            // OFF
            self.isBluetoothPoweredOn = false
            BluetoothPlugin.shared.callBackBluetoothState(BleState.poweredOff.rawValue)
        case .unsupported:
            // サポートなし
            self.isBluetoothSupported = false
            BluetoothPlugin.shared.callBackBluetoothState(BleState.unsupported.rawValue)
        default:
            print("centralManagerDidUpdateState ohter")
        }
    }

    /**
     * ペリフェラルを発見すると呼ばれる
     */
    func centralManager(_ central: CBCentralManager,
                        didDiscover peripheral: CBPeripheral,
                        advertisementData: [String : Any],
                        rssi RSSI: NSNumber) {
        // TODO: 必要？
//        // ヒットしたのでタイムアウトキャンセル
//        self.scanStop()

        if !searchPeripheralArray.contains(peripheral) {
            // 検知済みでない場合
            self.searchPeripheralArray.append(peripheral)
        }

        guard let name = advertisementData[CBAdvertisementDataLocalNameKey] as? String else {
            return
        }
        if name != DEVICE_NAME {
            // デバイス名が違う場合はリターン
            return
        }
        var deviceName = ""
        if let name = advertisementData[CBAdvertisementDataLocalNameKey] {
            deviceName = name as! String
        } else {
            if let name = peripheral.name {
                deviceName = name
            }
        }

        if let index = searchPeripheralArray.index(of: peripheral) {
            // 検索結果を送信
            BluetoothPlugin.shared.callBackDeviceInfo(deviceName,
                                                      address: "",
                                                      index: index)
        }
    }

    /**
     * 周辺機器との接続が正常に作成されたときに呼び出されます。
     */
    func centralManager(_ central: CBCentralManager,
                        didConnect peripheral: CBPeripheral) {
        self.peripheral = peripheral
        // 接続した機器のデリゲート登録
        self.peripheral.delegate = self
        willConnectPeripheral = nil// 接続中のやつ破棄
        // サービス検索 バックグラウンド実行は引数指定しないといけない
        let uuidArray = [CBUUID(string: self.serviceUUID)]
        peripheral.discoverServices(uuidArray)
    }

    /**
     * 中央マネージャが周辺機器との接続を作成できないときに呼び出されます。
     */
    func centralManager(_ central: CBCentralManager,
                        didFailToConnect peripheral: CBPeripheral,
                        error: Error?) {
        //エラー通知(コマンドIDないので0)
        BluetoothPlugin.shared.callBackError(0, errorCode: .connection)
        willConnectPeripheral = nil// 接続中のやつ破棄
    }

    /**
     * ペリフェラルとの既存の接続が切断されたときに呼び出されます。
     */
    func centralManager(_ central: CBCentralManager,
                        didDisconnectPeripheral peripheral: CBPeripheral,
                        error: Error?) {
        if self.isdeInitialize {
            // 終了処理の時は通知は出さない フラグを戻しておく
            self.isdeInitialize = false
            return
        }
        //エラー通知(コマンドIDないので0)
        // 接続が切れたことを通知する
        BluetoothPlugin.shared.callBackError(0, errorCode: .disconnected)

        self.peripheral = nil
        self.writeCharacteristic = nil
        self.indicationCharacteristic = nil
    }
}

extension BluetoothSwift: CBPeripheralDelegate {
    /**
     * 1つ以上のサービスが変更されたときに呼び出されます
     */
    func peripheral(_ peripheral: CBPeripheral,
                    didModifyServices invalidatedServices: [CBService]) {
        // ファームウェアアップデート変更時にサービスの設定をし直す。
        // 機器側で設定がしてあれば動く、駄目ならBluetoothをONOFFで更新される
        // サービス検索 バックグラウンド実行は引数指定しないといけない
        let uuidArray = [CBUUID(string: self.serviceUUID)]
        peripheral.discoverServices(uuidArray)
    }

    /**
     * 周辺機器の利用可能なサービスを検出すると呼び出されます。
     */
    func peripheral(_ peripheral: CBPeripheral,
                    didDiscoverServices error: Error?) {
        // サービスがない時は何もしない
        guard let _services = peripheral.services else {
            return
        }
        // 自分のサービスを持っているか
        let uuid = CBUUID(string: self.serviceUUID)
        guard let service = _services.first(where: { $0.uuid == uuid }) else {
            return
        }

        // サービスの特性を検出
        var uuidArray :Array<CBUUID>
        if serviceUUID == VUART_HDL_SVC_UUID {
            uuidArray = [CBUUID(string: VUART_HDL_INDICATION_CHAR_UUID),
                         CBUUID(string: VUART_HDL_WRITE_UUID)]
        } else {
            uuidArray = [CBUUID(string: self.VUARE_FWUP_CONTROL_CHAR_UUID),
                         CBUUID(string: self.VUARE_FWUP_DATA_CHAR_UUID)]
        }

        peripheral.discoverCharacteristics(uuidArray, for: service)
    }

    /**
     * 指定したサービスの特性を検出すると呼び出されます。
     */
    func peripheral(_ peripheral: CBPeripheral,
                    didDiscoverCharacteristicsFor service: CBService,
                    error: Error?) {
        guard let _characteristics = service.characteristics else {
            return
        }

        // UUIDを作成
        var indicationUuid :CBUUID
        var writeUuid:CBUUID
        if serviceUUID == VUART_HDL_SVC_UUID {
            indicationUuid = CBUUID(string: VUART_HDL_INDICATION_CHAR_UUID)
            writeUuid = CBUUID(string: VUART_HDL_WRITE_UUID)
        } else {
            indicationUuid = CBUUID(string: VUARE_FWUP_CONTROL_CHAR_UUID)
            writeUuid = CBUUID(string: VUARE_FWUP_DATA_CHAR_UUID)
        }

        for characteristic in _characteristics {
            print(characteristic)
            //ファームウェアアップデートのの場合
            if(service.uuid == CBUUID(string:VUART_FWUP_SVC_UUID)){
                //ファームウェアアップデート用に保持しておく
                if(characteristic.uuid == indicationUuid){
                    print("fwup indicate indicate uuid char")
                    self.writeCharacteristic = characteristic
                    self.FWUPControlCharacteristic = characteristic
                }
                if(characteristic.uuid == writeUuid){
                    print("fwup write uuid char")
                    self.FWUPDataCharacteristic = characteristic
                }
                //ファームウェアアップデートの場合はnotify要求はしない
                self.timeoutCancel()
                BluetoothPlugin.shared.callBackConnectionPeripheral(self.peripheral.identifier.description,
                                                                    deviceName: DEVICE_NAME,
                                                                    address: "")
                continue
            }
            if characteristic.uuid == indicationUuid {
                self.indicationCharacteristic = characteristic
                // 特性の値が変化したときに通知するよう申し込む
                peripheral.setNotifyValue(true, for: characteristic)
            } else if characteristic.uuid == writeUuid {
                // 書き込み保持
                self.writeCharacteristic = characteristic
            }
        }
    }

    /**
     * 指定された特性記述子の値を取得するときに呼び出されます。
     */
    func peripheral(_ peripheral: CBPeripheral,
                    didUpdateValueFor characteristic: CBCharacteristic,
                    error: Error?) {
        // タイムアウト処理はキャンセルしておく
        self.timeoutCancel()

        if error != nil {
            //TODO:コマンドID必要？エラーコードはタイムアウトに統一
//            BluetoothPlugin.shared.callBackError(0, errorCode: .dataAnalysis)
            BluetoothPlugin.shared.callBackError(0, errorCode: .timeOut)
            return
        }

        // データチェック
        guard let _data = characteristic.value else {
            //TODO:コマンドID必要？エラーコードはタイムアウトに統一
//            BluetoothPlugin.shared.callBackError(0, errorCode: .dataAnalysis)
            BluetoothPlugin.shared.callBackError(0, errorCode: .timeOut)
            return
        }
        // データ解析
        self.resultCommandData(data: _data)
    }

    /**
     * ペリフェラルが、指定された特性値の通知を開始または停止する要求を受け取ると呼び出されます。
     */
    func peripheral(_ peripheral: CBPeripheral,
                    didUpdateNotificationStateFor characteristic: CBCharacteristic,
                    error: Error?) {
        if error != nil {
            return
        }
        
        if characteristic.isNotifying {
            // コマンド送受信の準備完了したのでコールバックで知らせる
            timeoutCancel()
            BluetoothPlugin.shared.callBackConnectionPeripheral(self.peripheral.identifier.description,
                                                                deviceName: DEVICE_NAME,
                                                                address: "")
        }
    }

    /**
     * 特性値にデータを書き込むときに呼び出されます。
     */
    func peripheral(_ peripheral: CBPeripheral,
                    didWriteValueFor characteristic: CBCharacteristic,
                    error: Error?) {
        var commandId:Int = 0
        if let data = characteristic.value {
            let commandType = DataManager.shared.checkCommandType(data)
            commandId = commandType.rawValue
        }

        if error != nil {
            // タイムアウト処理はキャンセルしておく
            self.timeoutCancel()
            // エラーを返す
            BluetoothPlugin.shared.callBackWrite(commandId, isOK: false)
        } else {
            // タイムアウト設定
            startTimeoutTimer(currentCommand)
            // 書き込み成功
            BluetoothPlugin.shared.callBackWrite(commandId, isOK: true)
        }
    }
}
