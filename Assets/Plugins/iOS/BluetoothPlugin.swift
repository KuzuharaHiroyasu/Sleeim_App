//
//  BluetoothPlugin.swift
//  KaiminApp
//

import Foundation
import ReplayKit

@objc public class BluetoothPlugin: NSObject {

    @objc static let shared = BluetoothPlugin()

    var isWriteOK = false

    // エラー定数
    enum ErrorType: Int {
        // エラーなし
        case none = 0
        // Bluetoothサポートされてない
        case bluetoothSupported = -1
        // BluetoothがOFF
        case bluetoothOff = -2
        // 機器と接続できない
        case connection = -3
        // 機器と接続が切れた
        case disconnected = -4
        // タイムアウトエラー
        case timeOut = -5
        // Bluetooth送信エラー
        case sendBluetooth = -6
        // データ解析エラー
        case dataAnalysis = -7
        // 識別子(UUID)のキャッシュがない
        case peripheralCaseRemoved = -8
        // ディスク領域の不足のために書き込みエラー。
        case fileWriteOutOfSpace = -9
        // ファイル書き込みエラー
        case fileWrite = -10
    }

    /**
     * イニシャライズ
     * アプリケーション起動時に呼ばれる想定
     */
    @objc public func initialize() {
        BluetoothSwift.shared.initialize()
    }

    /**
     * ファイナライズ
     * アプリケーション終了時に呼ばれる想定
     */
    @objc public func deInitialize() {
        BluetoothSwift.shared.deInitialize()
    }

    /**
     * 設定画面を開く（BLE用）
     */
    @objc public func openBLESetting() {
        BluetoothSwift.shared.openBLESetting()
    }

    /**
     * 設定画面を開く（ローカル通知用）
     */
    @objc public func openLocalNotificationSetting() {
        AlarmManager.shared.openLocalNotificationSetting()
    }

    /**
     * ローカル通知の設定を返す
     */
    @objc public func checkLocalNotificationSetting() {
        AlarmManager.shared.checkLocalNotificationSetting()
    }

    /**
     * 接続中断or切断処理
     */
    @objc public func disConnectPeripheral() {
        BluetoothSwift.shared.disConnectPeripheral()
    }

    /**
     * Bluetoothをサポートしているかチェック
     *
     * @return Bool true:サポート/false:非サポート
     */
    @objc public func checkBluetoothSupported() -> Bool {
        return BluetoothSwift.shared.isBluetoothSupported
    }

    /**
     * BluetoothがONになっているか
     *
     * @return Bool true:ON/false:OFF
     */
    @objc public func checkBluetoothPoweredOn() -> Bool {
        return BluetoothSwift.shared.isBluetoothPoweredOn
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
    @objc public func setCsvHeaderInfo(_ deviceId: String, nickname: String, sex: String,
                                       birthday: String, tall: String,
                                       weight: String, sleepStartTime: String,
                                       sleepEndTime: String, g1dVersion: String) {
        BluetoothSwift.shared.setCsvHeaderInfo(deviceId,
                                               nickname: nickname,
                                               sex: sex,
                                               birthday: birthday,
                                               tall: tall,
                                               weight: weight,
                                               sleepStartTime: sleepStartTime,
                                               sleepEndTime: sleepEndTime,
                                               g1dVersion: g1dVersion)
    }

    /**
     * スキャン開始
     */
    @objc public func scanStart() {
        BluetoothSwift.shared.scanStart()
    }

    /**
     * スキャン停止
     */
    @objc public func scanStop() {
        BluetoothSwift.shared.scanStop()
    }

    /**
     * ペリフェアル接続
     */
    @objc public func connectionPeripheral(_ identifierUuid: String) {
        BluetoothSwift.shared.connectionPeripheral(identifierUuid)
    }

    /**
     * ペリフェアル再接続
     *
     * @param UUID identifier ペリフェアル識別子
     */
    @objc public func reConnectionPeripheral(_ uuid: String) {
        if let peripheralUuid = UUID(uuidString: uuid) {
            BluetoothSwift.shared.reConnectionPeripheral(peripheralUuid)
        }
    }

    /**
     * コマンド送信
     *
     * @param Int commandId コマンド
     */
    @objc public func sendCommand(_ commandId: Int) {
        BluetoothSwift.shared.sendCommand(commandId)
    }

    /**
     * コマンド送信
     *
     * @param Int commandId コマンド
     */
    @objc public func sendBleCommand(_ commandId: Int, value: Data) {
        BluetoothSwift.shared.sendBleCommand(commandId, command: value)
    }

    /**
     * サービスUUIDを汎用通信用に変更する
     */
    @objc public func changeServiceUUIDToNormal() {
        BluetoothSwift.shared.changeServiceUUIDToNormal();
    }

    /**
     * サービスUUIDをファーム更新用に変更する
     */
    @objc public func changeServiceUUIDToFirmwareUpdate() {
        BluetoothSwift.shared.changeServiceUUIDToFirmwareUpdate();
    }

    /**
     * ファーム更新制御コマンド通信用キャラクタリスティックに変更する
     */
    @objc public func changeCharacteristicUUIDToFirmwareUpdateControl() {
        BluetoothSwift.shared.changeCharacteristicUUIDToFirmwareUpdateControl();
    }

    /**
     * ファーム更新データ通信用キャラクタリスティックに変更する
     */
    @objc public func changeCharacteristicUUIDToFirmwareUpdateData() {
        BluetoothSwift.shared.changeCharacteristicUUIDToFirmwareUpdateData();
    }

    /**
     * End送信
     *
     * @param Bool isOK データ取得OKかどうか
     */
    @objc public func sendGetEnd(_ isOK: Bool) {
        BluetoothSwift.shared.sendGetEnd(isOK)
    }

    /**
     * 日時設定
     * フォーマット yyyy/mm/dd hh:mm:ss
     *
     * @param String date 日付
     * @param Int weekDay 曜日
     */
    @objc public func sendDateSetting(_ date: String) {
        BluetoothSwift.shared.sendDateSetting(date)
    }

    /**
     * H1Dプログラムデータ転送
     *
     * @param [UInt8] data 転送データ
     */
    @objc public func sendH1DDate(_ data: NSData) {
        BluetoothSwift.shared.sendH1DDate(Data(referencing: data))
    }

    /**
     * H1Dプログラム転送結果
     *
     * @param Int sum アドレス
     */
    @objc public func sendH1DCheckSum(_ sum: NSData) {
        BluetoothSwift.shared.sendH1DCheckSUM(Data(referencing: sum))
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
    @objc public func sendAlarmSetting(_ alarm: Int, snoreAlarm: Int, snoreSensitivity: Int,
                                       apneaAlarm: Int, alarmDelay: Int, bodyMoveStop: Int, alramTime: Int) {
        BluetoothSwift.shared.sendAlarmSetting(alarm,
                                               snoreAlarm: snoreAlarm,
                                               snoreSensitivity: snoreSensitivity,
                                               apneaAlarm: apneaAlarm,
                                               alarmDelay: alarmDelay,
                                               bodyMoveStop: bodyMoveStop,
                                               alramTime: alramTime)
    }

    /**
     * アラーム停止
     */
    @objc public func stopAlarm() {
        AlarmManager.shared.stopAlarm()
    }

    /**
     * エラーコールバック
     *
     * @param Int commandId コマンド
     * @param Int errorCode エラーコード
     */
    func callBackError(_ commandId: Int, errorCode: ErrorType) {

        _callBackError(Int32(commandId), Int32(errorCode.rawValue))
    }

    /**
     * ペリフェアル接続完了コールバック
     *
     * @param String identifier 識別子
     * @param String deviceName デバイス名
     * @param String address アドレス
     */
    func callBackConnectionPeripheral(_ uuid: String, deviceName: String, address: String) {

        _callBackConnectionPeripheral(uuid, deviceName, address)
    }

    /**
     * 検索にヒットしたペリフェアル情報コールバック
     *
     * @param String deviceName デバイス名
     * @param String address アドレス
     */
    func callBackDeviceInfo(_ deviceName: String, address: String, identifierUuid: String) {

        _callBackDeviceInfo(deviceName, address, identifierUuid)
    }

    /**
     * 成否コマンド完了コールバック
     *
     * @param Int commandId コマンド
     * @param Bool isOK 成否
     */
    func callBackBool(_ commandId: Int, isOK: Bool) {

        _callBackBool(Int32(commandId), isOK)
    }

    /**
     * バージョン情報コールバック
     *
     * @param Int g1dAppVerMajor G1DアプリVer情報(メジャー)
     * @param Int g1dAppVerMinor G1DアプリVer情報(マイナー)
     * @param Int g1dAppVerRevision G1DアプリVer情報(リビジョン)
     * @param Int g1dAppVerBuild G1DアプリVer情報(ビルド)
     */
    func callBackGetVersion(_ g1dAppVerMajor: Int, g1dAppVerMinor: Int,
                            g1dAppVerRevision: Int, g1dAppVerBuild: Int) {

        _callBackGetVersion(Int32(g1dAppVerMajor), Int32(g1dAppVerMinor),
                            Int32(g1dAppVerRevision), Int32(g1dAppVerBuild))
    }

    /**
     * 電池情報コールバック
     *
     * @param Int batteryLevel 電池残量
     */
    func callBackBattery(_ batteryLevel: Int) {

        _callBackBattery(Int32(batteryLevel))
    }

    /**
     * データ取得コールバック
     *
     * @param Int count 現在のデータカウント
     * @param Bool isNext
     * @param Bool isEnd true 終了/flse 終了じゃない
     * @param String! tempPath テンプパス
     * @param String! fileName ファイル名
     */
    func callBackGetData(_ count: Int, isNext: Bool, isEnd: Bool, tempPath: String!, fileName: String!) {

        _callBackGetData(Int32(count), isNext, isEnd, tempPath, fileName)
    }

    /**
     * プログラム転送結果コールバック
     *
     * @param Int state 結果
     */
    func callBackH1dTransferDataResult(_ state: Int) {

        _callBackH1dTransferDataResult(Int32(state))
    }

    /**
     * プログラム更新完了コールバック
     *
     * @param Int state 結果状態
     * @param Int verMajor ver情報(メジャー)
     * @param Int verMinor ver情報(マイナー)
     * @param Int verRevision ver情報(リビジョン)
     * @param Int verBuild ver情報(ビルド)
     */
    func callBackH1dTransferDataDone(_ state: Int, verMajor: Int, verMinor: Int, verRevision: Int, verBuild: Int) {

        _callBackH1dTransferDataDone(Int32(state), Int32(verMajor),
                                     Int32(verMinor), Int32(verRevision),
                                     Int32(verBuild))
    }

    /**
     * アラーム通知コールバック
     *
     * @param Int type 種別
     * @param Bool isOn true 発生/false 解除
     */
    func callBackAlarm(_ type: Int, isOn: Bool) {

        _callBackAlarm(Int32(type), isOn)
    }

    /**
     * Bluetooth送信完了通知コールバック
     */
    func callBackWrite(_ commandId: Int, isOK: Bool) {
        _callBackWrite(Int32(commandId), isOK)
    }

    /**
     * BluetoothState変更通知コールバック
     */
    func callBackBluetoothState(_ state: Int) {
        _callBackBluetoothState(Int32(state))
    }

    /**
     * 通知の許可状態取得コールバック
     */
    func callBackNotificationStatus(_ status: Int) {
        _callBackNotificationStatus(Int32(status))
    }

    /// デバイス状況取得コールバック
    ///
    /// - Parameters:
    ///   - address: デバイスアドレス
    ///   - dataCount: 測定データ保持数
    ///   - year: 年
    ///   - month: 月
    ///   - day: 日
    ///   - hour: 時
    ///   - minute: 分
    ///   - second: 秒
    ///   - weekDay: 曜日
    func callBackDeviceStatus(_ address: String,
                              dataCount: Int,
                              year: Int,
                              month: Int,
                              day: Int,
                              hour: Int,
                              minute: Int,
                              second: Int,
                              weekDay: Int) {
        _callBackDeviceStatus(address,
                              Int32(dataCount),
                              Int32(year),
                              Int32(month),
                              Int32(day),
                              Int32(hour),
                              Int32(minute),
                              Int32(second),
                              Int32(weekDay))
    }
}
