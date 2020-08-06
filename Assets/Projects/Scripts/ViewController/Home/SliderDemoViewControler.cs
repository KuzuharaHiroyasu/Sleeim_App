using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kaimin.Managers;
using UnityEngine.UI;
using System;
using System.Linq;

public class SliderDemoViewControler : ViewControllerBase {

	[SerializeField] Canvas canvas;
	[SerializeField] ScrollRect scrollRect;
	[SerializeField] PieChart pieChart;

    //PieChart
    public Color[] pieColors; //Colors of Fumei, Mukokyu, Ibiki, Kaimin

    string[] filePaths;
	int selectFilePosition = -1;
	int MIN_FILE_POSITION = 0;
	int MAX_FILE_POSITION = -1;


	// Use this for initialization
	protected override void Start () {
		base.Start();

    
        loadCharts();
    }

    public void loadCharts()
    {
        //var canvas = GetComponentInParent<Canvas>();
        var snap = canvas.GetComponentInChildren<ScrollSnap>();
        LayoutElement layoutElementPrefab = pieChart.GetComponent<LayoutElement>();
        //layoutElementPrefab.gameObject.SetActive(true);


        GetSetFilePaths(); //Important

        //Recalculate MIN_FILE_POSITION
        SetMinFilePosition();

        int numPie = 1;
        if (MIN_FILE_POSITION == MAX_FILE_POSITION)
        {
            UpdatePieChart(pieChart, MIN_FILE_POSITION);
        }
        else
        {
            for (int i = MIN_FILE_POSITION + 1; i <= MAX_FILE_POSITION; i++)
            {
                UpdatePieChart(pieChart, i);

                snap.PushLayoutElement(Instantiate(layoutElementPrefab));
                numPie++;
            }
        }

        snap.MoveToIndex(numPie - 1);
    }

	public void SetMinFilePosition()
	{
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

    public void UpdatePieChart(PieChart pieChart, int filePosition, bool isDescOrder = true)
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
                if (isDescOrder)
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
                }
                else
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
                    pieChart.sleepTimeText.text = sleepTime;

                    DateTime fileDateTime = Kaimin.Common.Utility.TransFilePathToDate(filePaths[_selectIndex]);
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
                        makePieChart(pieChart, pieValues, pieLabels);
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

                    //System.Random random = new System.Random();
                    //sleepLevel = random.Next(0, 5);
                    String[] levelColors = new String[5] { "#ff0000", "#ff6600", "#ffff4d", "#72ef36", "#0063dc" };
                    pieChart.circleOuter.GetComponent<Image>().color = convertHexToColor(levelColors[sleepLevel - 1]);
                }
            }
        }
        catch (System.Exception e)
        {
        }

        if (chartInfo == null) //No data 
        {
            //circleOuter.SetActive(false);
            pieChart.circleOuter.GetComponent<Image>().color = convertHexToColor("#0063dc"); //Default is level 5
            pieChart.pieInfo.hidePieInfo();
            pieChart.piePrefab.fillAmount = 0;
            pieChart.sleepTimeText.text = "-";
            pieChart.sleepDateText.text = "";
        }
    }

    public void makePieChart(PieChart pieChart, double[] pieValues, string[] pieLabels)
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
                pieChart.pieInfo.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, zPieInfoRotation));
                pieChart.pieInfo.hidePieInfo();

                float fillAmount = (float)(pieValues[i] / total);
                Image image = Instantiate(pieChart.piePrefab) as Image;
                image.transform.SetParent(pieChart.circleOuter.transform, false);
                image.color = pieColors[i];
                image.fillAmount = fillAmount;
                image.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, zRotation));

                int[] position = getPositionByPercent(fillAmount * 100, drawAngleTotal);
                PieInfo p = Instantiate(pieChart.pieInfo) as PieInfo;
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
        dict[0] = new int[2] { -2, -320 };
        dict[0.25] = new int[2] { -3, -320 };
        dict[0.5] = new int[2] { -4, -320 };
        dict[1] = new int[2] { -5, -320 };
        dict[1.5] = new int[2] { -7, -320 };
        dict[2] = new int[2] { -9, -320 };
        dict[2.5] = new int[2] { -10, -320 };
        dict[3] = new int[2] { -11, -320 };
        dict[4] = new int[2] { -15, -320 };

        if ((45 <= drawAngleTotal && drawAngleTotal <= 135) || (225 <= drawAngleTotal && drawAngleTotal <= 315))
        {
            dict[5] = new int[2] { -28, -325 };
            dict[6] = new int[2] { -35, -325 };
            dict[7] = new int[2] { -35, -325 };
            dict[7.5] = new int[2] { -35, -325 };
            dict[8] = new int[2] { -35, -325 };
            dict[9] = new int[2] { -35, -325 };
        }
        else
        {
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
            if (percent <= entry.Key)
            {
                key2 = entry.Key;
                break;
            }
            else
            {
                key1 = entry.Key;
            }
        }

        return (key2 - percent > percent - key1) ? dict[key1] : dict[key2];
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
