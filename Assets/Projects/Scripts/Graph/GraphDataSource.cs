using System.Collections;
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
    public class GraphDataSource : MonoBehaviour, IIbikiData, IBreathData, IHeadDirData, ISleepInfo
    {
		[SerializeField] Image noDataImage = null;	//データがない場合に表示する画像
		[SerializeField] GameObject scrollView;
        /// <summary>
        /// グラフに表示するデータが変更された際に通知する
        /// </summary>
        public Subject<Unit> OnGraphDataChange = new Subject<Unit>();
        List<SleepData> sleepDataList;      //取得した睡眠データ
        SleepHeaderData sleepHeaderData;    //取得したCSVヘッダーに記述された睡眠データ

        Button _nextDateButton;
        Button _backDateButton;

        string[] _filepath; //取得したファイル一覧
        int _selectIndex = 0; //選択中の日付Index
        int _selectMin = 0; //選択範囲のMIN
        int _selectMax = 0; //選択範囲のMAX

        string[] filePath = null;
        string[] FilePath
        {
            get
            {
                if (filePath == null)
                    filePath = Kaimin.Common.Utility.GetAllFiles(Kaimin.Common.Utility.GsDataPath(), "*.csv");
                return filePath;
            }
        }

        void Start()
        {
            GameObject cube;
            cube = GameObject.Find("NextDateButton");
            _nextDateButton = cube.GetComponent<Button>();

            cube = GameObject.Find("BackDateButton");
            _backDateButton = cube.GetComponent<Button>();

            _filepath = Kaimin.Common.Utility.GetAllFiles(Kaimin.Common.Utility.GsDataPath(), "*.csv");
            //取得したファイルパスを確認
            Debug.Log("StartCheckGraphData----------------------------");
            foreach (var filePath in _filepath)
            {
                Debug.Log("FilePath:" + filePath);
            }
            Debug.Log("EndCheckGraphData----------------------------");
            _selectMax = _filepath.Length - 1;//最新のファイルを取得


			scrollView.SetActive (false);
            if (_filepath.Length != 0)
            { //エラーが出ないように
                DateTime targetDate = UserDataManager.Scene.GetGraphDate();
                //合致する日付データを検索する
                bool isExistSelectData = _filepath
                    .Where(path => Kaimin.Common.Utility.TransFilePathToDate(path) == targetDate)
                    .Count() > 0;
                if (isExistSelectData)
                {
                    //日付を選択して表示したい場合
                    _selectIndex = _filepath
                        .Select((path, index) => new { Path = path, Index = index })
                        .Where(data => Kaimin.Common.Utility.TransFilePathToDate(data.Path) == targetDate)
                        .First().Index;
                }
                else
                {
                    //最新データを表示したい場合
                    _selectIndex = _selectMax;
                }
                sleepDataList = ReadSleepDataFromCSV(_filepath[_selectIndex]);         //睡眠データをCSVから取得する
                sleepHeaderData = ReadSleepHeaderDataFromCSV(_filepath[_selectIndex]); //睡眠のヘッダーデータをCSVから取得する
                AttachData();
			}
			//表示するデータがなければ、NODATAを表示する
			noDataImage.enabled = _filepath.Length == 0;
			scrollView.SetActive (!noDataImage.enabled);
            NextCheckRange(); //暫定：次のインデックスが存在有無で有効/無効を切り替え
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
            List<string> todayDataPathList = PickFilePathInPeriod(FilePath, from, to).Where(path => IsSameDay(bedTime, Utility.TransFilePathToDate(path))).ToList();
            int dateIndex = todayDataPathList
                .Select((path, index) => new { Path = path, Index = index })
                .Where(data => data.Path == FilePath[_selectIndex])
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
                sleepMode);
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
            OnGraphDataChange.OnNext(Unit.Default); //データの変更を通知
        }

        /// <summary>
        /// とりあえず日付送り機能用に
        /// ボタンから呼び出される
        /// </summary>
        public void ChangeNextDate()
        {
            if (CheckSelecRange(1))
            { //暫定：範囲内であれば処理を実行
                _selectIndex++;
                sleepDataList = ReadSleepDataFromCSV(_filepath[_selectIndex]);         //睡眠データをCSVから取得する
                sleepHeaderData = ReadSleepHeaderDataFromCSV(_filepath[_selectIndex]); //睡眠のヘッダーデータをCSVから取得する
                AttachData();
                NextCheckRange();
            }
        }
        /// <summary>
        /// とりあえず日付送り機能用に
        /// ボタンから呼び出される
        /// </summary>
        public void ChangeBackDate()
        {
            if (CheckSelecRange(0))
            { //暫定：範囲内であれば処理を実行
                _selectIndex--;
                sleepDataList = ReadSleepDataFromCSV(_filepath[_selectIndex]);         //睡眠データをCSVから取得する
                sleepHeaderData = ReadSleepHeaderDataFromCSV(_filepath[_selectIndex]); //睡眠のヘッダーデータをCSVから取得する
                AttachData();
                NextCheckRange();
            }
        }

        /// <summary>
        /// 範囲内に選択日付があるかチェック
        /// </summary>
        /// <param name="path">減算・加算を指定</param>
        /// <returns></returns>
        public Boolean CheckSelecRange(int mode)
        {
            if (mode == 0) //減算
            {
                int tmpMin = _selectIndex - 1;
                if (tmpMin < _selectMin) return false;
            }
            else //加算
            {
                int tmpMax = _selectIndex + 1;
                if (tmpMax > _selectMax) return false;
            }

            return true;
        }

        /// <summary>
        /// 次のインデックスが存在有無で有効/無効を切り替え
        /// </summary>
        /// <returns></returns>
        public void NextCheckRange()
        {
            _backDateButton.interactable = CheckSelecRange(0);
            _nextDateButton.interactable = CheckSelecRange(1);
        }
    }
}
