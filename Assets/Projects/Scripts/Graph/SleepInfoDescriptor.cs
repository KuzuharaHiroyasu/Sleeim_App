using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Graph;
using System.Linq;
using System;
using UniRx;
using UnityEngine.UI;

/// <summary>
/// 睡眠のデータを取得し、詳細欄に必要な情報を表示させるためのクラス
/// </summary>
public class SleepInfoDescriptor : MonoBehaviour {

    /// <summary>
    /// 睡眠データの取得元
    /// </summary>
    public GraphItem Input;

    /// <summary>
    /// 表示する情報を持っておく
    /// </summary>
    SleepDataDetail Data;

    /// <summary>
    /// 日付
    /// </summary>
    //public Text DateText;

    /// <summary>
    /// 就寝時間
    /// </summary>
    public Text[] BedTimeText;

    /// <summary>
    /// 起床時間
    /// </summary>
    public Text[] GetUpTimeText;

    /// <summary>
    /// 睡眠時間 (2か所あるため複数個指定できるように)
    /// </summary>
    public Text[] SleepTimeText;

    /// <summary>
    /// いびき時間
    /// </summary>
    public Text SnoreTimeText;

    /// <summary>
    /// いびき回数
    /// </summary>
    public Text SnoreCount;

    /// <summary>
    /// 振動でいびきが止まった回数
    /// </summary>
    public Text SnoreStopCount;

    /// <summary>
    /// いびき割合
    /// </summary>
    public Text SnoreRateText;

    /// <summary>
    /// 無呼吸時間
    /// </summary>
    public Text ApneaTimeText;

    /// <summary>
    /// 無呼吸回数
    /// </summary>
    public Text ApneaCountText;

    /// <summary>
    /// 無呼吸最長継続時間
    /// </summary>
    public Text LongestApneaTimeText;

    /// <summary>
    /// 無呼吸平均回数(時)
    /// </summary>
	public Text ApneaAverageCount;

	/// <summary>
	/// 動作モード
	/// </summary>
	public Text ActionModeText;



    /// <summary>
    /// 睡眠時間
    /// </summary>
    private String SleepTime {
        get {
            //睡眠時間を秒に変換して取得
            int sec = Graph.Time.GetDateDifferencePerSecond(Data.BedTime, Data.GetUpTime);
            return Graph.Time.CreateHMSString(sec);
        }
    }

    /// <summary>
    /// 無呼吸検知回数
    /// </summary>
    private String ApneaCount {
        get {
            return Data.ApneaCount.ToString () + "<size=24>回</size>";
        }
    }

    /// <summary>
    /// 最高無呼吸時間
    /// </summary>
    private String LongestApneaTime {
        get {
            return Data.LongestApneaTime.ToString () + "<size=24>秒</size>";
        }
    }

    /// <summary>
    /// いびき割合
    /// </summary>
    private String SnoreRate {
        get {
            double rate = Data.SnoreRate;
            int per = (int)(rate * 10) % 10;
            return ((int) Data.SnoreRate).ToString () + "." + per.ToString() + "<size=24>％</size>";
        }
    }

    void Awake () {
        Input.OnGraphDataChange.Subscribe (_ => {
            //グラフに表示するデータが変更された際に実行される
            this.Data = Input.GetSleepInfoData ();
            DescriptInfoToDetails();
        });
    }

    //日付
    String GetDateText(int dateIndex, int crossSunCount, int sameDateNum, int crossSunNum) {
        DateTime startTime = Data.BedTime;
        DateTime endTime = CSVManager.getRealDateTime(Data.GetUpTime);

        if (CSVManager.isCrossTheSun (startTime, endTime)) {
            //就寝時と起床時の日付が異なっていたら「就寝日～起床日」を返す
            bool isNecessaryIndex = crossSunNum > 1;
            int indexCount = crossSunCount;
            return
                CSVManager.getJpDateString(startTime) +
                " ～ " +
                CSVManager.getJpDateString(endTime) + (isNecessaryIndex ? " (" + indexCount.ToString () + ")" : "");
        } else {
            startTime = CSVManager.getRealDateTime(startTime);

            if (CSVManager.isInvalidDate(startTime)) return "ー";

            //就寝時と起床時の日付が同じであれば「就寝日」を返す
            bool isNecessaryIndex = (sameDateNum - crossSunNum) > 1;
            int indexCount = dateIndex + 1;
            return CSVManager.getJpDateString(startTime) + (isNecessaryIndex ? " (" + indexCount.ToString () + ")" : "");
        }
    }

    bool isSameDay (DateTime date1, DateTime date2) {
        if (date1.Year != date2.Year)
            return false;
        if (date1.Month != date2.Month)
            return false;
        if (date1.Day != date2.Day)
            return false;
        return true;
    }

    //詳細欄に情報を設定する
    void DescriptInfoToDetails () {
        Input.graphDateText = GetDateText(
            Data.DateIndex,
            Data.CrossSunCount,
            Data.SameDateNum,
            Data.CrossSunNum);
        foreach (var bedTimeText in BedTimeText) {
            bedTimeText.text = CSVManager.TransTimeToHHMM(Data.BedTime);
        }
        foreach (var getUpTimeText in GetUpTimeText) {
            getUpTimeText.text = CSVManager.TransTimeToHHMM(Data.GetUpTime);
        }
        SetSleepTimes ();
        ApneaCountText.text = ApneaCount;
        LongestApneaTimeText.text = LongestApneaTime;
        SnoreRateText.text = SnoreRate;
        SnoreTimeText.text = Graph.Time.CreateHMSString(Data.SnoreTimeSec);
        ApneaTimeText.text = Graph.Time.CreateHMSString(Data.ApneaTime);
        SnoreCount.text = Data.SnoreCount.ToString() + "<size=24>回</size>";
        if(Data.SnoreStopCount >= Data.SnoreCount && Data.SnoreStopCount >= 0)
        {
            SnoreStopCount.text = SnoreCount.text;
        } else
        {
            SnoreStopCount.text = (Data.SnoreStopCount < 0 ? "-" : Data.SnoreStopCount.ToString()) + "<size=24>回</size>";
        }
        
        ApneaAverageCount.text = Data.ApneaAverageCount.ToString() + "<size=24>回</size>";

        switch (Data.SleepMode)
        {
            case 0:
            ActionModeText.text = "振動モード(いびき)";
            break;
            case 1:
            ActionModeText.text = "振動(いびき+呼吸レス)";
            break;
            case 2:
            ActionModeText.text = "モニタリングモード";
            break;
            default:
            ActionModeText.text = "振動モード(呼吸レス)";
            break;
        }
    }

    //睡眠時間をテキストに設定する
    void SetSleepTimes () {
        foreach (Text sleepTime_Text in SleepTimeText) {
            sleepTime_Text.text = SleepTime;
        }
    }
}

public interface ISleepInfo {
    SleepDataDetail GetSleepInfoData ();
}
