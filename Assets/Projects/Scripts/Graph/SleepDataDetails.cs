using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Graph;
using System.Linq;
using System;
using UniRx;
using UnityEngine.UI;

/// <summary>
/// 睡眠データ詳細クラス
/// </summary>
public class SleepDataDetail {

    /// <summary>
    /// 就寝時間
    /// </summary>
    public DateTime BedTime { get; }

    /// <summary>
    /// 起床時間
    /// </summary>
    public DateTime GetUpTime { get; }

    /// <summary>
    /// いびき時間(秒)
    /// </summary>
    public int SnoreTimeSec { get; }

    /// <summary>
    /// 無呼吸時間(秒)
    /// </summary>
    public int ApneaTime { get; }

    /// <summary>
    /// いびき検知数
    /// </summary>
    public int SnoreCount { get; }

    /// <summary>
    /// 振動でいびきが止まった回数
    /// </summary>
    public int SnoreStopCount { get; }

    /// <summary>
    /// 無呼吸検知数
    /// </summary>
    public int ApneaCount { get; }

    /// <summary>
    /// 無呼吸最長継続時間
    /// </summary>
    public int LongestApneaTime { get; }

    /// <summary>
    /// いびきの割合
    /// </summary>
    public double SnoreRate { get; }

    /// <summary>
    /// 無呼吸平均回数(時)
    /// </summary>
    public double ApneaAverageCount { get; }

    /// <summary>
    /// 同日のデータのインデックス
    /// </summary>
    public int DateIndex { get; }

    /// <summary>
    /// 現在のデータまでで何個目のの日またぎデータか
    /// </summary>
    public int CrossSunCount { get; }

    /// <summary>
    /// 同一日のすべてのデータ個数
    /// </summary>
    public int SameDateNum { get; }

    /// <summary>
    /// 同一日の日マタギのみのデータ個数
    /// </summary>
    public int CrossSunNum { get; }

    /// <summary>
    /// 動作モード
    /// </summary>
    public int SleepMode { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="bedTime">就寝時間</param>
    /// <param name="getUpTime">起床時間</param>
    /// <param name="snoreTime">いびき時間(秒)</param>
    /// <param name="apneaTime">無呼吸時間(秒)</param>
    /// <param name="snoreCount">いびき検知数</param>
    /// <param name="apneaCount">無呼吸検知数</param>
    /// <param name="longestApneaTime">無呼吸最長継続時間</param>
    /// <param name="snoreRate">いびき割合</param>
    /// <param name="apneaAverageCount">無呼吸平均回数(時)</param>
    /// <param name="dateIndex"></param>
    /// <param name="crossSunCount"></param>
    /// <param name="sameDateNum"></param>
    /// <param name="crossSunNum"></param>
    /// <param name="sleepMode">動作モード</param>
    /// <param name="snoreStopCount">振動でいびきが止まった回数</param>
    public SleepDataDetail (
        DateTime bedTime,
        DateTime getUpTime,
        int snoreTime,
        int apneaTime,
        int snoreCount,
        int apneaCount,
        int longestApneaTime,
        double snoreRate,
        double apneaAverageCount,
        int dateIndex,
        int crossSunCount,
        int sameDateNum,
        int crossSunNum,
        int sleepMode,
        int snoreStopCount)
    {
        this.BedTime = bedTime;
        this.GetUpTime = getUpTime;
        this.SnoreTimeSec = snoreTime;
        this.ApneaTime = apneaTime;
        this.SnoreCount = snoreCount;
        this.SnoreStopCount = snoreStopCount;
        this.ApneaCount = apneaCount;
        this.LongestApneaTime = longestApneaTime;
        this.SnoreRate = snoreRate;
        this.ApneaAverageCount = apneaAverageCount;
        this.DateIndex = dateIndex;
        this.CrossSunCount = crossSunCount;
        this.SameDateNum = sameDateNum;
        this.CrossSunNum = crossSunNum;
        this.SleepMode = sleepMode;
    }
}
