using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kaimin.Managers;
using UnityEngine.UI;
using System;
using System.Linq;
using Kaimin.Common;

public class SliderDemoViewControler : ViewControllerBase {

	[SerializeField] Canvas canvas;
	[SerializeField] PieChart pieChart;

    //PieChart
    public Color[] pieColors; //Colors of Fumei, Mukokyu, Ibiki, Kaimin

    string[] filePaths;

	int selectedPieIndex = -1;
	int MIN_FILE_POSITION = -1;
	int MAX_FILE_POSITION = -1;
    PieChartSlider pieChartSlider;

    // Use this for initialization
    protected override void Start () {
		base.Start();

        loadCharts();
    }

    public void loadCharts()
    {
        //var canvas = GetComponentInParent<Canvas>();
        //LayoutElement layoutElementPrefab = pieChart.GetComponent<LayoutElement>();

        pieChartSlider = canvas.GetComponentInChildren<PieChartSlider>();
        //pieChartSlider.controllerDelegate = this;

        string dataPath = Kaimin.Common.Utility.GsDataPath();
        filePaths = Kaimin.Common.Utility.GetAllFiles(dataPath, "*.csv");

        SetMaxFilePosition(); //Recalculate MAX_FILE_POSITION
        SetMinFilePosition(); //Recalculate MIN_FILE_POSITION
       
        int pieIndex = 0;
        if (MIN_FILE_POSITION >= 0 && MAX_FILE_POSITION >= 0)
        {
            pieChartSlider.pieCharts.Add(pieChart); //Add Min
            pieChartSlider.filePaths.Add(filePaths[MIN_FILE_POSITION]); //Add Min
            for (int i = MIN_FILE_POSITION + 1; i <= MAX_FILE_POSITION; i++)
            {
                pieIndex++;
                pieChartSlider.PushPieChart(Instantiate(pieChart), pieIndex, filePaths[i]);
            }

            pieChartSlider.MoveToIndex(pieIndex);
            this.UpdatePieChart(pieChartSlider, pieIndex);
        } else
        {
            //No data
        }
    }

    public void SetMaxFilePosition()
    {
        if (filePaths != null)
        {
            for (int i = filePaths.Length - 1; i >= 0; i--)
            {
                List<SleepData> sleepDatas = CSVSleepDataReader.GetSleepDatas(filePaths[i]); //睡眠データのリスト
                SleepHeaderData sleepHeaderData = CSVSleepDataReader.GetSleepHeaderData(filePaths[i]); //睡眠データのヘッダーデータ

                if (sleepHeaderData != null && sleepDatas != null && sleepDatas.Count > 0)
                {
                    MAX_FILE_POSITION = i; //ファイルを取得
                    break;
                }
            }
        }
    }

    public void SetMinFilePosition()
	{
        MIN_FILE_POSITION = MAX_FILE_POSITION; //Default
        for (int i = 0; i < MAX_FILE_POSITION; i++)
		{
			List<SleepData> sleepDatas = CSVSleepDataReader.GetSleepDatas(filePaths[i]); //睡眠データのリスト
			SleepHeaderData sleepHeaderData = CSVSleepDataReader.GetSleepHeaderData(filePaths[i]); //睡眠データのヘッダーデータ

			if (sleepHeaderData != null && sleepDatas != null && sleepDatas.Count > 0)
			{
				MIN_FILE_POSITION = i; //ファイルを取得
				break;
			}
		}
	}

	// Update is called once per frame
	void Update () {
		
	}

	public override SceneTransitionManager.LoadScene SceneTag
	{
		get
		{
			return SceneTransitionManager.LoadScene.SliderDemo;
		}
	}

	/// <summary>
	/// 戻るボタン押下イベントハンドラ
	/// </summary>
	public void OnReturnButtonTap()
	{
		SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Setting);
	}

    public void UpdatePieChart(PieChartSlider slider, int pieIndex, bool isToNext = false)
    {
        ChartInfo chartInfo = null;

        try
        {
            String filePath = slider.filePaths[pieIndex];
            PieChart pieChart = slider.pieCharts[pieIndex];
            List<SleepData> sleepDatas = CSVSleepDataReader.GetSleepDatas(filePath); //睡眠データのリスト
            SleepHeaderData sleepHeaderData = CSVSleepDataReader.GetSleepHeaderData(filePath); //睡眠データのヘッダーデータ
            
            if (sleepHeaderData != null && sleepDatas != null && sleepDatas.Count > 0)
            {
                selectedPieIndex = pieIndex;

                DateTime startTime = sleepHeaderData.DateTime;
                DateTime endTime = sleepDatas.Select(data => data.GetDateTime()).Last();

                UserDataManager.Scene.SaveGraphDate(sleepHeaderData.DateTime); //Used to move to graph when call OnToGraphButtonTap()

                //Step1: Update SleepTime
                int sleepTimeSec = Graph.Time.GetDateDifferencePerSecond(startTime, endTime);
                System.TimeSpan ts = new System.TimeSpan(hours: 0, minutes: 0, seconds: sleepTimeSec);
                int hourWithDay = 24 * ts.Days + ts.Hours;      // 24時間超えた場合の時間を考慮
                string sleepTime = string.Format("{0:00}:{1:00}", hourWithDay, ts.Minutes);
                pieChart.sleepTimeText.text = sleepTime;

                DateTime fileDateTime = Kaimin.Common.Utility.TransFilePathToDate(filePath);
                DateTime realDateTime = CSVManager.getRealDateTime(fileDateTime);
                pieChart.sleepDateText.text = CSVManager.isInvalidDate(realDateTime) ? "-" : CSVManager.getJpDateString(realDateTime);

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
                    Utility.makePieChart(pieChart, pieValues, pieLabels, pieColors);
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
                if (apneaAverageCount >= 5)
                {
                    sleepLevel = 1;
                }
                else if (chartInfo.pIbiki >= 0.5)
                {
                    sleepLevel = 2;
                }
                else if (chartInfo.pIbiki >= 0.25)
                {
                    sleepLevel = 3;
                }
                else if (sleepTimeTotal < 7 * 3600)
                {
                    sleepLevel = 4;
                }

                String[] levelColors = new String[5] { "#ff0000", "#ff6600", "#ffff4d", "#72ef36", "#0063dc" };
                pieChart.circleOuter.GetComponent<Image>().color = Utility.convertHexToColor(levelColors[sleepLevel - 1]);
            }
       
        }
        catch (System.Exception e)
        {
        }

        if (chartInfo == null) //Invalid Data
        {
            Utility.makePieChartEmpty(pieChart);

            slider.RemoveLayoutElement(pieIndex);

            if(isToNext)
            {
                if (pieIndex < slider.filePaths.Count - 1)
                {
                    slider.MoveToIndex(pieIndex + 1);
                    this.UpdatePieChart(slider, pieIndex + 1);
                }
            } else
            {
                if (pieIndex > 0)
                {
                    slider.MoveToIndex(pieIndex - 1);
                    this.UpdatePieChart(slider, pieIndex - 1);
                }
            }
            
        }
    }
}
