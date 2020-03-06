using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

public class CSVManager
{
    public const int MAX_MUKOKYU_CONTINUOUS_TIME = 180; //無呼吸状態が３分(180s)以上続いている箇所は不明状態にする

    /**
     * Get list of all csv files in app order by name (date)
     * Return array(index -> filePath)
     */
    public static string[] getCsvFileList()
    {
        string[] fileList = Kaimin.Common.Utility.GetAllFiles(Kaimin.Common.Utility.GsDataPath(), "*.csv");

        return fileList;
    }

    /**
     * Get list of csv files by page (used to display chartWeek)
     * Param page: Calculated from 1
     * Return array(index -> filePath)
     */
    public static string[] getCsvFileListByPage(string[] fileList, int page)
    {
        int fileNum = fileList.Length;
        List<string> pageFileList = new List<string>();

        if (fileNum > 0)
        {
            if (page == 1)
            {
                for (int i = 0; i < Mathf.Min(7, fileNum); i++)
                {
                    pageFileList.Add(fileList[i]);
                }
            }
            else if (page > 1)
            {
                int end = (fileNum % 7 == 0) ? page * 7 : ((page - 1) * 7 + fileNum % 7);
                for (int i = end - 7; i < end; i++)
                {
                    pageFileList.Add(fileList[i]);
                }
            }
        }

        return pageFileList.ToArray();
    }

    public static List<SleepData> readSleepDataFromCsvFile(string filePath)
    {
        return CSVSleepDataReader.GetSleepDatas(filePath);
    }

    public static ChartInfo convertSleepHeaderToChartInfo(ChartInfo chartInfo, string filePath)
    {
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        StreamReader reader = new StreamReader(stream);

        string[] profileInfo = reader.ReadLine().Split(','); //CsvFileの1行目 (Row 0)
        reader.ReadLine();　//Skip Row 1
        string[] sleepRecordStartTimeLine = reader.ReadLine().Split(','); //CsvFileの3行目 (Row 2)

        string sleepTime = "";
        if (chartInfo.endSleepTime != null && sleepRecordStartTimeLine.Length >= 3)
        {
            string date = sleepRecordStartTimeLine[0];
            string time = sleepRecordStartTimeLine[2];
            string[] dateArr = date.Split('/');
            string[] timeArr = time.Split(':');
            int year = int.Parse(dateArr[0]);
            int month = int.Parse(dateArr[1]);
            int day = int.Parse(dateArr[2]);
            int hour = int.Parse(timeArr[0]);
            int min = int.Parse(timeArr[1]);
            int sec = int.Parse(timeArr[2]);
            chartInfo.startSleepTime = new DateTime(year, month, day, hour, min, sec);

            int sleepTimeSec = Graph.Time.GetDateDifferencePerSecond(chartInfo.startSleepTime, chartInfo.endSleepTime);
            System.TimeSpan ts = new System.TimeSpan(hours: 0, minutes: 0, seconds: sleepTimeSec);
            int hourWithDay = 24 * ts.Days + ts.Hours;      // 24時間超えた場合の時間を考慮
            sleepTime = string.Format("{0:00}:{1:00}", hourWithDay, ts.Minutes);
        }

        DateTime fileDateTime = Kaimin.Common.Utility.TransFilePathToDate(filePath);
        chartInfo.realDateTime = getRealDateTime(fileDateTime);
        chartInfo.fileName = fileDateTime.ToString();
        chartInfo.sleepTime = sleepTime;
        chartInfo.date = isInvalidDate(chartInfo.realDateTime) ? "ー" : chartInfo.realDateTime.ToString("M/d"); 

        if (sleepRecordStartTimeLine.Length > 9) //New format
        {
            chartInfo.sleepMode = int.Parse(sleepRecordStartTimeLine[8]);
            chartInfo.vibrationStrength = int.Parse(sleepRecordStartTimeLine[9]);
        } else
        { 
            //Default
            chartInfo.sleepMode = (int) SleepMode.Suppress;
            chartInfo.vibrationStrength = (int)VibrationStrength.Medium;
        }

        return chartInfo;
    }

    public static ChartInfo convertSleepDataToChartInfo(List<SleepData> sleepData)
    {
        float numKaiMin = 0;
        float numIbiki = 0;
        float numMukokyu = 0;
        float numFumei = 0;

        //無呼吸状態が3分(180s)以上続いている箇所は不明状態にする
        float numFumeiAble = 0; // 不明に変更される可能性のある無呼吸の数
        long mukokyuContinuousTime = 0;
        long mukokyuStartTime = 0;
        
        foreach (var item in sleepData)
        {
            // statesはitemの0秒後、10秒後、20秒後の状態
            int[] states = { item.BreathState1, item.BreathState2, item.BreathState3 };
            System.DateTime tmpDateTime = item.GetDateTime();
            long tmpTime = ((System.DateTimeOffset)tmpDateTime).ToUnixTimeSeconds();
            long[] tmpTimes = {tmpTime,tmpTime+10,tmpTime+20}; // 各状態になった時間をlong値に変換

            for (int i = 0; i < states.Length; i++) {
                var state = states[i];
            
                if (state == (int)SleepData.BreathState.Apnea)
                {
                    numMukokyu++;
                    numFumeiAble++;
                    
                    if (mukokyuStartTime == 0)
                    {
                        mukokyuStartTime = tmpTimes[i] - 10;
                    } else
                    {
                        mukokyuContinuousTime = tmpTimes[i] - mukokyuStartTime;
                    }
                } else
                {
                    if (mukokyuContinuousTime > MAX_MUKOKYU_CONTINUOUS_TIME) //無呼吸状態が3分(180s)以上続いている場合
                    {
                        numMukokyu -= numFumeiAble;
                        numFumei += numFumeiAble;
                    }
                    //Reset
                    numFumeiAble = 0;
                    mukokyuContinuousTime = 0;
                    mukokyuStartTime = 0;

                    if (state == (int)SleepData.BreathState.Normal)
                    {
                        numKaiMin++;
                    }
                    else if (state == (int)SleepData.BreathState.Snore)
                    {
                        numIbiki++;
                    }
                    else if (state == (int)SleepData.BreathState.Empty)
                    {
                        numFumei++;
                    }
                }
            }
        }

        //Check when end of file
        if (mukokyuContinuousTime > MAX_MUKOKYU_CONTINUOUS_TIME) //無呼吸状態が3分(180s)以上続いている場合
        {
            numMukokyu -= numFumeiAble;
            numFumei += numFumeiAble;
        }

        float numTotal = numKaiMin + numIbiki + numMukokyu + numFumei;
        if (numTotal > 0)
        {
            ChartInfo chartInfo = new ChartInfo();
            chartInfo.pKaiMin = numKaiMin / numTotal;
            chartInfo.pIbiki = numIbiki / numTotal;
            chartInfo.pMukokyu = numMukokyu / numTotal;
            chartInfo.pFumei = 1 - (chartInfo.pKaiMin + chartInfo.pIbiki + chartInfo.pMukokyu);

            return chartInfo;
        }
        else
        {
            return null;
        }
    }

    /**
     * Get duration time (HH:mm)
     * Parmams startTime (HH:mm), endTime (HH:mm)
     */
    public static string getDurationTime(string startTime, string endTime)
    {
        if (startTime != endTime && startTime.Contains(":") && endTime.Contains(":"))
        {
            string[] arrS = startTime.Split(':');
            string[] arrE = endTime.Split(':');

            if(arrS.Length == 2 && arrE.Length == 2)
            {
                int sHour = int.Parse(arrS[0]);
                int sMinute = int.Parse(arrS[1]);
                int eHour = int.Parse(arrE[0]);
                int eMinute = int.Parse(arrE[1]);
                int diffDate = (eHour * 60 + eMinute) > (sHour * 60 + sMinute) ? 0 : 1;

                System.TimeSpan start = new System.TimeSpan(0, sHour, sMinute, 0);
                System.TimeSpan end = new System.TimeSpan(diffDate, eHour, eMinute, 0);
                System.TimeSpan diff = end - start;

                return string.Format("{0:00}:{1:00}", diff.Hours, diff.Minutes);
            }
        }

        return "00:00";
    }



    public static DateTime getRealDateTime(DateTime dateTime)
    {
        string tmpTime = dateTime.ToString("HH:mm:ss");
        if (string.Compare(tmpTime, "00:00:00") >= 0 && string.Compare(tmpTime, "09:00:00") <= 0)
        {
            //データ開始時刻がAM00:00～09:00までのデータに前日の日付として表示
            dateTime = dateTime.AddDays(-1);
        }

        return dateTime;
    }

    /**
     * Return date string to display
     * isShort = true: 1(水)
     * isShort = false: 1月1日(水)
     * if invalid Date: -
     */
    public static String getJpDateString(DateTime dateTime, bool isShort = false)
    {
        if (isInvalidDate(dateTime)) return "ー";

        string day = dateTime.Day.ToString();
        string dayOfWeek = dateTime.ToString("ddd", new System.Globalization.CultureInfo("ja-JP")); //曜日
        if (isShort)
        {
            return day + "(" + dayOfWeek + ")";
        } else
        {
            string month = dateTime.Month.ToString() + "月";
            return month + day + "日(" + dayOfWeek + ")";
        }
    }

    public static bool isInvalidDate(DateTime dateTime)
    {
        return dateTime.Year <= 2016;
    }

    public static string ConvertTimeToHHMM(DateTime dateTime)
    {
        if (isInvalidDate(dateTime)) return "ー";

        return String.Format("{0:00}", dateTime.Hour) + ":" + String.Format("{0:00}", dateTime.Minute);
    }

    //時間を以下の形式の文字列に変換する
    //例：2018/06/20/14:08　→ １４：０８
    public static string TransTimeToHHMM(DateTime time)
    {
        if (isInvalidDate(time)) return "ー";

        string hh = time.Hour.ToString("00");
        string mm = time.Minute.ToString("00");
        string result = hh + ":" + mm;
        return result.ToUpper();
    }

    //日付をまたいでいるかどうか
    public static bool isCrossTheSun(DateTime start, DateTime end)
    {
        return end.Year >= start.Year && (end.Month > start.Month || (end.Month == start.Month && end.Day > start.Day));
    }
}