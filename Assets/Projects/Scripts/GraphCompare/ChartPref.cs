using UnityEngine;

public class ChartPref
{
    public int savedNumMonitor = 0;
    public int savedNumSuppress = 0;
    public string savedEverageMonitor = ""; //Ex: 0.25_0.25_0.25 (pKaimin_pIbiki_pMukokyu)
    public string savedEverageSuppress = "";
    public int savedLastFileId = 0;

    public void saveData(bool isMonitor, ChartInfo chartEverage, int chartNum)
    {
        string key = isMonitor ? "Monitor" : "Suppress";
        PlayerPrefs.SetInt("savedNum" + key, chartNum);
        PlayerPrefs.SetString("savedEverage" + key, chartEverage.pKaiMin + "_" + chartEverage.pIbiki + "_" + chartEverage.pMukokyu);
    }

    public void saveLastFileId(int lastFileId)
    {
        PlayerPrefs.SetInt("savedLastFileId", lastFileId);
    }

    public void loadData()
    {
        savedNumMonitor = PlayerPrefs.GetInt("savedNumMonitor", 0);
        savedNumSuppress = PlayerPrefs.GetInt("savedNumSuppress", 0);
        savedEverageMonitor = PlayerPrefs.GetString("savedEverageMonitor", "");
        savedEverageSuppress = PlayerPrefs.GetString("savedEverageSuppress", "");
        savedLastFileId = PlayerPrefs.GetInt("savedLastFileId", 0);
    }

    public static void updateEverageDataAfterDelete(ChartInfo chartInfo)
    {
        bool isMonitor = (chartInfo.sleepMode == (int)SleepMode.Monitor);
        string key = isMonitor ? "Monitor" : "Suppress";

        int leftChartNum = PlayerPrefs.GetInt("savedNum" + key, 0) - 1;
        if(leftChartNum <= 0)
        {
            PlayerPrefs.SetInt("savedNum" + key, 0);
            PlayerPrefs.SetString("savedEverage" + key, "");
        } else {
            string savedEverage = PlayerPrefs.GetString("savedEverage" + key, "");
            string[] tmpEverages = savedEverage.Split('_');
            if (tmpEverages.Length == 3) {
                PlayerPrefs.SetInt("savedNum" + key, leftChartNum);

                var new_pKaiMin  = (float.Parse(tmpEverages[0]) * (leftChartNum + 1) - chartInfo.pKaiMin)/leftChartNum;
                var new_pIbiki = (float.Parse(tmpEverages[1]) * (leftChartNum + 1) - chartInfo.pIbiki) /leftChartNum;
                var new_pMukokyu = (float.Parse(tmpEverages[2]) * (leftChartNum + 1) - chartInfo.pMukokyu)/leftChartNum;

                PlayerPrefs.SetString("savedEverage" + key, new_pKaiMin + "_" + new_pIbiki + "_" + new_pMukokyu);
            } else {   
                //Reset
                PlayerPrefs.SetInt("savedNum" + key, 0);
                PlayerPrefs.SetString("savedEverage" + key, "");
            }
        }
    }

    public void resetEverageData()
    {
        PlayerPrefs.SetInt("savedNumMonitor", 0);
        PlayerPrefs.SetInt("savedNumSuppress", 0);
        PlayerPrefs.SetString("savedEverageMonitor", "");
        PlayerPrefs.SetString("savedEverageSuppress", "");
        PlayerPrefs.SetInt("savedLastFileId", 0);
    }
}