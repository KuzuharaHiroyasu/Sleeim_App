using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Kaimin.Managers;
using UnityEngine.UI;
using System;
using System.Linq;
using MiniJSON;
using System.Threading;
using System.Threading.Tasks;
using Asyncoroutine;
using Kaimin.Common;

public class HomeViewController : ViewControllerBase
{

    [SerializeField] Text nickNameText = null;			//ニックネーム
    [SerializeField] Button syncButton = null;			//同期ボタン
    [SerializeField] Image deviceIcon = null;			//機器との接続状態を表すアイコン
    [SerializeField] Image batteryIcon = null;			//機器の電池残量を表すアイコン
    [SerializeField] Text dataReceptionTimeText = null;	//最終データ受信時刻
    [SerializeField] Image apneaCount_icon = null;		//無呼吸検知回数の顔アイコン
    [SerializeField] Sprite apneaIcon_normal = null;		//無呼吸検知回数の顔アイコン通常
    [SerializeField] Sprite apneaIcon_nodata = null;		//無呼吸検知回数の顔アイコンデータが一件もないとき
    [SerializeField] Sprite batteryIcon_unkwon = null;
    [SerializeField] Sprite batteryIcon_low = null;
    [SerializeField] Sprite batteryIcon_half = null;
    [SerializeField] Sprite batteryIcon_full = null;
    [SerializeField] Sprite deviceIcon_Connecting = null;
    [SerializeField] Sprite deviceIcon_NotConnecting = null;

    /// <summary>
    /// 動作モードの値
    /// </summary>
    [SerializeField] Text ActionModeValue = null;

    /// <summary>
    /// いびき感度の値
    /// </summary>
    [SerializeField] Text SnoreSensitivityValue = null;

    /// <summary>
    /// 低減強度の値
    /// </summary>
    [SerializeField] Text SuppressionStrengthValue = null;

    /// <summary>
    /// いびき低減の連続時間の値
    ///
    /// NOTE: 旧「低減動作最大継続時間」
    /// </summary>
    [SerializeField] Text SuppressionOperationMaxTimeValue = null;

    /// <summary>
    /// 低減開始時間の値
    /// </summary>
    //[SerializeField] Text SuppressionStartTimeValue = null;

    /// <summary>
    /// 期間テキスト
    /// </summary>
    [SerializeField] Text Period = null;

    /// <summary>
    /// 睡眠時間の値
    /// </summary>
    [SerializeField] Text SleepTimeValue = null;

    /// <summary>
    /// いびき検知数の値
    /// </summary>
    [SerializeField] Text SnoreCountValue = null;

    /// <summary>
    /// 無呼吸検知数の値
    /// </summary>
    [SerializeField] Text ApneaCountValue = null;

    protected override void Start()
    {
        base.Start();
        //ホーム画面をロードした事を記録する
        UserDataManager.State.SaveLoadHomeScene();
        //ホーム画面でデバイス接続が切断された際に、デバイスアイコンに反映できるよう設定
        DeviceStateManager.Instance.OnDeviceDisConnectEvent += UpdateDeviceIcon;
        //ホーム画面でペアリングが解除された際に、同期ボタンに反映できるよう設定
        DeviceStateManager.Instance.OnDevicePareringDisConnectEvent += UpdateSyncButton;
        //ニックネーム設定
        UpdateNicknameDisp();
        //デバイス関連設定
        UpdateSyncButton();
        UpdateDeviceIcon();
        UpdateBatteryIcon();
        UpdateDataReceptionTime();
        //無呼吸検知関連設定
        UpdateApneaCountIcon();
        UpdateApneaCountDate();

        UpdateDeviceSetting();
    }

    public override SceneTransitionManager.LoadScene SceneTag
    {
        get
        {
            return SceneTransitionManager.LoadScene.Home;
        }
    }

    void OnDisable()
    {
        //初めに登録したデバイス接続のコールバック登録を解除
        DeviceStateManager.Instance.OnDeviceDisConnectEvent -= UpdateDeviceIcon;
        //ペアリング解除のコールバック登録を解除
        DeviceStateManager.Instance.OnDevicePareringDisConnectEvent -= UpdateSyncButton;
    }

    //ニックネーム設定
    void UpdateNicknameDisp()
    {
        nickNameText.text = UserDataManager.Setting.Profile.GetNickName();
    }

    //同期ボタンを更新
    void UpdateSyncButton()
    {
        //機器とペアリングしてない場合使用不可にする
        bool isPareringDevice = UserDataManager.State.isDoneDevicePareing();
        syncButton.interactable = isPareringDevice;
    }

    //機器との接続状態を表すアイコンを更新
    void UpdateDeviceIcon()
    {
        bool isConnecting = UserDataManager.State.isConnectingDevice();
        deviceIcon.sprite = isConnecting ? deviceIcon_Connecting : deviceIcon_NotConnecting;
    }

    //機器の電池残量を表すアイコンを更新
    void UpdateBatteryIcon()
    {
        switch (UserDataManager.Device.GetBatteryState())
        {
            case 0:
                batteryIcon.sprite = batteryIcon_full;
                break;
            case 1:
                batteryIcon.sprite = batteryIcon_half;
                break;
            case 2:
                batteryIcon.sprite = batteryIcon_low;
                break;
            default:
                batteryIcon.sprite = batteryIcon_unkwon;
                break;
        }
    }

    //最終データ受信時刻を更新
    void UpdateDataReceptionTime()
    {
        bool isExistData = UserDataManager.State.GetDataReceptionTime() != DateTime.MinValue;
        DateTime time = UserDataManager.State.GetDataReceptionTime();
        string dispText = time.Month.ToString("00") + "/" + time.Day.ToString("00") + " " + time.Hour.ToString("00") + ":" + time.Minute.ToString("00");
        dataReceptionTimeText.text = isExistData ? dispText : "-";	//データがなければハイフンを表示
    }

    //睡眠データをリソースのCSVファイルから取得
    List<SleepData> GetLatestSleepDatas()
    {
        Debug.Log("GetLatestSleepDates");
        Debug.Log("GsDataPath:" + Kaimin.Common.Utility.GsDataPath());
        Debug.Log("GetAllFile_Count:" + Kaimin.Common.Utility.GetAllFiles(Kaimin.Common.Utility.GsDataPath(), "*.csv").Count());
        string[] _filepath = Kaimin.Common.Utility.GetAllFiles(Kaimin.Common.Utility.GsDataPath(), "*.csv");
        if (_filepath != null && _filepath.Length != 0)
        {
            int _selectIndex = _filepath.Length - 1;	//最新のファイルを取得
            return CSVSleepDataReader.GetSleepDatas(_filepath[_selectIndex]);			//睡眠データをCSVから取得する
        }
        else
        {
            return null;
        }
    }

    SleepHeaderData GetLatestSleepHeaderData()
    {
        string[] _filepath = Kaimin.Common.Utility.GetAllFiles(Kaimin.Common.Utility.GsDataPath(), "*.csv");
        if (_filepath != null && _filepath.Length != 0)
        {
            int _selectIndex = _filepath.Length - 1;	//最新のファイルを取得
            return CSVSleepDataReader.GetSleepHeaderData(_filepath[_selectIndex]);			//睡眠データをCSVから取得する
        }
        else
        {
            return null;
        }
    }

    //就寝日を文字列で返す。ここでどのようなスタイルで表示するか決定する
    String GetSleepDateForText(DateTime startTime, DateTime endTime, int dateIndex, int crossSunCount, int sameDateNum, int crossSunNum)
    {
        //就寝時
        string start_year = startTime.Year.ToString();
        string start_month = startTime.Month.ToString("00");
        string start_day = startTime.Day.ToString("00");
        string start_dayOfWeek = startTime.ToString("ddd", new System.Globalization.CultureInfo("ja-JP"));	//曜日
        //起床時
        string end_year = endTime.Year.ToString();
        string end_month = endTime.Month.ToString("00");
        string end_day = endTime.Day.ToString("00");
        string end_dayOfWeek = endTime.ToString("ddd", new System.Globalization.CultureInfo("ja-JP"));	//曜日

        if (CSVManager.isCrossTheSun(startTime, endTime))
        {
            //就寝時と起床時の日付が異なっていたら「就寝日～起床日」を返す
            bool isNecessaryIndex = crossSunNum > 1;
            int indexCount = crossSunCount;
            return start_year + "/" + start_month + "/" + start_day + "(" + start_dayOfWeek + ")" + "\n"
                + "▼" + "\n"
                + end_year + "/" + end_month + "/" + end_day + "(" + end_dayOfWeek + ")"
                + (isNecessaryIndex ? " (" + indexCount.ToString() + ")" : "");
        }
        else
        {
            //就寝時と起床時の日付が同じであれば「就寝日」を返す
            bool isNecessaryIndex = (sameDateNum - crossSunNum) > 1;
            int indexCount = dateIndex + 1;
            return start_year + "/" + start_month + "/" + start_day + "(" + start_dayOfWeek + ")" + (isNecessaryIndex ? " (" + indexCount.ToString() + ")" : "");
        }
    }

    //無呼吸検知回数の表示のアイコンをデータの有無によって変更
    void UpdateApneaCountIcon()
    {
        apneaCount_icon.sprite = GetLatestSleepDatas() == null ? apneaIcon_nodata : apneaIcon_normal;
    }

    //無呼吸検知回数で表示するデータの日時を更新
    void UpdateApneaCountDate()
    {
        string dateText = "-";
        string sleepTime = "-";
        if (GetLatestSleepDatas() != null)
        {
            List<SleepData> latestSleepDatas = GetLatestSleepDatas();				//最新の睡眠データのリスト
            SleepHeaderData latestSleepHeaderData = GetLatestSleepHeaderData();	//最新の睡眠データのヘッダーデータ
            UpdateApneaCountSnoreCountValue(latestSleepHeaderData);     // 無呼吸検知数といびき検知数を画面に反映する
            DateTime startTime = latestSleepHeaderData.DateTime;
            DateTime endTime = latestSleepDatas.Select(data => data.GetDateTime()).Last();

            string[] _filepath = Kaimin.Common.Utility.GetAllFiles(Kaimin.Common.Utility.GsDataPath(), "*.csv");

            DateTime from = new DateTime(startTime.Year, startTime.Month, startTime.Day, 0, 0, 0);
            DateTime to = new DateTime(startTime.Year, startTime.Month, startTime.Day, 23, 59, 59);
            List<string> todayDataPathList = PickFilePathInPeriod(_filepath, from, to).Where(path => IsSameDay(startTime, Utility.TransFilePathToDate(path))).ToList();
            int dateIndex = todayDataPathList
                .Select((path, index) => new { Path = path, Index = index })
                .Where(data => Utility.TransFilePathToDate(data.Path) == startTime)
                .Select(data => data.Index)
                .First();								//同一日の何個目のデータか(0はじまり)
            int crossSunCount = todayDataPathList
                .Take(dateIndex + 1)
                .Where(path => CSVManager.isCrossTheSun(startTime, ReadSleepDataFromCSV(path).Last().GetDateTime()))
                .Count();								//現在のデータまでの日マタギデータの個数
            int sameDateNum = todayDataPathList.Count;	//同一日のすべてのデータ個数
            int crossSunNum = todayDataPathList
                .Where(path => CSVManager.isCrossTheSun(startTime, ReadSleepDataFromCSV(path).Last().GetDateTime()))
                .Count();
            dateText = GetSleepDateForText(startTime, endTime, dateIndex, crossSunCount, sameDateNum, crossSunNum);

            // 睡眠時間取得処理
            // NOTE: 時間操作系メソッドが何故かGraphネームスペースのクラスに定義されているため、止むを得ずこのような使い方をしている
            int sleepTimeSec = Graph.Time.GetDateDifferencePerSecond(startTime, endTime);
            sleepTime = Graph.Time.CreateHMSString(sleepTimeSec);
        }
        Period.text = dateText;
        SleepTimeValue.text = sleepTime;
    }

    //睡眠データのファイル一覧から指定した期間のもののみを取得
    List<string> PickFilePathInPeriod(string[] sleepFilePathList, DateTime from, DateTime to)
    {
        return sleepFilePathList.Where(path => (from == DateTime.MinValue || Utility.TransFilePathToDate(path).CompareTo(from) >= 0) && (to == DateTime.MaxValue || Utility.TransFilePathToDate(path).CompareTo(to) <= 0)).ToList();
    }

    bool IsSameDay(DateTime date1, DateTime date2)
    {
        if (date1.Year != date2.Year)
            return false;
        if (date1.Month != date2.Month)
            return false;
        if (date1.Day != date2.Day)
            return false;
        return true;
    }

    //睡眠データをリソースのCSVファイルから取得します
    List<SleepData> ReadSleepDataFromCSV(string filepath)
    {
        return CSVSleepDataReader.GetSleepDatas(filepath);
    }

    /// <summary>
    /// いびき検知数と無呼吸検知数を更新する
    /// </summary>
    void UpdateApneaCountSnoreCountValue(SleepHeaderData sleepHeaderData)
    {
        string countText = "";
        string snoreCountText = "";
        if (GetLatestSleepDatas() == null)
        {
            countText = "-";		//データが一件もなければブランク
            snoreCountText = "-";
        }
        else
        {
            countText = sleepHeaderData.ApneaDetectionCount.ToString();
            snoreCountText = sleepHeaderData.SnoreDetectionCount.ToString();
        }
        ApneaCountValue.text = countText;
        SnoreCountValue.text = snoreCountText;
    }

    /// <summary>
    /// デバイス設定の表示を更新する
    /// </summary>
    private void UpdateDeviceSetting()
    {
        DeviceSetting deviceSetting = UserDataManager.Setting.DeviceSettingData.Load();
        switch (deviceSetting.ActionMode)
        {
            case ActionMode.SuppressModeIbiki:
                ActionModeValue.text = "低減モード(いびき)";
                break;
            case ActionMode.SuppressMode:
                ActionModeValue.text = "低減モード(いびき+無呼吸)";
                break;
            case ActionMode.MonitoringMode:
                ActionModeValue.text = "モニタリング";
                break;
            case ActionMode.SuppressModeMukokyu:
                ActionModeValue.text = "低減モード（無呼吸）";
                break;
            default:
                // 何もしない
                break;
        }

        switch (deviceSetting.SnoreSensitivity)
        {
            case SnoreSensitivity.Low:
                SnoreSensitivityValue.text = "弱";
                break;
            case SnoreSensitivity.Mid:
                SnoreSensitivityValue.text = "中";
                break;
            case SnoreSensitivity.High:
                SnoreSensitivityValue.text = "強";
                break;
            default:
                // 何もしない
                break;
        }

        switch (deviceSetting.SuppressionStrength)
        {
            case SuppressionStrength.Low:
                SuppressionStrengthValue.text = "弱";
                break;
            case SuppressionStrength.Mid:
                SuppressionStrengthValue.text = "中";
                break;
            case SuppressionStrength.High:
                SuppressionStrengthValue.text = "強";
                break;
            case SuppressionStrength.HighGrad:
                SuppressionStrengthValue.text = "徐々に強";
                break;
            default:
                // 何もしない
                break;
        }

        switch (deviceSetting.SuppressionOperationMaxTime)
        {
            case SuppressionOperationMaxTime.FiveMin:
                SuppressionOperationMaxTimeValue.text = "5分";
                break;
            case SuppressionOperationMaxTime.TenMin:
                SuppressionOperationMaxTimeValue.text = "10分";
                break;
            case SuppressionOperationMaxTime.NoSettings:
                SuppressionOperationMaxTimeValue.text = "設定なし";
                break;
            default:
                // 何もしない
                break;
        }

        if (deviceSetting.SuppressionStartTime >= 0)
        {
            //SuppressionStartTimeValue.text = deviceSetting.SuppressionStartTime + "分";
        }
    }

    //グラフに遷移するためのボタンをタップした際に実行
    public void OnToGraphButtonTap()
    {
        //最新データに遷移するように設定
        UserDataManager.Scene.SaveGraphDate(DateTime.MinValue);
        //タブは初期状態で選択されるように設定
        UserDataManager.Scene.InitGraphTabSave();
        UserDataManager.Scene.InitGraphDataTabSave();
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Graph);
    }

    /// <summary>
    /// デバイス設定ボタン押下イベントハンドラ
    /// </summary>
    public void OnDeviceSettingButtonTap()
    {
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.DeviceSetting);
    }

    //同期ボタンを押した際に実行
    public void OnSyncButtonTap()
    {
        StartCoroutine(SyncDevice());
    }

    //デバイスと同期をとる
    IEnumerator SyncDevice()
    {
        //Ble通信部分実行
        yield return StartCoroutine(SyncDeviceBleFlow());
        //未アップロードのCsvファイルが存在すれば、アップロードする

        //取得したデータをFTPにアップロードする
        yield return StartCoroutine (HttpManager.UploadUnsendDatas());
    }

    //デバイスとの同期のBLE通信関連部分
    //実行途中にエラーが起こった際に以降のBle通信処理を全て行わないようにするためにBle処理をまとめてる
    IEnumerator SyncDeviceBleFlow()
    {
        //Bluetoothが有効かのチェックを行う
        bool isBluetoothActive = false;
        yield return StartCoroutine(BluetoothActiveCheck((bool isActive) => isBluetoothActive = isActive));
        if (!isBluetoothActive)
        {
            yield break;	//接続エラー時に以降のBle処理を飛ばす
        }
        //デバイスと接続
        if (!UserDataManager.State.isConnectingDevice())
        {
            string deviceName = UserDataManager.Device.GetPareringDeviceName();
            string deviceAdress = UserDataManager.Device.GetPareringBLEAdress();
            bool isDeviceConnectSuccess = false;
            yield return StartCoroutine(DeviceConnect(deviceName, deviceAdress, (bool isSuccess) => isDeviceConnectSuccess = isSuccess));
            Debug.Log("Connecting_Result:" + isDeviceConnectSuccess);
            if (!isDeviceConnectSuccess)
            {
                //デバイス接続に失敗すれば
                yield break;
            }
            //デバイスアイコンの表示を更新する
            UpdateDeviceIcon();
        }
        UpdateDialog.Show("同期中");
        //デバイス時刻補正
        bool isCorrectTimeSuccess = false;
        DateTime correctDeviceTime = DateTime.MinValue;
        yield return StartCoroutine(CorrectDeviceTime((bool isSuccess, DateTime correctTime) =>
        {
            isCorrectTimeSuccess = isSuccess;
            correctDeviceTime = correctTime;
        }));
        if (!isCorrectTimeSuccess)
        {
            UpdateDialog.Dismiss();
            yield return StartCoroutine(TellFailedSync());
            yield break;	//エラーが発生した場合は以降のBle処理を飛ばす
        }
        //電池残量を取得
        bool isGetBatteryStateSuccess = false;
        yield return StartCoroutine(GetBatteryState((bool isSuccess) => isGetBatteryStateSuccess = isSuccess));
        if (!isGetBatteryStateSuccess)
        {
            UpdateDialog.Dismiss();
            yield return StartCoroutine(TellFailedSync());
            yield break;	//エラーが発生した場合は以降のBle処理を飛ばす
        }
        //電池アイコン表示更新
        UpdateBatteryIcon();
        UpdateDialog.Dismiss();
        //デバイスに睡眠データがある場合、取得するかどうかユーザに尋ねる
        bool isOk = false;
        int getDataCount = -1;
        List<string> csvPathList = null;
        List<string> csvNameList = null;
        yield return StartCoroutine(AskGetData(
            (bool _isOk) => isOk = _isOk,
            (int _getDataCount) => getDataCount = _getDataCount));
        if (getDataCount == -1)
        {	//エラー発生時
            yield return StartCoroutine(TellFailedSync());
            yield break;	//以降のBle処理を飛ばす
        }
        if (getDataCount == 0)
        {	//データが0件の時
            //同期時刻を保存
            UserDataManager.State.SaveDataReceptionTime(DateTime.Now);
            //同期時刻表示更新
            UpdateDataReceptionTime();
            yield break;
        }
        if (!isOk)
        {		//睡眠データを取得しないなら
            yield break;	//以降のBle処理を飛ばす
        }
        //睡眠データを取得
        yield return StartCoroutine(GetSleepDataFlow(
            getDataCount,
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
            yield return StartCoroutine(Utility.RegistDataToDB(csvPathList, csvNameList));
            //DBに取得したデータの登録が完了。送信完了コマンドを送信する
            yield return StartCoroutine(FinishGetData());
            //同期時刻を保存(端末の現在時刻を保存して表示させる)
            UserDataManager.State.SaveDataReceptionTime(DateTime.Now);
            //同期時刻表示更新
            UpdateDataReceptionTime();
            //無呼吸検知関連設定
            UpdateApneaCountIcon();
            UpdateApneaCountDate();
            UpdateDialog.Dismiss();
            //データ取得完了のダイアログ表示
            if (csvPathList.Count > 0)
                yield return StartCoroutine(TellGetDataComplete(csvPathList.Count));
        }
        else
        {
            //睡眠データの取得に失敗すれば
            yield return StartCoroutine(TellFailedSync());
        }
    }

    //デバイス時刻取得
    IEnumerator GetDeviceTime(Action<bool, DateTime> onResponse)
    {
        Debug.Log("GetDeviceTime");
        bool isSuccess = false;
        bool isFailed = false;
        string receiveData = "";
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
                Debug.Log("commandresponce:" + data);
                //falseのみ返ってくる
                isFailed = true;
            },
            (string data) =>
            {
                //Ver情報
                Debug.Log("success:" + data);
                receiveData = data;
                isSuccess = true;
            });
        yield return new WaitUntil(() => isSuccess || isFailed);	//応答待ち
        if (isSuccess)
        {
            if (receiveData != "")
            {
                //デバイス情報取得成功時
                var json = Json.Deserialize(receiveData) as Dictionary<string, object>;
                int year = Convert.ToInt32(json["KEY3"]);		//年
                int month = Convert.ToInt32(json["KEY4"]);	//月
                int date = Convert.ToInt32(json["KEY5"]);		//曜日
                int day = Convert.ToInt32(json["KEY6"]);		//日
                int hour = Convert.ToInt32(json["KEY7"]);		//時
                int minute = Convert.ToInt32(json["KEY8"]);	//分
                int second = Convert.ToInt32(json["KEY9"]);	//秒
                //でたらめなデータでエラーにならないように処理
                string deviceTimeString = "20" + year.ToString("00") + "/" + month.ToString("00") + "/" + day.ToString("00") + " " + hour.ToString("00") + ":" + minute.ToString("00") + ":" + second.ToString("00");
                Debug.Log("deviceTime :" + deviceTimeString);
                DateTime deviceTime;
                if (DateTime.TryParse(deviceTimeString, out deviceTime))
                {
                    Debug.Log("success DateTime parse");
                    //デバイス時刻が正常に取得できたとき
                    onResponse(true, deviceTime);
                    yield break;
                }
                else
                {
                    //デバイス時刻が取得できたが、正常な値でないとき
                    onResponse(true, DateTime.MinValue);
                    yield break;
                }
            }
        }
        onResponse(false, DateTime.MinValue);
    }

    //デバイス接続の流れ
    IEnumerator DeviceConnect(string deviceName, string deviceAdress, Action<bool> onResponse)
    {
        UpdateDialogAddButton.Show(deviceName + "に接続しています。",
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
            "キャンセル");
        bool isSuccess = false;	//接続成功
        bool isFailed = false;	//接続失敗
        string receiveData = "";		//デバイス接続で成功・失敗時に受け取るデータ（JSONにパースして使用）
        string uuid = "";       //ペアリング中のデバイスのUUID(iOSでのみ必要)
#if UNITY_IOS
        uuid = UserDataManager.Device.GetPareringDeviceUUID ();
#endif

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
        yield return new WaitUntil(() => isSuccess || isFailed);	//応答待ち
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
            UpdateDialogAddButton.Dismiss();
        }
        else
        {
            //接続失敗時
            var json = Json.Deserialize(receiveData) as Dictionary<string, object>;
            int error1 = Convert.ToInt32(json["KEY1"]);
            int error2 = Convert.ToInt32(json["KEY2"]);
            UpdateDialogAddButton.Dismiss();
#if UNITY_IOS
            if (error2 == -8) {
            //iOSの_reconnectionPeripheralコマンドでのみ返ってくる、これ以降接続できる見込みがないときのエラー
            Debug.Log ("Connect_OcuurSeriousError");
            //接続を解除
            UserDataManager.State.SaveDeviceConnectState (false);
            //接続が解除された事を伝える
            DeviceStateManager.Instance.OnDeviceDisConnect ();
            //ペアリングを解除
            UserDataManager.State.ClearDeviceParering ();
            //ペアリングが解除された事を伝える
            DeviceStateManager.Instance.OnDevicePrearingDisConnect ();
            //接続に失敗した旨のダイアログを表示
            yield return StartCoroutine (TellFailedConnect (deviceName));
            //再度ペアリングを要求するダイアログを表示
            yield return StartCoroutine (TellNeccesaryParering ());
            onResponse (false);
            yield break;
            }
#endif
            if (error2 == -3)	//何らかの原因で接続できなかった場合(タイムアウト含む)
                Debug.Log("OccurAnyError");
            else if (error2 == -4)	//接続が切れた場合(GATTサーバには接続できたが、サービスまで全て接続できないと接続完了にはならない。)
                Debug.Log("DisConnectedError");
            //接続に失敗した旨のダイアログを表示
            yield return StartCoroutine(TellFailedConnect(deviceName));
        }
        onResponse(isSuccess);
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

    //デバイスにデータがある場合、睡眠データを取得するかどうかのダイアログを表示させる
    IEnumerator AskGetData(Action<bool> onSelectButton, Action<int> onGetDataCount)
    {
        //デバイスのデータ件数を取得する
        int dataCount = -1;
        yield return StartCoroutine(GetDataCountInDevice((int _dataCount) => { dataCount = _dataCount; }));
        if (dataCount > 0)
        {
            bool isOk = false;
            bool isCancel = false;
            MessageDialog.Show(
                dataCount + "件の睡眠データがあります。\n取得しますか？",
                true,
                true,
                () => isOk = true,
                () => isCancel = true,
                "はい",
                "いいえ");
            yield return new WaitUntil(() => isOk || isCancel);
            onSelectButton(isOk);
            onGetDataCount(dataCount);
        }
        else
        {
            onSelectButton(false);
            onGetDataCount(dataCount);
        }
    }

    //デバイスからデータを取得完了した事をユーザに伝えるダイアログを表示させる
    IEnumerator TellGetDataComplete(int getDataCount)
    {
        bool isOk = false;
        MessageDialog.Show("<size=30>" + getDataCount + "件の睡眠データを取得しました。</size>", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk);
    }

    //デバイスが保持しているデータ件数を取得する
    //取得に失敗した場合はonGetDataCountで-1を返す
    IEnumerator GetDataCountInDevice(Action<int> onGetDataCount)
    {
        bool isSuccess = false;
        bool isFailed = false;
        string receiveData = "";
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
            int dataCount = Convert.ToInt32(json["KEY2"]);	//デバイスにたまってる睡眠データの個数
            onGetDataCount(dataCount);
        }
        else
        {
            onGetDataCount(-1);
        }
    }

    //同期に失敗した事をユーザーに伝えるダイアログを表示する
    IEnumerator TellFailedSync()
    {
        bool isOk = false;
        MessageDialog.Show("同期に失敗しました。", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk);
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

    //CSVファイル作成時のヘッダ情報をセットする
    //GETコマンド送信前に必須
    void CsvHeaderSet()
    {
        //GETコマンド実行の前準備としてCsvHeaderSetコマンド実行
        string deviceId = UserDataManager.Device.GetPareringDeviceAdress();
        string nickName = UserDataManager.Setting.Profile.GetNickName();
        string sex = UserDataManager.Setting.Profile.GetSex() == UserDataManager.Setting.Profile.Sex.Female
            ? "女性"
            : "男性";	//Unkownの場合は男性になる
        var birthDay = UserDataManager.Setting.Profile.GetBirthDay();
        string birthDayString = birthDay.Year.ToString("0000") + "/" + birthDay.Month.ToString() + "/" + birthDay.Day.ToString();
        string tall = UserDataManager.Setting.Profile.GetBodyLength().ToString("0.0");
        string weight = UserDataManager.Setting.Profile.GetWeight().ToString("0.0");
        string sleepStartTime = UserDataManager.Setting.Profile.GetIdealSleepStartTime().ToString("HH:mm");
        string sleepEndTime = UserDataManager.Setting.Profile.GetIdealSleepEndTime().ToString("HH:mm");
        string g1dVersion = UserDataManager.Device.GetG1DAppVersion();
        BluetoothManager.Instance.CsvHeaderSet(deviceId, nickName, sex, birthDayString, tall, weight, sleepStartTime, sleepEndTime, g1dVersion);
    }

    //デバイスから睡眠データを取得する
    IEnumerator GetSleepData(int dataCount, Action<List<string>> onGetCSVPathList, Action<List<string>> onGetCSVNameList)
    {
        //スリープしないように設定
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        //データが存在すれば以下の処理を実行
        Debug.Log("データ取得コマンド");
        CsvHeaderSet();	//GET前に必ず実行する
        //データ取得開始
        UpdateDialog.Show("本体から睡眠データを取得しています。\n" + 0 + "/" + dataCount + "件");
        bool isSuccess = false;
        bool isFailed = false;
        List<string> filePathList = new List<string>();	//CSVの添付パスリスト
        List<string> fileNameList = new List<string>();	//CSVのファイル名リスト
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
                var json = Json.Deserialize(data) as Dictionary<string, object>;
                int currentDataCount = Convert.ToInt32(json["KEY1"]);	//現在の取得カウント（例：1件取得完了したら1で返される）
                bool isExistNextData = Convert.ToBoolean(json["KEY2"]);	//TRUEなら次のデータがある
                bool isEndData = Convert.ToBoolean(json["KEY3"]);		//TRUEなら次のデータはな（Unity側でアプリ処理を行ってから、5秒以内にデータ取得完了応答を返す）
                string csvFilePath = (string)json["KEY4"];				//CSVのパスの添付パス。dataフォルダ以下のパスが返される（例：/1122334455:66/yyyyMMdd/tmp01.csv）
                csvFilePath = csvFilePath.Substring(1);					//先頭のスラッシュを取り除く
                string csvFileName = (string)json["KEY5"];				//CSVのファイル名。最終的にUnity側でDB登録時にリネームしてもらうファイル名（例：20180624182431.csv）
                filePathList.Add(csvFilePath);
                fileNameList.Add(csvFileName);
                UpdateDialog.ChangeMessage("本体から睡眠データを取得しています。\n" + currentDataCount + "/" + dataCount + "件");
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
        //スリープ設置解除
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
        UpdateDialog.Dismiss();
        onGetCSVPathList(filePathList.Count > 0 ? filePathList : null);
        onGetCSVNameList(fileNameList.Count > 0 ? fileNameList : null);
        Debug.Log("Return Get Data");
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

    //機器時刻を補正する
    IEnumerator CorrectDeviceTime(Action<bool, DateTime> onResponse)
    {
        //ＮＴＰサーバから時刻を取得してきて、
        DateTime correctTime = DateTime.MinValue;	//補正のための正しい時間
        float timeout = 5f;		//タイムアウト時間設定
        float timeCounter = 0;	//タイムアウト計測のためのカウンタ
        NTP.Instance.GetTimeStamp((DateTime? time) =>
        {
            if (time != null)
            {
                //NTP時刻が取得できれば
                correctTime = time.Value;
            }
        });
        yield return new WaitUntil(() =>
        {
            timeCounter += Time.deltaTime;
            //タイムアウトまたは、サーバから時刻が取得できれば抜ける
            return timeCounter > timeout || correctTime != DateTime.MinValue;
        });
        bool isTimeout = timeCounter > timeout;
        if (isTimeout)
        {
            //スマホ時刻を補正のための正しい時間として使用する
            correctTime = System.DateTime.Now;
        }
        //デバイス時刻取得
        bool isGetDeviceTimeSuccess = false;
        DateTime deviceTime = DateTime.MinValue;
        yield return StartCoroutine(GetDeviceTime((bool _isGetDeviceTimeSuccess, DateTime _deviceTime) =>
        {
            isGetDeviceTimeSuccess = _isGetDeviceTimeSuccess;
            deviceTime = _deviceTime;
        }));
        if (!isGetDeviceTimeSuccess)
        {
            //デバイス時刻の取得に失敗すれば、これ以降の処理を行わない
            onResponse(false, correctTime);
            yield break;
        }
        else
        {
            //デバイスの時刻取得に成功すれば、機器の時刻と正しい時刻を比較
            if (deviceTime == DateTime.MinValue)
            {
                //デバイス時刻がでたらめな値だった場合、無条件に時刻設定を行う
                yield return StartCoroutine(SetDeviceTime(correctTime, onResponse));
            }
            else
            {
                //デバイス時刻が正常な値なら、正確な時間との比較を行う
                var timeDiff = deviceTime - correctTime;
                Debug.Log("DeviceTime:" + deviceTime + ",CorrectTime:" + correctTime + ",TimeDiffMinute:" + timeDiff.TotalMinutes);
                if (Mathf.Abs(Mathf.Abs((float)timeDiff.TotalMinutes)) >= 30f)
                {	//時間差が30分以上であれば
                    Debug.Log("TimeDiff 30min over");
                    yield return StartCoroutine(SetDeviceTime(correctTime, onResponse));
                }
                else
                {
                    Debug.Log("TimeDiff 30min under");
                    onResponse(true, correctTime);
                }
            }
        }
    }

    //デバイスの時刻を設定する
    IEnumerator SetDeviceTime(DateTime time, Action<bool, DateTime> onResponse)
    {
        //機器の時刻補正を行う
        Debug.Log("Set Device Date " + time);
        string dateString = time.Year.ToString("0000") + "/" + time.Month.ToString("00") + "/" + time.Day.ToString("00") + " " +
            time.Hour.ToString("00") + ":" + time.Minute.ToString("00") + ":" + time.Second.ToString("00");
        bool isSuccess = false;
        bool isFailed = false;
        BluetoothManager.Instance.SendCommandDate(
            dateString,
            (string data) =>
            {
                //エラー時
                Debug.Log("SendCommandDate-OnError:" + data);
                isFailed = true;
            },
            (bool success) =>
            {
                if (!success)
                    isFailed = true;
            },
            (string data) =>
            {
                //応答結果
                Debug.Log("SendCommandDate-OnResponse:" + data);
                var json = Json.Deserialize(data) as Dictionary<string, object>;
                bool success = Convert.ToBoolean(json["KEY2"]);
                if (success)
                    isSuccess = true;
                else
                    isFailed = true;
            });
        yield return new WaitUntil(() => isSuccess || isFailed);
        onResponse(isSuccess, time);
        yield return null;
    }

    //電池残量を取得する
    IEnumerator GetBatteryState(Action<bool> onResponse)
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
            int batteryState = Convert.ToInt32(json["KEY1"]);	//電池残量0~3
            //電池残量を記録
            //バッテリー残量で3が返ってきた場合は、ありえない？ため2に変換する
            int _batteryState = batteryState == 3 ? 2 : batteryState;
            UserDataManager.Device.SaveBatteryState(_batteryState);
            Debug.Log("Success Get BatteryState:" + batteryState);
        }
        onResponse(isSuccess);
        yield return null;
    }
}
