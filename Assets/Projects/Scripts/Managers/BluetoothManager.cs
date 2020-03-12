using MiniJSON;
using System;

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Runtime.InteropServices;
using AOT;

namespace Kaimin.Managers
{
    /// <summary>
    /// Bluetoothマネージャー
    /// </summary>
    public class BluetoothManager : SingletonMonoBehaviour<BluetoothManager>
    {
        private const string _packageNameMainActivity = "jp.co.onea.sleeim.unityandroidplugin.main.MainActivity";
        private const string _packageNameMyApplication = "jp.co.onea.sleeim.unityandroidplugin.main.MyApplication";
        private const string _packageNameBluetoothActivity = "jp.co.onea.sleeim.unityandroidplugin.main.BluetoothActivity";
        private readonly string _gameObjectName = "Kaimin.Managers.BluetoothManager (singleton)";

        private const string _JKEY1 = "KEY1";
        private const string _JKEY2 = "KEY2";
        private const string _JKEY3 = "KEY3";
        private const string _JKEY4 = "KEY4";
        private const string _JKEY5 = "KEY5";
        private const string _JKEY6 = "KEY6";
        private const string _JKEY7 = "KEY7";
        private const string _JKEY8 = "KEY8";
        private const string _JKEY9 = "KEY9";
        private const string _JKEY10 = "KEY10";
        private const string _JKEY11 = "KEY11";
        private const string _JKEY12 = "KEY12";
        private const string _JKEY13 = "KEY13";

        private const int _COMMAND1 = 1;//状態変更
        private const int _COMMAND2 = 2;//待機状態
        private const int _COMMAND3 = 3;//GET状態

        /// <summary>
        /// プログラム更新状態G1D
        /// </summary>
        public const int CommandUpdateG1d = 5;

        private const int _COMMAND6 = 6;//日時設定
        private const int _COMMAND7 = 7;//情報取得(電池残量取得)
        private const int _COMMAND8 = 8;//バージョン取得
        private const int _COMMAND9 = 9;//
        private const int _COMMAND10 = 10;//
        private const int _COMMAND11 = 11;//
        private const int _COMMAND12 = 12;//
        private const int _COMMAND13 = 13;//プログラムデータ転送
        private const int _COMMAND14 = 14;//プログラム転送結果
        private const int _COMMAND15 = 15;//プログラム更新完了確認
        private const int _COMMAND16 = 16;//アラーム設定
        private const int _COMMAND17 = 17;//
        private const int _COMMAND18 = 18;//デバイス状況取得
        private const int _COMMAND19 = 19;//データ取得完了通知

        /// <summary>
        /// G1Dファーム更新データコマンドコード
        ///
        /// このコマンドコードはBLEコマンド仕様として定められているものではない
        /// </summary>
        public readonly byte CommandG1dUpdateData = 0xD0;

        /// <summary>
        /// G1Dファーム更新制御コマンドコード
        ///
        /// このコマンドコードはBLEコマンド仕様として定められているものではない
        /// </summary>
        public readonly byte CommandG1dUpdateControl = 0xD1;

        private const int CODE0 = 0; //なし
        private const int CODE1 = -1; //
        private const int CODE2 = -2; //
        private const int CODE3 = -3; //機器と接続できない
        private const int CODE4 = -4; //機器と接続がきれた
        private const int CODE5 = -5; //タイムアウトエラー
        private const int CODE6 = -6; //
        private const int CODE7 = -7; //データ解析エラー
        private const int CODE8 = -8; //
        private const int CODE9 = -9; //ディスク容量不足でCSV書き込みエラー
        private const int CODE10 = -10; //CSV書き込みエラー

        private const int OK = 0; //0：OK(成功)
        private const int NG = 1; //1：NG(失敗)

        List<List<string>> _deviceList = new List<List<string>>();

        // CallBackErrorのコールバック
        private Action<string> _onCallBackError = null;
        // CallBackCommandWriteのコールバック
        private Action<Boolean> _onCallBackCommandWrite = null;
        // CallBackCommandResponseのコールバック
        private Action<string> _onCallBackCommandResponse = null;

        // CallbackConnectのコールバック
        private Action<string> _onCallbackConnect = null;
        // CallbackScanBleDeviceのコールバック
        private Action<string> _onCallbackScanBleDevice = null;

        // SendCommandId(COMMAND3 / COMMAND7 / COMMAND8 / COMMAND15 / COMMAND18)のコールバック
        private Action<string> _onCallBackCommandVariable = null;
        // CallbackH1dTransferDataResultのコールバック
        private Action<Boolean> _onCallbackH1dTransferDataResult = null;

        private static int _bluetoothState = -1;	//iOS専用。CallBackBluetoothStateDelegateで受け取った値を保持する
        private static int _notificationState = 0;	//iOS専用。callBackNotificationStatusDelegateで受け取った値を保持する

        /// <summary>
        /// iOS専用
        /// BLEに対応しているかどうか
        /// </summary>
        public static bool IsBLESupport {
            get {
                return _bluetoothState != 0;
            }
        }

        /// <summary>
        /// iOS専用
        /// BluetoothがONになっているかどうか
        /// </summary>
        public static bool IsBluetoothON {
            get {
                return _bluetoothState == 2;
            }
        }

        /// <summary>
        /// iOS専用
        /// ローカル通知が許可されているかどうか
        /// </summary>
        /// <value><c>true</c> if is allow notification; otherwise, <c>false</c>.</value>
        public static bool IsAllowNotification {
            get {
                return _notificationState == 2;
            }
        }

        [DllImport ("__Internal")]
        private static extern void _checkLocalNotificationSetting ();

        /// <summary>
        /// iOS専用
        /// 通知許可があるかどうか確認する前に必要な準備をします。
        /// </summary>
        public void NotificationCheckPrepare (Action onComplete) {
            StartCoroutine (NotificationCheckPrepareFlow (onComplete));
        }

        IEnumerator NotificationCheckPrepareFlow (Action onComplete) {
            _notificationState = -1;	//初期化
            _checkLocalNotificationSetting ();
            yield return new WaitUntil (() => _notificationState != -1);
            onComplete ();
        }

        /// <summary>
        /// システムの初期化
        /// </summary>
        public void Initialize(Action onComplete)
        {
            StartCoroutine(BluetoothInitialize(onComplete));
        }

        /// <summary>
        /// Bluetoothが有効か
        /// </summary>
        /// <returns>Bluetoothが有効か</returns>
        public bool IsBluetoothEnabled()
        {
            NativeManager.Instance.Initialize();
            return NativeManager.Instance.BluetoothValidCheck();
        }

        /// <summary>
        /// Bluetoothシステムの初期化
        /// </summary>
        private IEnumerator BluetoothInitialize(Action onComplete)
        {
            yield return BleInitialize();
            onComplete ();
        }

        /// <summary>
        /// デバイスを接続する
        /// </summary>
        /// <param name="deviceName">デバイス名</param>
        /// <param name="deviceAdress">デバイスアドレス</param>
        /// <param name="callback">デバイス接続に成功したかどうかを返す</param>
        /// <returns></returns>
        public IEnumerator ConnectDeviceCoroutine(string deviceName, string deviceAdress, Action<bool> callback) {
            bool? isConnected = null;
            string receivedData = "";		//デバイス接続で成功・失敗時に受け取るデータ（JSONにパースして使用）
            string uuid = "";		//ペアリング中のデバイスのUUID(iOSでのみ必要)
#if UNITY_IOS
            uuid = UserDataManager.Device.GetPareringDeviceUUID();
#endif
            Connect(
                deviceAdress,
                (string data) => {
                    //エラー時
                    receivedData = data;
                    isConnected = false;
                },
                (string data) => {
                    //接続完了時
                    receivedData = data;
                    isConnected = true;
                },
                uuid);
            yield return new WaitUntil(() => isConnected != null);
            if ((bool)isConnected) {
                var json = Json.Deserialize(receivedData) as Dictionary<string, object>;
                var name = (string)json["KEY1"];
                var adress = (string)json["KEY2"];
                UserDataManager.Device.SavePareringDeviceName(name);
                UserDataManager.Device.SavePareringBLEAdress(adress);
            } else {
                var json = Json.Deserialize(receivedData) as Dictionary<string, object>;
                int error1 = Convert.ToInt32(json["KEY1"]);
                int error2 = Convert.ToInt32(json["KEY2"]);
#if UNITY_IOS
                // iOSの_reconnectionPeripheralコマンドでのみ返ってくる、
                // これ以降接続できる見込みがないときのエラー
                const int ConnectionError = -8;
                if (error2 == ConnectionError) {
                    Debug.Log("Connect_OcuurSeriousError");
                    UserDataManager.State.SaveDeviceConnectState(false);
                    DeviceStateManager.Instance.OnDeviceDisConnect();
                    UserDataManager.State.ClearDeviceParering();
                    DeviceStateManager.Instance.OnDevicePrearingDisConnect();
                    callback(false);
                    yield break;
                }
#endif
                const int AnyError = -3;    //何らかの原因で接続できなかった場合(タイムアウト含む)
                const int DisconnectionError = -4;  //接続が切れた場合(GATTサーバには接続できたが、サービスまで全て接続できないと接続完了にはならない。)
                if (error2 == AnyError) {
                    Debug.Log ("OccurAnyError");
                }
                else if (error2 == DisconnectionError) {
                    Debug.Log ("DisConnectedError");
                }
            }
            callback((bool)isConnected);
        }

        /// <summary>
        /// Bluetooth初期化処理
        /// </summary>
        private IEnumerator BleInitialize()
        {
            bool result = BleInitialize(
                _gameObjectName,
                "CallBackError",
                "CallBackCommandResponse",
                "CallbackConnect",
                "CallbackScanBleDevice",
                "CallBackBattery",
                "CallBackGetVersion",
                "CallBackGetDeviceCond",
                "CallBackGetData",
                "CallbackH1dTransferDataResult",
                "CallbackH1dTransferDataDone",
                "CallBackAlarm",
                "CallBackCommandWrite");
            #if UNITY_IOS
            //iOSの場合は、Initializeの後に状態を確認しに行く必要がある。
            //応答が返ってくれば完了とする
            yield return new WaitUntil (() => _bluetoothState != -1);
            Debug.Log ("BleInitializeComplete_State:" + _bluetoothState);
            #endif
            yield break;
        }

        delegate void callback_delegate1 (int comandId, int errorType);

        delegate void callback_delegate2 (string uuid, string deviceName, string address);

        delegate void callback_delegate3 (string deviceName, string address, string identifierUuid);

        delegate void callback_delegate4 (int comandId, bool isOK);

        delegate void callback_delegate5 (int g1dAppVerMajor, int g1dAppVerMinor, int g1dAppVerRevision, int g1dAppVerBuild);

        delegate void callback_delegate6 (int batteryLevel);

        delegate void callback_delegate7 (int count, bool isNext, bool isEnd, string tempPath, string fileName);

        delegate void callback_delegate8 (int state);

        delegate void callback_delegate9 (int state, int verMajor, int verMinor, int verRevision, int verBuild);

        delegate void callback_delegate10 (int type, bool isOn);

        delegate void callback_delegate11 (int comandId, bool isOK);

        delegate void callback_delegate12 (int state);

        delegate void callback_delegate13 (string address, int dataCount, int year, int month, int day, int hour, int minute, int second, int weekDay);

        delegate void callback_delegate14 (int status);

        [DllImport ("__Internal")]
        private static extern void _initialize (callback_delegate1 callBackError,
            callback_delegate2 callBackConnectionPeripheral,
            callback_delegate3 callBackDeviceInfo,
            callback_delegate4 callBackBool,
            callback_delegate5 callBackGetVersion,
            callback_delegate6 callBackBattery,
            callback_delegate7 callBackGetData,
            callback_delegate8 callBackH1dTransferDataResult,
            callback_delegate9 callBackH1dTransferDataDone,
            callback_delegate10 callBackAlarm,
            callback_delegate11 callBackWrite,
            callback_delegate12 callBackBluetoothState,
            callback_delegate13 callBackDeviceStatus,
            callback_delegate14 callBackNotificationStatus);

        // コールバック関数を、MonoPInvokeCallbackを付けてstaticで定義
        [MonoPInvokeCallback (typeof(callback_delegate1))]
        private static void callBackError (int comandId, int errorType)
        {
            string jsonText = "{" +
                "\"KEY1\":\"" + comandId.ToString () + "\"," +
                "\"KEY2\":\"" + errorType.ToString () + "\"}";
            BluetoothManager.Instance.CallBackError (jsonText);
            Debug.Log ("callBackError : " + "comandId : " + comandId + "errorType : " + errorType);
        }

        [MonoPInvokeCallback (typeof(callback_delegate2))]
        private static void callBackConnectionPeripheral (string uuid, string deviceName, string address)
        {
            string jsonText = "{" +
                "\"KEY1\":\"" + deviceName + "\"," +
                "\"KEY2\":\"" + address + "\"}";
            //接続が完了したデバイスのUUIDを保存する
            UserDataManager.Device.SavePareringDeviceUUID (uuid);

            BluetoothManager.Instance.CallbackConnect (jsonText);
            Debug.Log ("callBackConnectionPeripheral : " + "uuid : " + uuid + "deviceName : " + deviceName + "address : " + address);
        }

        [MonoPInvokeCallback (typeof(callback_delegate3))]
        private static void callBackDeviceInfo (string deviceName, string address, string identifierUuid)
        {
			Debug.Log ("callBackDeviceInfo : " + "deviceName : " + deviceName + "address : " + address + "identifierUuid : " + identifierUuid);
            string jsonText = "{" +
                "\"KEY1\":\"" + deviceName + "\"," +
                "\"KEY2\":\"" + address + "\"" +
				"\"KEY3\":\"" + identifierUuid + "\"}";		//iOSでのみ使用する機器と接続する際に必要となる識別番号
            BluetoothManager.Instance.CallbackScanBleDevice (jsonText);
        }

        [MonoPInvokeCallback (typeof(callback_delegate4))]
        private static void callBackBool (int comandId, bool isOK)
        {
            string jsonText = "{" +
                "\"KEY1\":\"" + comandId.ToString () + "\"," +
                "\"KEY2\":\"" + (isOK ? "true" : "false") + "\"}";
            BluetoothManager.Instance.CallBackCommandResponse (jsonText);
            Debug.Log ("callBackBool : " + "comandId : " + comandId + "isOK : " + isOK);
        }

        [MonoPInvokeCallback (typeof(callback_delegate5))]
        private static void callBackGetVersion (int g1dAppVerMajor, int g1dAppVerMinor, int g1dAppVerRevision, int g1dAppVerBuild)
        {
            Debug.Log("callBackGetVersion");
            string jsonText = "{" +
                "\"KEY1\":\"" + g1dAppVerMajor.ToString () + "\"," +
                "\"KEY2\":\"" + g1dAppVerMinor.ToString () + "\"," +
                "\"KEY3\":\"" + g1dAppVerRevision.ToString () + "\"," +
                "\"KEY4\":\"" + g1dAppVerBuild.ToString () + "\"}";
            BluetoothManager.Instance.CallBackGetVersion (jsonText);
            Debug.Log (
                "callBackGetVersion : "
                + "g1dAppVerMajor : " + g1dAppVerMajor
                + "g1dAppVerMinor : " + g1dAppVerMinor
                + "g1dAppVerRevision : " + g1dAppVerRevision
                + "g1dAppVerBuild : " + g1dAppVerBuild);
        }

        [MonoPInvokeCallback (typeof(callback_delegate6))]
        private static void callBackBattery (int batteryLevel)
        {
            string jsonText = "{" +
                "\"KEY1\":\"" + batteryLevel.ToString () + "\"}";
            BluetoothManager.Instance.CallBackBattery (jsonText);
            Debug.Log ("callBackBattery : " + "batteryLevel : " + batteryLevel);
        }

        [MonoPInvokeCallback (typeof(callback_delegate7))]
        private static void callBackGetData (int count, bool isNext, bool isEnd, string tempPath, string fileName)
        {
            string jsonText = "{" +
                "\"KEY1\":\"" + count.ToString () + "\"," +
                "\"KEY2\":\"" + (isNext ? "true" : "false") + "\"," +
                "\"KEY3\":\"" + (isEnd ? "true" : "false") + "\"," +
                "\"KEY4\":\"" + tempPath + "\"," +
                "\"KEY5\":\"" + fileName + "\"}";
            BluetoothManager.Instance.CallBackGetData (jsonText);
            Debug.Log ("callBackGetData : " + "count : " + count + "isNext : " + isNext + "isEnd : " + isEnd + "tempPath : " + tempPath + "fileName : " + fileName);
        }

        [MonoPInvokeCallback (typeof(callback_delegate8))]
        private static void callBackH1dTransferDataResult (int state)
        {
            string jsonText = "{" +
                "\"KEY1\":\"" + state.ToString () + "\"}";
            BluetoothManager.Instance.CallbackH1dTransferDataResult (jsonText);
            Debug.Log ("callBackH1dTransferDataResult : " + "state : " + state);
        }

        [MonoPInvokeCallback (typeof(callback_delegate9))]
        private static void callBackH1dTransferDataDone (int state, int verMajor, int verMinor, int verRevision, int verBuild)
        {
            string jsonText = "{" +
                "\"KEY1\":\"" + state.ToString () + "\"," +
                "\"KEY2\":\"" + verMajor.ToString () + "\"," +
                "\"KEY3\":\"" + verMinor.ToString () + "\"," +
                "\"KEY4\":\"" + verRevision.ToString () + "\"," +
                "\"KEY5\":\"" + verBuild.ToString () + "\"}";
            BluetoothManager.Instance.CallbackH1dTransferDataDone (jsonText);
            Debug.Log ("callBackH1dTransferDataDone : " + "state : " + state + "verMajor : " + verMajor + "verMinor : " + verMinor + "verRevision : " + verRevision + "verBuild : " + verBuild);
        }

        [MonoPInvokeCallback (typeof(callback_delegate10))]
        private static void callBackAlarm (int type, bool isOn)
        {
            string jsonText = "{" +
                "\"KEY1\":\"" + type.ToString () + "\"," +
                "\"KEY2\":\"" + (isOn ? "true" : "false") + "\"}";
            BluetoothManager.Instance.CallBackAlarm (jsonText);
            Debug.Log ("callBackAlarm : " + "type : " + type + "isOn : " + isOn);
        }

        [MonoPInvokeCallback (typeof(callback_delegate11))]
        private static void callBackWrite (int commandId, bool isOK)
        {
            string jsonText = "{" +
                "\"KEY1\":\"" + commandId.ToString () + "\"," +
                "\"KEY2\":\"" + isOK.ToString() + "\"}";
            BluetoothManager.Instance.CallBackCommandWrite (jsonText);
            Debug.Log ("callBackWrite : " + "commandId : " + commandId + "isOK : " + isOK);
        }

        [MonoPInvokeCallback (typeof(callback_delegate12))]
        private static void callBackBluetoothState (int state)
        {
            //0:BLEをサポートしていない 1:BluetoothOFF 2:BluetoothON
            _bluetoothState = state;
            Debug.Log ("callBackBluetoothState : " + "state : " + state);
        }

        [MonoPInvokeCallback (typeof(callback_delegate13))]
        private static void callBackDeviceStatus (string address,
            int dataCount,
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            int weekDay)
        {
            string jsonText = "{" +
                "\"KEY1\":\"" + address + "\"," +
                "\"KEY2\":\"" + dataCount.ToString () + "\"," +
                "\"KEY3\":\"" + year.ToString () + "\"," +
                "\"KEY4\":\"" + month.ToString () + "\"," +
                "\"KEY5\":\"" + weekDay.ToString () + "\"," +
                "\"KEY6\":\"" + day.ToString () + "\"," +
                "\"KEY7\":\"" + hour.ToString () + "\"," +
                "\"KEY8\":\"" + minute.ToString () + "\"," +
                "\"KEY9\":\"" + second.ToString () + "\"}";
            BluetoothManager.Instance.CallBackGetDeviceCond (jsonText);
            Debug.Log ("callBackDeviceStatus : " + "address : " + address + "dataCount : " + dataCount + "year : " + year + "month : " + month + "day : " + day + "hour : " + hour + "minute : " + minute + "second : " + second + "weekDay : " + weekDay);
        }

        [MonoPInvokeCallback (typeof(callback_delegate14))]
        private static void callBackNotificationStatus (int status)
        {
            //0:未設定 1:通知拒否 2:通知許可
            _notificationState = status;
            Debug.Log ("callBackNotificationStatus : " + "status : " + status);
        }

        /// <summary>
        /// Bluetooth初期化処理
        /// <param name="gameObjectName">コールバックメソッドが属しているオブジェクト</param>
        /// <param name="callbackName1">コールバック1</param> //エラー発生
        /// <param name="callbackName2">コールバック2</param> //デバイス側の応答結果
        /// <param name="callbackName3">コールバック3</param> //デバイス接続結果
        /// <param name="callbackName4">コールバック4</param> //スキャン結果
        /// <param name="callbackName5">コールバック5</param> //情報取得（電池残量）のOK応答時の結果
        /// <param name="callbackName6">コールバック6</param> //バージョン取得のOK応答時の結果
        /// <param name="callbackName7">コールバック7</param> //デバイス状況取得のOK応答時の結果
        /// <param name="callbackName8">コールバック8</param> //データ取得(GET)時の進捗状況の結果
        /// <param name="callbackName9">コールバック9</param> //プログラム転送結果
        /// <param name="callbackName10">コールバック10</param> //プログラム更新完了確認結果
        /// <param name="callbackName11">コールバック11</param> //アラーム通知受信時
        /// <param name="callbackName12">コールバック12</param> //コマンド送信書き込み結果
        /// </summary>
        public Boolean BleInitialize(
            string gameObjectName,
            string callbackName1,
            string callbackName2,
            string callbackName3,
            string callbackName4,
            string callbackName5,
            string callbackName6,
            string callbackName7,
            string callbackName8,
            string callbackName9,
            string callbackName10,
            string callbackName11,
            string callbackName12)
        {
            Boolean ret=false;

#if UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ret = ajo.CallStatic<Boolean>(
                    "Initialize",
                    gameObjectName,
                    callbackName1,
                    callbackName2,
                    callbackName3,
                    callbackName4,
                    callbackName5,
                    callbackName6,
                    callbackName7,
                    callbackName8,
                    callbackName9,
                    callbackName10,
                    callbackName11,
                    callbackName12);
            }
#elif UNITY_IOS
            _initialize (callBackError, callBackConnectionPeripheral, callBackDeviceInfo, callBackBool, callBackGetVersion, callBackBattery, callBackGetData, callBackH1dTransferDataResult, callBackH1dTransferDataDone, callBackAlarm, callBackWrite, callBackBluetoothState, callBackDeviceStatus, callBackNotificationStatus);
            ret = true;//initialize後のdelegateで取得
#endif
            return ret;
        }

        [DllImport ("__Internal")]
        private static extern void _deInitialize ();

        /// <summary>
        /// Bluetooth終了処理
        /// </summary>
        public void BleDeinitialize()
        {
#if UNITY_ANDROID
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("Deinitialize");
            }
#elif UNITY_IOS
            _deInitialize ();
#endif
        }

        [DllImport ("__Internal")]
        private static extern void _scanStart ();

        /// <summary>
        /// 検索開始
        /// </summary>
        public void ScanBleDevice(Action<string> onCallBackError, Action<string> onCallbackScanBleDevice)
        {
#if UNITY_ANDROID
            _onCallBackError = onCallBackError;
            _onCallbackScanBleDevice = onCallbackScanBleDevice;

            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("ScanBleDevice");
            }
#elif UNITY_IOS
            _onCallBackError = onCallBackError;
            _onCallbackScanBleDevice = onCallbackScanBleDevice;
            _scanStart ();
#endif
        }

        [DllImport ("__Internal")]
        private static extern void _scanStop ();

        /// <summary>
        /// 検索停止
        /// </summary>
        public void StopScanning()
        {
#if UNITY_ANDROID
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("StopScanning");
            }
#elif UNITY_IOS
            _scanStop ();
#endif
        }

        [DllImport ("__Internal")]
		private static extern void _connectionPeripheral (string identifierUuid);

        [DllImport ("__Internal")]
        private static extern void _reConnectionPeripheral (string uuid);

        /// <summary>
        /// ペリフェラル接続
        /// <param name="deviceAddress">接続先のデバイスアドレス</param>
		/// <param name="identifierUuid">iOSでのみ使用するデバイス識別番号</param>
        /// </summary>
		public void Connect(String deviceAddress, Action<string> onCallBackError, Action<string> onCallbackConnect, string uuid = "", string identifierUuid = "")
        {
#if UNITY_ANDROID
            _onCallBackError = onCallBackError;
            _onCallbackConnect = onCallbackConnect;

            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("Connect", deviceAddress);
            }
#elif UNITY_IOS
			if (identifierUuid != "") {
            //新しくデバイスと接続する場合(デバイス接続画面でスキャンしたデバイスと接続する場合)
            _onCallBackError = onCallBackError;
            _onCallbackConnect = onCallbackConnect;
			_connectionPeripheral (identifierUuid);
            } else {
            //ペアリング中のデバイスと接続する場合
            _onCallBackError = onCallBackError;
            _onCallbackConnect = onCallbackConnect;
            _reConnectionPeripheral (uuid);
            }
#endif
        }

        [DllImport ("__Internal")]
        private static extern void _disConnectPeripheral ();

        /// <summary>
        /// ペリフェラル切断
        /// </summary>
        public void Disconnect()
        {
#if UNITY_ANDROID
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("Disconnect");
            }
#elif UNITY_IOS
            _disConnectPeripheral ();
#endif
        }

        [DllImport ("__Internal")]
        private static extern void _setCsvHeaderInfo(string deviceId, string nickname, string sex,
            string birthday, string tall, string weight,
            string sleepStartTime, string sleepEndTime, string g1dVersion);

        /// <summary>
        /// データ取得用（ヘッダ設定）
        /// GET実施前に設定を行う
        /// <param name="deviceId">デバイスID</param>
        /// <param name="nickName">ニックネーム</param>
        /// <param name="sex">男性 / 女性</param>
        /// <param name="birthday">生年月日　フォーマット yyyy/m/d</param>
        /// <param name="tall">身長</param>
        /// <param name="weight">体重</param>
        /// <param name="sleepStartTime">睡眠開始時間　フォーマット hh:mm</param>
        /// <param name="sleepEndTime">睡眠終了時間　フォーマット hh:mm</param>
        /// <param name="g1dVersion">G1Dファームウェアバージョン  000.000.000.000</param>
        /// </summary>
        public void CsvHeaderSet(String deviceId, String nickName, String sex, String birthday, String tall, String weight, String sleepStartTime, String sleepEndTime, String g1dVersion)
        {
#if UNITY_ANDROID
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("CsvHeaderSet", deviceId, nickName, sex, birthday, tall, weight, sleepStartTime, sleepEndTime, g1dVersion);
            }
#elif UNITY_IOS
            _setCsvHeaderInfo(deviceId, nickName, sex, birthday, tall, weight, sleepStartTime, sleepEndTime, g1dVersion);
#endif
        }

        [DllImport ("__Internal")]
        private static extern void _sendDateSetting (string date);

        /// <summary>
        /// コマンド：日時設定
        /// <param name="date">デバイスに設定する日時(yyyy/MM/dd hh:mm:ss)</param>
        /// </summary>
        public void SendCommandDate(String date, Action<string> onCallBackError, Action<Boolean> onCallBackCommandWrite, Action<string> onCallBackCommandResponse)
        {
#if UNITY_ANDROID
            _onCallBackError = onCallBackError;
            _onCallBackCommandWrite = onCallBackCommandWrite;
            _onCallBackCommandResponse = onCallBackCommandResponse;

            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("SendCommandDate", date);
            }
#elif UNITY_IOS
            _onCallBackError = onCallBackError;
            _onCallBackCommandWrite = onCallBackCommandWrite;
            _onCallBackCommandResponse = onCallBackCommandResponse;
            _sendDateSetting (date);
#endif
        }

        /// <summary>
        /// ファーム更新用サービスUUIDに変更する
        /// </summary>
        [DllImport ("__Internal")]
        private static extern void _changeServiceUUIDToFirmwareUpdate();
        public void ChangeServiceUUIDToFirmwareUpdate()
        {
#if UNITY_ANDROID
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("changeServiceUUIDToFirmwareUpdate");
            }
#elif UNITY_IOS
            _changeServiceUUIDToFirmwareUpdate();
#endif
        }

        /// <summary>
        /// ファーム更新制御コマンド送信用キャラクタリスティックUUIDに変更する
        /// </summary>
        [DllImport ("__Internal")]
        private static extern void _changeCharacteristicUUIDToFirmwareUpdateControl();
        public void ChangeCharacteristicUUIDToFirmwareUpdateControl()
        {
#if UNITY_ANDROID
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("changeCharacteristicUUIDToFirmwareUpdateControl");
            }
#elif UNITY_IOS
            _changeCharacteristicUUIDToFirmwareUpdateControl();
#endif
        }

        /// <summary>
        /// ファーム更新データ送信用キャラクタリスティックUUIDに変更する
        /// </summary>
        [DllImport ("__Internal")]
        private static extern void _changeCharacteristicUUIDToFirmwareUpdateData();
        public void ChangeCharacteristicUUIDToFirmwareUpdateData()
        {
#if UNITY_ANDROID
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("changeCharacteristicUUIDToFirmwareUpdateData");
            }
#elif UNITY_IOS
            _changeCharacteristicUUIDToFirmwareUpdateData();
#endif
        }

        /// <summary>
        /// 汎用通信用サービスUUIDに変更する
        /// </summary>
        [DllImport ("__Internal")]
        private static extern void _changeServiceUUIDToNormal();
        public void ChangeServiceUUIDToNormal()
        {
#if UNITY_ANDROID
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("changeServiceUUIDToNormal");
            }
#elif UNITY_IOS
            _changeServiceUUIDToNormal();
#endif
        }

        [DllImport ("__Internal")]
        private static extern void _sendAlarmSetting (int alarm, int snoreAlarm, int snoreSensitivity, int apneaAlarm,
            int alarmDelay, int bodyMoveStop, int alramTime);

        /// <summary>
        /// コマンド：アラーム設定
        /// <param name="alarmSet">アラーム有効/無効</param>
        /// <param name="snoreArm">いびきアラーム</param>
        /// <param name="snoreSens">いびきアラーム感度</param>
        /// <param name="hypopnea">低呼吸アラーム</param>
        /// <param name="delay">アラーム遅延</param>
        /// <param name="motion">体動停止</param>
        /// <param name="rumbling">鳴動時間</param>
        ///
        /// </summary>
        public void SendCommandAlarm(int alarmSet, int snoreArm, int snoreSens, int hypopnea, int delay, int motion, int rumbling, Action<string> onCallBackError, Action<Boolean> onCallBackCommandWrite, Action<string> onCallBackCommandResponse)
        {
#if UNITY_ANDROID
            _onCallBackError = onCallBackError;
            _onCallBackCommandWrite = onCallBackCommandWrite;
            _onCallBackCommandResponse = onCallBackCommandResponse;

            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("SendCommandAlarm", alarmSet, snoreArm, snoreSens, hypopnea, delay, motion, rumbling);
            }
#elif UNITY_IOS
            _onCallBackError = onCallBackError;
            _onCallBackCommandWrite = onCallBackCommandWrite;
            _onCallBackCommandResponse = onCallBackCommandResponse;
            _sendAlarmSetting (alarmSet, snoreArm, snoreSens, hypopnea, delay, motion, rumbling);
#endif
        }

        /// <summary>
        /// コマンドを送信する (デバイス設定変更、バイブレーション確認、バイブレーション停止など)
        /// </summary>
        /// <param name="commandCode">CommandCode(デバイス設定変更)、CommandCodeVibrationConfirm(バイブレーション確認)、CommandCodeVibrationStop(バイブレーション停止)</param>
        /// <param name="deviceSetting">デバイス設定</param>
        /// <param name="onCallBackError"></param>
        /// <param name="onCallBackCommandWrite"></param>
        /// <param name="onCallBackCommandResponse"></param>
        public void SendCommandToDevice(
            byte commandCode,
            DeviceSetting deviceSetting,
            Action<string> onCallBackError,
            Action<Boolean> onCallBackCommandWrite,
            Action<string> onCallBackCommandResponse)
        {
            _onCallBackError = onCallBackError;
            _onCallBackCommandWrite = onCallBackCommandWrite;
            _onCallBackCommandResponse = onCallBackCommandResponse;

            if (commandCode == DeviceSetting.CommandCode)
            {
                //デバイス設定変更コマンドを送信する
                SendBleCommand(DeviceSetting.CommandCode, deviceSetting.Command);
            } 
            else if (commandCode == DeviceSetting.CommandCodeVibrationConfirm)
            {
                //バイブレーション確認コマンドを送信する
                SendBleCommand(DeviceSetting.CommandCodeVibrationConfirm, deviceSetting.CommandVibrationConfirm);
            }
            else if (commandCode == DeviceSetting.CommandCodeVibrationStop)
            {
                //バイブレーション停止コマンドを送信する
                SendBleCommand(DeviceSetting.CommandCodeVibrationStop, deviceSetting.CommandVibrationStop);
            }
        }

        /// <summary>
        /// ファーム更新データを送信する
        /// </summary>
        /// <param name="data">ファーム更新データ</param>
        /// <param name="onCallBackCommandWrite"></param>
        public void SendFirmwareUpdateData(
            byte[] data,
            Action<string> onCallBackError,
            Action<Boolean> onCallBackCommandWrite)
        {
            _onCallBackError = onCallBackError;
            _onCallBackCommandWrite = onCallBackCommandWrite;
            SendBleCommand(CommandG1dUpdateData, data);
        }

        /// <summary>
        /// ファーム更新制御コマンドを送信する
        /// </summary>
        /// <param name="controlCommand">ファーム更新制御コマンド</param>
        /// <param name="onCallBackError">受信タイムアウト時処理</param>
        /// <param name="onCallBackCommandWrite">書き込み完了時処理</param>
        public void SendFirmwareUpdateControlCommand(
            byte[] value,
            Action<string> onCallBackError,
            Action<Boolean> onCallBackCommandWrite)
        {
            _onCallBackError = onCallBackError;
            _onCallBackCommandWrite = onCallBackCommandWrite;
            SendBleCommand(CommandG1dUpdateControl, value);
        }

        [DllImport ("__Internal")]
        private static extern void _sendCommand (int commandId);

        /// <summary>
        /// コマンド送信
        /// <param name="id">コマンドID</param>
        /// _COMMAND2 / _COMMAND3 / _COMMAND4 / _COMMAND5 / _COMMAND7 / _COMMAND8 / _COMMAND15 / _COMMAND18 に対応
        /// _COMMAND15(プログラム更新完了確認)のタイムアウトはUnity側で実施すること
        /// </summary>
        public void SendCommandId(int id, Action<string> onCallBackError, Action<Boolean> onCallBackCommandWrite, Action<string> onCallBackCommandResponse, Action<string> onCallBackCommandVariable)
        {
#if UNITY_ANDROID
            _onCallBackError = onCallBackError; //COMMAND15ではタイムアウトは発生しない
            _onCallBackCommandWrite = onCallBackCommandWrite;
            _onCallBackCommandResponse = onCallBackCommandResponse;
            _onCallBackCommandVariable = onCallBackCommandVariable; //COMMAND7 / COMMAND8 / COMMAND15 / COMMAND18用

            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("SendCommandId", id);
            }
#elif UNITY_IOS
            _onCallBackError = onCallBackError; //COMMAND15ではタイムアウトは発生しない
            _onCallBackCommandWrite = onCallBackCommandWrite;
            _onCallBackCommandResponse = onCallBackCommandResponse;
            _onCallBackCommandVariable = onCallBackCommandVariable; //COMMAND7 / COMMAND8 / COMMAND15 / COMMAND18用
            _sendCommand (id);
#endif
        }

        [DllImport ("__Internal")]
        private static extern void _sendGetEnd (bool isOK);

        /// <summary>
        /// データ取得完了通知
        /// <param name="result">データ取得完了結果</param>
        /// TRUE:OK, FALSE:NG
        /// </summary>
        public void SendCommandGetFinish(Boolean result, Action<string> onCallBackError, Action<Boolean> onCallBackCommandWrite)
        {
#if UNITY_ANDROID
            _onCallBackError = onCallBackError;
            _onCallBackCommandWrite = onCallBackCommandWrite;

            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("SendCommandGetFinish", result);
            }
#elif UNITY_IOS
            _onCallBackError = onCallBackError;
            _onCallBackCommandWrite = onCallBackCommandWrite;
            _sendGetEnd (result);
#endif
        }

        [DllImport ("__Internal")]
        private static extern void _sendH1DDate (byte[] data, int length);

        /// <summary>
        /// G1Dプログラム転送データ
        /// <param name="code">転送コード</param>
        /// <param name="len">転送長（20byte固定)</param>
        /// </summary>
        public void SendCommandG1dCode(byte[] code, int len, Action<string> onCallBackError, Action<Boolean> onCallBackCommandWrite)
        {
#if UNITY_ANDROID
            _onCallBackError = onCallBackError;
            _onCallBackCommandWrite = onCallBackCommandWrite;

            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("SendCommandH1dCode", code, len);
            }
#elif UNITY_IOS
            _onCallBackError = onCallBackError;
            _onCallBackCommandWrite = onCallBackCommandWrite;
            _sendH1DDate (code, len);
#endif
        }

        [DllImport ("__Internal")]
        private static extern void _sendH1DCheckSum (byte[] data, int length);

        /// <summary>
        /// H1Dプログラム転送結果確認
        /// <param name="chksum">チェックサム</param>
        /// </summary>
        public void SendCommandG1dSum(byte[] chksum, Action<string> onCallBackError, Action<Boolean> onCallBackCommandWrite, Action<Boolean> onCallbackH1dTransferDataResult)
        {
#if UNITY_ANDROID
            _onCallBackError = onCallBackError;
            _onCallBackCommandWrite = onCallBackCommandWrite;
            _onCallbackH1dTransferDataResult = onCallbackH1dTransferDataResult;

            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("SendCommandH1dSum", chksum);
            }
#elif UNITY_IOS
            _onCallBackError = onCallBackError;
            _onCallBackCommandWrite = onCallBackCommandWrite;
            _onCallbackH1dTransferDataResult = onCallbackH1dTransferDataResult;
            _sendH1DCheckSum (chksum, chksum.Length);
#endif
        }


        /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [DllImport ("__Internal")]
        private static extern void _sendBleCommand(int commandType, byte[] command, int length);

        /// <summary>
        /// BLEコマンドを送信する
        ///
        /// NOTE: プラグイン側の実装を確認して使用する
        /// </summary>
        /// <param name="commandType">コマンド種別</param>
        /// <param name="command">コマンド</param>
        private void SendBleCommand(int commandType, byte[] command)
        {
#if UNITY_ANDROID
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ajo.CallStatic("sendBleCommand", commandType, command);
            }
#elif UNITY_IOS
            _sendBleCommand(commandType, command, command.Length);
#endif
        }

        /// <summary>
        /// エラー発生時に呼ばれる
        /// </summary>
        private void CallBackError(string result)
        {
            Dictionary<string, object> _itemDic = Json.Deserialize(result) as Dictionary<string, object>;

            int _commandId;
            int _errorType;

            try
            {
                _commandId = Convert.ToInt32(_itemDic[_JKEY1]);
                _errorType = Convert.ToInt32(_itemDic[_JKEY2]);

                Debug.Log(_JKEY1 + _commandId.ToString()); //エラーになったコマンドID
                Debug.Log(_JKEY2 + _errorType.ToString()); //エラータイプ

                switch (_commandId)
                {
                    default:
                        break;
                }

                switch (_errorType)
                {
                    case CODE4: //機器との接続がきれた
                        //CODE4は、機器と接続していればどこにいてもコマンド実行中でなくても、接続が切れたらCODE4が返却される
                        //デバイスとの接続が切れた事を記録する
                        UserDataManager.State.SaveDeviceConnectState(false);
                        //デバイスとの接続が切れた事をどのシーンからでも受け取れるようにする
                        DeviceStateManager.Instance.OnDeviceDisConnect();
                        break;
                    default:
                        break;
                }

                if (_onCallBackError != null)
                {
                    Action<string> onFinished = _onCallBackError;
                    _onCallBackError = null;
                    onFinished(result);
                }

            }
            catch (KeyNotFoundException e)
            {
                Debug.Log("CallBackError/KeyNotFoundException:" + e.Message);
            }
            catch (ArgumentNullException e)
            {
                Debug.Log("CallBackError/ArgumentNullException:" + e.Message);
            }
            catch (FormatException e)
            {
                Debug.Log("CallBackError/FormatException:" + e.Message);
            }
        }

        /// <summary>
        /// コマンド送信後、デバイスからの応答を受け取った時呼ばれる
        /// </summary>
        private void CallBackCommandResponse(string result)
        {
            Dictionary<string, object> _itemDic = Json.Deserialize(result) as Dictionary<string, object>;

            int _commandId;
            bool _result;

            try
            {
                _commandId = Convert.ToInt32(_itemDic[_JKEY1]);
                _result = Convert.ToBoolean(_itemDic[_JKEY2]);

                Debug.Log(_JKEY1 + _commandId.ToString()); //コマンドID
                Debug.Log(_JKEY2 + _result.ToString()); //応答結果（OK：TRUE、NG:FALSE）

                if (_onCallBackCommandResponse != null)
                {
                    Action<string> onFinished = _onCallBackCommandResponse;
                    _onCallBackCommandResponse = null;
                    onFinished(result);
                }
            }
            catch (KeyNotFoundException e)
            {
                Debug.Log("CallBackCommandResponse/KeyNotFoundException:" + e.Message);
            }
            catch (ArgumentNullException e)
            {
                Debug.Log("CallBackCommandResponse/ArgumentNullException:" + e.Message);
            }
            catch (FormatException e)
            {
                Debug.Log("CallBackCommandResponse/FormatException:" + e.Message);
            }
        }

        /// <summary>
        /// コマンド送信の書き込み結果を受けた時呼ばれる
        /// </summary>
        private void CallBackCommandWrite(string result)
        {
            Dictionary<string, object> _itemDic = Json.Deserialize(result) as Dictionary<string, object>;

            int _commandId;
            bool _result;

            try
            {
                _commandId = Convert.ToInt32(_itemDic[_JKEY1]);
                _result = Convert.ToBoolean(_itemDic[_JKEY2]);

                Debug.Log(_JKEY1 + _commandId.ToString()); //コマンドID
                Debug.Log(_JKEY2 + _result.ToString()); //送信結果（OK：TRUE、NG:FALSE）

                if (_result == false)
                {
                }
                switch (_commandId)
                {
                    default:
                        break;
                }

                if (_onCallBackError != null)
                {
                    Action<Boolean> onFinished = _onCallBackCommandWrite;
                    _onCallBackCommandWrite = null;
                    onFinished(_result); //string ではなくTRUE/FALSEのみ返却　必要であればstringにすること
                }
            }
            catch (KeyNotFoundException e)
            {
                Debug.Log("CallBackError/KeyNotFoundException:" + e.Message);
            }
            catch (ArgumentNullException e)
            {
                Debug.Log("CallBackError/ArgumentNullException:" + e.Message);
            }
            catch (FormatException e)
            {
                Debug.Log("CallBackError/FormatException:" + e.Message);
            }
        }

        /// <summary>
        /// デバイスに接続できた時呼ばれる
        /// </summary>
        private void CallbackConnect(string result)
        {
            //返ってきたjsonデータをエンコード
            // JSON(string) -> Dictionary

            string _deviceName = "";
            string _deviceAddress = "";

            Dictionary<string, object> _itemDic = Json.Deserialize(result) as Dictionary<string, object>;

            try
            {
                Debug.Log(_JKEY1 + (string)_itemDic[_JKEY1]); //デバイス名
                Debug.Log(_JKEY2 + (string)_itemDic[_JKEY2]); //デバイスアドレス

                _deviceName = (string)_itemDic[_JKEY1];
                _deviceAddress = (string)_itemDic[_JKEY2];

                if (_onCallbackConnect != null)
                {
                    //接続した事を記録する
                    UserDataManager.State.SaveDeviceConnectState (true);
                    Action<string> onFinished = _onCallbackConnect;
                    _onCallbackConnect = null;
                    onFinished(result);
                }


            }
            catch (KeyNotFoundException e)
            {
                Debug.Log("CallbackConnect/KeyNotFoundException:" + e.Message);
            }
            catch (FormatException e)
            {
                Debug.Log("CallbackConnect/FormatException:" + e.Message);
            }
            catch(Exception e)
            {
                Debug.Log("CallbackConnect: " + e.Message);
            }
        }

        /// <summary>
        /// デバイススキャンで取得出来た時呼ばれる
        /// </summary>
        /// 注意：一度検出したものも再度検出されます
        private void CallbackScanBleDevice(string result)
        {
            //返ってきたjsonデータをエンコード
            // JSON(string) -> Dictionary

            string _deviceName = "";
            string _deviceAddress = "";

            Dictionary<string, object> _itemDic = Json.Deserialize(result) as Dictionary<string, object>;

            try
            {
                Debug.Log(_JKEY1 + (string)_itemDic[_JKEY1]); //デバイス名
                Debug.Log(_JKEY2 + (string)_itemDic[_JKEY2]); //デバイスアドレス
                #if UNITY_IOS
                Debug.Log (_JKEY3 + (string)_itemDic[_JKEY3]);	//iOSのみで使用するデバイス識別番号
                #endif

                if ((string)_itemDic[_JKEY1] != null) //デバイス名はネィティブ側でフィルタを実施している（iOSはUUIDでフィルタして返却）
                {
                    _deviceName = (string)_itemDic[_JKEY1];
                    _deviceAddress = (string)_itemDic[_JKEY2];

                    _deviceList.Add(new List<string>(new string[] { _deviceName, _deviceAddress })); //どこかでクリアと重複は排除が必要

                    if (_onCallbackScanBleDevice != null)
                    {
                        Action<string> onFinished = _onCallbackScanBleDevice;
                        _onCallbackScanBleDevice = null;
                        onFinished(result);
                    }
                }
            }
            catch (KeyNotFoundException e)
            {
                Debug.Log("CallbackScanBleDevice/KeyNotFoundException:" + e.Message);
            }
            catch (FormatException e)
            {
                Debug.Log("CallbackScanBleDevice/FormatException:" + e.Message);
            }
            catch(Exception e)
            {
                Debug.Log("CallbackScanBleDevice: " + e.Message);
            }

        }

        /// <summary>
        /// コマンド送信後、情報取得（電池残量）を受け取った時（OK応答時）呼ばれる
        /// </summary>
        private void CallBackBattery(string result)
        {
            //返ってきたjsonデータをエンコード
            // JSON(string) -> Dictionary

            int _res;

            Dictionary<string, object> _itemDic = Json.Deserialize(result) as Dictionary<string, object>;

            try
            {
                _res = Convert.ToInt32(_itemDic[_JKEY1]);

                if (_onCallBackCommandVariable != null)
                {
                    Action<string> onFinished = _onCallBackCommandVariable;
                    _onCallBackCommandVariable = null;
                    onFinished(result);
                }
            }
            catch (KeyNotFoundException e)
            {
                Debug.Log("CallBackBattery/KeyNotFoundException:" + e.Message);
            }
            catch (FormatException e)
            {
                Debug.Log("CallBackBattery/FormatException:" + e.Message);
            }
        }

        /// <summary>
        /// コマンド送信後、バージョン取得を受け取った時（OK応答時）呼ばれる
        /// </summary>
        private void CallBackGetVersion(string result)
        {
            //返ってきたjsonデータをエンコード
            // JSON(string) -> Dictionary

            Dictionary<string, object> _itemDic = Json.Deserialize(result) as Dictionary<string, object>;

            try
            {
                if (_onCallBackCommandVariable != null)
                {
                    Action<string> onFinished = _onCallBackCommandVariable;
                    _onCallBackCommandVariable = null;
                    onFinished(result);
                }
            }
            catch (KeyNotFoundException e)
            {
                Debug.Log("CallBackGetVersion/KeyNotFoundException:" + e.Message);
            }
            catch (FormatException e)
            {
                Debug.Log("CallBackGetVersion/FormatException:" + e.Message);
            }
        }

        /// <summary>
        /// コマンド送信後、デバイス状況取得を受け取った時（OK応答時）呼ばれる
        /// </summary>
        private void CallBackGetDeviceCond(string result)
        {
            //返ってきたjsonデータをエンコード
            // JSON(string) -> Dictionary

            int _dataCount;
            string _deviceAddress = "";

            Dictionary<string, object> _itemDic = Json.Deserialize(result) as Dictionary<string, object>;

            try
            {
                _deviceAddress = (string)_itemDic[_JKEY1];
                _dataCount = Convert.ToInt32(_itemDic[_JKEY2]); //機器側に保存されているデータ取得件数を把握する

                string _syear = "20" + Convert.ToInt32(_itemDic[_JKEY3]).ToString("00");
                int _year = int.Parse(_syear);
                int _month = Convert.ToInt32(_itemDic[_JKEY4]);
                //int _week = Convert.ToInt32(_itemDic[_JKEY5]);
                int _day = Convert.ToInt32(_itemDic[_JKEY6]);
                int _hour = Convert.ToInt32(_itemDic[_JKEY7]);
                int _min = Convert.ToInt32(_itemDic[_JKEY8]);
                int _sec = Convert.ToInt32(_itemDic[_JKEY9]);

                //エラーがでてる？
                //ArgumentOutOfRangeException:Argument is out of range.
                string dateString = _syear + "/" + _month.ToString("00") + "/" + _day.ToString("00") + " " + _hour.ToString("00") + ":" + _min.ToString("00") + ":" + _sec.ToString("00");

                DateTime dt;
                if (DateTime.TryParse (dateString, out dt)) {	//異常な値でないかチェック
                } else {
                    //エラーとして扱っても良い？とりあえずDateTime.MinValueで対応
                    Debug.LogError ("DateTime Parse Error!!");
                    dateString = DateTime.MinValue.ToString ("yyyy/MM/dd HH:mm:ss");
                }

                if (_onCallBackCommandVariable != null)
                {
                    Action<string> onFinished = _onCallBackCommandVariable;
                    _onCallBackCommandVariable = null;
                    onFinished(result);
                }
            }
            catch (KeyNotFoundException e)
            {
                Debug.Log("CallBackGetDeviceCond/KeyNotFoundException:" + e.Message);
            }
            catch (FormatException e)
            {
                Debug.Log("CallBackGetDeviceCond/FormatException:" + e.Message);
            }
        }

        /// <summary>
        /// データ取得(GET)のNEXT,END応答時に呼ばれる（進捗管理用）
        /// </summary>
        private void CallBackGetData(string result)
        {
            //返ってきたjsonデータをエンコード
            // JSON(string) -> Dictionary

            Dictionary<string, object> _itemDic = Json.Deserialize(result) as Dictionary<string, object>;

            try
            {
                int _count = Convert.ToInt32(_itemDic[_JKEY1]); //取得完了状況（例：1件取得完了したら1で返される）
                Boolean _next = Convert.ToBoolean(_itemDic[_JKEY2]); //NEXTがTRUEなら次のデータがある
                Boolean _end = Convert.ToBoolean(_itemDic[_JKEY3]); //ENDがTRUEなら次のデータはない。（Unity側でアプリ処理を行ってから、5秒以内にデータ取得完了応答を返す）
                string _before = (string)_itemDic[_JKEY4]; //dataフォルダ以下のパスが返される（例：/11:22:33:44:55:66/yyyyMMdd/tmp01.csv） ※データ取得件数に応じてtmp01~tmp10.csvが作られる
                string _after = (string)_itemDic[_JKEY5]; //最終的にUnity側でDB登録時にリネームしてもらうファイル名（例：20180624182431.csv）


                //同一日時のデータを再度取得した場合を考慮して、リネーム前とリネーム後の情報をUnity側に渡し、DB登録時にリネームしてもらう。
                //END応答まで来たら、アプリにてDB更新等の処理を行ってから、データ取得完了応答をデバイス側に返すこと（5秒以内）

                if (_onCallBackCommandVariable != null)
                {
                    Action<string> onFinished = _onCallBackCommandVariable;
                    onFinished(result);
                }
            }
            catch (KeyNotFoundException e)
            {
                Debug.Log("CallbackH1dTransferDataDone/KeyNotFoundException:" + e.Message);
            }
            catch (FormatException e)
            {
                Debug.Log("CallbackH1dTransferDataDone/FormatException:" + e.Message);
            }
            catch (Exception e)
            {
                Debug.Log("CallBackGetData exception: " + e.Message);
            }
        }

        /// <summary>
        /// プログラム転送結果(H1D)の応答時に呼ばれる
        /// </summary>
        private void CallbackH1dTransferDataResult(string result)
        {
            //返ってきたjsonデータをエンコード
            // JSON(string) -> Dictionary

            int _res;

            Dictionary<string, object> _itemDic = Json.Deserialize(result) as Dictionary<string, object>;

            try
            {
                _res = Convert.ToInt32(_itemDic[_JKEY1]);

                if (_res == OK)
                {
                    //OK(成功)
                }
                else
                {
                    //NG(失敗)
                }

                if (_onCallbackH1dTransferDataResult != null)
                {
                    Action<Boolean> onFinished = _onCallbackH1dTransferDataResult;
                    _onCallbackH1dTransferDataResult = null;
                    onFinished (_res == 0 ? true : false);
                }
            }
            catch (KeyNotFoundException e)
            {
                Debug.Log("CallbackH1dTransferDataResult/KeyNotFoundException:" + e.Message);
            }
            catch (FormatException e)
            {
                Debug.Log("CallbackH1dTransferDataResult/FormatException:" + e.Message);
            }
        }

        /// <summary>
        /// プログラム更新完了確認(H1D)の応答時に呼ばれる
        /// </summary>
        private void CallbackH1dTransferDataDone(string result)
        {
            //返ってきたjsonデータをエンコード
            // JSON(string) -> Dictionary

            int _res;

            Dictionary<string, object> _itemDic = Json.Deserialize(result) as Dictionary<string, object>;

            try
            {
                _res = Convert.ToInt32(_itemDic[_JKEY1]);

                string str = Convert.ToInt32(_itemDic[_JKEY2]).ToString("000") + "." + Convert.ToInt32(_itemDic[_JKEY3]).ToString("000") + "." + Convert.ToInt32(_itemDic[_JKEY4]).ToString("000") + "." + Convert.ToInt32(_itemDic[_JKEY5]).ToString("000"); //H1Dアプリ

                if (_onCallBackCommandVariable != null)
                {
                    Action<string> onFinished = _onCallBackCommandVariable;
                    _onCallBackCommandVariable = null;
                    onFinished(result);
                }
            }
            catch (KeyNotFoundException e)
            {
                Debug.Log("CallbackH1dTransferDataDone/KeyNotFoundException:" + e.Message);
            }
            catch (FormatException e)
            {
                Debug.Log("CallbackH1dTransferDataDone/FormatException:" + e.Message);
            }
        }

        /// <summary>
        /// アラーム通知受信時に呼ばれる
        /// （基本的にここを起点に処理をする）
        /// </summary>
        private void CallBackAlarm(string result)
        {
            //返ってきたjsonデータをエンコード
            // JSON(string) -> Dictionary

            int _res;
            bool _result;

            Dictionary<string, object> _itemDic = Json.Deserialize(result) as Dictionary<string, object>;

            try
            {
                _res = Convert.ToInt32(_itemDic[_JKEY1]);
                _result = Convert.ToBoolean(_itemDic[_JKEY2]);
                if (!_result) {
                    Debug.Log ("OccurAlerm");
                    //アラームが発生すれば
                    //アラーム停止のためのダイアログを表示するように設定
                    string dialogTitle = _res == 0 ? "いびきアラーム" : "低呼吸アラーム";
                    string dialogMessage = _res == 0 ? "いびきを検知しました。" : "低呼吸を検知しました。";
                    AlermDialog.Show (dialogTitle, dialogMessage, () => {
                        //停止ボタンが押されたら、アラームを停止させる
                        Debug.Log ("StopAlerm");
                        NativeManager.Instance.StopAlerm ();
                    });
                } else {
                    //アラームが解除されれば
                    Debug.Log ("UnSetAlerm");
                    //表示しているアラーム通知を閉じる
                    AlermDialog.Dismiss ();
                }
            }
            catch (KeyNotFoundException e)
            {
                Debug.Log("CallBackAlarm/KeyNotFoundException:" + e.Message);
            }
            catch (FormatException e)
            {
                Debug.Log("CallBackAlarm/FormatException:" + e.Message);
            }
        }
    }

    /// <summary>
    /// 状態変更コマンドで指定する値
    /// </summary>
    public enum DeviceStatus : byte
    {
        Wait = 0x00,    // 待機状態
        Dummy1,         // 未使用
        UpdateH1D,      // プログラム更新状態(H1D) 未使用
        Get,            // GET状態
        Set,            // SET状態(デバッグ機能)
        UpdateG1D,      // プログラム更新状態(G1D)
        SelfDiagnosis,  // 自己診断
    }
}
