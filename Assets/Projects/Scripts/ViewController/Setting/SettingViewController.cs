using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Kaimin.Managers;
using MiniJSON;
using System;
using System.IO;
using System.Linq;
using Asyncoroutine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class SettingViewController : ViewControllerBase
{

    [SerializeField] Text g1dVersionText = null;                    //ファームウェアバージョンの値を表示するテキストフィールド(G1D
    [SerializeField] Text appVersionText = null;                    //アプリバージョンの値を表示するテキストフィールド
    [SerializeField] Image firmwareUpdateNoticeIcon = null;     //ファームウェアアップデートが必要かどうかを表すアイコン(ビックリマーク)
    [SerializeField] Image deviceConnectNoticeIcon = null;      //デバイスとの接続が必要かどうかを表すアイコン(ビックリマーク)

    /// <summary>
    /// デバイス起動状態
    ///
    /// ファーム更新時、デバイスの状態がアプリかブートかで挙動が変わる
    /// </summary>
    private DeviceActivationStatusType DeviceActivationStatus;

    /// <summary>
    /// UpdateAreaの最後のエリア
    /// </summary>
    private byte LastAreaCount { get; } = 4;

    /// <summary>
    /// ファーム更新バイナリファイルの更新データ領域
    /// </summary>
    private byte[][] UpdateArea { get; } = new byte[][]
    {
        /* embededde */
        /* start block, end block */
        new byte[]{  0,  3},		/* 0x00000-0x00FFF( 4block) */
        new byte[]{ 17, 42},		/* 0x04400-0x0ABFF(26block) */
        new byte[]{ 44, 47},		/* 0x0B000-0x0BFFF( 4block) */
        new byte[]{192,235}		    /* 0x30000-0x3AFFF(44block) */
    };

    /// <summary>
    /// ファームウェア更新データの1ブロックのサイズ
    /// </summary>
    private readonly ushort FirmwareUpdateBlockSize = 1024;

    protected override void Start()
    {
        base.Start();
        UpdateFirmwareVersionText();
        UpdateAppVersionText();
        UpdateFirmwareUpdateNoticeIcon();
        UpdateDeviceConnectNoticeIcon();
        //ペアリングが解除された際に、接続注意アイコンを表示するよう設定
        DeviceStateManager.Instance.OnDevicePareringDisConnectEvent += UpdateDeviceConnectNoticeIcon;
        DeviceActivationStatus = UserDataManager.DeviceActivationStatus.Load();
    }

    void OnDisable()
    {
        //ペアリングが解除された際にコールバックを受け取る設定を解除
        DeviceStateManager.Instance.OnDevicePareringDisConnectEvent -= UpdateDeviceConnectNoticeIcon;
    }

    public override SceneTransitionManager.LoadScene SceneTag
    {
        get
        {
            return SceneTransitionManager.LoadScene.Setting;
        }
    }

    //ファームウェアバージョンのテキスト表示を更新する
    void UpdateFirmwareVersionText()
    {
        string g1dVersionString = UserDataManager.Device.GetG1DAppVersion();
        g1dVersionText.text = g1dVersionString;
    }

    //アプリバージョンのテキスト表示を更新する
    void UpdateAppVersionText()
    {
        appVersionText.text = Application.version;
    }

    //ファームウェアアップデートが必要かどうかのアイコンの表示を更新する
    void UpdateFirmwareUpdateNoticeIcon()
    {
        //デバイスのファームウェアバージョンと、最新のファームウェアバージョンに差があればアイコンを表示する
        bool isDisp = UserDataManager.Device.IsExistFirmwareVersionDiff();
        firmwareUpdateNoticeIcon.enabled = isDisp;
    }

    //デバイスとの接続が必要かどうかのアイコンの表示を更新する
    void UpdateDeviceConnectNoticeIcon()
    {
        //デバイスと接続できてなければアイコンを表示する
        bool isDisp = !UserDataManager.State.isDoneDevicePareing();
        deviceConnectNoticeIcon.enabled = isDisp;
    }

    //「プロフィール」ボタンが押されると呼び出される
    public void OnProfileButtonTap()
    {
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Profile);
    }

    //「アラーム設定」ボタンが押されると呼び出される
    public void OnAlermSettingButtonTap()
    {
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.AlermSetting);
    }

    // 「デバイス設定」ボタンが押されると呼び出される
    public void OnDeviceSettingButtonTap()
    {
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.DeviceSetting);
    }

    //「デバイス接続」ボタンが押されると呼び出される
    public void OnDataCommunicateSettingButtonTap()
    {
        //Bluetooth接続画面へ遷移
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.BTConnect);
    }

    /// <summary>
    /// ファーム更新ボタン押下イベントハンドラ
    /// </summary>
    public void OnFirmwareUpdateButtonTap()
    {
        StartCoroutine(FirmwareUpdateCoroutine());
    }

    /// <summary>
    /// ファームウェアアップデートコルーチン
    /// </summary>
    /// <returns></returns>
    private IEnumerator FirmwareUpdateCoroutine()
    {
        yield return StartCoroutine(FirmwareUpdateMainFlow());

        // アップデートでの変更を設定画面に反映する
        // ファームウェアのバージョンを更新する
        UpdateFirmwareVersionText();

        // 最新バージョンとデバイスのバージョンに差分がある場合、アイコンを表示させる
        UpdateFirmwareUpdateNoticeIcon();

        // フッターのアイコン表示も反映する
        // 設定画面とフッター画面のアイコン表示を更新
        if (UserDataManager.Device.IsExistFirmwareVersionDiff())
        {
            DeviceStateManager.Instance.OnFirmwareUpdateNecessary();
        }
        else
        {
            DeviceStateManager.Instance.OnFirmwareUpdateNonNecessary();
        }
    }

    /// <summary>
    /// デバイスとの通信を確立する
    /// </summary>
    /// <param name="callback">接続できたかどうかを返すコールバック関数</param>
    /// <returns></returns>
    private IEnumerator EstablishConnectionWithDevice(Action<bool> callback)
    {
        const string tag = "EstablishConnectionWithDevice: ";
        //Bluetoothが有効かのチェックを行う
        bool isBluetoothActive = false;
        yield return StartCoroutine(BluetoothActiveCheck(
            isActive => isBluetoothActive = isActive
        ));
        if (!isBluetoothActive)
        {
            Debug.Log(tag + "Bluetooth非アクティブ");
            callback(false);
            yield break;
        }

        //ペアリング済みか確認
        if (UserDataManager.State.isDoneDevicePareing())
        {
            //デバイスと接続
            if (!UserDataManager.State.isConnectingDevice())
            {
                string deviceName = UserDataManager.Device.GetPareringDeviceName();
                string deviceAdress = UserDataManager.Device.GetPareringBLEAdress();
                bool isDeviceConnectSuccess = false;
                var errCode = 0;
                var errCount = 0;
                var isContinue = false;
                // 特定のエラーの場合、何回か接続処理を繰り返す
                do
                {
                    yield return StartCoroutine(
                        DeviceConnect(
                            deviceName,
                            deviceAdress,
                            code => errCode = code,
                            showDialog: false
                        )
                    );
                    if ((errCode == -4 || errCode == 133) && errCount < 10)
                    {
                        errCount++;
                        isContinue = true;
                    }
                    else if (errCode == 0)
                    {
                        // 接続成功
                        isContinue = false;
                        isDeviceConnectSuccess = true;
                    }
                    else
                    {
                        // 接続失敗
                        isContinue = false;
                        isDeviceConnectSuccess = false;
                    }
                }
                while(isContinue);
                Debug.Log(tag + "errCount = " + errCount);
                Debug.Log("Connecting_Result:" + isDeviceConnectSuccess);
                if (!isDeviceConnectSuccess)
                {
                    Debug.Log(tag + "接続失敗");
                    // デバイス接続に失敗
                    callback(false);
                    yield break;
                }
            }
        }
        else
        {
            Debug.Log(tag + "ペアリングしてない");
            yield return StartCoroutine(TellNotParering());
            callback(false);
            yield break;
        }

        callback(true);
        yield break;
    }

    //ペアリング出来てない事をユーザーに伝える
    IEnumerator TellNotParering()
    {
        bool isOK = false;
        MessageDialog.Show(
            "本体機器とのペアリングが完了していないため、処理を行えません。\n本体機器とのペアリングを行ってください。",
            true,
            false,
            () => isOK = true,
            null,
            "OK",
            "キャンセル");
        yield return new WaitUntil(() => isOK);
    }

    /// <summary>
    /// ファーム更新メイン処理
    /// </summary>
    /// <returns></returns>
    IEnumerator FirmwareUpdateMainFlow()
    {
        string g1dAppVersion = "000.000.000.000";
        //スリープしないように設定
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        //Bluetoothが有効かのチェックを行う
        bool isBluetoothActive = false;
        yield return StartCoroutine(BluetoothActiveCheck(
            (isActive) => isBluetoothActive = isActive)
        );
        if (!isBluetoothActive)
        {
            yield break;    //接続エラー時に以降のBle処理を飛ばす
        }
        //ペアリング済みか確認
        if (UserDataManager.State.isDoneDevicePareing())
        {
            //デバイスと接続
            if (!UserDataManager.State.isConnectingDevice())
            {
                string deviceName = UserDataManager.Device.GetPareringDeviceName();
                string deviceAdress = UserDataManager.Device.GetPareringBLEAdress();
                bool isDeviceConnectSuccess = false;
                var errorCode = -1;
                yield return StartCoroutine(DeviceConnect(
                    deviceName,
                    deviceAdress,
                    code => errorCode = code));
                isDeviceConnectSuccess = errorCode == 0;
                Debug.Log("Connecting_Result:" + isDeviceConnectSuccess);
                if (!isDeviceConnectSuccess)
                {
                    //接続に失敗した旨のダイアログを表示
                    yield return StartCoroutine(TellFailedConnect(deviceName));
                    yield break;
                }
            }
        }
        else
        {
            yield return StartCoroutine(TellNotParering());
            yield break;
        }

        UpdateDialog.Show("同期中");

        // アプリ状態ならコマンドを送信する　
        if (DeviceActivationStatus == DeviceActivationStatusType.App)
        {
            //デバイスのファームウェアバージョン取得
            Debug.Log("Get Device Version");
            bool isGetVersionSuccess = false;

            yield return StartCoroutine(GetFirmwareVersionFromDevice(
                (bool isSuccess, string g1dAppVer) =>
            {
                isGetVersionSuccess = isSuccess;
                if (isSuccess)
                {
                    g1dAppVersion = g1dAppVer;
                    //デバイスのバージョンを保存
                    UserDataManager.Device.SaveG1dAppVersion(g1dAppVersion);
                }
            }));
            if (!isGetVersionSuccess)
            {
                UpdateDialog.Dismiss();
                yield return StartCoroutine(TellFailedFirmwareUpdate());
                //スリープ設定解除
                Screen.sleepTimeout = SleepTimeout.SystemSetting;
                yield break;
            }
            Debug.Log("Success");
        }

        // G1Dのファームウェアの更新があるかどうか調べる
        bool isExistG1DLatestFirmware = false;
        string latestG1dFileName = "";
        bool isGetFirmwareFileError = false;
        long g1dVersionInDevice = 0;            //デバイスのファームウェアバージョンを比較しやすいように整数値に変換した値
        long g1dVersionLatest = 0;				//最新のファームウェアバージョンを比較しやすいように整数値に変換した値
        yield return StartCoroutine(GetLatestFirmwareFileNameFromFtp(
            "/RD8001/Update/G1D",
            (string fileName) => latestG1dFileName = fileName,
            (bool _isError) => isGetFirmwareFileError = _isError));

        if (latestG1dFileName == null)
        {
            UpdateDialog.Dismiss();
            if (isGetFirmwareFileError)
            {
                yield return StartCoroutine(TellFailedFirmwareUpdate());
            }
            else
            {
                //ファームウェアファイルがなければ
                yield return StartCoroutine(TellNotNessesaryFirmwareUpdate());
            }
            //スリープ設定解除
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            yield break;
        }
        Debug.Log("Latest G1D Firmware is " + latestG1dFileName);
        // デバイスのファームウェアバージョンと、最新のファームウェアバージョンを比較する
        g1dVersionInDevice = FirmwareVersionStringToLong(g1dAppVersion);
        g1dVersionLatest = FirmwareFileNameToVersionLong("/Update/G1D/" + latestG1dFileName);
        isExistG1DLatestFirmware = g1dVersionLatest > g1dVersionInDevice;
        UpdateDialog.Dismiss();

        // デバイスのファームウェアバージョンと最新のファームウェアバージョンに差があるか設定
        UserDataManager.Device.SaveIsExistFirmwareVersionDiff(isExistG1DLatestFirmware);

        // アイコン表示更新
        UpdateFirmwareUpdateNoticeIcon();

        // フッターのアイコン表示更新
        if (isExistG1DLatestFirmware)
        {
            DeviceStateManager.Instance.OnFirmwareUpdateNecessary();
        }
        else
        {
            DeviceStateManager.Instance.OnFirmwareUpdateNonNecessary();
        }

        //ファームウェアの更新があれば
        if (isExistG1DLatestFirmware)
        {
            //ファームウェアのアップデートを行うかユーザーに確認する
            bool doFirmwareUpdate = false;
            yield return StartCoroutine(AskDoFirmwareUpdate(
                (bool _doFirmwareUpdate) =>
                    doFirmwareUpdate = _doFirmwareUpdate));
            if (!doFirmwareUpdate)
            {
                yield break;    //アップデートを行わないなら処理を抜ける
            }

            //ファームウェアアップデートを行うのに十分な電池残量があるか確認する
            Debug.Log("Check Battery");
            bool isSuccessGetBattery = false;
            int batteryState = -1;

            // アプリ状態ならコマンドを送信する
            if (DeviceActivationStatus == DeviceActivationStatusType.App)
            {

                yield return StartCoroutine(CheckBatteryToFirmwareUpdate((bool _isSuccessGetBattery, int _batteryState) =>
                {
                    isSuccessGetBattery = _isSuccessGetBattery;
                    batteryState = _batteryState;
                }));
                if (isSuccessGetBattery)
                {
                    //取得した電池残量を記録する
                    UserDataManager.Device.SaveBatteryState(batteryState);
                    //バッテリーが充分にあれば
                    bool isBatteryOK = batteryState != 3;
                    if (!isBatteryOK)
                    {
                        //充分な電池残量がない事を伝える
                        yield return StartCoroutine(TellBatteryNotEnoughToFirmwareUpdate());
                        //スリープ設定解除
                        Screen.sleepTimeout = SleepTimeout.SystemSetting;
                        yield break;
                    }
                }
                else
                {
                    //通信エラーなら
                    yield return StartCoroutine(TellFailedFirmwareUpdate());
                    //スリープ設定解除
                    Screen.sleepTimeout = SleepTimeout.SystemSetting;
                    yield break;
                }

                //デバイス情報を取得し、デバイス内のデータ保存数を確認する
                Debug.Log("Check Device DataCount");
                int dataCountInDevice = -1;
                yield return StartCoroutine(GetDataCountInDevice((int dataCount) => dataCountInDevice = dataCount));
                Debug.Log("DeviceDataCount:" + dataCountInDevice);
                if (dataCountInDevice == -1)
                {           //デバイスのデータ数取得エラー時
                    yield return StartCoroutine(TellFailedFirmwareUpdate());
                    //スリープ設定解除
                    Screen.sleepTimeout = SleepTimeout.SystemSetting;
                    yield break;
                }
                else if (dataCountInDevice > 0)
                {       //デバイスにデータが残っているとき
                        //デバイスに残っているデータを全て取得し、DBに登録する
                    bool isSuccessGetRemainData = false;
                    yield return StartCoroutine(GetRemainDataFromDevice(
                        dataCountInDevice,
                        (bool _isSuccessGetRemainData) =>
                            isSuccessGetRemainData = _isSuccessGetRemainData));
                    if (!isSuccessGetRemainData)
                    {
                        Debug.Log("Failed GetRemainData");
                        //データの取得・DB登録に失敗すれば
                        yield return StartCoroutine(TellFailedFirmwareUpdate());
                        //スリープ設定解除
                        Screen.sleepTimeout = SleepTimeout.SystemSetting;
                        yield break;
                    }

                    //取得したデータをHttpでアップロードする
                    yield return StartCoroutine(HttpManager.UploadUnsendDatasByHttp());

                    //機器にデータが残っていないか確認する
                    Debug.Log("Check Device DataCount Again");
                    dataCountInDevice = -1;
                    yield return StartCoroutine(GetDataCountInDevice((int dataCount) => dataCountInDevice = dataCount));
                    if (dataCountInDevice == -1 || dataCountInDevice > 0)
                    {           //デバイスのデータ数取得エラー、もしくはデータが残っていたとき
                        yield return StartCoroutine(TellFailedFirmwareUpdate());
                        //スリープ設定解除
                        Screen.sleepTimeout = SleepTimeout.SystemSetting;
                        yield break;
                    }
                }
                else
                {   //デバイスにデータが残ってないとき
                    //本体データ同期時刻を更新
                    UserDataManager.State.SaveDataReceptionTime(DateTime.Now);
                }
            }

            //ファームウェアダウンロードを行う
            UpdateDialog.Show("ファームウェアダウンロード中");
            string g1dSavedPathInApp = "";  // G1Dのアプリ内保存パス
            if (isExistG1DLatestFirmware)
            {
                Debug.Log("Run G1DFirmware Update.");
                // G1Dの最新ファームウェアが存在すればダウンロードを行う
                yield return StartCoroutine(DownloadFirmware("/RD8001/Update/G1D/" + latestG1dFileName, (string savedPath) => g1dSavedPathInApp = savedPath));
                if (g1dSavedPathInApp == null)
                {
                    // G1Dのファームウェアのダウンロードに失敗したら
                    UpdateDialog.Dismiss();
                    yield return StartCoroutine(TellFailedFirmwareDownload());
                    Debug.Log("Download G1D Firmware Failed...");
                    // スリープ設定解除
                    Screen.sleepTimeout = SleepTimeout.SystemSetting;
                    yield break;
                }
                Debug.Log("Download G1D Firmware Success!");
            }
            UpdateDialog.Dismiss();

            // ファームウェアの更新を行う
            bool isSuccess = false;
            bool doRetry = true;
            while ( doRetry == true && isSuccess == false) {
                // 3回までリトライする
                int failureCount = 0;
                const int maxRetryCount = 3;
                while (failureCount < maxRetryCount)
                {
                    yield return StartCoroutine(UpdateG1DFirmwareFlow(
                        g1dSavedPathInApp,
                        _isSuccessFirmwareUpdate =>
                            isSuccess = _isSuccessFirmwareUpdate));
                    if (isSuccess)
                    {
                        // breakキーワードを使用するとコルーチン自体が終了するので、以下のようにしてループを抜ける
                        failureCount = maxRetryCount;
                    }
                    else
                    {
                        failureCount++;
                    }
                }

                // 失敗時、リトライするかどうか
                if (!isSuccess)
                {
                    // ファームウェアアップデートが失敗すれば
                    // スリープ設定解除
                    Screen.sleepTimeout = SleepTimeout.SystemSetting;
                    // リトライするかどうか
                    yield return StartCoroutine(AskRetryFirmwareUpdate(
                        b => doRetry = b
                    ));
                }
            };

            // 失敗かつリトライしないなら終了
            if (isSuccess == false && !doRetry)
            {
                yield return StartCoroutine(TellFirmwareUpdateFailed());
                yield break;
            }

            // 現状、ファームウェアアップデートすぐに以降のコマンドを送って応答NGとなっているため少しコマンド送信までの間隔を空ける
            yield return new WaitForSeconds(3f);    // とりあえず3秒
                                                    // デバイスのバージョンを取得して保存する
            bool isGetVersionSuccess = false;
            yield return StartCoroutine(GetFirmwareVersionFromDevice(
                (bool b, string g1dAppVer) =>
            {
                isGetVersionSuccess = b;
                if (b)
                {
                    g1dAppVersion = g1dAppVer;
                    //デバイスのバージョンを保存
                    UserDataManager.Device.SaveG1dAppVersion(g1dAppVersion);
                    //ファームウェアアップデートが成功したため、最新のバージョンになったことを記録する
                    UserDataManager.Device.SaveIsExistFirmwareVersionDiff(false);
                }
            }));
        }
        else
        {
            //ファームウェアの更新が必要なければ
            yield return StartCoroutine(TellNotNessesaryFirmwareUpdate());
        }
        //スリープ設定解除
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
    }

    //ファームウェアのアップデートが不要であることをユーザーに伝える
    IEnumerator TellNotNessesaryFirmwareUpdate()
    {
        bool isOK = false;
        MessageDialog.Show("<size=32>アップデートの必要はありません。</size>", true, false, () => isOK = true);
        yield return new WaitUntil(() => isOK);
    }

    /// <summary>
    /// デバイスを接続する
    /// </summary>
    /// <param name="deviceName">デバイス名</param>
    /// <param name="deviceAdress">デバイスアドレス</param>
    /// <param name="onResponse">結果を返すコールバック関数</param>
    /// <param name="showDialog">ダイアログを表示するかどうか</param>
    /// <returns></returns>
    IEnumerator DeviceConnect(
        string deviceName,
        string deviceAdress,
        Action<int> onResponse,
        bool showDialog = true)
    {
        if (showDialog)
        {
            UpdateDialogAddButton.Show(
                deviceName + "に接続しています。",
                false,
                true,
                null,
                () =>
                {
                    //キャンセルボタン押下時
                    //デバイスとの接続を切る
                    BluetoothManager.Instance.Disconnect();
                },
                "OK",
                "キャンセル"
            );
        }
        bool isSuccess = false; //接続成功
        bool isFailed = false;  //接続失敗
        string receiveData = "";        //デバイス接続で成功・失敗時に受け取るデータ（JSONにパースして使用）
        string uuid = "";       //ペアリング中のデバイスのUUID(iOSでのみ必要)
#if UNITY_IOS
        uuid = UserDataManager.Device.GetPareringDeviceUUID();
#endif

        var errorCount = 0;    // BLEエラー出現回数

        BluetoothManager.Instance.Connect(
            deviceAdress,
            (string data) =>
            {
                //エラー時
                receiveData = data;
                isFailed = true;
            },
            (string data) =>
            {
                //接続完了時
                receiveData = data;
                isSuccess = true;
            },
            uuid);
        yield return new WaitUntil(() => isSuccess || isFailed);    //応答待ち
        if (isSuccess)
        {
            //接続成功時
            //接続したデバイス情報読み出し
            var json = Json.Deserialize(receiveData) as Dictionary<string, object>;
            string name = (string)json["KEY1"];
            string adress = (string)json["KEY2"];
            //接続したデバイスを記憶しておく
            UserDataManager.Device.SavePareringBLEAdress(adress);
            UserDataManager.Device.SavePareringDeviceName(name);
            if (showDialog) UpdateDialogAddButton.Dismiss();
        }
        else
        {
            //接続失敗時
            var json = Json.Deserialize(receiveData) as Dictionary<string, object>;
            int error1 = Convert.ToInt32(json["KEY1"]);
            int error2 = Convert.ToInt32(json["KEY2"]);
            if (showDialog) UpdateDialogAddButton.Dismiss();
#if UNITY_IOS
            if (error2 == -8) {
                //iOSの_reconnectionPeripheralコマンドでのみ返ってくる、これ以降接続できる見込みがないときのエラー
                Debug.Log ("Connect_OcuurSeriousError");
                //接続を解除
                UserDataManager.State.SaveDeviceConnectState(false);
                //接続が解除された事を伝える
                DeviceStateManager.Instance.OnDeviceDisConnect();
                //ペアリングを解除
                UserDataManager.State.ClearDeviceParering();
                //ペアリングが解除された事を伝える
                DeviceStateManager.Instance.OnDevicePrearingDisConnect();
                //接続に失敗した旨のダイアログを表示
                yield return StartCoroutine (TellFailedConnect(deviceName));
                //再度ペアリングを要求するダイアログを表示
                yield return StartCoroutine (TellNeccesaryParering());
                onResponse(error2);
                yield break;
            }
#endif

            //何らかの原因で接続できなかった場合(タイムアウト含む)
            if (error2 == -3)
            {
                Debug.Log("OccurAnyError");
            }

            //接続が切れた場合(GATTサーバには接続できたが、サービスまで全て接続できないと接続完了にはならない。)
            if (error2 == -4 && errorCount < 10)
            {
                Debug.Log("DisconnectedError");
                onResponse(error2);
                yield break;
            }

            // BLEエラー133
            // ペリフェラル側で処理負荷が高くなっていると起きやすいらしい
            // このエラーが出ても数回は接続処理を再トライする
            // 参考: https://qiita.com/zzt-osamuhanzawa/items/a2b538bf5f564173ec88
            if (error2 == 133 && errorCount < 10)
            {
                onResponse(error2);
                yield break;
            }
        }

        onResponse(0);
    }

    //再ペアリングが必要な事をユーザーに伝える
    IEnumerator TellNeccesaryParering()
    {
        bool isOK = false;
        MessageDialog.Show("再度ペアリング設定を行ってください。", true, false, () => isOK = true);
        yield return new WaitUntil(() => isOK);
    }

    //デバイスと接続できなかった事をユーザーに伝える
    IEnumerator TellFailedConnect(string deviceName)
    {
        bool isOk = false;
        MessageDialog.Show("<size=32>" + deviceName + "と接続できませんでした。</size>", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk);
    }

    //bluetoothが有効になっているかどうか確認する
    IEnumerator BluetoothActiveCheck(Action<bool> onResponse)
    {
        NativeManager.Instance.Initialize();
        bool isActive = NativeManager.Instance.BluetoothValidCheck();
        if (!isActive)
        {
            //無効になっているため、設定画面を開くかどうかのダイアログを表示する
            bool isSetting = false;
            yield return StartCoroutine(AskOpenSetting((bool _isSetting) => isSetting = _isSetting));
            if (isSetting)
            {
                //Bluetoothを有効にするなら
                NativeManager.Instance.BluetoothRequest();
#if UNITY_ANDROID
                yield return new WaitUntil(() => NativeManager.Instance.PermissionCode > 0);
                isActive = NativeManager.Instance.PermissionCode == 1;
#elif UNITY_IOS
                isActive = false;	//iOSの場合、ユーザーの選択が受け取れなかったため、拒否された前提で進める
#endif
                if (isActive)
                {
                    //Bluetoothが有効になったら
                }
                else
                {
                    //Bluetoothが有効にされなかったら
                    //ダイアログを閉じるだけ
                }
            }
            else
            {
                //Bluetoothが有効にされなかったなら
                isActive = false;
            }
        }
        onResponse(isActive);
        yield return null;
    }

    //端末の設定画面を開くかどうかユーザーに尋ねる
    IEnumerator AskOpenSetting(Action<bool> onResponse)
    {
        bool isSetting = false;
        bool isCancel = false;
        MessageDialog.Show("<size=30>Bluetoothがオフになっています。\nSleeimと接続できるようにするには、\nBluetoothをオンにしてください。</size>",
            true,
            true,
            () => isSetting = true,
            () => isCancel = true,
            "設定",
            "キャンセル");
        yield return new WaitUntil(() => isSetting || isCancel);
        onResponse(isSetting);
    }

    /// <summary>
    /// 最新のファームウェアをダウンロードする
    /// </summary>
    /// <param name="filePath">最新のファームウェアファイルのパス(/RD8001/Update/G1D/xxx.mot)</param>
    /// <param name="onComplete">ダウンロード完了時に保存先のフルパスを返すコールバック</param>
    IEnumerator DownloadFirmware(string filePath, Action<string> onComplete)
    {
        Debug.Log("Download Firmware");
        string savePath = Application.temporaryCachePath + "/Firmware/"; //保存先のパス(Android端末内)
        string fileName = filePath.Substring(filePath.LastIndexOf('/') + 1); //例：RD8001G1D_Ver000.000.000.007.mot
        string saveFilePath = savePath + fileName;
        Debug.Log("SavePath:" + savePath + ", FileName:" + fileName);

        //既に同じファイルが存在してないか確認する
        if (System.IO.File.Exists(saveFilePath))
        {
            //同じファイルが存在した場合は前のファイルを削除する
            Debug.Log("Same FirmwareFile Exist...Delete This File.");
            System.IO.File.Delete(saveFilePath);
            Debug.Log("Delete.");
        }

        //ファイルアップロードのためにサーバーと接続
        bool isConnectionSuccess = HttpManager.IsInternetAvailable();
        if (!isConnectionSuccess)
        {
            //サーバーとの接続に失敗すれば
            onComplete(null);
            yield break;
        }

        Debug.Log("DownloadData:" + savePath + " from " + filePath);
        string downloadUrl = HttpManager.API_DOWNLOAD_URL + "?file_path=../.." + filePath;
        var downloadTask = HttpManager.DownloadFile(saveFilePath, downloadUrl);
        yield return downloadTask.AsCoroutine();
        Debug.Log(downloadTask.Result ? "Download Success!" : "Download Failed...");

        if (downloadTask.Result)
        {
            onComplete(saveFilePath);
            Debug.Log("Download Success!");
        }
        else
        {
            onComplete(null);
            Debug.Log("Download Failed...");
        }
    }

    /// <summary>
    /// G1Dを更新する
    /// </summary>
    /// <param name="fileFullPath"></param>
    /// <param name="onResponse"></param>
    /// <returns></returns>
    IEnumerator UpdateG1DFirmwareFlow(string fileFullPath, Action<bool> onResponse)
    {
        // G1Dファームウェアファイル読み込み(前準備)
        if (!System.IO.File.Exists(fileFullPath))
        {
            yield return StartCoroutine(TellNotFoundFirmwareFile());
            onResponse(false);
            yield break;
        }
        // ファームウェアファイルが正常に存在していれば
        Debug.Log("G1DFirmware OK");
        yield return StartCoroutine(UpdateG1DFirmware(fileFullPath, onResponse));
    }

    /// <summary>
    /// G1Dのファームウェアアップデートを行う
    /// </summary>
    /// <param name="fileFullPath"></param>
    /// <param name="onResponse"></param>
    /// <returns></returns>
    IEnumerator UpdateG1DFirmware(string fileFullPath, Action<bool> onResponse)
    {
        bool? isSuccess = null;
        Debug.Log("UpdateG1DFirmware");

        // G1Dプログラム更新状態へ変更
        if (DeviceActivationStatus == DeviceActivationStatusType.App)
        {
            yield return StartCoroutine(NotifyFirmwareUpdate(
                b => isSuccess = b
            ));
            yield return new WaitUntil(() => isSuccess != null);

            if (isSuccess == false)
            {
                //状態変更失敗時
                Debug.Log("DeviceStateToG1DUpdate Failed...");
                onResponse(false);
                yield break;
            }
        }

        // デバイスの再起動を待つ
        UpdateDialog.Show("デバイスの再起動中です");
        yield return new WaitForSeconds(15);

        ///
        /// 以降、デバイスはBOOT状態
        ///

        // サービスUUIDをFW更新用に変更
        BluetoothManager.Instance.ChangeServiceUUIDToFirmwareUpdate();
        // デバイス状態を記憶
        DeviceActivationStatus = DeviceActivationStatusType.Boot;
        UserDataManager.DeviceActivationStatus.Save(DeviceActivationStatus);

        UpdateDialog.Show("<color=red>ファームウェアをアップデートしています。</color>\nファームウェアを機器に転送中");
        // プログラム転送中にスリープするとBLEでのコード転送が止まってしまう。
        // 自動スリープを無効にする場合
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Debug.Log("Start Transfer");
        // プログラム更新データ送信する
        FileStream firmwareUpdateBin = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read);

        /// <summary>
        /// プログラム更新データ送信する
        /// </summary>
        isSuccess = null;
        yield return StartCoroutine(FirmwareUpdateCycle(
            firmwareUpdateBin,
            b => isSuccess = b
        ));
        yield return new WaitUntil(() => isSuccess != null);
        UpdateDialog.Dismiss();
        if (isSuccess == false)
        {
            onResponse(false);
            yield break;
        }

        /// <summary>
        /// 再接続する
        /// </summary>
        isSuccess = null;
        yield return StartCoroutine(EstablishConnectionWithDevice(
            b => isSuccess = b
        ));
        if (isSuccess == false)
        {
            onResponse(false);
            yield break;
        }

        UpdateDialog.Show("書き込みデータを確認します");
        yield return new WaitForSeconds(5);     // ペリフェラルの負荷軽減のため数秒待機する

        // ファーム更新に成功したかどうか確認する
        isSuccess = null;
        yield return StartCoroutine(CheckFinishFirmwareUpdate(
            b => isSuccess = b
        ));
        if (isSuccess == false)
        {
            onResponse(false);
            yield break;
        }

        // 切断
        Debug.Log("切断");
        BluetoothManager.Instance.Disconnect();

        // デバイスの再起動を待つ
        UpdateDialog.Show("デバイスの再起動中です");
        yield return new WaitForSeconds(15);

        // 以降、デバイスはAPP状態

        // 汎用通信用のUUIDに戻す
        BluetoothManager.Instance.ChangeServiceUUIDToNormal();
        // デバイス状態を記憶
        DeviceActivationStatus = DeviceActivationStatusType.App;
        UserDataManager.DeviceActivationStatus.Save(DeviceActivationStatus);

        UpdateDialog.Show("接続しています");

        // 再接続する
        isSuccess = null;
        yield return StartCoroutine(EstablishConnectionWithDevice(
            b => isSuccess = b
        ));
        if (isSuccess == false)
        {
            onResponse(false);
            yield break;
        }

        bool isOK = false;
        MessageDialog.Show(
            "ファームウェアアップデートが完了しました",
            useOK: true,
            useCancel: false,
            onOK: () => isOK = true);
        yield return new WaitUntil(() => isOK == true);

        // デフォルトの設定にする場合
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
        onResponse(true);
        yield break;
    }

    /// <summary>
    /// ファーム書き換えが完了したかどうか確認する
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator CheckFinishFirmwareUpdate(Action<bool> callback)
    {
        bool? isSuccess = null;
        const string tag = "NotifyFinishOfFirmwareUpdate ";

        byte[] value = new byte[]
        {
            (byte)FirmwareUpdateControlCommand.SendFinish
        };

        BluetoothManager.Instance.SendFirmwareUpdateControlCommand(
            value,
            errorData => {
                // エラー
                isSuccess = false;
            },
            success => {
                //コマンド書き込み結果
                Debug.Log (tag + "write:" + success);
                isSuccess = success;
            }
        );

        yield return new WaitUntil(() => isSuccess != null);
        callback(isSuccess ?? false);     // 戻り値として返す
        yield break;
    }

    /// <summary>
    /// ファームウェア更新開始をデバイスに通知する
    /// </summary>
    /// <param name="callback">成功したかどうか</param>
    /// <returns></returns>
    private IEnumerator NotifyFirmwareUpdate(Action<bool> callback)
    {
        Debug.Log("NotifyFirmwareUpdate");
        bool? isSuccess = null;        // 送信結果

        // デバイス状態をプログラム更新状態に変更する
        yield return StartCoroutine(ChangeToFirmwareUpdateStatus(
            b => isSuccess = b
        ));
        yield return new WaitUntil(() => isSuccess != null);
        callback(isSuccess ?? false);
        yield break;
    }

    /// <summary>
    /// デバイス状態をプログラム更新状態に変更する
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator ChangeToFirmwareUpdateStatus(Action<bool> callback)
    {
        const string tag = "ChangeToFirmwareUpdateStatus ";
        bool? isSuccess = null;        // 送信結果

        Debug.Log(tag + "Start!");
        BluetoothManager.Instance.SendCommandId(
            BluetoothManager.CommandUpdateG1d,
            (string data) => {
                //エラー時
                Debug.Log (tag + "error:" + data);
                isSuccess = false;
            },
            (bool success) => {
                //コマンド書き込み結果
                Debug.Log (tag + "write:" + success);
                if (!success) isSuccess = false;
            },
            (string data) => {
                //応答結果
                Debug.Log (tag + "response:" + data);
                var json = Json.Deserialize(data) as Dictionary<string, object>;
                bool response = Convert.ToBoolean(json["KEY2"]);
                isSuccess = response;
            },
            // ダミー
            s => Debug.Log(s)
        );

        yield return new WaitUntil(() => isSuccess != null);
        callback(isSuccess ?? false);
        yield break;
    }

    /// <summary>
    /// ブロックごとにファーム更新処理を行う
    /// </summary>
    /// <param name="updateData">ファーム更新データ</param>
    /// <param name="blockCount">ファーム更新データのブロック数</param>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator FirmwareUpdateCycle(
        FileStream updateData,
        Action<bool> callback)
    {
        bool? isSuccess = null;     // 各コルーチンの結果を格納する変数

        for (byte currentArea = 0; currentArea < LastAreaCount; currentArea++)
        {
            // エリア内の最初のブロックと最後のブロックを設定する
            byte startBlock = UpdateArea[currentArea][0];
            byte endBlock = UpdateArea[currentArea][1];

            for (byte currentBlock = startBlock; currentBlock <= endBlock; currentBlock++)
            {
                // 接続処理
                Debug.Log("接続します");
                isSuccess = null;
                yield return StartCoroutine(EstablishConnectionWithDevice(
                    b => isSuccess = b
                ));
                yield return new WaitUntil(() => isSuccess != null);
                if (isSuccess == false)
                {
                    Debug.Log("接続失敗");
                    callback(false);
                    yield break;
                }

                // 1ブロック送信し終えたかどうか確認する(2回目以降)
                if (currentBlock != startBlock) {
                    yield return new WaitForSeconds(5);     // ペリフェラルの負荷軽減のため待機

                    Debug.Log("書き込み確認します");
                    isSuccess = null;
                    yield return StartCoroutine(CheckWrite(
                        b => isSuccess = b
                    ));
                    if (isSuccess == false)
                    {
                        Debug.Log("書き込み失敗");
                        callback(false);
                        yield break;
                    }
                }

                yield return new WaitForSeconds(5);     // ペリフェラルの負荷軽減のため待機

                // STARTコマンド送信する
                Debug.Log("開始通知");
                isSuccess = null;
                yield return StartCoroutine(StartFirmwareUpdate(
                    currentBlock,
                    FirmwareUpdateBlockSize,
                    b => isSuccess = b
                ));
                yield return new WaitUntil(() => isSuccess != null);
                if (isSuccess == false) {
                    Debug.Log("開始失敗");
                    callback(false);
                    yield break;
                }

                // 更新データを1ブロック分送信する
                Debug.Log("現在ブロック: " + currentBlock);
                Debug.Log("更新データ送信開始");
                BluetoothManager.Instance.ChangeCharacteristicUUIDToFirmwareUpdateData();
                List<byte[]> updateDataBlock = ExtractFirmwareUpddateDataByBlock(updateData, currentBlock);
                foreach (var bytes in updateDataBlock)
                {
                    yield return new WaitForSeconds(0.2f);
                    isSuccess = null;
                    yield return StartCoroutine(SendFirmwareUpdateData(
                        bytes,
                        b => isSuccess = b
                    ));
                    yield return new WaitUntil(() => isSuccess != null);
                    if (isSuccess == false) {
                        Debug.Log("更新データ送信失敗");
                        callback(false);
                        yield break;
                    }
                }

                BluetoothManager.Instance.ChangeCharacteristicUUIDToFirmwareUpdateControl();
                yield return new WaitForSeconds(10);     // ペリフェラルの負荷軽減のため待機

                // 送信完了を通知する
                Debug.Log(currentBlock + "ブロック送信完了");
                isSuccess = null;
                yield return StartCoroutine(SendComplete(
                    b => isSuccess = b
                ));
                yield return new WaitUntil(() => isSuccess != null);
                if (isSuccess == false) {
                    Debug.Log("送信完了通知失敗");
                    callback(false);
                    yield break;
                }

                // 切断
                Debug.Log("切断");
                BluetoothManager.Instance.Disconnect();

                // 切断完了を確実に待つ
                yield return new WaitForSeconds(5);
            }
        }

        callback(isSuccess ?? false);
        yield break;
    }

    /// <summary>
    /// ファームウェア更新開始コマンドを送信する
    /// </summary>
    /// <param name="blockCount">送信するブロック</param>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator StartFirmwareUpdate(
        byte blockCount,
        ushort dataSize,
        Action<bool> callback)
    {
        const string tag = "SendFirmwareUpdateStartCommand ";
        bool? isSuccess = null;
        byte[] value = new byte[]
        {
            (byte)FirmwareUpdateControlCommand.Start,
            blockCount,
        };
        byte[] dataSizeArray = BitConverter.GetBytes(dataSize);
        value = value.Concat(dataSizeArray).ToArray();

        var s = BitConverter.ToString(value);
        Debug.Log(tag + "value = " + s);

        // デバイス状態変更
        BluetoothManager.Instance.SendFirmwareUpdateControlCommand(
            value,
            errorData => {
                // エラー
                isSuccess = false;
            },
            success => {
                //コマンド書き込み結果
                Debug.Log (tag + "write:" + success);
                isSuccess = success;
            }
        );

        yield return new WaitUntil(() => isSuccess != null);
        callback(isSuccess ?? false);
    }

    /// <summary>
    /// ファーム書き換えが完了したかどうか確認する
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator CheckWrite(Action<bool> callback)
    {
        bool? isSuccess = null;
        const string tag = "CheckWrite ";

        byte[] value = new byte[]
        {
            (byte)FirmwareUpdateControlCommand.CheckWrite
        };

        BluetoothManager.Instance.SendFirmwareUpdateControlCommand(
            value,
            errorData => {
                // エラー
                isSuccess = false;
            },
            success => {
                //コマンド書き込み結果
                Debug.Log (tag + "write:" + success);
                isSuccess = success;
            }
        );

        yield return new WaitUntil(() => isSuccess != null);
        callback(isSuccess ?? false);     // 戻り値として返す
        yield break;
    }

    /// <summary>
    /// 指定したブロックの更新データを抽出する
    /// </summary>
    /// <param name="firmwareUpdateBin">ファーム更新バイナリファイル</param>
    /// <returns>ファームウェア更新データ</returns>
    private List<byte[]> ExtractFirmwareUpddateDataByBlock(FileStream firmwareUpdateBin, byte blockCount)
    {
        List<byte[]> firmwareUpdateBlock = new List<byte[]>();  // 1ブロックの更新データ
        bool isContinue = true;
        int dataPointer = 0;   // 1ブロック内のデータポインタ

        while(isContinue)
        {
            byte dataSize = 0;  // 配列のサイズ
            if ((FirmwareUpdateBlockSize - dataPointer) > 19)
            {
                dataSize = 19;
            }
            else
            {
                dataSize = (byte)(FirmwareUpdateBlockSize - dataPointer);
                isContinue = false;
            }

            // データサイズ + チェックサム用の1byte で配列を作成
            byte[] data = Enumerable.Repeat<byte>(0, dataSize + 1).ToArray();

            // バイナリファイル読み込み
            firmwareUpdateBin.Seek(blockCount * 0x400 + dataPointer, SeekOrigin.Begin);
            firmwareUpdateBin.Read(data, 0, dataSize);

            dataPointer += dataSize;
            data[dataSize] = 0;
            for (int i = 0; i < dataSize; i++)
            {
                data[dataSize] += data[i];      // 配列の最後にチェックサム入れる
            }

            firmwareUpdateBlock.Add(data);
        }

        return firmwareUpdateBlock;
    }

    /// <summary>
    /// ファーム更新データを送信する
    /// </summary>
    /// <param name="fwData">ファームウェアデータ</param>
    /// <param name="callback">通信成功したかどうかを返すコールバック関数</param>
    /// <returns></returns>
    private IEnumerator SendFirmwareUpdateData(byte[] fwData, Action<bool> callback)
    {
        bool? isSuccess = null;        // 送信結果
        BluetoothManager.Instance.SendFirmwareUpdateData(
            fwData,
            errorData => {
                // エラー
                isSuccess = false;
            },
            b => isSuccess = b      // 書き込みコールバック
        );
        yield return new WaitUntil(() => isSuccess != null);

        callback(isSuccess ?? false);     // 戻り値として返す
        yield break;
    }

    /// <summary>
    /// 更新データを1ブロック分送信完了したことを通知する
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator SendComplete(Action<bool> callback)
    {
        byte[] value = new byte[]
        {
            (byte)FirmwareUpdateControlCommand.SendComplete
        };

        bool? isSuccess = null;
        BluetoothManager.Instance.SendFirmwareUpdateControlCommand(
            value,
            errorData => {
                // エラー
                isSuccess = false;
            },
            success => {
                //コマンド書き込み結果
                Debug.Log (tag + "write:" + success);
                isSuccess = success;
            }
        );
        yield return new WaitUntil(() => isSuccess != null);

        callback(isSuccess ?? false);     // 戻り値として返す
        yield break;
    }

    // アプリ内に保存したファームウェアファイルのパスから「000.000.000.000」の形式のバージョン情報を抜き出して返す
    string FirmwareAppFilePathToVersionString(string fileFullPath)
    {
        string result = fileFullPath;
        result = result.Substring(result.LastIndexOf('/') + 1); //例：RD8001G1D_Ver000.000.000.008.mot
        result = result.Substring(0, result.LastIndexOf('.'));  //例：RD8001G1D_Ver000.000.000.008
        result = result.Substring(result.Length - 15);              //例：000.000.000.008
        return result;
    }

    // プログラム更新完了確認
    IEnumerator FirmwareUpdateCompleteCheck()
    {
        UpdateDialog.Show("<color=red>ファームウェアをアップデートしています。</color>\nファームウェア書き換え中");
        //タイムアウト検知のため開始時の時刻を記録する
        var startTime = System.DateTime.Now;
        TimeSpan timeoutTime = new TimeSpan(0, 15, 0);  //タイムアウトを15分で設定
        bool isFinish = false;
        while (!isFinish)
        {
            bool isSuccess = false;
            bool isFailed = false;
            string receiveData = "";
            BluetoothManager.Instance.SendCommandId(
                15,
                (string data) =>
                {
                    //エラー時
                    Debug.Log("failed:" + data);
                    receiveData = data;
                    isFailed = true;
                },
                (bool success) =>
                {
                    Debug.Log("commandWrite:" + success);
                    if (!success)
                        isFailed = true;
                },
                (string data) =>
                {
                    Debug.Log("commandResponse:" + data);
                    //使用しない
                },
                (string data) =>
                {
                    //デバイス状況取得
                    Debug.Log("success:" + data);
                    receiveData = data;
                    isSuccess = true;
                });
            yield return new WaitUntil(() => isSuccess || isFailed);
            if (isSuccess)
            {
                if (receiveData != "")
                {
                    var json = Json.Deserialize(receiveData) as Dictionary<string, object>;
                    int completeState = Convert.ToInt32(json["KEY1"]);
                    if (completeState == 1)
                    {
                        //正常完了
                        UpdateDialog.Dismiss();
                        yield return StartCoroutine(TellFirmwareUpdateSuccess());
                        isFinish = true;
                        break;
                    }
                    else if (completeState == 2)
                    {
                        //異常完了
                        UpdateDialog.Dismiss();
                        yield return StartCoroutine(TellFirmwareUpdateFailed());
                        isFinish = true;
                        break;
                    }
                    else
                    {
                        //完了していなかったら
                        yield return new WaitForSeconds(5f);
                    }
                }
            }
            else
            {
                UpdateDialog.Dismiss();
                yield return StartCoroutine(TellFirmwareUpdateFailed());
                isFinish = true;
                break;
            }
            //タイムアウト確認
            var timeCount = System.DateTime.Now - startTime;
            if (timeCount > timeoutTime)
            {
                //タイムアウト
                UpdateDialog.Dismiss();
                yield return StartCoroutine(TellFirmwareUpdateFailed());
                isFinish = true;
                break;
            }
        }
        yield return null;
    }

    //ファームウェアの転送に失敗した事をユーザーに伝える
    IEnumerator TellFirmwareTransferFailed()
    {
        bool isOk = false;
        MessageDialog.Show("ファームウェアの転送に失敗しました。", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk = true);
    }

    //ファームウェアの転送でタイムアウトした事をユーザーに伝える
    IEnumerator TellFirmwareTransferTimeout()
    {
        bool isOk = false;
        MessageDialog.Show("ファームウェアの転送に失敗しました。(タイムアウト)", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk = true);
    }

    //ファームウェア更新が成功した事をユーザーに伝える
    IEnumerator TellFirmwareUpdateSuccess()
    {
        bool isOk = false;
        MessageDialog.Show("アップデートが完了しました。", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk = true);
    }

    //ファームウェア更新が失敗した事をユーザーに伝える
    IEnumerator TellFirmwareUpdateFailed()
    {
        bool isOk = false;
        MessageDialog.Show("アップデートに失敗しました。", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk = true);
    }

    //ファームウェアファイルに異常がある事をユーザーに伝える
    IEnumerator TellFirmwareFileExistError()
    {
        bool isOk = false;
        MessageDialog.Show("ファームウェアファイルに異常があります。", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk);
    }

    //ファームウェアファイルが存在しない事をユーザーに伝える
    IEnumerator TellNotFoundFirmwareFile()
    {
        bool isOk = false;
        MessageDialog.Show("ファイルが存在しません。", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk);
    }

    

    //デバイスのデータを全て取得し、DBに登録まで行う
    IEnumerator GetRemainDataFromDevice(int dataCount, Action<bool> onResponse)
    {
        Debug.Log("GetRemainDataFromDevice");
        List<string> csvPathList = null;
        List<string> csvNameList = null;
        //睡眠データを取得
        yield return StartCoroutine(GetSleepDataFlow(
            dataCount,
            (List<string> _csvPathList) =>
            {
                csvPathList = _csvPathList;
            },
            (List<string> _csvNameList) =>
            {
                csvNameList = _csvNameList;
            }));
        if (csvPathList != null && csvPathList.Count > 0)
        {
            UpdateDialog.Show("同期中");
            //データが1件以上取得できれば
            //データをリネームしてDBに登録
            yield return StartCoroutine(RegistDataToDB(csvPathList, csvNameList));
            //DBに取得したデータの登録が完了。送信完了コマンドを送信する
            yield return StartCoroutine(FinishGetData());
            UpdateDialog.Dismiss();
            //同期時刻を保存する
            UserDataManager.State.SaveDataReceptionTime(DateTime.Now);
            onResponse(true);
            yield break;
        }
        onResponse(false);
    }

    //デバイスからの睡眠データ取得処理を終了した事をデバイスに伝える
    IEnumerator FinishGetData()
    {
        Debug.Log("データ取得完了応答");
        bool isSuccess = false;
        bool isFailed = false;
        BluetoothManager.Instance.SendCommandGetFinish(
            true,
            (string data) =>
            {
                //エラー
                Debug.Log("failed:" + data);
                isFailed = true;
            },
            (bool success) =>
            {
                Debug.Log("commandWrite:" + success);
                if (success)
                    isSuccess = true;
                else
                    isFailed = true;
            });
        yield return new WaitUntil(() => isSuccess || isFailed);
        Debug.Log("Getting data is Complete.");
    }

    //デバイスから取得したデータをリネームしてDBに登録する
    IEnumerator RegistDataToDB(List<string> dataPathList, List<string> dataNameList)
    {
        //DB登録
        for (int i = 0; i < dataPathList.Count; i++)
        {
            var sleepTable = MyDatabase.Instance.GetSleepTable();
            //仮のファイル名を指定されたファイル名に変更する
            var filePath = dataPathList[i];                                                     //例：112233445566/yyyyMM/tmp01.csv
            var untilSlashCountFromLast = filePath.LastIndexOf('/');                            //はじめから最後の'/'までの文字数。0はじまり
            filePath = filePath.Substring(0, untilSlashCountFromLast + 1);                      //例：112233445566/yyyyMM/
            filePath = filePath + dataNameList[i];                                              //例：112233445566/yyyyMM/20180827092055.csv
            var fullOriginalFilePath = Kaimin.Common.Utility.GsDataPath() + dataPathList[i];
            var fullRenamedFilePath = Kaimin.Common.Utility.GsDataPath() + filePath;

            //ファイルが存在しているか確認する
            if (System.IO.File.Exists(fullOriginalFilePath))
            {
                //リネーム後に名前が重複するデータがないか確認する
                if (System.IO.File.Exists(fullRenamedFilePath))
                {
                    //既に同じ名前のデータが存在した場合、元あったデータを削除する
                    System.IO.File.Delete(fullRenamedFilePath);
                }
                //ファイルを正常に処理できる事が確定したら
                System.IO.File.Move(fullOriginalFilePath, fullRenamedFilePath); //リネーム処理
            }
            else
            {
                Debug.Log(filePath + " is not Exist...");
            }
            //データベースに変更後のファイルを登録する
            var untilLastDotCount = dataNameList[i].LastIndexOf('.');   //はじめから'.'までの文字数。0はじまり
            var dateString = dataNameList[i].Substring(0, untilLastDotCount);
            Debug.Log("date:" + dateString + ", filePath:" + filePath);
            sleepTable.Update(new DbSleepData(dateString, filePath, false));
            Debug.Log("Insert Data to DB." + "path:" + filePath);
        }
        //DBに正しく保存できてるか確認用
        var st = MyDatabase.Instance.GetSleepTable();
        foreach (string path in st.SelectAllOrderByAsc().Select(data => data.file_path))
        {
            Debug.Log("DB All FilePath:" + path);
        }
        yield return null;
    }

    //デバイスから睡眠データを取得する
    IEnumerator GetSleepDataFlow(int dataCount, Action<List<string>> onGetCSVPathList, Action<List<string>> onGetCSVNameList)
    {
        List<string> csvPathList = null;
        List<string> csvNameList = null;
        //デバイスが保持しているデータ件数が1件以上であれば、睡眠データを取得する
        if (dataCount > 0)
        {
            //デバイスに睡眠データがあれば
            //睡眠データ取得
            yield return StartCoroutine(GetSleepData(
                dataCount,
                (List<string> _csvPathList) =>
                {
                    csvPathList = _csvPathList;
                },
                (List<string> _csvNameList) =>
                {
                    csvNameList = _csvNameList;
                }));
        }
        onGetCSVPathList(csvPathList);
        onGetCSVNameList(csvNameList);
    }

    //デバイスから睡眠データを取得する
    IEnumerator GetSleepData(int dataCount, Action<List<string>> onGetCSVPathList, Action<List<string>> onGetCSVNameList)
    {
        //データが存在すれば以下の処理を実行
        Debug.Log("データ取得コマンド");
        CsvHeaderSet(); //GET前に必ず実行する
                        //データ取得開始
        //UpdateDialog.Show("本体から睡眠データを取得しています。\n" + 0 + "/" + dataCount + "件");
        ProgressDialog.Show("本体から睡眠データを取得しています。", dataCount);
        bool isSuccess = false;
        bool isFailed = false;
        List<string> filePathList = new List<string>(); //CSVの添付パスリスト
        List<string> fileNameList = new List<string>(); //CSVのファイル名リスト
        BluetoothManager.Instance.SendCommandId(
            3,
            (string data) =>
            {
                //エラー時
                Debug.Log("GetData:failed:" + data);
                isFailed = true;
            },
            (bool success) =>
            {
                Debug.Log("GetData:commandWrite:" + success);
                if (!success)
                    isFailed = true;
            },
            (string data) =>
            {
                Debug.Log("GetData:commandResponse:" + data);
                var j = Json.Deserialize(data) as Dictionary<string, object>;
                bool success = Convert.ToBoolean(j["KEY2"]);
                if (!success)
                    isFailed = true;
            },
            (string data) =>
            {
                //データ取得情報
                Debug.Log("GetData:success:" + data);
                var json = Json.Deserialize(data) as Dictionary<string, object>;
                int currentDataCount = Convert.ToInt32(json["KEY1"]);   //現在の取得カウント（例：1件取得完了したら1で返される）
                bool isExistNextData = Convert.ToBoolean(json["KEY2"]); //TRUEなら次のデータがある
                bool isEndData = Convert.ToBoolean(json["KEY3"]);       //TRUEなら次のデータはな（Unity側でアプリ処理を行ってから、5秒以内にデータ取得完了応答を返す）
                string csvFilePath = (string)json["KEY4"];              //CSVのパスの添付パス。dataフォルダ以下のパスが返される（例：/1122334455:66/yyyyMMdd/tmp01.csv）
                csvFilePath = csvFilePath.Substring(1);                 //先頭のスラッシュを取り除く
                string csvFileName = (string)json["KEY5"];              //CSVのファイル名。最終的にUnity側でDB登録時にリネームしてもらうファイル名（例：20180624182431.csv）
                filePathList.Add(csvFilePath);
                fileNameList.Add(csvFileName);
                //UpdateDialog.ChangeMessage("本体から睡眠データを取得しています。\n" + currentDataCount + "/" + dataCount + "件");
                ProgressDialog.UpdateProgress(currentDataCount);
                if (isEndData)
                {
                    //最後のデータを取得完了すれば
                    isSuccess = true;
                }
            });
        yield return new WaitUntil(() =>
        {
            return isSuccess || isFailed;
        });
        //UpdateDialog.Dismiss();
        ProgressDialog.Dismiss();
        onGetCSVPathList(filePathList.Count > 0 ? filePathList : null);
        onGetCSVNameList(fileNameList.Count > 0 ? fileNameList : null);
        Debug.Log("Return Get Data");
    }

    //CSVファイル作成時のヘッダ情報をセットする
    //GETコマンド送信前に必須
    void CsvHeaderSet()
    {
        //GETコマンド実行の前準備としてCsvHeaderSetコマンド実行
        string deviceId = UserDataManager.Device.GetPareringDeviceAdress();
        string nickName = UserDataManager.Setting.Profile.GetNickName();
        string sex = UserDataManager.Setting.Profile.GetSex() == UserDataManager.Setting.Profile.Sex.Female
            ? "女性"
            : "男性"; //Unkownの場合は男性になる
        var birthDay = UserDataManager.Setting.Profile.GetBirthDay();
        string birthDayString = birthDay.Year.ToString("0000") + "/" + birthDay.Month.ToString() + "/" + birthDay.Day.ToString();
        string tall = UserDataManager.Setting.Profile.GetBodyLength().ToString("0.0");
        string weight = UserDataManager.Setting.Profile.GetWeight().ToString("0.0");
        string sleepStartTime = UserDataManager.Setting.Profile.GetIdealSleepStartTime().ToString("HH:mm");
        string sleepEndTime = UserDataManager.Setting.Profile.GetIdealSleepEndTime().ToString("HH:mm");
        string g1dVersion = UserDataManager.Device.GetG1DAppVersion();
        BluetoothManager.Instance.CsvHeaderSet(deviceId, nickName, sex, birthDayString, tall, weight, sleepStartTime, sleepEndTime, g1dVersion);
    }


    //デバイスが保持しているデータ件数を取得する
    //取得に失敗した場合はonGetDataCountで-1を返す
    IEnumerator GetDataCountInDevice(Action<int> onGetDataCount)
    {
        UpdateDialog.Show("同期中");
        bool isSuccess = false;
        bool isFailed = false;
        string receiveData = "";
        List<string> csvPathList = null;
        Debug.Log("状態取得コマンド");
        BluetoothManager.Instance.SendCommandId(
            18,
            (string data) =>
            {
                //エラー時
                Debug.Log("failed:" + data);
                isFailed = true;
            },
            (bool success) =>
            {
                Debug.Log("commandWrite:" + success);
                if (!success)
                    isFailed = true;
            },
            (string data) =>
            {
                Debug.Log("commandResponse:" + data);
                isFailed = true;
            },
            (string data) =>
            {
                //デバイス状況取得
                Debug.Log("success:" + data);
                receiveData = data;
                isSuccess = true;
            });
        yield return new WaitUntil(() => isSuccess || isFailed);
        if (isSuccess)
        {
            //デバイス状況取得成功
            var json = Json.Deserialize(receiveData) as Dictionary<string, object>;
            int dataCount = Convert.ToInt32(json["KEY2"]);  //デバイスにたまってる睡眠データの個数
            onGetDataCount(dataCount);
        }
        else
        {
            onGetDataCount(-1);
        }
        UpdateDialog.Dismiss();
    }

    //ファームウェアアップデートを行うのに充分な電池残量がない事をユーザーに伝える
    IEnumerator TellBatteryNotEnoughToFirmwareUpdate()
    {
        bool isOk = false;
        MessageDialog.Show("電池の残量が不足しています。本体機器を充電してから再度行ってください。", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk);
    }

    /// <summary>
    /// ファームウェアアップデートを行えるバッテリー残量があるか確認する
    /// </summary>
    /// <param name="onResponse">結果を受け取るためのコールバック
    /// 第一引数：成功かどうか
    /// 第二引数：電池レベル</param>
    IEnumerator CheckBatteryToFirmwareUpdate(Action<bool, int> onResponse)
    {
        bool isSuccess = false;
        bool isFailed = false;
        string receiveData = "";
        Debug.Log("電池残量取得コマンド");
        BluetoothManager.Instance.SendCommandId(
            7,
            (string data) =>
            {
                //エラー時
                Debug.Log("failed:" + data);
                receiveData = data;
                isFailed = true;
            },
            (bool success) =>
            {
                Debug.Log("commandWrite:" + success);
                if (!success)
                    isFailed = true;
            },
            (string data) =>
            {
                Debug.Log("commandResponse:" + data);
                //falseしか返ってこない
                isFailed = true;
            },
            (string data) =>
            {
                //デバイス状況取得
                Debug.Log("success:" + data);
                receiveData = data;
                isSuccess = true;
            });
        yield return new WaitUntil(() => isSuccess || isFailed);
        if (isSuccess)
        {
            //電池残量取得成功
            var json = Json.Deserialize(receiveData) as Dictionary<string, object>;
            int batteryState = Convert.ToInt32(json["KEY1"]);   //電池残量0~3
            Debug.Log("Success Get BatteryState:" + batteryState);
            onResponse(true, batteryState);
            yield break;
        }
        onResponse(false, -1);
    }

    //ユーザーにファームウェアアップデートを行うかどうか確認する
    IEnumerator AskDoFirmwareUpdate(Action<bool> onResponse)
    {
        bool isOk = false;
        bool isCancel = false;
        MessageDialog.Show(
            "<size=30>最新のファームウェアがあります。\nアップデートしますか？\n※Wi-Fi環境での実行を推奨します。\n※アップデート前に機器内の睡眠データの取得を行います。</size>",
            true,
            true,
            () => isOk = true,
            () => isCancel = true,
            "はい",
            "いいえ");
        yield return new WaitUntil(() => isOk || isCancel);
        onResponse(isOk);
    }

    /// <summary>
    /// ファームウェアアップデートをリトライするかどうかユーザーに尋ねる
    /// </summary>
    /// <param name="onResponse"></param>
    /// <returns></returns>
    IEnumerator AskRetryFirmwareUpdate(Action<bool> onResponse)
    {
        bool isOk = false;
        bool isCancel = false;
        MessageDialog.Show(
            "<size=30>アップデートに失敗しました。\nリトライしますか？</size>",
            true,
            true,
            () => isOk = true,
            () => isCancel = true,
            "はい",
            "いいえ");
        yield return new WaitUntil(() => isOk || isCancel);
        onResponse(isOk);
    }

    /// <summary>
    /// FTPサーバーから最新のファームウェアのファイル名を取得する
    /// </summary>
    /// <param name="firmwareDirectory">G1D・H1Dのディレクトリパス</param>
    /// <param name="onGetFileName">目的のファイル名を受け取るコールバック</param>
    IEnumerator GetLatestFirmwareFileNameFromFtp(string firmwareDirectoryPath, Action<string> onGetFileName, Action<bool> onResponse)
    {
        //FTPサーバー上にファームウェアのディレクトリが存在するか確認する
        int directoryExistResult = -1;
        bool isComplete = false;
        FtpManager.DirectoryExists(firmwareDirectoryPath, (int result) =>
        {
            directoryExistResult = result;
            isComplete = true;
        });
        yield return new WaitUntil(() => isComplete);
        Debug.Log(directoryExistResult == 1 ? "G1D directory is Exist!" : "G1D directory is NotExist...");
        if (directoryExistResult == 1)
        {
            //指定したファームウェアディレクトリの名のファイル名をすべて取得する
            var getAllFirmwareFileNameList = FtpManager.GetAllList(firmwareDirectoryPath);
            yield return getAllFirmwareFileNameList.AsCoroutine();
            List<string> firmwareFileNameList = new List<string>();
            if (getAllFirmwareFileNameList.Result != null)
            {
                //取得したものには、ファイル、ディレクトリ、Linkが混在してるためファイルのみを取り出す
                firmwareFileNameList = getAllFirmwareFileNameList.Result
                    .Where(data => int.Parse(data[0]) == 0) //ファイルのみ通す
                    .Select(data => data[1])        //ファイル名に変換
                    .ToList();
                //ファイルがあるか確認
                if (firmwareFileNameList.Count == 0)
                {
                    onGetFileName(null);
                    onResponse(false);
                    Debug.Log("No firmwareFile.");
                    yield break;
                }
                //ファームウェア以外のファイルをはじく
                firmwareFileNameList = firmwareFileNameList
                    .Where(fileName => fileName.Contains(".bin"))
                    .ToList();
                //ファイルがあるか確認
                if (firmwareFileNameList.Count == 0)
                {
                    onGetFileName(null);
                    onResponse(false);
                    Debug.Log("No firmwareFile.");
                    yield break;
                }
                //取得したディレクトリを確認
                foreach (var fileName in firmwareFileNameList)
                {
                    Debug.Log("GetFile:" + fileName);
                }
            }
            else
            {
                //エラー時
                onGetFileName(null);
                onResponse(true);
                yield break;
            }
            //ファイル名のリストが取得できれば、その中から最新のものを探す
            var ratestVersionFileIndex = firmwareFileNameList
                .Select((fileName, index) => new { FileName = fileName, Index = index })
                .Aggregate((max, current) => (FirmwareFileNameToVersionLong(max.FileName) > FirmwareFileNameToVersionLong(current.FileName) ? max : current))
                .Index;
            onResponse(false);
            onGetFileName(firmwareFileNameList[ratestVersionFileIndex]);
            yield break;
        }
        onResponse(true);
        onGetFileName(null);
    }

    //「000.000.000.000」の形式のバージョンの文字列から整数値に変換する
    long FirmwareVersionStringToLong(string version)
    {
        //ドットを取り除く
        string versionString = version;
        versionString = versionString.Replace(".", "");
        return long.Parse(versionString);
    }

    //ファームウェアのアップデートファイルのフォルダ名からバージョン情報を抜き出して比較しやすいように整数型にして返す。
    //その値が大きいものほど新しいバージョン
    long FirmwareFileNameToVersionLong(string filePath)
    {
        string versionString = filePath;                                                //例：/Update/G1D/RD8001G1D_Ver000.000.000.004.mot
        versionString = versionString.Substring(0, versionString.LastIndexOf('.'));     //例：/Update/G1D/RD8001G1D_Ver000.000.000.004
        versionString = versionString.Substring(versionString.Length - 15);             //例：000.000.000.004
        versionString = versionString.Replace(".", "");                                 //例：000000000004
        return long.Parse(versionString);
    }

    /// <summary>
    /// デバイスのファームウェアバージョンを取得する
    /// </summary>
    /// <param name="onResponse">結果を受け取るためのコールバック
    /// 第一引数：成功かどうか
    /// 第三引数：G1Dアプリバージョン</param>
    IEnumerator GetFirmwareVersionFromDevice(Action<bool, string> onResponse)
    {
        bool isSuccess = false;
        bool isFailed = false;
        string receiveData = "";
        BluetoothManager.Instance.SendCommandId(
            8,
            (string data) =>
            {
                //エラー時
                Debug.Log("failed:" + data);
                //再ペアリング要求
                receiveData = data;
                isFailed = true;
            },
            (bool success) =>
            {
                Debug.Log("commandWrite:" + success);
                if (!success)
                    isFailed = true;
            },
            (string data) =>
            {
                Debug.Log("commandresponce:" + data);
                //falseのみ返ってくる
                //再ペアリング要求
                receiveData = data;
                isFailed = true;
            },
            (string data) =>
            {
                //Ver情報
                Debug.Log("success:" + data);
                receiveData = data;
                isSuccess = true;
            });
        yield return new WaitUntil(() => isSuccess || isFailed);    //応答待ち
        if (isSuccess)
        {
            //データ取得成功時
            var json = Json.Deserialize(receiveData) as Dictionary<string, object>;
            int g1dAppVerMajor = Convert.ToInt32(json["KEY1"]);
            int g1dAppVerMiner = Convert.ToInt32(json["KEY2"]);
            int g1dAppVerRevision = Convert.ToInt32(json["KEY3"]);
            int g1dAppVerBuild = Convert.ToInt32(json["KEY4"]);
            //アプリ内保存
            string g1dAppVer = g1dAppVerMajor.ToString("000") + "." +
                               g1dAppVerMiner.ToString("000") + "." +
                               g1dAppVerRevision.ToString("000") + "." +
                               g1dAppVerBuild.ToString("000");
            onResponse(true, g1dAppVer);
            yield break;
        }
        onResponse(false, null);
        yield return null;
    }

    //ファームウェアアップデートが開始できなかった事をユーザーに伝えるダイアログを表示する
    IEnumerator TellCantStartFirmwareUpdate()
    {
        bool isOK = false;
        MessageDialog.Show("ファームウェアアップデートを開始できませんでした。", true, false, () => isOK = true);
        yield return new WaitUntil(() => isOK);
    }

    //ファームウェアアップデートに失敗した事をユーザーに伝えるダイアログを表示する
    IEnumerator TellFailedFirmwareUpdate()
    {
        bool isOk = false;
        MessageDialog.Show("同期に失敗しました。", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk);
    }

    //ファームウェアのダウンロードに失敗した事をユーザーに伝えるダイアログを表示する
    IEnumerator TellFailedFirmwareDownload()
    {
        bool isOk = false;
        MessageDialog.Show("ファームウェアのダウンロードに失敗しました。\nネットワーク接続を確認の上、再度お試しください。", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk);
    }

    //「バックアップデータの削除」ボタンが押されると呼び出される
    public void OnDeleteBackupData()
    {
        //デバイスとペアリングしていなければ使えないように
        if (UserDataManager.State.isDoneDevicePareing())
        {
            //Ftpサーバーと通信してバックアップデータがあれば削除する
            FtpFunction.DeleteBackupData(this);
        }
        else
        {
            //ペアリングしていなければ
            StartCoroutine(TellNotParering());
        }
    }

    //「お問い合わせ」ボタンが押されると呼び出される
    public void OnHelpButtonTap()
    {
        HelpMailLuncher.Lunch();
    }

    //「利用規約」ボタンが押されると呼び出される
    public void OnTermsOfUseButtonTap()
    {
        PlayerPrefs.SetInt("tapFromSetting", 1);
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.TermsOfUse);
    }

    //「ライセンス情報」ボタンが押されると呼び出される
    public void OnLisenceInfoButtonTap()
    {
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.License);
    }

    //デバッグ用。「デバッグ」ボタンが押されると呼び出される
    public void OnDebugButtonTap()
    {
        SceneManager.LoadScene("Debug");
    }
}

/// <summary>
/// ファーム更新コントロールコマンド
/// </summary>
public enum FirmwareUpdateControlCommand : byte
{
    Start = 0x00,      // 更新開始
    SendComplete,  // 指定したサイズのデータ送信完了
    CheckWrite,     // データ書き込み確認
    SendFinish,    // 全データの送信が完了
    CheckUpdate     // FWアップデート完了確認
}
