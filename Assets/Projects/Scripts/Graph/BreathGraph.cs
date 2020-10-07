using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;
using UnityEngine.UI;
using System;

namespace Graph
{
    /// <summary>
    /// 呼吸のデータを受け取り、グラフに渡すためのクラス
    /// グラフに表示するためにデータの加工を行う
    /// </summary>
    public class BreathGraph : MonoBehaviour, IGraphDataSwitch
    {
        public Color lineColor;						//折れ線グラフの色
        public HeadDirGraph headDirGraph;			//頭の向きをグラフに表示する際の色のパターンを参照する
        public List<BreathState> breathLabelList;	//呼吸の状態をどうラベリングするか設定するリスト。閾値が低い順に設定する
        public GraphItem InputData;			        //呼吸データを持っている、IBreathGraphインターフェースを実装したクラス
        //IBreathData input;
        public BarChart Output_Bar;					//出力先のバーグラフ
        public WMG_Series Output_Line;				//出力先の折れ線グラフ
        public AxisTimeLabel Output_TimeLabel;		//時間を表示するためのラベル
        List<Data> dataList;						//表示する呼吸のデータを自分で持っておく
        public GameObject[] Label;					//呼吸グラフで表示するラベル　表示・非表示を切り替える
        public GameObject[] Legend;					//呼吸グラフで表示する凡例　表示・非表示を切り替える
        public GameObject Output_AnalyzeTable;		//呼吸グラフの分析データを表示する表全体のオブジェクト。表示・非表示に使用する
        public Text[] Output_AnalyzeText_Normal;	//体の向きごとの分析データ出力先＿通常 (左・上・右・下の順で設定)
        public Text[] Output_AnalyzeText_Apnea;		//体の向きごとの分析データ出力先＿無呼吸 (左・上・右・下の順で設定)

        public PercetageBarChart Output_Percentage_Left;	//体の向きが左のときの呼吸の状態の集計出力先
        public PercetageBarChart Output_Percentage_Up;		//体の向きが上のときの呼吸の状態の集計出力先
        public PercetageBarChart Output_Percentage_Right;	//体の向きが右のときの呼吸の状態の集計出力先
        public PercetageBarChart Output_Percentage_Down;	//体の向きが下のときの呼吸の状態の集計出力先

        public AxisTimeLabel Output_AggrigateTimeLabel;		//集計のグラフの時間を表示するためのラベル


        void Awake()
        {
            /*
            input = InputData.GetComponent<IBreathData>();
            InputData.OnGraphDataChange.Subscribe(_ =>
            {
                //グラフに表示するデータが変更された際に実行される
                //最新のデータを取得し、保持する
                dataList = input.GetBreathDatas(); //異常データを取り除く
            });
            */
        }

        /// <summary>
        /// センシングした呼吸のデータをグラフ表示に適したデータに変換したクラス
        /// </summary>
        public class Data
        {
            Graph.Time time;	//検知した時間
            float oxygenRate = 0;	//酸素量(0~100%) TODO: 使わない。消す

            /// <summary>
            /// 呼吸状態1
            /// </summary>
            public SleepData.BreathState BreathState1 { get; set; }

            /// <summary>
            /// 呼吸状態2
            /// </summary>
            public SleepData.BreathState BreathState2 { get; set; }

            /// <summary>
            /// 呼吸状態3
            /// </summary>
            public SleepData.BreathState BreathState3 { get; set; }

            /// <summary>
            /// 頭の向き1
            /// </summary>
            public SleepData.HeadDir HeadDir1 { get; }

            /// <summary>
            /// 頭の向き2
            /// </summary>
            public SleepData.HeadDir HeadDir2 { get; }

            /// <summary>
            /// 頭の向き3
            /// </summary>
            public SleepData.HeadDir HeadDir3 { get; }

            public Data(
                    Graph.Time time,
                    SleepData.BreathState breathState1,
                    SleepData.BreathState breathState2,
                    SleepData.BreathState breathState3,
                    SleepData.HeadDir headDir1,
                    SleepData.HeadDir headDir2,
                    SleepData.HeadDir headDir3)
            {
                this.time = time;
                this.BreathState1 = breathState1;
                this.BreathState2 = breathState2;
                this.BreathState3 = breathState3;
                this.HeadDir1 = headDir1;
                this.HeadDir2 = headDir2;
                this.HeadDir3 = headDir3;
            }

            /// <summary>
            /// データの検知時間を取得します
            /// </summary>
            public Graph.Time GetTime()
            {
                return this.time;
            }


            /// <summary>
            /// データの検知時間を取得します
            /// n=1の場合10秒後、n=2の場合20秒後を取得
            /// </summary>
            public DateTime GetTimeValue(int n)
            {
                switch (n){
                    case 1:
                        return this.time.Value.AddSeconds(10);
                    case 2:
                        return this.time.Value.AddSeconds(20);
                    default:
                        return this.time.Value;
                    
                }
            }

            /// <summary>
            /// 酸素量(0~100%)を取得します
            /// </summary>
            /// <returns>The oxygen rate.</returns>
            public float GetOxygenRate()
            {
                return this.oxygenRate;
            }

            /// <summary>
            /// 呼吸状態1を取得します
            /// </summary>
            public SleepData.BreathState GetBreathState1()
            {
                return this.BreathState1;
            }

            /// <summary>
            /// 呼吸状態2を取得します
            /// </summary>
            public SleepData.BreathState GetBreathState2()
            {
                return this.BreathState2;
            }

            /// <summary>
            /// 呼吸状態3を取得します
            /// </summary>
            public SleepData.BreathState GetBreathState3()
            {
                return this.BreathState3;
            }


            /// <summary>
            /// 呼吸状態を不明に変更します
            /// </summary>
            public void setBreathState(int n) 
            {
                switch (n) {
                    case 0:
                    this.BreathState1 = SleepData.BreathState.Empty;
                    break;
                    case 1:
                    this.BreathState2 = SleepData.BreathState.Empty;
                    break;
                    case 2:
                    this.BreathState3 = SleepData.BreathState.Empty;
                    break;
                }
            }
        }

        [System.Serializable]
        /// <summary>
        /// 呼吸の状態をラベリングするための、表示形式と判断基準をまとめたクラス
        /// 表示形式はラベルの名前と色
        /// 判断基準は、どの程度の酸素量をどのラベルに設定するかの閾値
        /// </summary>
        public class BreathState
        {
            [SerializeField]
            SleepData.BreathState state;
            [SerializeField]
            LabelData.Label label;
            [SerializeField]
            float valueRate;

            /// <summary>
            /// どの状態か取得します
            /// </summary>
            public SleepData.BreathState GetBreathState()
            {
                return state;
            }

            /// <summary>
            /// ラベルを取得します
            /// </summary>
            public LabelData.Label GetLabel()
            {
                return label;
            }

            /// <summary>
            /// ラベルの値の全体との比率を取得します
            /// バーチャートでの表示の高さに利用されます
            /// </summary>
            public float GetValueRate()
            {
                return valueRate;
            }
        }

        /// <summary>
        /// IGraphDataSwitch実装
        /// グラフに呼吸のデータをアタッチします
        /// </summary>
        public void SetActive()
        {
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

            if(dataList == null)
            {
                dataList = InputData.GetBreathDatas();
            }

            if (dataList != null)
            {
                //グラフに表示するためにラベルデータを作成
                List<LabelData> labelDataList = TransSensingDataToLabelData(dataList);
                //バーグラフに呼吸データを設定・表示
                SetBreathDataToBarChart(dataList, labelDataList);
                //分析データを設定・表示
                SetBreathDataToAnalyzeTable();
                //集計のバーグラフを設定・表示
                SetBreathDataToPercentageBarChart(dataList);
            }
        }

        /// <summary>
        /// IGraphDataSwitch実装
        /// グラフから呼吸のデータを取り除きます
        /// </summary>
        public void SetDisActive()
        {
            RemoveBreathDataFromLineGraph();	//折れ線グラフ初期化
            RemoveBreathDataFromBarChart();	//バーグラフ初期化
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
        }

        //折れ線グラフから表示中の呼吸データを取り除きます
        void RemoveBreathDataFromLineGraph()
        {
            Output_Line.ClearPointValues();
        }

        //バーグラフから表示中の呼吸データを取り除きます
        void RemoveBreathDataFromBarChart()
        {
            Output_Bar.ClearBarElements();
        }

        //呼吸のデータを分析して表に表示する
        void SetBreathDataToAnalyzeTable()
        {
            foreach (SleepData.HeadDir headDir in System.Enum.GetValues(typeof(SleepData.HeadDir)))
            {
                Output_AnalyzeText_Normal[(int)headDir].text = DataDetail(dataList, headDir, SleepData.BreathState.Normal, SleepData.BreathState.Snore);
                Output_AnalyzeText_Apnea[(int)headDir].text = DataDetail(dataList, headDir, SleepData.BreathState.Apnea);
            }
        }

        //データの詳細を決定する
        string DataDetail(
            List<Data> dataList,
            SleepData.HeadDir headDir,
            params SleepData.BreathState[] targetStates)
        {
            int detectTime = 0;
            foreach (SleepData.BreathState breathState in targetStates)
            {
                // 0~10秒
                detectTime += dataList.Where(
                    data =>
                        data.HeadDir1.Equals(headDir) && data.GetBreathState1().Equals(breathState)
                    ).Count() * 10;	//検知データ件数×10秒で検知時間を計算

                // 11~20秒
                detectTime += dataList.Where(
                    data =>
                        data.HeadDir2.Equals(headDir) && data.GetBreathState2().Equals(breathState)
                    ).Count() * 10;	//検知データ件数×10秒で検知時間を計算

                // 21~30秒
                detectTime += dataList.Where(
                    data =>
                        data.HeadDir3.Equals(headDir) && data.GetBreathState3().Equals(breathState)
                    ).Count() * 10;	//検知データ件数×10秒で検知時間を計算
            }

            System.TimeSpan timeSpan = new System.TimeSpan(0, 0, detectTime);

            return timeSpan.ToString();	//hh:mm:ssの形式に変換して返す
		}

		public void ResizeBig() {
			if (dataList != null)
			{
				//グラフに表示するためにラベルデータを作成
				List<LabelData> labelDataList = TransSensingDataToLabelData(dataList);

				//バーグラフに呼吸データを設定・表示
				SetBreathDataToBarChart(dataList, labelDataList);
				//分析データを設定・表示
				SetBreathDataToAnalyzeTable();
				//集計のバーグラフを設定・表示
				SetBreathDataToPercentageBarChart(dataList);
			}
		}

		public void ResizeMin() {
			if (dataList != null)
			{
				//グラフに表示するためにラベルデータを作成
				List<LabelData> labelDataList = TransSensingDataToLabelData(dataList);

				//バーグラフに呼吸データを設定・表示
				SetBreathDataToBarChart(dataList, labelDataList);
				//分析データを設定・表示
				SetBreathDataToAnalyzeTable();
				//集計のバーグラフを設定・表示
				SetBreathDataToPercentageBarChart(dataList);
			}
		}

		public float sleepingTime = 1.0f;


        //呼吸のデータを折れ線グラフに表示する
        void SetBreathDataToLineGraph(List<Data> breathDataList)
        {
            List<Vector2> valueList = new List<Vector2>();
            for (int i = 0; i < breathDataList.Count; i++)
            {
                float xValueRate = Graph.Time.GetPositionRate(breathDataList[i].GetTime().Value, breathDataList.First().GetTime().Value, breathDataList.Last().GetTime().Value);
                valueList.Add(new Vector2(xValueRate, breathDataList[i].GetOxygenRate()));
            }

            Output_Line.lineColor = lineColor;
            Output_Line.SetPointValues(valueList);
            //グラフの時間軸も合わせて設定
            List<System.DateTime> timeList = breathDataList.Select(ibikiData => ibikiData.GetTime().Value).ToList();
        }

        //呼吸のデータを集計用グラフに出力
        void SetBreathDataToPercentageBarChart(List<Data> breathDataList)
        {
            //頭の向きで最も要素数が大きいものを探す(異常データは含まない)
            int maxHeadDirCount = 0;
            foreach (SleepData.HeadDir headDir in System.Enum.GetValues(typeof(SleepData.HeadDir)))
            {
                int count = 0;

                // 0~10秒
                count += breathDataList
                    .Where(data => data.HeadDir1 == headDir)
                    .Where(data => data.GetBreathState1() != SleepData.BreathState.Empty)	//異常データをはじく
                    .Count();

                // 11~20秒
                count += breathDataList
                    .Where(data => data.HeadDir2 == headDir)
                    .Where(data => data.GetBreathState2() != SleepData.BreathState.Empty)	//異常データをはじく
                    .Count();

                // 21~13秒
                count += breathDataList
                    .Where(data => data.HeadDir3 == headDir)
                    .Where(data => data.GetBreathState3() != SleepData.BreathState.Empty)	//異常データをはじく
                    .Count();
                maxHeadDirCount = count > maxHeadDirCount ? count : maxHeadDirCount;
            }

            //呼吸状態をグラフに表示する順番に並べ替え
            SleepData.BreathState[] sortedBreathState = {
                SleepData.BreathState.Normal,
                SleepData.BreathState.Snore,
                SleepData.BreathState.Apnea,
                SleepData.BreathState.Empty
            };

            int addCount = 0;	//グラフの右端に余裕を持たせるために追加する値
            foreach (SleepData.HeadDir headDir in System.Enum.GetValues(typeof(SleepData.HeadDir)))
            {
                //体の向きそれぞれに対して処理を行う
                List<LabelData> labelDataList = new List<LabelData>();

                foreach (SleepData.BreathState breathState in sortedBreathState)
                {
                    //呼吸状態それぞれに対して処理を行う
                    //体の向きが合致して、なおかつ呼吸状態も合致するデータの個数を求める
                    int value = 0;
                    // 0~10秒
                    value += breathDataList.Where(
                        data =>
                            data.HeadDir1.Equals(headDir) && data.GetBreathState1().Equals(breathState)
                        ).Count();

                    // 11~20秒
                    value += breathDataList.Where(
                        data =>
                            data.HeadDir2.Equals(headDir) && data.GetBreathState2().Equals(breathState)
                        ).Count();

                    // 21~30秒
                    value += breathDataList.Where(
                        data =>
                            data.HeadDir3.Equals(headDir) && data.GetBreathState3().Equals(breathState)
                        ).Count();

                    if (value == 0) continue;
                    LabelData.Label label = this.breathLabelList
                        .Where(l => l.GetBreathState().Equals(breathState))
                        .First().GetLabel();
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
            // 呼吸状態のデータ数が3倍になったため、3で割って正しい時間に直している
            int hour = ((maxHeadDirCount + addCount) / 2) / 3 / 60;
            int min = ((maxHeadDirCount + addCount) / 2) / 3 % 60;
            int sec = ((maxHeadDirCount + addCount) % 2) / 3 * 30;
            Output_AggrigateTimeLabel.SetAxis(hour, min, sec);
        }

        /// <summary>
        /// 呼吸グラフ(通常・無呼吸)を表示する
        /// </summary>
        /// <param name="breathDataList"></param>
        /// <param name="labelDataList"></param>
        void SetBreathDataToBarChart(List<Data> breathDataList, List<LabelData> labelDataList)
        {
            List<Vector2> xValueRangeList = new List<Vector2>();
            List<float> yValueList = new List<float>();
            List<LabelData.Label> labelList = new List<LabelData.Label>();

            for (int i = 0; i < breathDataList.Count - 1; i++)
            {
                LabelData[] labelDatas = new LabelData[3] { labelDataList[i * 3 + 0], labelDataList[i * 3 + 1], labelDataList[i * 3 + 2] };

                SleepData.BreathState breathState1 = breathDataList[i].GetBreathState1();
                SleepData.BreathState breathState2 = breathDataList[i].GetBreathState2();
                SleepData.BreathState breathState3 = breathDataList[i].GetBreathState3();
                SleepData.BreathState[] breathStates = new SleepData.BreathState[3] { breathState1, breathState2, breathState3 };

                SleepData.BreathState[] priorityStates = new SleepData.BreathState[4] { SleepData.BreathState.Apnea, SleepData.BreathState.Snore, SleepData.BreathState.Normal, SleepData.BreathState.Empty };

                int selectIndex = -1;
                //呼吸レスが1つでもあれば呼吸レス、
                //呼吸レスがなくいびきが1つでもあればいびき、
                //呼吸レス、いびきが１つもない（全部快眠）なら快眠
                //（色の表示優先度が呼吸レス ＞ いびき ＞ 快眠 な感じですかね）
                for(int k = 0; k < priorityStates.Length; k++)
                {
                    for (int j = 0; j < breathStates.Length; j++)
                    {
                        if (breathStates[j] == priorityStates[k])
                        {
                            selectIndex = j;
                            break;
                        }
                    }

                    if (selectIndex != -1) break;
                }

                if(selectIndex >= 0)
                {
                    SleepData.BreathState breathState = breathStates[selectIndex];

                    float yValueRate = breathLabelList.Where(label => label.GetBreathState().Equals(breathState)).First().GetValueRate();

                    float xStart = Graph.Time.GetPositionRate(
                            breathDataList[i].GetTime().Value.AddSeconds(0),
                            breathDataList.First().GetTime().Value,
                            breathDataList.Last().GetTime().Value);
                    float xEnd = Graph.Time.GetPositionRate(
                        breathDataList[i].GetTime().Value.AddSeconds(30),
                        breathDataList.First().GetTime().Value,
                        breathDataList.Last().GetTime().Value);

                    Vector2 xValueRange = new Vector2(xStart, xEnd);

                    xValueRangeList.Add(xValueRange);
                    yValueList.Add(yValueRate);
                    labelList.Add(labelDatas[selectIndex].GetLabel());
                }
            }

            Kaimin.Common.Utility.refineByCombineSameContinuousLabel(ref xValueRangeList, ref yValueList, ref labelList);

            Output_Bar.SetData(xValueRangeList, yValueList, labelList);
        }

        //取得した酸素量のデータをグラフに表示しやすいようにラベルデータへ変換する
        List<LabelData> TransSensingDataToLabelData(List<BreathGraph.Data> dataList)
        {
            List<LabelData> labelDataList = new List<LabelData>();

            int mukokyuuStartRow = 0;
            int mukokyuuStartRow2 = 0;

            long mukokyuStartTime = 0;
            long mukokyuContinuousTime = 0;

            for (int i=0;i<dataList.Count;i++) {
                BreathGraph.Data item = dataList[i];
                // statesはitemの0秒後、10秒後、20秒後の状態
                SleepData.BreathState[] states = {item.GetBreathState1(), item.GetBreathState2(), item.GetBreathState3()};
                for (int j=0;j<states.Length;j++) {
                    var state = states[j];

                    if (state == SleepData.BreathState.Apnea)
                    {
                        long tmpTime = ((System.DateTimeOffset)item.GetTimeValue(j)).ToUnixTimeSeconds();
                        if (mukokyuStartTime == 0)
                        {
                            mukokyuStartTime = tmpTime - 10;
                            mukokyuuStartRow = i;
                            mukokyuuStartRow2 = j;
                        } else
                        {
                            mukokyuContinuousTime = tmpTime - mukokyuStartTime;
                        }
                    }
                    else {
                        if (mukokyuContinuousTime > CSVManager.MAX_MUKOKYU_CONTINUOUS_TIME) //無呼吸状態が3分(180s)以上続いている場合
                        {
                            // 無呼吸開始から終了までの無呼吸状態を「無呼吸」から「不明」に変更
                            BreathStateApneaToEmpty(mukokyuuStartRow,mukokyuuStartRow2,i,j);
                        }

                        //Reset
                        mukokyuContinuousTime = 0;
                        mukokyuStartTime = 0;
                    }
                }
            }

            //Check when end of file
            if (mukokyuContinuousTime > CSVManager.MAX_MUKOKYU_CONTINUOUS_TIME) //無呼吸状態が3分(180s)以上続いている場合
            {
                // 無呼吸開始から終了までの無呼吸状態を「無呼吸」から「不明」に変更
                BreathStateApneaToEmpty(mukokyuuStartRow,mukokyuuStartRow2,dataList.Count,0);
            }

            foreach (BreathGraph.Data data in dataList)
            {
                labelDataList.Add(new LabelData(1f, MatchLabelBreathState1(data)));
                labelDataList.Add(new LabelData(1f, MatchLabelBreathState2(data)));
                labelDataList.Add(new LabelData(1f, MatchLabelBreathState3(data)));
            }
            labelDataList.RemoveAll(
                s => s == null
            );

            return labelDataList;
        }

        bool IsEndMukokyuu(BreathGraph.Data data,int j){
            if (j == 0 && data.GetBreathState1() != SleepData.BreathState.Apnea){
                return true;
            }
            else if (j == 1 && data.GetBreathState2() != SleepData.BreathState.Apnea){
                return true;
            }
            else if (j == 2 && data.GetBreathState3() != SleepData.BreathState.Apnea){
                return true;
            }

            return false;
        }

        void BreathStateApneaToEmpty(int startRow,int startRow2,int endRow,int endRow2){
            dataList[startRow].BreathState3 = SleepData.BreathState.Empty;
            if (startRow2 <= 1) {
                dataList[startRow].BreathState2 = SleepData.BreathState.Empty;
                if (startRow2 == 0) {
                    dataList[startRow].BreathState1 = SleepData.BreathState.Empty;
                }
            }

            for (int i = startRow + 1;i<endRow;i++){
                    dataList[i].BreathState1 = SleepData.BreathState.Empty;
                    dataList[i].BreathState2 = SleepData.BreathState.Empty;
                    dataList[i].BreathState3 = SleepData.BreathState.Empty;
            }

            if (endRow2 >= 1) {
                dataList[endRow].BreathState1 = SleepData.BreathState.Empty;
                if (endRow2 == 2) {
                    dataList[endRow].BreathState2 = SleepData.BreathState.Empty;
                }
            }
        }

        /// <summary>
        /// 呼吸状態1のラベルを取得する
        /// </summary>
        /// <param name="data"></param>
        /// <returns>ラベル</returns>
        LabelData.Label MatchLabelBreathState1(BreathGraph.Data data)
        {
            foreach (BreathState breathLabel in breathLabelList)
            {
                if (breathLabel.GetBreathState().Equals(data.GetBreathState1()))
                {
                    return breathLabel.GetLabel();
                }
            }

            return null;
        }

        /// <summary>
        /// 呼吸状態2のラベルを取得する
        /// </summary>
        /// <param name="data"></param>
        /// <returns>ラベル</returns>
        LabelData.Label MatchLabelBreathState2(BreathGraph.Data data)
        {
            foreach (BreathState breathLabel in breathLabelList)
            {
                if (breathLabel.GetBreathState().Equals(data.GetBreathState2()))
                {
                    return breathLabel.GetLabel();
                }
            }

            return null;
        }

        /// <summary>
        /// 呼吸状態3のラベルを取得する
        /// </summary>
        /// <param name="data"></param>
        /// <returns>ラベル</returns>
        LabelData.Label MatchLabelBreathState3(BreathGraph.Data data)
        {
            foreach (BreathState breathLabel in breathLabelList)
            {
                if (breathLabel.GetBreathState().Equals(data.GetBreathState3()))
                {
                    return breathLabel.GetLabel();
                }
            }

            return null;
        }
    }

    public interface IBreathData
    {
        List<BreathGraph.Data> GetBreathDatas();
    }
}
