﻿using System.Collections;
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

public class HomeNewViewController : ViewControllerBase
{
    public Text nickNameText = null; //ニックネーム
    public Text dataReceptionTimeText = null; //最終データ受信時刻
    public Image batteryIcon = null; //機器の電池残量を表すアイコン
    public Image deviceIcon = null;  //機器との接続状態を表すアイコン
    public Button syncButton = null;		  //データ取得(同期)ボタン

    public Image actionModeIcon = null;
    public Image suppressionStrengthIcon = null;

    public Text sleepTimeText = null; //睡眠時間
    public Text sleepDateText = null; //睡眠日付 (選択したグラフの日付)

    //PieChart
    public Color[] pieColors; //Colors of Fumei, Mukokyu, Ibiki, Kaimin
    public Image piePrefab;
    public PieInfo pieInfo;
    public GameObject circleOuter;

    [SerializeField] Sprite batteryIcon_unknown = null;
    [SerializeField] Sprite batteryIcon_low = null;
    [SerializeField] Sprite batteryIcon_half = null;
    [SerializeField] Sprite batteryIcon_full = null;
    [SerializeField] Sprite deviceIcon_Connecting = null;
    [SerializeField] Sprite deviceIcon_NotConnecting = null;

    [SerializeField] Sprite actionModeIcon_monitor = null;
    [SerializeField] Sprite actionModeIcon_suppress = null;
    [SerializeField] Sprite suppressStrengthIcon_weak = null;
    [SerializeField] Sprite suppressStrengthIcon_mid = null;
    [SerializeField] Sprite suppressStrengthIcon_high = null;
    [SerializeField] Sprite suppressStrengthIcon_multi = null;

    public Button btnPrev;
    public Button btnNext;
    string[] filePaths;
    int selectFilePosition = -1;
    int MIN_FILE_POSITION = 0;
    int MAX_FILE_POSITION = -1;

    // Use this for initialization
    protected override void Start() {
        base.Start();
        //ホーム画面をロードした事を記録する
        UserDataManager.State.SaveLoadHomeScene();
        //ホーム画面でデバイス接続が切断された際に、デバイスアイコンに反映できるよう設定
        DeviceStateManager.Instance.OnDeviceDisConnectEvent += UpdateDeviceIcon;
        //ホーム画面でペアリングが解除された際に、同期ボタンに反映できるよう設定
        DeviceStateManager.Instance.OnDevicePareringDisConnectEvent += UpdateSyncButton;

        GetSetFilePaths(); //Important

        this.btnPrev.GetComponent<Button>().onClick.AddListener(delegate { this.onClickPrevBtn(); });
        this.btnNext.GetComponent<Button>().onClick.AddListener(delegate { this.onClickNextBtn(); });

        //ニックネーム設定
        UpdateNicknameDisp();

        //デバイス関連設定
        UpdateSyncButton();
        UpdateDeviceIcon();
        UpdateBatteryIcon();
        UpdateDataReceptionTime();

        //無呼吸検知関連設定
        //UpdateApneaCountIcon();
        //UpdateApneaCountDate();
        UpdateLatestPieChart();

        //Recalculate MIN_FILE_POSITION
        for (int i = 0; i <= MAX_FILE_POSITION; i++)
        {
            List<SleepData> sleepDatas = CSVSleepDataReader.GetSleepDatas(filePaths[i]); //睡眠データのリスト
            SleepHeaderData sleepHeaderData = CSVSleepDataReader.GetSleepHeaderData(filePaths[i]); //睡眠データのヘッダーデータ

            if (sleepHeaderData != null && sleepDatas != null && sleepDatas.Count > 0)
            {
                MIN_FILE_POSITION = i; //ファイルを取得
                break;
            }
        }
        updatePrevNextBtnState();

        UpdateDeviceSetting();
    }

    // Update is called once per frame
    void Update() {

    }

    public void GetSetFilePaths()
    {
        string dataPath = Kaimin.Common.Utility.GsDataPath();
        filePaths = Kaimin.Common.Utility.GetAllFiles(dataPath, "*.csv");
        if (filePaths != null)
        {
            MAX_FILE_POSITION = filePaths.Length - 1;
        }
    }

    public override SceneTransitionManager.LoadScene SceneTag
    {
        get
        {
            return SceneTransitionManager.LoadScene.Home;
        }
    }

    public void onClickNextBtn()
    {
        if (filePaths != null && selectFilePosition < MAX_FILE_POSITION)
        {
            selectFilePosition++;
            UpdatePieChart(selectFilePosition, false);
            updatePrevNextBtnState();
        }
    }

    public void onClickPrevBtn()
    {
        if(filePaths != null && selectFilePosition > MIN_FILE_POSITION)
        {
            selectFilePosition--;
            UpdatePieChart(selectFilePosition, true);
            updatePrevNextBtnState();
        }
    }

    public void updatePrevNextBtnState()
    {
        if (filePaths == null || MAX_FILE_POSITION <= 0 || MIN_FILE_POSITION == MAX_FILE_POSITION)
        {
            this.btnPrev.interactable = false;
            this.btnNext.interactable = false;
        } else
        {
            this.btnPrev.interactable = false;
            this.btnNext.interactable = false;
            if (selectFilePosition > MIN_FILE_POSITION)
            {
                this.btnPrev.interactable = true;
            } 
            
            if (selectFilePosition < MAX_FILE_POSITION)
            {
                this.btnNext.interactable = true;
            }
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

    //データ取得(同期)ボタンを更新
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
                batteryIcon.sprite = batteryIcon_unknown;
                break;
        }
    }

    //最終データ受信時刻を更新
    void UpdateDataReceptionTime()
    {
        if (dataReceptionTimeText == null)
        {
            return;
        }

        DateTime time = UserDataManager.State.GetDataReceptionTime();
        bool isExistData = time != DateTime.MinValue;

        if (isExistData) {
            dataReceptionTimeText.text = time.Month.ToString("00") + "/" + time.Day.ToString("00") + " " + time.Hour.ToString("00") + ":" + time.Minute.ToString("00");
        } else {
            dataReceptionTimeText.text = "";	//データがなければハイフンを表示
        }
    }

    /// <summary>
    /// デバイス設定の表示を更新する
    /// </summary>
    private void UpdateDeviceSetting()
    {
        DeviceSetting deviceSetting = UserDataManager.Setting.DeviceSettingData.Load();

        if (deviceSetting.ActionMode == ActionMode.MonitoringMode)
        {
            actionModeIcon.sprite = actionModeIcon_monitor;
            suppressionStrengthIcon.sprite = actionModeIcon_monitor;
        } else
        {
            actionModeIcon.sprite = actionModeIcon_suppress;

            switch (deviceSetting.SuppressionStrength)
            {
                case SuppressionStrength.Low:
                    suppressionStrengthIcon.sprite = suppressStrengthIcon_weak;
                    break;
                case SuppressionStrength.Mid:
                    suppressionStrengthIcon.sprite = suppressStrengthIcon_mid;
                    break;
                case SuppressionStrength.High:
                    suppressionStrengthIcon.sprite = suppressStrengthIcon_high;
                    break;
                case SuppressionStrength.HighGrad:
                    suppressionStrengthIcon.sprite = suppressStrengthIcon_multi;
                    break;
                default:
                    // 何もしない
                    break;
            }
        }
    }

    public void UpdateLatestPieChart()
    {
        selectFilePosition = MAX_FILE_POSITION; //Important
        this.UpdatePieChart(selectFilePosition, true);

        MAX_FILE_POSITION = selectFilePosition; //Update max position
        updatePrevNextBtnState();
    }


    public void UpdatePieChart(int filePosition, bool isDescOrder = true)
    {
        List<SleepData> sleepDatas = null;
        SleepHeaderData sleepHeaderData = null;
        ChartInfo chartInfo = null;

        try
        {
            if (filePaths != null && MIN_FILE_POSITION <= filePosition && filePosition <= MAX_FILE_POSITION)
            {
                //Get latest valid file 
                int _selectIndex = -1; 
                if(isDescOrder)
                {
                    for (int i = filePosition; i >= MIN_FILE_POSITION; i--)
                    {
                        sleepDatas = CSVSleepDataReader.GetSleepDatas(filePaths[i]); //睡眠データのリスト
                        sleepHeaderData = CSVSleepDataReader.GetSleepHeaderData(filePaths[i]); //睡眠データのヘッダーデータ

                        if (sleepHeaderData != null && sleepDatas != null && sleepDatas.Count > 0)
                        {
                            _selectIndex = i; //ファイルを取得
                            break;
                        }
                    }
                } else
                {
                    for (int i = filePosition; i <= MAX_FILE_POSITION; i++)
                    {
                        sleepDatas = CSVSleepDataReader.GetSleepDatas(filePaths[i]); //睡眠データのリスト
                        sleepHeaderData = CSVSleepDataReader.GetSleepHeaderData(filePaths[i]); //睡眠データのヘッダーデータ

                        if (sleepHeaderData != null && sleepDatas != null && sleepDatas.Count > 0)
                        {
                            _selectIndex = i; //ファイルを取得
                            break;
                        }
                    }
                }

                if (_selectIndex >= 0)
                {
                    selectFilePosition = _selectIndex;

                    DateTime startTime = sleepHeaderData.DateTime;
                    DateTime endTime = sleepDatas.Select(data => data.GetDateTime()).Last();

                    UserDataManager.Scene.SaveGraphDate(sleepHeaderData.DateTime); //Used to move to graph when call OnToGraphButtonTap()

                    //Step1: Update SleepTime
                    int sleepTimeSec = Graph.Time.GetDateDifferencePerSecond(startTime, endTime);
                    System.TimeSpan ts = new System.TimeSpan(hours: 0, minutes: 0, seconds: sleepTimeSec);
                    int hourWithDay = 24 * ts.Days + ts.Hours;      // 24時間超えた場合の時間を考慮
                    string sleepTime = string.Format("{0:00}:{1:00}", hourWithDay, ts.Minutes);
                    sleepTimeText.text = sleepTime;

                    DateTime fileDateTime = Kaimin.Common.Utility.TransFilePathToDate(filePaths[_selectIndex]);
                    DateTime realDateTime = CSVManager.getRealDateTime(fileDateTime);
                    sleepDateText.text = CSVManager.isInvalidDate(realDateTime) ? "-" : CSVManager.getJpDateString(realDateTime);

                    //Step2: Show pie chart
                    chartInfo = CSVManager.convertSleepDataToChartInfo(sleepDatas);
                    if (chartInfo != null)
                    {
                        double p1 = System.Math.Round((double)(chartInfo.pKaiMin * 100), 1);
                        double p2 = System.Math.Round((double)(chartInfo.pIbiki * 100), 1);
                        double p3 = System.Math.Round((double)(chartInfo.pMukokyu * 100), 1);
                        double p4 = 100 - p1 - p2 - p3;
                        p4 = p4 < 0.1 ? 0 : System.Math.Round(p4, 1);

                        //p1 = 5; p2 = 6; p3 = 7; p4 = 82;
                        //p1 = 95; p2 = 3; p3 = 2; p4 = 0;
                        double[] pieValues = new double[4] { p1, p2, p3, p4 }; //Percents of pKaiMin, Ibiki, Mukokyu, Fumei
                        string[] pieLabels = new string[4] { "快眠", "いびき", "呼吸レス", "不明" };
                        makePieChart(pieValues, pieLabels);
                    }

                    //Step 3: Change color of CircleOuter by sleepLevel (睡眠レベルによって色を変える)
                    //レベル１ 無呼吸平均回数(時)が５回以上
                    //レベル２ いびき割合が50％以上
                    //レベル３ いびき割合が25％以上
                    //レベル４ 睡眠時間が７時間未満
                    //レベル５ 上記すべての項目を満たしていない
                    int sleepLevel = 5; //Default
                    int apneaCount = sleepHeaderData.ApneaDetectionCount;
                    double sleepTimeTotal = endTime.Subtract(startTime).TotalSeconds;
                    //無呼吸平均回数(時)
                    double apneaAverageCount = sleepTimeTotal == 0 ? 0 : (double)(apneaCount * 3600) / sleepTimeTotal;  // 0除算を回避
                    apneaAverageCount = Math.Truncate(apneaAverageCount * 10) / 10.0;   // 小数点第2位以下を切り捨て
                    if (apneaAverageCount >= 5) {
                        sleepLevel = 1;
                    } else if (chartInfo.pIbiki >= 0.5) {
                        sleepLevel = 2;
                    } else if (chartInfo.pIbiki >= 0.25) {
                        sleepLevel = 3;
                    } else if (sleepTimeTotal < 7 * 3600) {
                        sleepLevel = 4;
                    }

                    //System.Random random = new System.Random();
                    //sleepLevel = random.Next(0, 5);
                    String[] levelColors = new String[5] { "#ff0000", "#ff6600", "#ffff4d", "#72ef36", "#0063dc" };
                    circleOuter.GetComponent<Image>().color = convertHexToColor(levelColors[sleepLevel-1]);
                }
            }
        } catch (System.Exception e) {
        }

        if (chartInfo == null) //No data 
        {
            //circleOuter.SetActive(false);
            circleOuter.GetComponent<Image>().color = convertHexToColor("#0063dc"); //Default is level 5
            pieInfo.hidePieInfo();
            piePrefab.fillAmount = 0;
            sleepTimeText.text = "-";
            sleepDateText.text = "";
        }
    }

    public void makePieChart(double[] pieValues, string[] pieLabels)
    {
        double total = 0f;
        int numPie = pieValues.Length;
        for (int i = 0; i < numPie; i++)
        {
            total += pieValues[i];
        }

        if (total > 0 && pieColors.Length >= numPie)
        {
            //Rotate image to top middle
            float zRotation = 180f;
            float zPieInfoRotation = 180f;
            float drawAngleTotal = 0;
            
            for (int i = 0; i < numPie; i++)
            {
                pieInfo.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, zPieInfoRotation));
                pieInfo.hidePieInfo();

                float fillAmount = (float)(pieValues[i] / total);
                Image image = Instantiate(piePrefab) as Image;
                image.transform.SetParent(circleOuter.transform, false);
                image.color = pieColors[i];
                image.fillAmount = fillAmount;
                image.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, zRotation));

                int[] position = getPositionByPercent(fillAmount * 100, drawAngleTotal);
                PieInfo p = Instantiate(pieInfo) as PieInfo;
                p.transform.SetParent(image.transform, false);
                p.transform.localPosition = new Vector3(position[0], position[1] + 189, 0);
                if (pieValues[i] > 0)
                {
                    p.drawPieInfo(pieLabels[i], pieValues[i]);
                }

                float rotateAngle = fillAmount * 360;
                zPieInfoRotation += rotateAngle;
                zRotation -= rotateAngle;
                drawAngleTotal += rotateAngle;
            }
        }
    }

    public int[] getPositionByPercent(double percent, float drawAngleTotal)
    {
        var dict = new Dictionary<double, int[]>();
        dict[0]    = new int[2] { -2, -320 };
        dict[0.25] = new int[2] { -3, -320 };
        dict[0.5]  = new int[2] { -4, -320 };
        dict[1]    = new int[2] { -5, -320 };
        dict[1.5]  = new int[2] { -7, -320 };
        dict[2]    = new int[2] { -9, -320 };
        dict[2.5]  = new int[2] { -10, -320 };
        dict[3]    = new int[2] { -11, -320 };
        dict[4]    = new int[2] { -15, -320 };

        if ((45 <= drawAngleTotal && drawAngleTotal <= 135) || (225 <= drawAngleTotal && drawAngleTotal <= 315))
        {
            dict[5] = new int[2] { -28, -325 };
            dict[6] = new int[2] { -35, -325 };
            dict[7] = new int[2] { -35, -325 };
            dict[7.5] = new int[2] { -35, -325 };
            dict[8] = new int[2] { -35, -325 };
            dict[9] = new int[2] { -35, -325 };
        } else {
            dict[5] = new int[2] { -19, -335 };
            dict[6] = new int[2] { -24, -335 };
            dict[7] = new int[2] { -28, -335 };
            dict[7.5] = new int[2] { -29, -335 };
            dict[8] = new int[2] { -32, -335 };
            dict[9] = new int[2] { -35, -335 };
        }

        dict[10] = new int[2] { -40, -320 };
        dict[11] = new int[2] { -43, -320 };
        dict[12] = new int[2] { -46, -320 };
        dict[13] = new int[2] { -50, -320 };
        dict[14] = new int[2] { -55, -315 };
        dict[15] = new int[2] { -60, -310 };
        dict[16] = new int[2] { -64, -310 };
        dict[17] = new int[2] { -68, -305 };
        dict[18] = new int[2] { -70, -300 };
        dict[19] = new int[2] { -72, -300 };

        dict[20] = new int[2] { -74, -295 };
        dict[21] = new int[2] { -77, -290 };
        dict[22] = new int[2] { -78, -288 };
        dict[23] = new int[2] { -80, -288 };
        dict[24] = new int[2] { -82, -286 };
        dict[25] = new int[2] { -86, -283 };
        dict[26] = new int[2] { -88, -278 };
        dict[27] = new int[2] { -89, -273 };
        dict[28] = new int[2] { -89, -271 };
        dict[29] = new int[2] { -91, -268 };

        dict[30] = new int[2] { -93, -265 };
        dict[31] = new int[2] { -95, -263 };
        dict[32] = new int[2] { -95, -257 };
        dict[33] = new int[2] { -97, -253 };
        dict[34] = new int[2] { -97, -251 };
        dict[35] = new int[2] { -97, -248 };
        dict[36] = new int[2] { -97, -245 };
        dict[37] = new int[2] { -99, -243 };
        dict[38] = new int[2] { -101, -241 };
        dict[39] = new int[2] { -101, -239 };

        dict[40] = new int[2] { -101, -236 };
        dict[41] = new int[2] { -103, -233 };
        dict[42] = new int[2] { -105, -230 };
        dict[43] = new int[2] { -107, -227 };
        dict[44] = new int[2] { -109, -224 };
        dict[45] = new int[2] { -109, -221 };
        dict[46] = new int[2] { -111, -218 };
        dict[47] = new int[2] { -112, -215 };
        dict[48] = new int[2] { -114, -212 };
        dict[49] = new int[2] { -115, -209 };

        dict[50] = new int[2] { -115, -205 };
        dict[51] = new int[2] { -118, -200 };
        dict[52] = new int[2] { -120, -196 };
        dict[53] = new int[2] { -122, -192 };
        dict[54] = new int[2] { -122, -188 };
        
        dict[55] = new int[2] { -122, -184 };
        dict[56] = new int[2] { -122, -180 };
        dict[57] = new int[2] { -122, -176 };
        dict[58] = new int[2] { -122, -172 };
        dict[59] = new int[2] { -122, -168 };

        dict[60] = new int[2] { -122, -164 };
        dict[61] = new int[2] { -122, -160 };
        dict[62] = new int[2] { -122, -156 };
        dict[63] = new int[2] { -122, -152 };
        dict[64] = new int[2] { -122, -148 };
        dict[65] = new int[2] { -122, -144 };
        dict[66] = new int[2] { -120, -140 };
        dict[67] = new int[2] { -118, -136 };
        dict[68] = new int[2] { -116, -132 };
        dict[69] = new int[2] { -112, -128 };

        dict[70] = new int[2] { -108, -124 };
        dict[71] = new int[2] { -106, -120 };
        dict[72] = new int[2] { -106, -116 };
        dict[73] = new int[2] { -106, -112 };
        dict[74] = new int[2] { -104, -108 };
        dict[75] = new int[2] { -100, -104 };
        dict[76] = new int[2] { -98, -100 };
        dict[77] = new int[2] { -96, -96 };
        dict[78] = new int[2] { -92, -92 };
        dict[79] = new int[2] { -88, -88 };

        dict[80] = new int[2] { -84, -84 };
        dict[81] = new int[2] { -80, -80 };
        dict[82] = new int[2] { -76, -76 };
        dict[83] = new int[2] { -74, -74 };
        dict[84] = new int[2] { -72, -72 };
        dict[85] = new int[2] { -70, -70 };
        dict[86] = new int[2] { -68, -70 };
        dict[87] = new int[2] { -66, -69 };
        dict[88] = new int[2] { -64, -69 };
        dict[89] = new int[2] { -61, -69 };

        dict[90] = new int[2] { -58, -69 };
        dict[91] = new int[2] { -54, -69 };
        dict[92] = new int[2] { -50, -69 };
        dict[93] = new int[2] { -46, -69 };
        dict[94] = new int[2] { -42, -69 };
        dict[95] = new int[2] { -38, -69 };
        dict[96] = new int[2] { -32, -69 };
        dict[97] = new int[2] { -26, -69 };
        dict[98] = new int[2] { -21, -69 };
        dict[99] = new int[2] { -11, -69 };
        dict[99.5] = new int[2] { -8, -69 };
        dict[100] = new int[2] { -0, -69 };
        
        //key1 <= percent <= key2
        double key1 = 0;
        double key2 = 0;
        foreach (KeyValuePair<double, int[]> entry in dict)
        {
            if(percent <= entry.Key) 
            {
                key2 = entry.Key;
                break;
            } else {
                key1 = entry.Key;
            }
        }

        return (key2 - percent > percent - key1) ? dict[key1] : dict[key2];
    }

    //「ニックネーム」ボタンが押されると呼び出される
    public void OnProfileButtonTap()
    {
        PlayerPrefs.SetInt("tapFromHome", 1);
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Profile);
    }

    //グラフに遷移するためのボタンをタップした際に実行
    public void OnToGraphButtonTap()
    {
        //現在データに遷移するように設定
        UserDataManager.Scene.SaveGraphDate(UserDataManager.Scene.GetGraphDate());
        
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
        PlayerPrefs.SetInt("tapFromHome", 1);
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.DeviceSetting);
    }

    public void OnActionModeButtonTap()
    {
        PlayerPrefs.SetInt("tapFromHome", 1);
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.DeviceSetting);
    }

    public void OnVibrationButtonTap()
    {
        PlayerPrefs.SetInt("tapFromHome", 1);
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.DeviceSetting);
    }

    //同期ボタンを押した際に実行
    public void OnSyncButtonTap()
    {
        StartCoroutine(SyncDevice());
    }

    public void onDeviceIconTap()
    {
        StartCoroutine(ConnectOrDisConnectDevice());
    }

    IEnumerator ConnectOrDisConnectDevice()
    {
        bool isConnecting = UserDataManager.State.isConnectingDevice();
        if (isConnecting)
        {
            //デバイスとの接続を切る
            BluetoothManager.Instance.Disconnect();
            deviceIcon.sprite = deviceIcon_NotConnecting;
        }
        else
        {
            //デバイスと接続
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
                yield break;    //エラーが発生した場合は以降のBle処理を飛ばす
            }

            //電池残量を取得
            bool isGetBatteryStateSuccess = false;
            yield return StartCoroutine(GetBatteryState((bool isSuccess) => isGetBatteryStateSuccess = isSuccess));
            if (!isGetBatteryStateSuccess)
            {
                UpdateDialog.Dismiss();
                yield return StartCoroutine(TellFailedSync());
                yield break;    //エラーが発生した場合は以降のBle処理を飛ばす
            }
            //電池アイコン表示更新
            UpdateBatteryIcon();
            UpdateDialog.Dismiss();
        }
    }

    /*** Private functions ***/
    //デバイスと同期をとる
    IEnumerator SyncDevice()
    {
        //Ble通信部分実行
        yield return StartCoroutine(SyncDeviceBleFlow());
        //未アップロードのCsvファイルが存在すれば、アップロードする

        //取得したデータをHttpでアップロードする
        yield return StartCoroutine (HttpManager.UploadUnsendDatasByHttp());
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
        }, true));
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
            //UpdateApneaCountIcon();
            //UpdateApneaCountDate();

            GetSetFilePaths(); //Re-update file paths before updating latest pie chart
            UpdateLatestPieChart();
            
            UserDataManager.Scene.ResetGraphDate();

            //データ取得完了後、自動的にBLE接続を切る
            bool isConnecting = UserDataManager.State.isConnectingDevice();
            if (isConnecting)
            {
                BluetoothManager.Instance.Disconnect();
                deviceIcon.sprite = deviceIcon_NotConnecting;
            }

            UpdateDialog.Dismiss();
            //データ取得完了のダイアログ表示
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
    public IEnumerator TellGetDataComplete(int getDataCount)
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
    public IEnumerator GetSleepDataFlow(int dataCount, Action<List<string>> onGetCSVPathList, Action<List<string>> onGetCSVNameList)
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
        LoadingDialog.Show("本体から睡眠データを取得しています。\n" + 0 + "/" + dataCount + "件");
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
                int currentDataCount = Convert.ToInt32(json["KEY1"]);		//現在の取得カウント（例：1件取得完了したら1で返される）
                bool isExistNextData = Convert.ToBoolean(json["KEY2"]);	//TRUEなら次のデータがある
                bool isEndData = Convert.ToBoolean(json["KEY3"]);			//TRUEなら次のデータはな（Unity側でアプリ処理を行ってから、5秒以内にデータ取得完了応答を返す）
                string csvFilePath = (string)json["KEY4"];				//CSVのパスの添付パス。dataフォルダ以下のパスが返される（例：/1122334455:66/yyyyMMdd/tmp01.csv）
                csvFilePath = csvFilePath.Substring(1);					//先頭のスラッシュを取り除く
                string csvFileName = (string)json["KEY5"];				//CSVのファイル名。最終的にUnity側でDB登録時にリネームしてもらうファイル名（例：20180624182431.csv）
                filePathList.Add(csvFilePath);
                fileNameList.Add(csvFileName);
                LoadingDialog.ChangeMessage("本体から睡眠データの" + dataCount + "件のうち" + currentDataCount + "件目取得中");
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
        LoadingDialog.Dismiss();
        onGetCSVPathList(filePathList.Count > 0 ? filePathList : null);
        onGetCSVNameList(fileNameList.Count > 0 ? fileNameList : null);
        Debug.Log("Return Get Data");
    }

    //デバイスからの睡眠データ取得処理を終了した事をデバイスに伝える
    public IEnumerator FinishGetData()
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
    IEnumerator CorrectDeviceTime(Action<bool, DateTime> onResponse, bool forceCorrectFlag = false)
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

            if (forceCorrectFlag)
            {
                yield return StartCoroutine(SetDeviceTime(correctTime, onResponse));
            }
            
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
    public IEnumerator GetBatteryState(Action<bool> onResponse)
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

    public Color convertHexToColor(String htmlValue)
    {
        Color newCol;
        if (!ColorUtility.TryParseHtmlString(htmlValue, out newCol))
        {
            newCol = Color.blue;
        }

        return newCol;
    }
}
