using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// センシングした睡眠データを格納するクラス
/// CsvReader.csに渡す事によって、データが格納される
/// </summary>
public class SleepData
{
    /// <summary>
    /// いびきの大きさ最大値
    /// </summary>
    public static readonly int MaxSnoreVolume = 800;

    /// <summary>
    /// 睡眠データCSVファイルのそれぞれのデータのカラム番号
    /// </summary>
    private enum ColumnIndex
    {
        Date = 0,           // 日付
        Day,                // 曜日
        Time,               // 時分秒
        BreathState1,       // 呼吸状態1
        BreathState2,       // 呼吸状態2
        BreathState3,       // 呼吸状態3
        SleepStage,         // 睡眠ステージ
        SnoreVolume1,       // いびきの大きさ1
        SnoreVolume2,       // いびきの大きさ2
        SnoreVolume3,       // いびきの大きさ3
        NeckOrientation1,   // 首の向き1
        NeckOrientation2,   // 首の向き2
        NeckOrientation3,   // 首の向き3
        PhotoSensor1,       // フォトセンサー1
        PhotoSensor2,       // フォトセンサー2
        PhotoSensor3        // フォトセンサー3
    }

    /// <summary>
    /// 日付(Y/M/D)
    /// </summary>
    [CsvColumnAttribute((int)ColumnIndex.Date, defaultValue: "")]
    string date;

    /// <summary>
    /// 時間(HH:MM:SS)
    /// </summary>
    [CsvColumnAttribute((int)ColumnIndex.Time, defaultValue: "")]
    string time;

    /// <summary>
    /// 呼吸状態1
    /// </summary>
    [CsvColumnAttribute((int)ColumnIndex.BreathState1, defaultValue: -1)]
    public int BreathState1 { get; set; }

    /// <summary>
    /// 呼吸状態2
    /// </summary>
    [CsvColumnAttribute((int)ColumnIndex.BreathState2, defaultValue: -1)]
    public int BreathState2 { get; set; }

    /// <summary>
    /// 呼吸状態3
    /// </summary>
    [CsvColumnAttribute((int)ColumnIndex.BreathState3, defaultValue: -1)]
    public int BreathState3 { get; set; }

    /// <summary>
    /// いびきの大きさ1
    /// </summary>
    [CsvColumnAttribute((int)ColumnIndex.SnoreVolume1, defaultValue: -1)]
    public int SnoreVolume1 { get; set; }

    /// <summary>
    /// いびきの大きさ2
    /// </summary>
    [CsvColumnAttribute((int)ColumnIndex.SnoreVolume2, defaultValue: -1)]
    public int SnoreVolume2 { get; set; }

    /// <summary>
    /// いびきの大きさ3
    /// </summary>
    [CsvColumnAttribute((int)ColumnIndex.SnoreVolume3, defaultValue: -1)]
    public int SnoreVolume3 { get; set; }

    /// <summary>
    /// 首の向き1
    /// </summary>
    [CsvColumnAttribute((int)ColumnIndex.NeckOrientation1, defaultValue: -1)]
    public int NeckOrientation1 { get; set; }

    /// <summary>
    /// 首の向き2
    /// </summary>
    [CsvColumnAttribute((int)ColumnIndex.NeckOrientation2, defaultValue: -1)]
    public int NeckOrientation2 { get; set; }

    /// <summary>
    /// 首の向き3
    /// </summary>
    [CsvColumnAttribute((int)ColumnIndex.NeckOrientation3, defaultValue: -1)]
    public int NeckOrientation3 { get; set; }

    //呼吸状態
    public enum BreathState
    {
        Normal,	// 正常
        Snore,	// いびき状態
        Apnea,	// 完全無呼吸
        Empty	// 空き
    }

    //首の向き
    public enum HeadDir
    {
        Left,	//左向き
        Up,		//上向き
        Right,	//右向き
        Down	//下向き
    }

    /// <summary>
    /// 日時(year/month/day/hour/min/sec)を取得します
    /// </summary>
    public System.DateTime GetDateTime()
    {
        string[] dateArr = date.Split('/');
        string[] timeArr = time.Split(':');
        int year = int.Parse(dateArr[0]);
        int month = int.Parse(dateArr[1]);
        int day = int.Parse(dateArr[2]);
        int hour = int.Parse(timeArr[0]);
        int min = int.Parse(timeArr[1]);
        int sec = int.Parse(timeArr[2]);
        return new System.DateTime(year, month, day, hour, min, sec);
    }

    /// <summary>
    /// 呼吸状態1を取得する
    /// </summary>
    /// <returns>呼吸状態1</returns>
    public BreathState GetBreathState1()
    {
        return GetBreathStateEnum(BreathState1);
    }

    /// <summary>
    /// 呼吸状態2を取得する
    /// </summary>
    /// <returns>呼吸状態2</returns>
    public BreathState GetBreathState2()
    {
        return GetBreathStateEnum(BreathState2);
    }

    /// <summary>
    /// 呼吸状態3を取得する
    /// </summary>
    /// <returns>呼吸状態3</returns>
    public BreathState GetBreathState3()
    {
        return GetBreathStateEnum(BreathState3);
    }

    /// <summary>
    /// 呼吸状態(enum)を取得する
    /// </summary>
    /// <param name="breathState">呼吸状態(int)</param>
    /// <returns>呼吸状態(enum)</returns>
    private BreathState GetBreathStateEnum(int breathState)
    {
        switch (breathState)
        {
            case (int)BreathState.Normal:
                return BreathState.Normal;
            case (int)BreathState.Snore:
                return BreathState.Snore;
            case (int)BreathState.Apnea:
                return BreathState.Apnea;
            default:
                return BreathState.Empty;
        }
    }

    /// <summary>
    /// 首の向き1を取得します
    /// </summary>
    public HeadDir GetHeadDir1()
    {
        return GetNeckOrientationEnum(NeckOrientation1);
    }

    /// <summary>
    /// 首の向き2を取得します
    /// </summary>
    public HeadDir GetHeadDir2()
    {
        return GetNeckOrientationEnum(NeckOrientation2);
    }

    /// <summary>
    /// 首の向き3を取得します
    /// </summary>
    public HeadDir GetHeadDir3()
    {
        return GetNeckOrientationEnum(NeckOrientation3);
    }

    /// <summary>
    /// 首の向き(enum)を取得する
    /// </summary>
    /// <param name="neckOrientation">首の向き(int)</param>
    /// <returns>首の向き(enum)</returns>
    private HeadDir GetNeckOrientationEnum(int neckOrientation)
    {
        switch (neckOrientation) {
        case (int)HeadDir.Left:
            return HeadDir.Left;
        case (int)HeadDir.Up:
            return HeadDir.Up;
        case (int)HeadDir.Right:
            return HeadDir.Right;
        case (int)HeadDir.Down:
            return HeadDir.Down;
        default:
            return HeadDir.Left;
        }
    }
}
