﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniRx;
using System.Linq;
using UnityEngine.UI;
using Kaimin.Common;

namespace Graph
{
    /// <summary>
    /// センシングデータをグラフにアタッチできる形式に変換する。
    /// グラフで使用できるデータを提供する。
    /// </summary>
    public class GraphDataSource : MonoBehaviour
    {
        List<SleepData> sleepDataList;      //取得した睡眠データ
        SleepHeaderData sleepHeaderData;    //取得したCSVヘッダーに記述された睡眠データ

        Button _nextDateButton;
        Button _backDateButton;
        public Text _dateText;

        [SerializeField] Canvas canvas;
        public GraphItem graphItem;
        GraphItemSlider graphItemSlider;

        string[] filePaths; //取得したファイル一覧
        int selectedGraphIndex = -1;
        int MIN_FILE_POSITION = 0;  //選択範囲のMIN
        int MAX_FILE_POSITION = -1; //選択範囲のMAX

        int CURRENT_START_INDEX = 0;
        int CURRENT_END_INDEX = 0;

        void Start()
        {
            GameObject cube;
            cube = GameObject.Find("NextDateButton");
            _nextDateButton = cube.GetComponent<Button>();

            cube = GameObject.Find("BackDateButton");
            _backDateButton = cube.GetComponent<Button>();

            graphItemSlider = canvas.GetComponentInChildren<GraphItemSlider>();
            graphItemSlider.controllerDelegate = this;

            GetSetFilePaths();
            SetMaxFilePosition(); //Recalculate MAX_FILE_POSITION
            SetMinFilePosition(); //Recalculate MIN_FILE_POSITION
            
            graphItemSlider.graphItems = new List<GraphItem>();
            graphItemSlider.filePaths = new List<String>();

            if (MIN_FILE_POSITION >= 0 && MAX_FILE_POSITION >= 0)
            {
                int graphIndex = 0;
                for (int i = MIN_FILE_POSITION; i <= MAX_FILE_POSITION; i++)
                {
                    graphItemSlider.PushGraphItemData(filePaths[i]); 
                    graphIndex++;
                }

                selectedGraphIndex = graphItemSlider.filePaths.Count - 1; //Default (最新データを表示)

                DateTime targetDate = UserDataManager.Scene.GetGraphDate();
                //合致する日付データを検索する
                bool isExistSelectData = graphItemSlider.filePaths
                    .Where(path => Kaimin.Common.Utility.TransFilePathToDate(path) == targetDate && targetDate != DateTime.MinValue)
                    .Count() > 0;
                if (isExistSelectData)
                {
                    //日付を選択して表示したい場合
                    selectedGraphIndex = graphItemSlider.filePaths
                        .Select((path, index) => new { Path = path, Index = index })
                        .Where(data => Kaimin.Common.Utility.TransFilePathToDate(data.Path) == targetDate)
                        .First().Index;
                }

                CURRENT_START_INDEX = Math.Max(0, selectedGraphIndex - 5);
                CURRENT_END_INDEX = Math.Min(graphItemSlider.filePaths.Count - 1, selectedGraphIndex + 5);

                graphIndex = 0;
                for (int i = MIN_FILE_POSITION; i <= MAX_FILE_POSITION; i++)
                {
                    if(graphIndex >= CURRENT_START_INDEX && graphIndex <= CURRENT_END_INDEX)
                    {
                        graphItemSlider.PushGraphItemLayout(Instantiate(graphItem), graphIndex);
                    }

                    graphIndex++;
                }

                graphItemSlider.RemoveDefaultLayoutElement(); //Remove default

                StartCoroutine(LoadSleepDataInFirstTime());

                graphItemSlider.MoveToIndex(selectedGraphIndex - CURRENT_START_INDEX);
            }
            else
            {
                //表示するデータがなければ、NODATAを表示する
                graphItem.noDataImage.enabled = true;
                graphItem.scrollView.SetActive(false);
                graphItem.ibikiGraph.zoomButton.SetActive(false);
            }

            updatePrevNextBtnState(); //暫定：次のインデックスが存在有無で有効/無効を切り替え
        }

        private IEnumerator LoadSleepDataInFirstTime()
        {
            loadSleepData(selectedGraphIndex);

            yield return new WaitForSeconds(0.01f);

            AttachData();

            yield return new WaitForSeconds(0.1f);

            graphItemSlider.graphItems[selectedGraphIndex].ibikiGraph.ReSizeMin();
        }

        // Update graph data here
        public void UpdateGraphItem(int cellIndex, bool isToNext = false)
        {
            int beforeSelectedGraphIndex = selectedGraphIndex;
            selectedGraphIndex = CURRENT_START_INDEX + cellIndex;

            if(beforeSelectedGraphIndex == selectedGraphIndex) //Not change
            {
                if(cellIndex == 0) //Load more data if need when slider first item
                {
                    while(selectedGraphIndex > 0)
                    {
                        String filePath = graphItemSlider.filePaths[selectedGraphIndex - 1];
                        SleepHeaderData tmpSleepHeaderData = ReadSleepHeaderDataFromCSV(filePath);
                        List<SleepData> tmpSleepDatas = ReadSleepDataFromCSV(filePath);
                        if (tmpSleepHeaderData != null && tmpSleepDatas != null && tmpSleepDatas.Count > 0)
                        {
                            UserDataManager.Scene.SaveGraphDate(tmpSleepHeaderData.DateTime);
                            reloadGraphScreen();
                            break;
                        } else
                        {
                            selectedGraphIndex--;
                        }
                    }

                    return;

                }
                else if (cellIndex == getNumGraphsCurrent() - 1) //Load more data if need when slider last item
                {
                    while (selectedGraphIndex < graphItemSlider.filePaths.Count - 1)
                    {
                        String filePath = graphItemSlider.filePaths[selectedGraphIndex + 1];
                        SleepHeaderData tmpSleepHeaderData = ReadSleepHeaderDataFromCSV(filePath);
                        List<SleepData> tmpSleepDatas = ReadSleepDataFromCSV(filePath);
                        if (tmpSleepHeaderData != null && tmpSleepDatas != null && tmpSleepDatas.Count > 0)
                        {
                            UserDataManager.Scene.SaveGraphDate(tmpSleepHeaderData.DateTime);
                            reloadGraphScreen();
                            break;
                        }
                        else
                        {
                            selectedGraphIndex++;
                        }
                    }

                    return;
                }
            }

            graphItemSlider.cellIndex = cellIndex;
            graphItemSlider.actualIndex = cellIndex;

            loadSleepData(selectedGraphIndex);

            if (sleepHeaderData != null && sleepDataList != null && sleepDataList.Count > 0)
            {
                AttachData();
            } else
            {
                String filePath = graphItemSlider.filePaths[selectedGraphIndex];
                graphItemSlider.RemoveGraphItemData(selectedGraphIndex);
                graphItemSlider.RemoveGraphItemLayout(selectedGraphIndex - CURRENT_START_INDEX);
                StartCoroutine(Utility.DeleteInvalidFile(filePath));

                if (isToNext)
                {
                    if (selectedGraphIndex < graphItemSlider.filePaths.Count - 1)
                    {
                        this.UpdateGraphItem(selectedGraphIndex - CURRENT_START_INDEX, isToNext);
                    }
                }
                else
                {
                    if (selectedGraphIndex > 0)
                    {
                        graphItemSlider.SnapToIndex(selectedGraphIndex - CURRENT_START_INDEX - 1);
                        //this.UpdateGraphItem(graphIndex - 1, isToNext);
                        //graphItemSlider.MoveToIndex(graphIndex - 1);
                    }
                }
            }

            updatePrevNextBtnState();
        }

        public int getNumGraphsCurrent()
        {
            int num = 0;

            for (int i = 0; i < graphItemSlider.graphItems.Count; i++)
            {
                if (i >= CURRENT_START_INDEX && i <= CURRENT_END_INDEX && graphItemSlider.graphItems[i] != null)
                {
                    num++;
                }
            }

            return num;
        }

        ////睡眠のヘッダーデータをCSVから取得する
        public void loadSleepData(int graphIndex)
        {
            String filePath = graphItemSlider.filePaths[graphIndex];

            if (graphItemSlider.sleepDatas[graphIndex] == null)
            {
                graphItemSlider.sleepDatas[graphIndex] = ReadSleepDataFromCSV(filePath); //睡眠データをCSVから取得する
                graphItemSlider.sleepHeaderDatas[graphIndex] = ReadSleepHeaderDataFromCSV(filePath); //睡眠のヘッダーデータをCSVから取得する
            }

            this.sleepDataList = graphItemSlider.sleepDatas[graphIndex];
            this.sleepHeaderData = graphItemSlider.sleepHeaderDatas[graphIndex];

            if (graphItemSlider.graphItems[graphIndex] != null)
            {
                graphItemSlider.graphItems[graphIndex].noDataImage.enabled = false;
                graphItemSlider.graphItems[graphIndex].scrollView.SetActive(true);
            } else
            {   //Reload Graph Screen
                UserDataManager.Scene.SaveGraphDate(sleepHeaderData.DateTime);

                reloadGraphScreen();
            }
        }

        public void reloadGraphScreen()
        {
            //タブは初期状態で選択されるように設定
            UserDataManager.Scene.InitGraphTabSave();
            UserDataManager.Scene.InitGraphDataTabSave();
            SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Graph);
        }

        public void GetSetFilePaths()
        {
            filePaths = Kaimin.Common.Utility.GetAllFiles(Kaimin.Common.Utility.GsDataPath(), "*.csv");
        }

        //Get valid MAX_FILE_POSITION
        public void SetMaxFilePosition() {
            MAX_FILE_POSITION = -1;
            for (int i = filePaths.Length - 1; i >= 0; i--)
            {
                List<SleepData> sleepDatas = CSVSleepDataReader.GetSleepDatas(filePaths[i]); //最新の睡眠データのリスト
                if (sleepDatas != null && sleepDatas.Count > 0)
                {
                    MAX_FILE_POSITION = i; //最新のファイルを取得
                    break;
                }
            }
        }

        //Get valid MIN_FILE_POSITION
        public void SetMinFilePosition()
        {
            MIN_FILE_POSITION = MAX_FILE_POSITION; //Default
            for (int i = 0; i <= MAX_FILE_POSITION; i++)
            {
                List<SleepData> sleepData = CSVSleepDataReader.GetSleepDatas(filePaths[i]); //最新の睡眠データのリスト
                if (sleepData != null && sleepData.Count > 0)
                {
                    MIN_FILE_POSITION = i; //最新のファイルを取得
                    break;
                }
            }
        }


        //AttachData()で自動的に呼び出される
        public List<IbikiGraph.Data> GetIbikiDatas()
        {
            //CSVから取得した睡眠データをIbikiGraph.Dataに変換して返す
            List<IbikiGraph.Data> resultList = new List<IbikiGraph.Data>();
            foreach (SleepData data in sleepDataList)
            {
                DateTime time = data.GetDateTime();                 // 睡眠データのセンシング時刻
                // いびきの大きさの値を、上限値に対する割合(0~1.0f)に修正する
                float snoreVolume1Rate = data.SnoreVolume1 / (float)SleepData.MaxSnoreVolume;  // いびきの大きさ1
                float snoreVolume2Rate = data.SnoreVolume2 / (float)SleepData.MaxSnoreVolume;  // いびきの大きさ2
                float snoreVolume3Rate = data.SnoreVolume3 / (float)SleepData.MaxSnoreVolume;  // いびきの大きさ3

                // 1超過時は1に丸める(デバイス側の設計上では、超過することはない)
                snoreVolume1Rate = snoreVolume1Rate > 1.0f ? 1.0f : snoreVolume1Rate;
                snoreVolume2Rate = snoreVolume2Rate > 1.0f ? 1.0f : snoreVolume2Rate;
                snoreVolume3Rate = snoreVolume3Rate > 1.0f ? 1.0f : snoreVolume3Rate;

                // ここで大きさ調整
                snoreVolume1Rate = snoreVolume1Rate * 0.8f;
                snoreVolume2Rate = snoreVolume2Rate * 0.8f;
                snoreVolume3Rate = snoreVolume3Rate * 0.8f;

                SleepData.HeadDir headDir1 = data.GetHeadDir1();
                SleepData.HeadDir headDir2 = data.GetHeadDir2();
                SleepData.HeadDir headDir3 = data.GetHeadDir3();

                resultList.Add(
                    new IbikiGraph.Data(
                        new Time(time),
                        snoreVolume1Rate,
                        snoreVolume2Rate,
                        snoreVolume3Rate,
                        headDir1,
                        headDir2,
                        headDir3));
            }

            return resultList;
        }

        //AttachData()で自動的に呼び出される
        public List<BreathGraph.Data> GetBreathDatas()
        {
            //CSVから取得した睡眠データをBreathGraph.Dataに変換して返す
            List<BreathGraph.Data> resultList = new List<BreathGraph.Data>();
            foreach (SleepData data in sleepDataList)
            {
                DateTime time = data.GetDateTime();
                SleepData.BreathState breathState1 = data.GetBreathState1();
                SleepData.BreathState breathState2 = data.GetBreathState2();
                SleepData.BreathState breathState3 = data.GetBreathState3();

                SleepData.HeadDir headDir1 = data.GetHeadDir1();
                SleepData.HeadDir headDir2 = data.GetHeadDir2();
                SleepData.HeadDir headDir3 = data.GetHeadDir3();

                resultList.Add(new BreathGraph.Data(
                    new Time(time),
                    breathState1,
                    breathState2,
                    breathState3,
                    headDir1,
                    headDir2,
                    headDir3));
            }

            return resultList;
        }

        //AttachData()で自動的に呼び出される
        public List<HeadDirGraph.Data> GetHeadDirDatas()
        {
            //CSVから取得した頭の向きのデータをHeadDirGraph.Dataに変換して返す
            List<HeadDirGraph.Data> resultList = new List<HeadDirGraph.Data>();
            foreach (SleepData data in sleepDataList)
            {
                DateTime time = data.GetDateTime();
                SleepData.HeadDir headDir1 = data.GetHeadDir1();
                SleepData.HeadDir headDir2 = data.GetHeadDir2();
                SleepData.HeadDir headDir3 = data.GetHeadDir3();

                resultList.Add(new HeadDirGraph.Data(
                    new Time(time),
                    headDir1,
                    headDir2,
                    headDir3));
            }

            return resultList;
        }

        //AttachData()で自動的に呼び出される
        public SleepDataDetail GetSleepInfoData()
        {
            DateTime bedTime = sleepHeaderData.DateTime;
            DateTime getUpTime = sleepDataList.Select(dataList => dataList.GetDateTime()).Last();
            int snoreCount = sleepHeaderData.SnoreDetectionCount;
            int apneaCount = sleepHeaderData.ApneaDetectionCount;
            int snoreTime = sleepHeaderData.SnoreTime;
            int apneaTime = sleepHeaderData.ApneaTime;
            int longestApneaTime = sleepHeaderData.LongestApneaTime;

            var sleepTimeSpan = getUpTime.Subtract(bedTime);
            double sleepTimeTotal = sleepTimeSpan.TotalSeconds;
            int snoreStopCount = getSnoreStopCount(sleepDataList, sleepHeaderData.SleepMode);

            double  snoreRate10 = (snoreTime / sleepTimeTotal ) * 1000.0;
            int snoreRate10Int = (int)snoreRate10;
            bool kuriagari = false;
            if (snoreRate10 - snoreRate10Int >= 0.5) {
                snoreRate10Int += 1;
            }

            double snoreRate = snoreRate10Int / 10.0;
            
            double apneaAverageCount = sleepTimeTotal == 0 ? 0 : (double) (apneaCount  * 3600) / sleepTimeTotal;  // 0除算を回避

            apneaAverageCount = Math.Truncate(apneaAverageCount * 10) / 10.0;   // 小数点第2位以下を切り捨て

            DateTime from = new DateTime(bedTime.Year, bedTime.Month, bedTime.Day, 0, 0, 0);
            DateTime to = new DateTime(bedTime.Year, bedTime.Month, bedTime.Day, 23, 59, 59);
            List<string> todayDataPathList = PickFilePathInPeriod(graphItemSlider.filePaths.ToArray(), from, to).Where(path => IsSameDay(bedTime, Utility.TransFilePathToDate(path))).ToList();
            int dateIndex = todayDataPathList
                .Select((path, index) => new { Path = path, Index = index })
                .Where(data => data.Path == graphItemSlider.filePaths[selectedGraphIndex])
                .Select(data => data.Index)
                .First();									//同一日の何個目のデータか(0はじまり)
            int crossSunCount = todayDataPathList
                .Take(dateIndex + 1)
                .Where(path => CSVManager.isCrossTheSun(bedTime, ReadSleepDataFromCSV(path).Last().GetDateTime()))
                .Count();									//現在のデータまでの日またぎデータの個数
            int sameDataNum = todayDataPathList.Count;		//同一日のすべてのデータ個数
            int crossSunNum = todayDataPathList
                .Where(path => CSVManager.isCrossTheSun(bedTime, ReadSleepDataFromCSV(path).Last().GetDateTime()))
                .Count();									//同一日の日マタギのみのデータ個数
                
            int sleepMode =  sleepHeaderData.SleepMode;

            return new SleepDataDetail(
                bedTime,
                getUpTime,
                snoreTime,
                apneaTime,
                snoreCount,
                apneaCount,
                longestApneaTime,
                snoreRate,
                apneaAverageCount,
                dateIndex,
                crossSunCount,
                sameDataNum,
                crossSunNum,
                sleepMode,
                snoreStopCount);
        }

        public static int getSnoreStopCount(List<SleepData> sleepData, int sleepMode)
        {
            if (sleepMode == 2 || sleepMode == 3) //2：モニタリングモード、3：振動モード（呼吸レス)
            {
                return -1;
            }
                
            int snoreStopCount = 0;
            int snoreContinueCount = 0;

            foreach (var item in sleepData)
            {
                int[] states = { item.BreathState1, item.BreathState2, item.BreathState3 };
                for (int i = 0; i < states.Length; i++)
                {
                    var state = states[i];
                    if (state == (int)SleepData.BreathState.Snore)
                    {
                        snoreContinueCount++;
                    } else
                    {
                        if(snoreContinueCount >= 1 && snoreContinueCount <= 2)
                        {
                            snoreStopCount++; //いびき判定が連続２回以下の場合
                        }

                        snoreContinueCount = 0; //Reset
                    }
                }
            }
               
            return snoreStopCount;
        }

        //睡眠データのファイル一覧から指定した期間のもののみを取得
        List<string> PickFilePathInPeriod(string[] sleepFilePathList, DateTime from, DateTime to)
        {
            return sleepFilePathList.Where(
                path => (from == DateTime.MinValue || Utility.TransFilePathToDate(path).CompareTo(from) >= 0)
                    && (to == DateTime.MaxValue || Utility.TransFilePathToDate(path).CompareTo(to) <= 0)).ToList();
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

        //睡眠データをリソースのCSVファイルから取得します
        List<SleepData> ReadSleepDataFromCSV(string filepath)
        {
            return CSVSleepDataReader.GetSleepDatas(filepath);
        }

        //睡眠のヘッダーデータをCSVファイルから取得します
        SleepHeaderData ReadSleepHeaderDataFromCSV(string filepath)
        {
            return CSVSleepDataReader.GetSleepHeaderData(filepath);
        }

        //データを反映する
        void AttachData()
        {
            UserDataManager.Scene.SaveGraphDate(sleepHeaderData.DateTime);

            GraphItem grapItm = graphItemSlider.graphItems[selectedGraphIndex];

            if(!grapItm.isActive)
            {
                grapItm.ibikiGraph.InputData = grapItm;
                grapItm.breathGraph.InputData = grapItm;

                grapItm.ibikiGraph.SetActive();
                grapItm.breathGraph.SetActive();

                grapItm.OnGraphDataChange.OnNext(Unit.Default); //データの変更を通知

                grapItm.isActive = true;
                graphItemSlider.graphItems[selectedGraphIndex] = grapItm;
            }

            _dateText.text = grapItm.graphDateText;
        }

        /// <summary>
        /// とりあえず日付送り機能用に
        /// ボタンから呼び出される
        /// </summary>
        public void ChangeNextDate()
        {
            while (selectedGraphIndex < graphItemSlider.filePaths.Count - 1)
            { //暫定：範囲内であれば処理を実行
                selectedGraphIndex++;
                loadSleepData(selectedGraphIndex);
                if (sleepHeaderData != null && sleepDataList != null && sleepDataList.Count > 0)
                {
                    graphItemSlider.MoveToIndex(selectedGraphIndex - CURRENT_START_INDEX);

                    AttachData();

                    break;
                }
            }

            updatePrevNextBtnState();
        }
        /// <summary>
        /// とりあえず日付送り機能用に
        /// ボタンから呼び出される
        /// </summary>
        public void ChangeBackDate()
        {
            while (selectedGraphIndex > 0)
            { //暫定：範囲内であれば処理を実行
                selectedGraphIndex--;
                loadSleepData(selectedGraphIndex);
                if (sleepHeaderData != null && sleepDataList != null && sleepDataList.Count > 0)
                {
                    graphItemSlider.MoveToIndex(selectedGraphIndex - CURRENT_START_INDEX);
                    AttachData();

                    break;
                }
            }

            updatePrevNextBtnState();
        }

        /// <summary>
        /// 次のインデックスが存在有無で有効/無効を切り替え
        /// </summary>
        /// <returns></returns>
        public void updatePrevNextBtnState()
        {
            this._backDateButton.interactable = false;
            this._nextDateButton.interactable = false;

            if (filePaths != null && MAX_FILE_POSITION > 0 && MIN_FILE_POSITION != MAX_FILE_POSITION)
            {
                if (selectedGraphIndex > 0)
                {
                    this._backDateButton.interactable = true;
                }

                if (selectedGraphIndex < graphItemSlider.filePaths.Count - 1)
                {
                    this._nextDateButton.interactable = true;
                }
            }
        }
    }
}
