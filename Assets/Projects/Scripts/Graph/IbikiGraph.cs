using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;
using UnityEngine.UI;

namespace Graph
{
    /// <summary>
    /// いびきのデータを受け取り、グラフに渡すためのクラス
    /// グラフに表示するためにデータの加工を行う
    /// </summary>
    public class IbikiGraph : MonoBehaviour, IGraphDataSwitch
    {

        public Color lineColor;					//折れ線グラフの色
        public HeadDirGraph headDirGraph;		//頭の向きをグラフに表示する際の色のパターンを参照する
        public List<IbikiLebel> ibikiLebelList;	//いびきの大きさをどうラベリングするか設定したリスト。閾値が低い順に設定する
        public GraphDataSource InputData;		//いびきデータを持っている、IIbikiDataインターフェースを実装したクラス
        IIbikiData input;						//入力データを取得するためのインターフェース
        public BarChart Output_Bar;				//出力先のバーグラフ
        public WMG_Series Output_Line;			//出力先の折れ線グラフ
		public AxisTimeLabel Output_TimeLabel;	//時間を表示するためのラベル
        List<Data> dataList;					//表示するいびきデータを自分で持っておく
        public GameObject[] Label;					//いびきグラフで表示するラベル　表示・非表示を切り替える
        public GameObject[] Legend;					//いびきグラフで表示する凡例　表示・非表示を切り替える
        public GameObject IbikiLebelBack;			//いびきグラフで表示する背景のいびきレベルの表示・非表示を切り替える
        public GameObject Output_AnalyzeTable;		//いびきグラフの分析データを表示する表全体のオブジェクト。表示・非表示に使用する
        public string[] Output_Analyze_ColumnLabelNameList;	//いびき吸グラフを分析した表の列に表示するデータリスト。インスペクタで設定したラベルの名前で、データを指定する
        public Text[] Output_Analyze_Text_None;		//体の向きごとの分析データ出力先 (列：左・上・右・下、行：いびき大・いびき小)
        public Text[] Output_Analyze_Text_Small;		//体の向きごとの分析データ出力先 (列：左・上・右・下、行：いびき大・いびき小)
        public Text[] Output_Analyze_Text_Large;		//体の向きごとの分析データ出力先 (列：左・上・右・下、行：いびき大・いびき小)

        public PercetageBarChart Output_Percentage_Left;	//体の向きが左のときの呼吸の状態の集計出力先
        public PercetageBarChart Output_Percentage_Up;		//体の向きが上のときの呼吸の状態の集計出力先
        public PercetageBarChart Output_Percentage_Right;	//体の向きが右のときの呼吸の状態の集計出力先
        public PercetageBarChart Output_Percentage_Down;	//体の向きが下のときの呼吸の状態の集計出力先

        public AxisTimeLabel Output_AggrigateTimeLabel;		//集計のグラフの時間を表示するためのラベル

		public GameObject IbikiMainGraph;
		public GameObject HeadDirGraphObject;
		public GameObject ScrollObject;
		public GameObject SeriesObject;
		public GameObject StartTimeObject;
		public GameObject EndTimeObject;
		public GameObject TimeObjectParent;

		public ScrollRect ScrollRect;
		public GameObject LinkParent;


		public RectTransform lineBottomRect;
		public RectTransform line250ImageRect;
		public RectTransform line500ImageRect;
		public RectTransform line750ImageRect;
		public RectTransform line1000ImageRect;

        public Text LineTitle = null;
        public Text LineTitleTop = null;
        public Text LineTitleBottom = null;

        public RectTransform lineTouchiRect;

		public GameObject scrollView;
		public GameObject zoomButton;

		bool onceDisplayFlag = false;
		List<System.DateTime> ibikiTimeList;
        // グラフ閾値
        int graphThreshold = 300;

        // 
        float lineUpValue = 68.0f;

        void Awake()
        {
            input = InputData.GetComponent<IIbikiData>();
            InputData.OnGraphDataChange.Subscribe(_ =>
            {
                //グラフに表示するデータが変更された際に実行される
                //最新のデータを取得し、保持する
                dataList = input.GetIbikiDatas();
            });
        }

        /// <summary>
        /// センシングした、いびきデータを扱いやすいデータ型に変換したクラス
        /// </summary>
        public class Data
        {
            Graph.Time time;		//検知した時間

            /// <summary>
            /// いびきの大きさ1
            /// </summary>
            public float SnoreVolume1 { get; }

            /// <summary>
            /// いびきの大きさ2
            /// </summary>
            public float SnoreVolume2 { get; }

            /// <summary>
            /// いびきの大きさ3
            /// </summary>
            public float SnoreVolume3 { get; }

            /// <summary>
            /// 頭の向き1
            /// </summary>
            public SleepData.HeadDir HeadDir1;

            /// <summary>
            /// 頭の向き2
            /// </summary>
            public SleepData.HeadDir HeadDir2;

            /// <summary>
            /// 頭の向き3
            /// </summary>
            public SleepData.HeadDir HeadDir3;

            public Data(
                    Graph.Time time,
                    float snoreVolume1,
                    float snoreVolume2,
                    float snoreVolume3,
                    SleepData.HeadDir headDir1,
                    SleepData.HeadDir headDir2,
                    SleepData.HeadDir headDir3)
            {
                this.time = time;
                this.SnoreVolume1 = snoreVolume1;
                this.SnoreVolume2 = snoreVolume2;
                this.SnoreVolume3 = snoreVolume3;
                this.HeadDir1 = headDir1;
                this.HeadDir2 = headDir2;
                this.HeadDir3 = headDir3;
            }

            /// <summary>
            /// データの検知時間を取得します
            /// </summary>
            public Graph.Time GetTime()
            {
                return time;
            }
        }

        [System.Serializable]
        /// <summary>
        /// いびきの大きさをラベリングするための、表示形式と判断基準をまとめたクラス
        /// 表示形式はラベルの名前、色
        /// 判断基準は、どの程度のいびきの大きさをどのラベルに設定するかの閾値
        /// </summary>
        public class IbikiLebel
        {
            [SerializeField]
            LabelData.Label label;	//ラベル名、色
            [SerializeField]
            float valueRate;		//ラベルの値の全体との比率。バーチャートでの表示の高さに利用する
            [SerializeField]
            float minValueRate;		//このラベルとする値の最小値の全体との比率(0~1f)

            /// <summary>
            /// ラベルを取得します
            /// </summary>
            /// <returns>The label.</returns>
            public LabelData.Label GetLabel()
            {
                return label;
            }
            /// <summary>
            /// ラベルの値の全体との比率を取得します
            /// バーチャートでの表示の高さに利用されます
            /// </summary>
            /// <returns>The value rate.</returns>
            public float GetValueRate()
            {
                return valueRate;
            }
            /// <summary>
            /// このラベルと決定する閾値を取得します
            /// </summary>
            public float GetThreshold()
            {
                return minValueRate;
            }
        }

        /// <summary>
        /// IGraphDataSwitch実装
        /// グラフにいびきデータをアタッチします
        /// </summary>
        public void SetActive()
        {
			zoomButton.SetActive(true);

            //ラベル・凡例を表示
            foreach (GameObject label in Label)
            {
                label.SetActive(true);
            }
            foreach (GameObject legend in Legend)
            {
                legend.SetActive(true);
            }
            Output_AnalyzeTable.SetActive(true);
            //背景のいびきレベルを表示
//            IbikiLebelBack.SetActive(true);

            LineTitle.gameObject.SetActive(true);
            LineTitleTop.gameObject.SetActive(true);
            LineTitleBottom.gameObject.SetActive(true);

            try
			{
				if (dataList != null)
				{
					//グラフに表示するためにラベルデータを作成
					List<LabelData> labelDataList = TransSensingDataToLabelData(dataList);

					float hour = sleepingTime (dataList);
					zoomButton.SetActive(true);
					if (hour <= 1.0f) {
						hour = 0.95f;
						zoomButton.SetActive(false);
					}
					else if (!scrollFlag){
						hour = 0.95f;
					}

					// スクロールビュー
					RectTransform sRect = ScrollObject.GetComponent<RectTransform>();
					var y2 = sRect.transform.position.y;
					sRect.sizeDelta=new Vector2(600*hour,186.3f); //サイズが変更できる

					// スクロール位置を初期化
					if (onceDisplayFlag) {
						ScrollRect.horizontalNormalizedPosition = 0.0f;
					}
						
                    // イビキグラフを取得
					RectTransform rect0 = IbikiMainGraph.GetComponent<RectTransform>();
					var x = rect0.transform.position.x;
					var y = rect0.transform.position.y;


					rect0.sizeDelta=new Vector2(600*hour,227);
					if (onceDisplayFlag) {
						rect0.localPosition=new Vector3(600*hour/2,200-321);
					} else {
						rect0.localPosition=new Vector3(600*hour/2,200);
					}

					RectTransform rectStart = StartTimeObject.GetComponent<RectTransform>();

					rectStart.localPosition=new Vector3(-600*hour/2+55,rectStart.transform.localPosition.y,0);

					RectTransform rectEnd = EndTimeObject.GetComponent<RectTransform>();
					rectEnd.localPosition = new Vector3(600*hour/2-55,rectStart.transform.localPosition.y,0);

					StartTimeObject.SetActive (false);
					EndTimeObject.SetActive (false);

					// 頭位置グラフの幅変更
					HeadDirGraphObject.GetComponent<HeadDirGraph> ().ResizeHeadDirDataBar (600*hour);

					//グラフの上限を1fに設定
					Output_Line.theGraph.yAxis.AxisMaxValue = 1f;
					//折れ線グラフにいびきデータを設定
					SetIbikiDataToLineGraph(dataList);

					//集計の詳細欄にデータを設定
					SetIbikiDataToAnalyzeTable();
					//集計のグラフにデータを設定
					SetBreathDataToPercentageBarChart(dataList);

					//目盛を調整
					Output_TimeLabel.SetIbikiScroll(hour);


                    // 縦軸閾値を計算
                    DeviceSetting deviceSetting = UserDataManager.Setting.DeviceSettingData.Load();
                    switch (deviceSetting.SnoreSensitivity) {
                        // 1 中
                        case SnoreSensitivity.Mid:
                            graphThreshold = 245;
                            break;
                        // 2 高
                        case SnoreSensitivity.High:
                            graphThreshold = 200;
                            break;
                        // 0 低
                        default:
                            graphThreshold = 300;
                            break;
                    }
					//線を調整
                    lineTouchiRect.sizeDelta = new Vector2(600*hour,3.0f);

					if (onceDisplayFlag) {
						lineTouchiRect.transform.localPosition = new Vector3(lineTouchiRect.transform.localPosition.x,21.85f-85.0f+(float)graphThreshold*85.0f/250.0f-321.0f + lineUpValue);
					} else {
						lineTouchiRect.transform.localPosition = new Vector3(lineTouchiRect.transform.localPosition.x,21.85f-85.0f+(float)graphThreshold*85.0f/250.0f + lineUpValue);
					}
                    
                    //lineTouchiRect.gameObject.SetActive(true);

					lineBottomRect.sizeDelta = new Vector2(600*hour,2.0f);
					line250ImageRect.sizeDelta = new Vector2(600*hour,2.0f);
					line500ImageRect.sizeDelta = new Vector2(600*hour,2.0f);
					line750ImageRect.sizeDelta = new Vector2(600*hour,2.0f);
					line1000ImageRect.sizeDelta = new Vector2(600*hour,2.0f);

                    // 最初の拡大縮小ボタンの設定
                    if (!onceDisplayFlag) {
                        zoomButton.GetComponent<GraphZoomManager>().setBigButtonAsDefault();
                    }

					StartCoroutine ("UpdateGraphPosition");
					SeriesObject.SetActive (false);

					onceDisplayFlag = true;
				}
			}
			catch
			{

				scrollView.SetActive (false);
			}


        }


        public void YLog(string prefix) {


            
            float ly0 = IbikiMainGraph.transform.localPosition.y;

            float yyy = line250ImageRect.transform.localPosition.y;
            float yy2 = line250ImageRect.position.y;

            GameObject obj = lineTouchiRect.gameObject.transform.parent.gameObject;


					RectTransform rect0 = IbikiMainGraph.GetComponent<RectTransform>();
					var y = rect0.position.y;

            Debug.Log(prefix + " graph " + ly0 + " " + IbikiMainGraph.transform.position.y);
            // Debug.Log(prefix + " parent " + obj.transform.localPosition.y + " " + obj.transform.position.y);
            // Debug.Log(prefix + " 250 " + yyy + " " + yy2);
        }

        /// <summary>
        /// IGraphDataSwitch実装
        /// グラフにアタッチしたデータを取り除きます
        /// </summary>
        public void SetDisActive()
        {
            RemoveIbikiDataFromLineGraph();	//折れ線グラフ初期化
            //グラフの上限を1.1fに戻す
            Output_Line.theGraph.yAxis.AxisMaxValue = 1.1f;
            //背景のいびきレベルを非表示
            IbikiLebelBack.SetActive(false);
            //ラベル・凡例を非表示
            foreach (GameObject label in Label)
            {
                label.SetActive(false);
            }
            foreach (GameObject legend in Legend)
            {
                legend.SetActive(false);
            }
            Output_AnalyzeTable.SetActive(false);

            LineTitle.gameObject.SetActive(false);
            LineTitleTop.gameObject.SetActive(false);
            LineTitleBottom.gameObject.SetActive(false);
        }

		public void ReSizeBig(){
			float hour = _sleepingTime;

			// スクロールビュー
			RectTransform sRect = ScrollObject.GetComponent<RectTransform>();
			var y2 = sRect.transform.position.y;
			sRect.sizeDelta=new Vector2(600*hour,186.3f); //サイズが変更できる

			// スクロール位置を初期化
			if (onceDisplayFlag) {
				ScrollRect.horizontalNormalizedPosition = 0.0f;
			}

			RectTransform rect0 = IbikiMainGraph.GetComponent<RectTransform>();
			var x = rect0.transform.position.x;
			var y = rect0.transform.position.y;


			rect0.sizeDelta=new Vector2(600*hour,227);
			if (onceDisplayFlag) {
				rect0.localPosition=new Vector3(600*hour/2f,200f-321f-8.55f);
			} else {
				rect0.localPosition=new Vector3(600*hour/2,200);
			}

			if (Output_Line.isRunningGraduallySetPointFlow ()) {
				Debug.Log ("描画中");
			} else {
				Debug.Log ("描画終了");
				// 再描画する
				SetIbikiDataToLineGraph(dataList);
			}

			// 頭の位置
			HeadDirGraphObject.GetComponent<HeadDirGraph> ().ResizeHeadDirDataBar (600 * hour);
			//グラフの時間軸も合わせて設定
			Output_TimeLabel.SetIbikiAxis (ibikiTimeList);
			//目盛を調整
			Output_TimeLabel.SetIbikiScroll(hour);

			StartCoroutine ("UpdateGraphPositionResizeBig");
			SeriesObject.SetActive (false);

            lineTouchiRect.sizeDelta = new Vector2(600*hour,3.0f);


			lineBottomRect.sizeDelta = new Vector2(600*hour,2.0f);
            line250ImageRect.sizeDelta = new Vector2(600*hour,2.0f);
            line500ImageRect.sizeDelta = new Vector2(600*hour,2.0f);
            line750ImageRect.sizeDelta = new Vector2(600*hour,2.0f);
            line1000ImageRect.sizeDelta = new Vector2(600*hour,2.0f);
		}


		public void ReSizeMin(){
			float hour = 0.95f;

			// スクロールビュー
			RectTransform sRect = ScrollObject.GetComponent<RectTransform>();
			var y2 = sRect.transform.position.y;
			sRect.sizeDelta=new Vector2(600*hour,186.3f); //サイズが変更できる

			// スクロール位置を初期化
			if (onceDisplayFlag) {
				ScrollRect.horizontalNormalizedPosition = 0.0f;
			}
				
			RectTransform rect0 = IbikiMainGraph.GetComponent<RectTransform>();
			var x = rect0.transform.position.x;
			var y = rect0.transform.position.y;


			rect0.sizeDelta=new Vector2(600*hour,227);
			if (onceDisplayFlag) {
				rect0.localPosition=new Vector3(600*hour/2,200-321);
			} else {
				rect0.localPosition=new Vector3(600*hour/2,200);
			}

			if (Output_Line.isRunningGraduallySetPointFlow ()) {
				Debug.Log ("描画中");
			} else {
				Debug.Log ("描画終了");
				// 再描画する
				SetIbikiDataToLineGraph(dataList);
			}

//			RectTransform rect1 = SeriesObject.GetComponent<RectTransform>();
//			rect1.localPosition=new Vector3(rect1.transform.localPosition.x,rect1.transform.localPosition.y-150,0);

			// 頭の位置
			HeadDirGraphObject.GetComponent<HeadDirGraph> ().ResizeHeadDirDataBar (600 * hour);
            HeadDirGraphObject.transform.localPosition = new Vector3(HeadDirGraphObject.transform.localPosition.x,-543.5f+8.5f);
			//グラフの時間軸も合わせて設定
			Output_TimeLabel.SetIbikiMinAxis (ibikiTimeList);
			//目盛を調整
			Output_TimeLabel.SetIbikiScroll(hour);

            IbikiMainGraph.transform.localPosition = new Vector3(IbikiMainGraph.transform.localPosition.x,-121.5f+8.5f);
			StartCoroutine ("UpdateGraphPositionResizeMin");
			SeriesObject.SetActive (false);

            //線を調整
            lineTouchiRect.sizeDelta = new Vector2(600*hour,3.0f);
            lineBottomRect.sizeDelta = new Vector2(600*hour,2.0f);
            
            line250ImageRect.sizeDelta = new Vector2(600*hour,2.0f);
            line500ImageRect.sizeDelta = new Vector2(600*hour,2.0f);
            line750ImageRect.sizeDelta = new Vector2(600*hour,2.0f);
            line1000ImageRect.sizeDelta = new Vector2(600*hour,2.0f);
		}

        //いびきのデータを分析して表に表示する
        void SetIbikiDataToAnalyzeTable()
        {
            Text[][] analyzeTable = { Output_Analyze_Text_None, Output_Analyze_Text_Small, Output_Analyze_Text_Large };
            foreach (SleepData.HeadDir headDir in System.Enum.GetValues(typeof(SleepData.HeadDir)))
            {
                for (int column = 0; column < Output_Analyze_ColumnLabelNameList.Count(); column++)
                {
                    analyzeTable[column][(int)headDir].text = GetTableText(dataList, headDir, Output_Analyze_ColumnLabelNameList[column]);
                }
            }
        }

        //表に表示するテキストを決定します
        string GetTableText(List<Data> dataList, SleepData.HeadDir headDir, string ibikiLabelName)
        {
            //頭の向き、いびきの大きさ両方の条件に合致したデータ個数
            int detectCount = 0;
            detectCount += dataList.Where(data => data.HeadDir1 == headDir && TransIbikiLoudnessToLabel(data.SnoreVolume1).GetName() == ibikiLabelName).Count();
            detectCount += dataList.Where(data => data.HeadDir2 == headDir && TransIbikiLoudnessToLabel(data.SnoreVolume2).GetName() == ibikiLabelName).Count();
            detectCount += dataList.Where(data => data.HeadDir3 == headDir && TransIbikiLoudnessToLabel(data.SnoreVolume3).GetName() == ibikiLabelName).Count();
            System.TimeSpan timeSpan = new System.TimeSpan(0, 0, detectCount * 10);	//データ個数から10秒をかけて検知時間とする
            return timeSpan.ToString();	//hh:mm:ssの形式に変換して返す
        }

        //いびきの大きさのデータを折れ線グラフに設定する
        void SetIbikiDataToLineGraph(List<Data> ibikiDataList)
        {

			RemoveIbikiDataFromLineGraph();	//折れ線グラフ初期化
            List<Vector2> valueList = new List<Vector2>();
            float xValueTotalRate = 0.0f;
            float yValueTotalRate = 0.0f;
            for (int i = 0; i < ibikiDataList.Count; i++)
            {
                System.DateTime detectionStartTime = ibikiDataList.First().GetTime().Value;
                System.DateTime detectionEndTime = ibikiDataList.Last().GetTime().Value.AddSeconds(20);

                // いびきの大きさ1の打点位置のx座標
                float xValueRate1 = Graph.Time.GetPositionRate(
                    ibikiDataList[i].GetTime().Value,
                    detectionStartTime,
                    detectionEndTime);
                // いびきの大きさ2の打点位置のx座標
                float xValueRate2 = Graph.Time.GetPositionRate(
                    ibikiDataList[i].GetTime().Value.AddSeconds(10),
                    detectionStartTime,
                    detectionEndTime);
                // いびきの大きさ3の打点位置のx座標
                float xValueRate3 = Graph.Time.GetPositionRate(
                    ibikiDataList[i].GetTime().Value.AddSeconds(20),
                    detectionStartTime,
                    detectionEndTime);

                // ６つの点の平均値の１点を加える
                if (i % 2 == 0) {
                    // ６つの点の内、最初の３点の情報を保持
                    xValueTotalRate = xValueRate1 + xValueRate2 + xValueRate3;
                    yValueTotalRate = ibikiDataList[i].SnoreVolume1 + ibikiDataList[i].SnoreVolume2 + ibikiDataList[i].SnoreVolume3;
                    // 最後が３点だけだった場合は、その３点の平均値を
                    if ( i == ibikiDataList.Count + 1){
                        
                        float aveXValueRate = xValueTotalRate / 3.0f;
                        float aveYValueRate = yValueTotalRate / 3.0f;

                        valueList.Add(new Vector2(aveXValueRate, aveYValueRate));
                    }
                }
                else {
                    // 前に保持した３点と今回取得した３点の合計６点の平均値の１点を加える
                    float aveXValueRate = (xValueTotalRate + xValueRate1 + xValueRate2 + xValueRate3) / 6.0f;
                    float aveYValueRate = (yValueTotalRate + ibikiDataList[i].SnoreVolume1 + ibikiDataList[i].SnoreVolume2 + ibikiDataList[i].SnoreVolume3) / 6.0f;

                    valueList.Add(new Vector2(aveXValueRate, aveYValueRate));
                }
                
                // valueList.Add(new Vector2(
                //     xValueRate2, ibikiDataList[i].SnoreVolume2));
                // valueList.Add(new Vector2(
                //     xValueRate3, ibikiDataList[i].SnoreVolume3));
            }

            Output_Line.lineColor = lineColor;
            Output_Line.SetPointValues(valueList);
            //グラフの時間軸も合わせて設定
			ibikiTimeList
                = ibikiDataList.Select(
				ibikiData => ibikiData.GetTime().Value).ToList();
			if (scrollFlag) {
				Output_TimeLabel.SetIbikiAxis (ibikiTimeList);
			} else {
				Output_TimeLabel.SetIbikiMinAxis (ibikiTimeList);
			}

            
        }

        //折れ線グラフから表示中のいびきデータを取り除きます
        void RemoveIbikiDataFromLineGraph()
        {
            Output_Line.ClearPointValues();
        }

		private float _sleepingTime;

		public float sleepingTime(){
			return _sleepingTime;
		}

		private bool scrollFlag = false;

		public bool IsScroll() {
			return scrollFlag;
		}

		public void setScroll(bool flag) {
			scrollFlag = flag;
		}

		float sleepingTime(List<IbikiGraph.Data> dataList){

			System.DateTime detectionStartTime = dataList.First().GetTime().Value;
			System.DateTime detectionEndTime = dataList.Last().GetTime().Value.AddSeconds(20);

			System.TimeSpan diff = detectionEndTime - detectionStartTime;

			_sleepingTime = (float)(diff.TotalHours);

			return (float)(diff.TotalHours);
		}


		IEnumerator UpdateGraphPosition()
		{
			yield return new WaitForEndOfFrame();

			SeriesObject.SetActive (true);
			RectTransform rect0 = SeriesObject.GetComponent<RectTransform>();
			rect0.localPosition=new Vector3(rect0.transform.localPosition.x,-263.5f + lineUpValue);

            Vector3 ibikiMainPosition = IbikiMainGraph.transform.localPosition;
            if (zoomButton.activeSelf && IsScroll()) {
                IbikiMainGraph.transform.localPosition = new Vector3(ibikiMainPosition.x,-121.5f);
            } else {
                IbikiMainGraph.transform.localPosition = new Vector3(ibikiMainPosition.x,-130.0f);
            }

            Vector3 headLocalPosition = HeadDirGraphObject.transform.localPosition;
            HeadDirGraphObject.transform.localPosition = new Vector3(headLocalPosition.x,-563.5f+20.0f);

            //線を調整
            if (zoomButton.activeSelf && IsScroll()) {
                lineTouchiRect.transform.localPosition = new Vector3(lineTouchiRect.transform.localPosition.x,-299.65f-85.0f-8.0f+(float)graphThreshold*85.0f/250.0f+8.5f + lineUpValue);
            } else {
                lineTouchiRect.transform.localPosition = new Vector3(lineTouchiRect.transform.localPosition.x,-299.65f-85.0f-8.0f+(float)graphThreshold*85.0f/250.0f + lineUpValue);
            }
		}

		IEnumerator UpdateGraphPositionResizeMin()
		{
			yield return new WaitForEndOfFrame();

			SeriesObject.SetActive (true);
			RectTransform rect0 = SeriesObject.GetComponent<RectTransform>();
			rect0.localPosition=new Vector3(rect0.transform.localPosition.x,rect0.transform.localPosition.y-150 + lineUpValue,0);

            Vector3 ibikiMainPosition = IbikiMainGraph.transform.localPosition;
            IbikiMainGraph.transform.localPosition = new Vector3(ibikiMainPosition.x,-130f);

            Vector3 headLocalPosition = HeadDirGraphObject.transform.localPosition;
            HeadDirGraphObject.transform.localPosition = new Vector3(headLocalPosition.x,-563.5f+20.0f);

            //線を調整
            lineTouchiRect.transform.localPosition = new Vector3(lineTouchiRect.transform.localPosition.x,-299.65f-85.0f+(float)graphThreshold*85.0f/250.0f+0.5f-8.5f + lineUpValue);

            lineBottomRect.transform.localPosition = new Vector3(lineBottomRect.transform.localPosition.x,-316.65f-6.8f-68.0f + lineUpValue);
            
            line250ImageRect.transform.localPosition = new Vector3(line250ImageRect.transform.localPosition.x,-316.65f-6.8f + lineUpValue);
            line500ImageRect.transform.localPosition = new Vector3(line500ImageRect.transform.localPosition.x,-316.65f-6.8f+68.0f + lineUpValue);
            line750ImageRect.transform.localPosition = new Vector3(line750ImageRect.transform.localPosition.x,-316.65f-6.8f+68.0f*2 + lineUpValue);
            line1000ImageRect.transform.localPosition = new Vector3(line1000ImageRect.transform.localPosition.x,-316.65f-6.8f+68.0f*3 + lineUpValue);
		}


		IEnumerator UpdateGraphPositionResizeBig()
		{
			yield return new WaitForEndOfFrame();

			SeriesObject.SetActive (true);
			RectTransform rect0 = SeriesObject.GetComponent<RectTransform>();
			rect0.localPosition=new Vector3(rect0.transform.localPosition.x,rect0.transform.localPosition.y-150 + lineUpValue,0);
			float y = IbikiMainGraph.transform.localPosition.y;

            Vector3 ibikiMainPosition = IbikiMainGraph.transform.localPosition;
            IbikiMainGraph.transform.localPosition = new Vector3(ibikiMainPosition.x,-121.5f);
            float y2 = IbikiMainGraph.transform.localPosition.y;

            Vector3 headLocalPosition = HeadDirGraphObject.transform.localPosition;
            HeadDirGraphObject.transform.localPosition = new Vector3(headLocalPosition.x,-563.5f+20.0f);

            //線を調整
            lineTouchiRect.transform.localPosition = new Vector3(lineTouchiRect.transform.localPosition.x,-299.65f-85.0f+(float)graphThreshold*85.0f/250.0f+0.5f + lineUpValue);
            lineBottomRect.transform.localPosition = new Vector3(lineBottomRect.transform.localPosition.x,-316.65f - 68.0f + lineUpValue);
            
            line250ImageRect.transform.localPosition = new Vector3(line250ImageRect.transform.localPosition.x,-316.65f + lineUpValue);
            line500ImageRect.transform.localPosition = new Vector3(line500ImageRect.transform.localPosition.x,-316.65f+68.0f + lineUpValue);
            line750ImageRect.transform.localPosition = new Vector3(line750ImageRect.transform.localPosition.x,-316.65f+68.0f*2 + lineUpValue);
            line1000ImageRect.transform.localPosition = new Vector3(line1000ImageRect.transform.localPosition.x,-316.65f+68.0f*3 + lineUpValue);
		}

        void LogYPosition(){
            Debug.Log("scrollObject " + ScrollObject.transform.position.y + " " + ScrollObject.transform.localPosition.y);
            Debug.Log("graph " + IbikiMainGraph.transform.position.y + " " + IbikiMainGraph.transform.localPosition.y);
            Debug.Log("headDirGraph " + HeadDirGraphObject.transform.position.y + " " + HeadDirGraphObject.transform.localPosition.y);
            Debug.Log("line250 " + line250ImageRect.transform.position.y + " " + line250ImageRect.transform.localPosition.y);
            Debug.Log("line750 " + line750ImageRect.transform.position.y + " " + line750ImageRect.transform.localPosition.y);
        }

        //取得した、いびきの大きさのデータをグラフに表示しやすいようにラベルデータへ変換する
        List<LabelData> TransSensingDataToLabelData(List<IbikiGraph.Data> dataList)
        {
            List<LabelData> labelDataList = new List<LabelData>();
            foreach (IbikiGraph.Data data in dataList)
            {
                LabelData.Label label = TransIbikiLoudnessToLabel(data.SnoreVolume2);	//ラベルの名前と色を設定する
                labelDataList.Add(new LabelData(data.SnoreVolume2, label));
            }
            return labelDataList;
        }

        //いびきの大きさをラベルに変換するロジックを実装する
        LabelData.Label TransIbikiLoudnessToLabel(float ibikiLoudnessRate)
        {
            //いびきの大きさの値をどのラベルに割り当てるか、割り当て方の設定
            for (int i = 0; i < ibikiLebelList.Count; i++)
            {
                //ラベルリストの並び順が閾値が低い順になっている事を前提とする
                if (ibikiLoudnessRate < ibikiLebelList[i].GetThreshold())
                {
                    //指定の値よりも閾値が大きいラベルがあれば、その一つ前のラベルを返す
                    return i > 0 ? ibikiLebelList[i - 1].GetLabel() : ibikiLebelList[i].GetLabel();
                }
            }
            return ibikiLebelList[ibikiLebelList.Count - 1].GetLabel();
        }

        //いびきのデータを集計用グラフに出力
        void SetBreathDataToPercentageBarChart(List<Data> ibikiDataList)
        {
            //頭の向きで最も要素数が大きいものを探す
            int maxHeadDirCount = 0;
            foreach (SleepData.HeadDir headDir in System.Enum.GetValues(typeof(SleepData.HeadDir)))
            {
                int count = 0;
                count += ibikiDataList
                    .Where(data => data.HeadDir1 == headDir)
                    .Count();

                count += ibikiDataList
                    .Where(data => data.HeadDir2 == headDir)
                    .Count();

                count += ibikiDataList
                    .Where(data => data.HeadDir3 == headDir)
                    .Count();
                maxHeadDirCount = count > maxHeadDirCount ? count : maxHeadDirCount;
            }
            int addCount = 0;	//グラフの右端に余裕を持たせるために追加する値
            foreach (SleepData.HeadDir headDir in System.Enum.GetValues(typeof(SleepData.HeadDir)))
            {
                //体の向きそれぞれに対して処理を行う
                List<LabelData> labelDataList = new List<LabelData>();
                foreach (IbikiLebel ibikiLebel in ibikiLebelList)
                {
                    //いびきのレベルそれぞれに対して処理を行う
                    //体の向きが合致して、なおかついびきレベルも合致するデータの個数を求める
                    int value = 0;
                    value += ibikiDataList.Where(
                        data =>
                            (data.HeadDir1.Equals(headDir) && TransIbikiLoudnessToLabel(data.SnoreVolume1).GetName().Equals(ibikiLebel.GetLabel().GetName()))
                    ).Count();

                    value += ibikiDataList.Where(
                        data =>
                            (data.HeadDir2.Equals(headDir) && TransIbikiLoudnessToLabel(data.SnoreVolume2).GetName().Equals(ibikiLebel.GetLabel().GetName()))
                    ).Count();

                    value += ibikiDataList.Where(
                        data =>
                            (data.HeadDir3.Equals(headDir) && TransIbikiLoudnessToLabel(data.SnoreVolume3).GetName().Equals(ibikiLebel.GetLabel().GetName()))
                    ).Count();

                    if (value == 0)
                        continue;
                    LabelData.Label label = ibikiLebel.GetLabel();
                    labelDataList.Add(new LabelData(value, label));
                }
                PercetageBarChart output = null;
                //呼吸データを体の向きごとに集計したものをグラフに出力する
                switch (headDir)
                {
                    case SleepData.HeadDir.Left:
                        output = Output_Percentage_Left;
                        break;
                    case SleepData.HeadDir.Up:
                        output = Output_Percentage_Up;
                        break;
                    case SleepData.HeadDir.Right:
                        output = Output_Percentage_Right;
                        break;
                    case SleepData.HeadDir.Down:
                        output = Output_Percentage_Down;
                        break;
                }
                addCount = maxHeadDirCount / 9;		//1割空白を作成するように
                output.SetPercentageData(maxHeadDirCount + addCount, labelDataList);
            }
            //データ個数からラベルとして表示するための時間を算出します
            // 体の向きのデータ個数が3倍になったため、3で割っている
            int hour = ((maxHeadDirCount + addCount) / 2) / 3 / 60;
            int min = ((maxHeadDirCount + addCount) / 2) / 3 % 60;
            int sec = ((maxHeadDirCount + addCount) % 2) / 3 * 30;
//            Output_AggrigateTimeLabel.SetAxis(hour, min, sec);
        }
    }

    public interface IIbikiData
    {
        List<IbikiGraph.Data> GetIbikiDatas();
    }
}
