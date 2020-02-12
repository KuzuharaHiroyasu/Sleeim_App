using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Kaimin.Common;
using UnityEngine.UI;

/// <summary>
/// 睡眠履歴の表示に必要なデータを提供するクラス
/// </summary>
public class SleepListDataSource : MonoBehaviour
{

    public string[] displayFilePaths = null;

    [SerializeField] Sprite actionModeIcon_monitor = null;
	[SerializeField] Sprite actionModeIcon_suppress = null;
	[SerializeField] Sprite actionModeIcon_suppress_weak = null;
	[SerializeField] Sprite actionModeIcon_suppress_strong = null;
	[SerializeField] Sprite actionModeIcon_suppress_multi = null;

    [SerializeField] Sprite sleepLevelIcon_1 = null;
    [SerializeField] Sprite sleepLevelIcon_2 = null;
    [SerializeField] Sprite sleepLevelIcon_3 = null;
    [SerializeField] Sprite sleepLevelIcon_4 = null;
    [SerializeField] Sprite sleepLevelIcon_5 = null;

    Dictionary<string, List<SleepData>> sleepDataDict = new Dictionary<string, List<SleepData>>();
    Dictionary<string, DateTime> filePathToDateDict = new Dictionary<string, DateTime>();

    string[] FilePath
    {
        get
        {
            if (displayFilePaths == null || displayFilePaths.Length == 0)
                displayFilePaths = Kaimin.Common.Utility.GetAllFiles(Kaimin.Common.Utility.GsDataPath(), "*.csv");
            return displayFilePaths;
        }
    }

    /// <summary>
    /// 睡眠履歴をリスト表示するのに必要なデータを取得します
    /// fromにDateTime.MinValue・toにDateTime.MaxValueで期間を指定しない事が可能です
    /// </summary>
    public IEnumerator GetSleepListElementDataCoroutine(ScrollRect scrollRect, DateTime from, DateTime to, Action<SleepListElement.Data> onGetData, Action onComplete, Action deleteAft)
    {
        int initLoadNum = 7;    //はじめにまとめてロードするアイテム数
        int multiLoadNum = 5;   //スクロールした際にまとめてロードするアイテム数
        int multiLoadCount = 0;
        bool popItem = false;

        scrollRect.verticalNormalizedPosition = 1f;
        //CSVから取得した睡眠データをSleepListElement.Dataに変換して返す
        //fromからtoまでの期間の睡眠データを取得する
        //取得したファイル一覧から指定した期間のファイルのみのリストを作成する
        List<string> filePaths = PickFilePathInPeriod(FilePath, from, to.AddDays(1));
        for (int i = 0; i < filePaths.Count; i++)
        {
            string filePath = filePaths[i];
            List<SleepData> sleepDataList = null;
            SleepHeaderData sleepHeaderData = null;
            try
            {
                //一日ごとのデータを取得する
                sleepDataList = ReadSleepDataFromCSV(filePath);         //睡眠データをCSVから取得する
                sleepHeaderData = ReadSleepHeaderDataFromCSV(filePath); //睡眠のヘッダーデータをCSVから取得する
            } catch (System.Exception e) 
            {
            }

            if (sleepDataList == null || sleepHeaderData == null)
            {
                continue;
            }

            //データを設定する
            ChartInfo chartInfo = CSVManager.convertSleepDataToChartInfo(sleepDataList);
            if (chartInfo == null)
            {
                continue;
            }

            chartInfo.endSleepTime = sleepDataList.Select(data => data.GetDateTime()).Last();
            CSVManager.convertSleepHeaderToChartInfo(chartInfo, filePath);
            if (!CSVManager.isInvalidDate(chartInfo.realDateTime) && chartInfo.realDateTime.Month != from.Month)
            {
                continue;
            }

            DateTime bedTime = sleepHeaderData.DateTime;
            DateTime getUpTime = sleepDataList.Last().GetDateTime();
            List<DateTime> dateList = sleepDataList.Select(data => data.GetDateTime()).ToList();
            int longestApneaTime = sleepHeaderData.LongestApneaTime;
            int apneaCount = CulcApneaCount(sleepDataList);
            List<string> todayDataPathList = filePaths.Where(path => IsSameDay(bedTime, TransFilePathToDate(path))).ToList();
            int dateIndex = todayDataPathList
                .Select((path, index) => new { Path = path, Index = index })
                .Where(data => data.Path == filePath)
                .Select(data => data.Index)
                .First();                               //同一日の何個目のデータか(0はじまり)
            int crossSunCount = todayDataPathList
                .Take(dateIndex + 1)
                .Where(path => CSVManager.isCrossTheSun(bedTime, ReadSleepDataFromCSV(path).Last().GetDateTime()))
                .Count();                               //現在のデータまでの日マタギデータの個数
            int sameDataNum = todayDataPathList.Count;  //同一日のすべてのデータ個数
            int crossSunNum = todayDataPathList
                .Where(path => CSVManager.isCrossTheSun(bedTime, ReadSleepDataFromCSV(path).Last().GetDateTime()))
                .Count();                               //同一日の日マタギのみのデータ個数

            onGetData(new SleepListElement.Data(bedTime, dateList, longestApneaTime, apneaCount, dateIndex, crossSunCount, sameDataNum, crossSunNum,
                GetSleepLevel(bedTime,getUpTime, sleepHeaderData.ApneaDetectionCount, sleepDataList),
				GetActionModeIcon(chartInfo), filePath, deleteAft, chartInfo));
            if (i + 1 < initLoadNum)
            {
                //初期アイテムロード
            }
            else if (popItem && (multiLoadCount < multiLoadNum))
            {
                //追加アイテムロード
                multiLoadCount++;
            }
            else
            {
                yield return new WaitUntil(() => scrollRect.verticalNormalizedPosition < 0.1f);
                popItem = true;
                multiLoadCount = 1; //既に一つは読み込み済み
            }
        }
        onComplete();
    }

    private Sprite GetSleepLevel(DateTime startTime, DateTime endTime, int apneaCount, List<SleepData> sleepDataList)
    {
        double sleepTimeTotal = endTime.Subtract(startTime).TotalSeconds;
        //無呼吸平均回数(時)
        double apneaAverageCount = sleepTimeTotal == 0 ? 0 : (double)(apneaCount * 3600) / sleepTimeTotal;  // 0除算を回避
        apneaAverageCount = Math.Truncate(apneaAverageCount * 10) / 10.0;   // 小数点第2位以下を切り捨て

        var chartInfo = CSVManager.convertSleepDataToChartInfo(sleepDataList);
        if (apneaAverageCount >= 5)
        {
            return sleepLevelIcon_1;
        }
        if (chartInfo.pIbiki >= 0.5)
        {
            return sleepLevelIcon_2;
        }
        if (chartInfo.pIbiki >= 0.25)
        {
            return sleepLevelIcon_3;
        }
        if (sleepTimeTotal < 7 * 3600)
        {
            return sleepLevelIcon_4;
        }

        return sleepLevelIcon_5;
    }

	private Sprite GetActionModeIcon(ChartInfo info) {

		if (info.sleepMode == (int)SleepMode.Monitor) {
			return actionModeIcon_monitor; //Default
        }

		if (info.vibrationStrength == (int)VibrationStrength.Weak)
		{
			return actionModeIcon_suppress_weak;
		}
		else if (info.vibrationStrength == (int)VibrationStrength.Strong)
		{
			return actionModeIcon_suppress_strong;
		}
		else if (info.vibrationStrength == (int)VibrationStrength.Multi)
		{
			return actionModeIcon_suppress_multi;
		}

		return actionModeIcon_suppress;
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

    /// <summary>
    /// 無呼吸検知数を求める
    /// </summary>
    /// <param name="sleepDataList"></param>
    /// <returns></returns>
    int CulcApneaCount(List<SleepData> sleepDataList)
    {
        int totalCount = 0;
        totalCount += sleepDataList.Where(sleepData => sleepData.BreathState1 == (int)SleepData.BreathState.Apnea).Count();
        totalCount += sleepDataList.Where(sleepData => sleepData.BreathState2 == (int)SleepData.BreathState.Apnea).Count();
        totalCount += sleepDataList.Where(sleepData => sleepData.BreathState3 == (int)SleepData.BreathState.Apnea).Count();
        return totalCount;
    }

    /// <summary>
    /// 最新の日付を取得します
    /// 一件もデータがなかった場合はDateTime.MinValueを返します
    /// </summary>
    public DateTime GetLatestDate()
    {
        for(int i = FilePath.Length - 1; i >= 0; i--)
        {
            var fileDateTime = Utility.TransFilePathToDate(FilePath[i]);
            if (!CSVManager.isInvalidDate(fileDateTime))
            {
                return fileDateTime;
            }
        }

        return FilePath.Length >= 1 ? Utility.TransFilePathToDate(FilePath[0]) : DateTime.MinValue;
    }

    /// <summary>
    /// 指定した期間にデータが存在するかどうかを返します
    /// </summary>
    public bool IsExistData(DateTime from, DateTime to)
    {
        return FilePath.Where(path => (from == DateTime.MinValue || TransFilePathToDate(path).CompareTo(from) >= 0) && (to == DateTime.MaxValue || TransFilePathToDate(path).CompareTo(to) <= 0)).Count() != 0;
    }

    //睡眠データのファイル一覧から指定した期間のもののみを取得
    List<string> PickFilePathInPeriod(string[] sleepFilePathList, DateTime from, DateTime to)
    {
        List<string> filePaths = new List<string>();

        int length = sleepFilePathList.Count();
        if (length == 0) {
            return filePaths; //Empty
        }
        
        int fromIndex = 0; //Default
        if(from != DateTime.MinValue)
        {
            for(int i = 0; i < length; i++)
            {
                DateTime dt = TransFilePathToDate(sleepFilePathList[i]);
                if(!CSVManager.isInvalidDate(dt))
                {
                    if (dt.CompareTo(from) >= 0)
                    {
                        fromIndex = i;
                        break;
                    } else {
                        fromIndex = i + 1;
                    }
                    
                }
            }
        }
        
        if (fromIndex >= length)
        {
            return filePaths; //Empty
        }

        int endIndex = length - 1; 
        if (to != DateTime.MaxValue)
        {
            for (int i = fromIndex; i < length; i++)
            {
                DateTime dt = TransFilePathToDate(sleepFilePathList[i]);
                if (!CSVManager.isInvalidDate(dt))
                {
                    if (dt.CompareTo(to) <= 0)
                    {
                        endIndex = i;
                    } else {
                        break;
                    }
                } else {
                    endIndex = i;
                }
            }
        }

        for(int i = fromIndex; i <= endIndex; i++) {
            filePaths.Add(sleepFilePathList[i]);
        }

        return filePaths;
        //return sleepFilePathList.Where(path => (from == DateTime.MinValue || TransFilePathToDate(path).CompareTo(from) >= 0) && (to == DateTime.MaxValue || TransFilePathToDate(path).CompareTo(to) <= 0)).ToList();
    }

    //睡眠データをリソースのCSVファイルから取得します
    List<SleepData> ReadSleepDataFromCSV(string filepath)
    {
        if(!sleepDataDict.ContainsKey(filepath))
        {
            sleepDataDict[filepath] = CSVSleepDataReader.GetSleepDatas(filepath);
        }

        return sleepDataDict[filepath];
    }

    DateTime TransFilePathToDate(string filepath)
    {
        if (!filePathToDateDict.ContainsKey(filepath))
        {
            filePathToDateDict[filepath] = Utility.TransFilePathToDate(filepath);
        }

        return filePathToDateDict[filepath];
    }

    //睡眠のヘッダーデータをCSVファイルから取得します
    SleepHeaderData ReadSleepHeaderDataFromCSV(string filepath)
    {
        return CSVSleepDataReader.GetSleepHeaderData(filepath);
    }
}
