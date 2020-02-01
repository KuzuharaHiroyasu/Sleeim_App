using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UI;
using naichilab.InputEvents;

public class HistoryListViewController : ViewControllerBase {

	[SerializeField]SleepListDataSource DataSource = null;
	[SerializeField]ListAdapter Adapter = null;
	[SerializeField]GameObject ListElementPrehab = null;
	[SerializeField]Text YearText = null;
	[SerializeField]Text MonthText = null;
	[SerializeField]Button NextButton = null;	//データ送りボタン
	[SerializeField]Button PrivButton = null;	//データ戻しボタン
	[SerializeField]ScrollRect scrollRect = null;

	DateTime currentDispDate;	//現在表示中の日付

	protected override void Start () {
		base.Start ();
		InitView ();
	}

	public override SceneTransitionManager.LoadScene SceneTag {
		get {
			return SceneTransitionManager.LoadScene.History_List;
		}
	}

	void OnEnable () {
		//タッチマネージャーのイベントリスナーを設定
//		TouchManager.Instance.FlickComplete += OnFlickComplete;
	}

	void OnDisable () {
		//後処理
//		TouchManager.Instance.FlickComplete -= OnFlickComplete;
	}

	void InitView () {
		//前に見た画面を表示する
		if (UserDataManager.Scene.GetHistoryDate () != DateTime.MinValue) {
			currentDispDate = UserDataManager.Scene.GetHistoryDate ();
			UpdateView ();
		} else {
			//初回なら
			ToLatestMonth ();
		}
	}

	IEnumerator GetSleepListElementData = null;	//コルーチンを止めるために処理を保持しておく変数
	public void SetDataToList (DateTime from, DateTime to) {
		//既にデータ読み込み中であれば、前の処理を止める
		if (GetSleepListElementData != null)
			StopCoroutine (GetSleepListElementData);
		GetSleepListElementData = DataSource.GetSleepListElementDataCoroutine (
			scrollRect,
			from, 
			to, 
			(SleepListElement.Data sleepData) => {
				//データ取得時
				var elementObj = Instantiate (ListElementPrehab);
				var element = elementObj.GetComponent <SleepListElement> ();
				element.SetInfo (sleepData);
				Adapter.SetElementToList (elementObj);
			},
			() => {
				//データ取得完了時
				GetSleepListElementData = null;
			},
            UpdateListViewAfterDelete);
		StartCoroutine (GetSleepListElementData);
	}

    void UpdateListViewAfterDelete()
    {
        //Reset
        GetSleepListElementData = null;
        DataSource.displayFilePaths = null;
        InitView();
    }

	//画面表示を更新する
	void UpdateView () {
		UpdateListView ();
		UpdateDataSelectButton ();
	}

	//リストビューの内容をcurrentDispDateの内容で更新する
	void UpdateListView () {
		//リストビューの初期化
		Adapter.ClearAllElement ();
		//表示できるデータが一件もなければ
		if (currentDispDate == DateTime.MinValue) {
			YearText.text = "-";
			MonthText.text = "-";
		} 
		//表示できるデータがあれば
		else {
			//一か月分のデータを表示する
			SetDataToList (new DateTime (currentDispDate.Year, currentDispDate.Month, 1, 0, 0, 0), 
				new DateTime (currentDispDate.Year, currentDispDate.Month, DateTime.DaysInMonth (currentDispDate.Year, currentDispDate.Month), 23, 59, 59));
			//画面の日付表示も更新
			YearText.text = currentDispDate.Year.ToString ();
			MonthText.text = currentDispDate.Month.ToString () + "月";
			//表示した日付を記録
			UserDataManager.Scene.SaveHistoryDate (currentDispDate);
		}
	}

	//データ送り・戻しボタンの表示を更新する
	void UpdateDataSelectButton () {
		bool isExistNextData = GetNextMonthExistData (currentDispDate) != DateTime.MinValue;
		bool isExistPrivData = GetPrivMonthExistData (currentDispDate) != DateTime.MinValue;
		NextButton.interactable = isExistNextData;
		PrivButton.interactable = isExistPrivData;
	}

	//最新データのある月を表示
	public void ToLatestMonth () {
		//最新データの日付を取得する
		currentDispDate = DataSource.GetLatestDate ();
		UpdateView ();	//表示更新
	}

	//来月のデータを表示
	public void ToNextMonth () {
		bool isExistNextData = GetNextMonthExistData (currentDispDate) != DateTime.MinValue;
		if (!isExistNextData)
			return;
		currentDispDate = GetNextMonthExistData (currentDispDate);
		UpdateView ();
	}

	//データが存在する以降の月を取得する
	//以降にデータが存在しなければDateTime.MinValueを返す
	DateTime GetNextMonthExistData (DateTime currentDate) {
		DateTime nextMonthDate = currentDate.AddMonths (1);
		//来月以降データがなければ
		if (!DataSource.IsExistData (new DateTime (nextMonthDate.Year, nextMonthDate.Month, 1), DateTime.MaxValue)) {
			Debug.Log ("これ以降の睡眠データはありません");
			return DateTime.MinValue;
		}
		//来月のデータがあれば
		if (DataSource.IsExistData (new DateTime (nextMonthDate.Year, nextMonthDate.Month, 1), 
			new DateTime (nextMonthDate.Year, nextMonthDate.Month, DateTime.DaysInMonth (nextMonthDate.Year, nextMonthDate.Month), 23, 59, 59))) {
			return nextMonthDate;
		} 
		//来月のデータがなければ
		else {
			//次のデータがある月を探す
			for (int i = 0; i < 1000; i++) {	//バグがあると怖いためforにしてるが、whileの意
				nextMonthDate = nextMonthDate.AddMonths (1);
				bool isExistData = DataSource.IsExistData (new DateTime (nextMonthDate.Year, nextMonthDate.Month, 1), 
					new DateTime (nextMonthDate.Year, nextMonthDate.Month, DateTime.DaysInMonth (nextMonthDate.Year, nextMonthDate.Month), 23, 59, 59));
				bool isLastMonth = new DateTime (nextMonthDate.Year, nextMonthDate.Month, DateTime.DaysInMonth (nextMonthDate.Year, nextMonthDate.Month), 23, 59, 59).CompareTo (DataSource.GetLatestDate ()) >= 0;
				if (isExistData || isLastMonth) {
					return nextMonthDate;	//データがあった、もしくは最終月まで到達したら終了
				}
			}
		}
		return DateTime.MinValue;
	}

	//データが存在する以前の月を取得する
	//以前にデータが存在しなければDateTime.MinValueを返す
	DateTime GetPrivMonthExistData (DateTime currentDate) {
		DateTime priviousMonthDate = currentDate != DateTime.MinValue ? currentDate.AddMonths (-1) : currentDate;	//エラー回避
		//先月以前にデータがなければ
		if (!DataSource.IsExistData (DateTime.MinValue, 
			new DateTime (priviousMonthDate.Year, priviousMonthDate.Month, DateTime.DaysInMonth (priviousMonthDate.Year, priviousMonthDate.Month), 23, 59, 59))) {
			Debug.Log ("これ以前の睡眠データはありません");
			return DateTime.MinValue;
		}
		//先月のデータがあれば
		if (DataSource.IsExistData (new DateTime (priviousMonthDate.Year, priviousMonthDate.Month, 1), 
			new DateTime (priviousMonthDate.Year, priviousMonthDate.Month, DateTime.DaysInMonth (priviousMonthDate.Year, priviousMonthDate.Month), 23, 59, 59))) {
			return priviousMonthDate;
		} 
		//先月のデータがなければ
		else {
			//次のデータがある月を探す
			for (int i = 0; i < 1000; i++) {	//バグがあると怖いためforにしてるが、whileの意
				priviousMonthDate = priviousMonthDate.AddMonths (-1);
				bool isExistData = DataSource.IsExistData (new DateTime (priviousMonthDate.Year, priviousMonthDate.Month, 1), 
					new DateTime (priviousMonthDate.Year, priviousMonthDate.Month, DateTime.DaysInMonth (priviousMonthDate.Year, priviousMonthDate.Month), 23, 59, 59));
				if (isExistData) {
					return priviousMonthDate;	//データがあったら終了
				}
			}
		}
		return DateTime.MinValue;
	}

	//先月のデータを表示
	public void ToPriviousMonth () {
		bool isExistPriviousData = GetPrivMonthExistData (currentDispDate) != DateTime.MinValue;
		if (!isExistPriviousData)
			return;
		currentDispDate = GetPrivMonthExistData (currentDispDate);
		UpdateView ();
	}

	void OnFlickComplete (object sender, FlickEventArgs e) {
		string text = string.Format ("OnFlickComplete [{0}] Speed[{1}] Accel[{2}] ElapseTime[{3}]", new object[] {
			e.Direction.ToString (),
			e.Speed.ToString ("0.000"),
			e.Acceleration.ToString ("0.000"),
			e.ElapsedTime.ToString ("0.000")
		});
		Debug.Log (text);

		if (e.Direction == FlickEventArgs.Direction4.Left)
			ToNextMonth ();
		else if (e.Direction == FlickEventArgs.Direction4.Right)
			ToPriviousMonth ();
	}
}
