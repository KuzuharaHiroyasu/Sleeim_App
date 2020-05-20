using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 睡眠データのCSVファイルヘッダーに記述されたデータ
/// </summary>
public class SleepHeaderData {

    /// <summary>
    /// 日付と時刻
    /// </summary>
    /// <value></value>
    public System.DateTime DateTime { get; }

    /// <summary>
    /// 曜日(整数値)
    /// </summary>
    public int Day { get; }

    /// <summary>
    /// いびき検知数
    /// </summary>
    public int SnoreDetectionCount { get; }

    /// <summary>
    /// 無呼吸検知数
    /// </summary>
    public int ApneaDetectionCount { get; }

    /// <summary>
    /// いびき時間(秒)
    /// </summary>
    public int SnoreTime { get; }

    /// <summary>
    /// 無呼吸時間(秒)
    /// </summary>
    public int ApneaTime { get; }

    /// <summary>
    /// 最高無呼吸時間(秒)
    /// </summary>
    public int LongestApneaTime { get; }

    /// <summary>
    /// 動作モード  0～3 整数(0：振動モード(いびき)、1：振動モード(いびき+無呼吸)、2：モニタリングモード、3：振動モード（無呼吸）)	
    /// </summary>
    public int SleepMode;

    /// <summary>
    /// バイブレーションの強さ	0～3 整数(0：弱、1：中、2：強、3：徐々に強く)
    /// </summary>
    public int VibrationStrength;

    /// <summary>
    /// いびき検出感度 0～3 整数(0：低、1：中、2：高)
    /// </summary>
    public int SnoreSensitivity;

    /// <summary>
    /// 無呼吸検出感度 0～3 整数(現在は設定がないため０固定）							
    /// </summary>
    public int ApneaSensitivity;

    /// <summary>
    /// 睡眠データ長(Max:1440)
    /// </summary>
    public int DataLength { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="sleepRecordStartTimeLine">睡眠記録開始時間</param>
    public SleepHeaderData(string[] sleepRecordStartTimeLine) {
        int indexCnt = 0;
        string date = sleepRecordStartTimeLine[indexCnt++];
        this.Day = int.Parse(sleepRecordStartTimeLine[indexCnt++]);
        string time = sleepRecordStartTimeLine[indexCnt++];
        this.SnoreDetectionCount = int.Parse(sleepRecordStartTimeLine[indexCnt++]);
        this.ApneaDetectionCount = int.Parse(sleepRecordStartTimeLine[indexCnt++]);
        this.SnoreTime = int.Parse(sleepRecordStartTimeLine[indexCnt++]);
        this.ApneaTime = int.Parse(sleepRecordStartTimeLine[indexCnt++]);
        this.LongestApneaTime = int.Parse(sleepRecordStartTimeLine[indexCnt++]);

        if (sleepRecordStartTimeLine.Length > 9) { //New format 
            this.SleepMode = int.Parse(sleepRecordStartTimeLine[indexCnt++]);
            this.VibrationStrength = int.Parse(sleepRecordStartTimeLine[indexCnt++]);
            this.SnoreSensitivity = int.Parse(sleepRecordStartTimeLine[indexCnt++]);
            this.ApneaSensitivity = int.Parse(sleepRecordStartTimeLine[indexCnt++]);
        }

        this.DataLength = int.Parse(sleepRecordStartTimeLine[indexCnt++]);

        string[] dateArr = date.Split('/');
        string[] timeArr = time.Split(':');
        int year = int.Parse(dateArr[0]);
        int month = int.Parse(dateArr[1]);
        int day = int.Parse(dateArr[2]);
        int hour = int.Parse(timeArr[0]);
        int min = int.Parse(timeArr[1]);
        int sec = int.Parse(timeArr[2]);

        try
        {
            this.DateTime = new System.DateTime(year, month, day, hour, min, sec);
        } catch
        {
            this.DateTime = System.DateTime.MinValue;
        }
    }
}
