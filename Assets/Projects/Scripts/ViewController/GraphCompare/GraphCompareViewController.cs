using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GraphCompareViewController : ViewControllerBase
{
    //平均・最新のグラフの用
    public BarChart barChart1; //Everage chart1 (Monitor)
    public BarChart barChart2; //Everage chart2 (Suppress)
    public BarChart barChart3; //Latest chart

    public BarPercent barPercent1;
    public BarPercent barPercent2;
    public BarPercent barPercent3;

    //7日分のグラフの用
    public Button btnPrev;
    public Button btnNext;
    public Text lbChartTitle;
    public BarChart barChartWeek1;
    public BarChart barChartWeek2;
    public BarChart barChartWeek3;
    public BarChart barChartWeek4;
    public BarChart barChartWeek5;
    public BarChart barChartWeek6;
    public BarChart barChartWeek7;

    public BarPercentWeek barPercentWeek1;
    public BarPercentWeek barPercentWeek2;
    public BarPercentWeek barPercentWeek3;
    public BarPercentWeek barPercentWeek4;
    public BarPercentWeek barPercentWeek5;
    public BarPercentWeek barPercentWeek6;
    public BarPercentWeek barPercentWeek7;

    public Text txtNoDataEverage;
    public Text txtNoDataLatest;
    public Text txtNoDataWeek;

    public int currentPage = 0; //Using for chartWeek. Calculated from 1 if have
    public int lastPage = 0; //Using for chartWeek. Calculated from 1 if have, default display chartWeek of lastPage

    public ChartPref chartPref = new ChartPref();
    public string[] fileList = { }; //Using to display chartWeeks
    public List<ChartInfo> chartsOfWeek = new List<ChartInfo>(); //Using to display chartWeek

    public List<ChartInfo> chartsOfMonitor = new List<ChartInfo>();  //Using with saved average values to calculate average of monitor (Chart1)
    public List<ChartInfo> chartsOfSuppress = new List<ChartInfo>(); //Using with saved average values to calcuate average of suppress (Chart2)

    const string MSG_NO_DATA = "No Data";
    const string MSG_RECENT_PART = "直近の分";

    // Use this for initialization
    protected override void Start()
    {
        base.Start();

        //Important inits
        this.chartPref.loadData();
        this.fileList = CSVManager.getCsvFileList();

        this.txtNoDataEverage.text = MSG_NO_DATA;
        this.txtNoDataLatest.text = "";
        this.txtNoDataWeek.text = "";

        this.initInfoForChartWeek();
        this.initInfoForChartLatest();
        this.initInfoForChartEverage();

        this.btnPrev.GetComponent<Button>().onClick.AddListener(delegate { this.onClickPrevBtn(); });
        this.btnNext.GetComponent<Button>().onClick.AddListener(delegate { this.onClickNextBtn(); });
    }
	
	// Update is called once per frame
	void Update () {
        
    }

    public override SceneTransitionManager.LoadScene SceneTag
    {
        get
        {
            return SceneTransitionManager.LoadScene.Graph;
        }
    }

    public void initInfoForChartEverage()
    {
        Dictionary<int, string> unreadFileList = Kaimin.Common.Utility.getUnreadCsvFileList(this.chartPref.savedLastFileId);

        foreach (KeyValuePair<int, string> entry in unreadFileList)
        {
            try
            {
                var filePath = entry.Value;
                List<SleepData> sleepData = CSVManager.readSleepDataFromCsvFile(filePath);
                ChartInfo chartInfo = CSVManager.convertSleepDataToChartInfo(sleepData);
                if (chartInfo != null)
                {
                    CSVManager.convertSleepHeaderToChartInfo(chartInfo, filePath);

                    if (chartInfo.sleepMode == (int)SleepMode.Monitor)
                    {
                        this.chartsOfMonitor.Add(chartInfo);
                    } else
                    {
                        this.chartsOfSuppress.Add(chartInfo);
                    }
                }
            } catch (System.Exception e) {
            }
        }

        this.showChartEverage(chartsOfMonitor, SleepMode.Monitor);
        this.showChartEverage(chartsOfSuppress, SleepMode.Suppress);

        int fileNum = unreadFileList.Count;
        if (fileNum > 0)
        {
            this.chartPref.saveLastFileId(unreadFileList.Keys.Last());
        }
    }

    public void showChartEverage(List<ChartInfo> chartInfos, SleepMode sleepMode)
    {
        bool isMonitor = (sleepMode == SleepMode.Monitor);
        ChartInfo chartEverage = new ChartInfo();

        BarChart barChartE = isMonitor ? this.barChart1 : this.barChart2;
        BarPercent barPercentE = isMonitor ? this.barPercent1 : this.barPercent2;

        //Load saved info
        int savedChartNum = isMonitor ? this.chartPref.savedNumMonitor : this.chartPref.savedNumSuppress;
        if(savedChartNum > 0)
        {
            string savedEverage = isMonitor ? this.chartPref.savedEverageMonitor : this.chartPref.savedEverageSuppress;
            string[] tmpEverages = savedEverage.Split('_');
            if(tmpEverages.Length == 3)
            {
                chartEverage.pKaiMin = float.Parse(tmpEverages[0]) * savedChartNum;
                chartEverage.pIbiki = float.Parse(tmpEverages[1]) * savedChartNum;
                chartEverage.pMukokyu = float.Parse(tmpEverages[2]) * savedChartNum;
            } else
            {
                savedChartNum = 0; //Reset
            }
        }

        int chartNum = chartInfos.Count + savedChartNum;
        if (chartNum > 0)
        {
            foreach (var chartInfo in chartInfos)
            {
                chartEverage.pKaiMin += chartInfo.pKaiMin;
                chartEverage.pIbiki += chartInfo.pIbiki;
                chartEverage.pMukokyu += chartInfo.pMukokyu;
            }

            chartEverage.pKaiMin = chartEverage.pKaiMin / chartNum;
            chartEverage.pIbiki = chartEverage.pIbiki / chartNum;
            chartEverage.pMukokyu = chartEverage.pMukokyu / chartNum;

            barChartE.drawBarChart(chartEverage.pKaiMin, chartEverage.pIbiki, chartEverage.pMukokyu);
            barPercentE.drawChartInfo(chartEverage.pKaiMin, chartEverage.pIbiki, chartEverage.pMukokyu, (int)sleepMode);

            this.chartPref.saveData(isMonitor, chartEverage, chartNum);
            this.txtNoDataEverage.text = "";
        }
        else
        {
            //Empty chart
            barChartE.drawBarChartWhenEmpty();
            barPercentE.drawChartInfoWhenEmpty();
        }
    }

    public void initInfoForChartLatest()
    {
        //Latest chart
        int chartCount = this.chartsOfWeek.Count;
        if (chartCount > 0)
        {
            ChartInfo chartInfo = this.chartsOfWeek[chartCount - 1];
            this.barChart3.drawBarChart(chartInfo.pKaiMin, chartInfo.pIbiki, chartInfo.pMukokyu);
            this.barPercent3.drawChartInfo(chartInfo.pKaiMin, chartInfo.pIbiki, chartInfo.pMukokyu, chartInfo.sleepMode, chartInfo.vibrationStrength);
        }
        else
        {
            this.txtNoDataLatest.text = MSG_NO_DATA;
            this.barChart3.drawBarChartWhenEmpty();
            this.barPercent3.drawChartInfoWhenEmpty();
        }
    }


    public void initInfoForChartWeek()
    {
        int fileNum = this.fileList.Length;
        if(fileNum > 0)
        {
            this.lastPage = (fileNum % 7 == 0) ? fileNum / 7 : ((int)Mathf.Round(fileNum / 7) + 1);
            this.currentPage = this.lastPage; //Default  
        }

        this.updatePrevNextBtnState();
        this.showChartWeekByPage(lastPage);
    }

    public void updatePrevNextBtnState()
    {
        if(this.lastPage <= 1) 
        {
            //No show prev/next button
            this.btnPrev.interactable = false;
            this.btnNext.interactable = false;
        }
        else
        {
            if (this.currentPage == this.lastPage)
            {
                //No show next button
                this.btnPrev.interactable = true;
                this.btnNext.interactable = false;
            }
            else if (this.currentPage == 1)
            {
                //No show prev button
                this.btnPrev.interactable = false;
                this.btnNext.interactable = true;
            }
        }
    }

    public void showChartWeekByPage(int page)
    {
        this.chartsOfWeek = new List<ChartInfo>();
        BarChart[] barChartWeeks = { barChartWeek1, barChartWeek2, barChartWeek3, barChartWeek4, barChartWeek5, barChartWeek6, barChartWeek7 };
        BarPercentWeek[] barPercentWeeks = { barPercentWeek1, barPercentWeek2, barPercentWeek3, barPercentWeek4, barPercentWeek5, barPercentWeek6, barPercentWeek7 };

        if (page > 0)
        {
            string[] pageFileList = CSVManager.getCsvFileListByPage(this.fileList, page);
            foreach (var filePath in pageFileList)
            {
                try
                {
                    List<SleepData> sleepData = CSVManager.readSleepDataFromCsvFile(filePath);
                    ChartInfo chartInfo = CSVManager.convertSleepDataToChartInfo(sleepData);
                    if (chartInfo != null)
                    {
                        chartInfo.endSleepTime = sleepData.Select(data => data.GetDateTime()).Last();
                        CSVManager.convertSleepHeaderToChartInfo(chartInfo, filePath);

                        this.chartsOfWeek.Add(chartInfo);
                    }
                } catch (System.Exception e) {

                }
            }
        }

        int chartCount = this.chartsOfWeek.Count;
        if (chartCount > 0)
        {
            this.lbChartTitle.text = MSG_RECENT_PART;
            if(chartCount > 1)
            {
                if (this.chartsOfWeek[0].date != "-" || this.chartsOfWeek[chartCount - 1].date != "-") {
                    this.lbChartTitle.text += "(" + this.chartsOfWeek[0].date + "~" + this.chartsOfWeek[chartCount - 1].date + ")";
                }
            }
            for (int i = 0; i < chartCount; i++)
            {
                ChartInfo chartInfo = this.chartsOfWeek[i];
                barChartWeeks[i].drawBarChart(chartInfo.pKaiMin, chartInfo.pIbiki, chartInfo.pMukokyu);
                barPercentWeeks[i].drawChartInfoWeek(chartInfo.pKaiMin, chartInfo.pIbiki, chartInfo.pMukokyu, chartInfo.date, chartInfo.sleepTime, chartInfo.sleepMode, chartInfo.vibrationStrength);
            }

            for (int i = chartCount; i < barChartWeeks.Length; i++)
            {
                barChartWeeks[i].drawBarChartWhenEmpty();
                barPercentWeeks[i].drawChartInfoWeekWhenEmpty();
            }
        }
        else
        {
            //Show no chart week
            this.txtNoDataWeek.text = MSG_NO_DATA;
            this.lbChartTitle.text = MSG_RECENT_PART;
            for (int i = 0; i < barChartWeeks.Length; i++) {
                barChartWeeks[i].drawBarChartWhenEmpty();
                barPercentWeeks[i].drawChartInfoWeekWhenEmpty();
            }
        }
    }

    public void onClickNextBtn()
    {
        if (this.currentPage < this.lastPage)
        {
            this.currentPage++;
            this.lbChartTitle.text = "ロード中...";
            this.showChartWeekByPage(this.currentPage);
            this.updatePrevNextBtnState();
        }
    }

    public void onClickPrevBtn()
    {
        if (this.currentPage > 1)
        {
            this.currentPage--;
            this.lbChartTitle.text = "ロード中...";
            this.showChartWeekByPage(this.currentPage);
            this.updatePrevNextBtnState();
        }
    }

    public float[] generateRandomPercents()
    {
        float p1 = Random.Range(10, 30);
        float p2 = Random.Range(10, 30);
        float p3 = Random.Range(10, 30);
        float[] percents = { p1 / 100, p2 / 100, p3 / 100 };

        return percents;
    }
}