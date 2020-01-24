using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class BirthViewController : ViewControllerBase {


	public Dropdown dropDownYear;
	public Dropdown dropDownMonth;
	public Dropdown dropDownDay;

	private int yearValue = 1900;
	private int monthValue = 1;
	private int dayValue = 1;

	protected override void Start () {
		base.Start ();

		if (dropDownYear) {
			dropDownYear.ClearOptions();    //現在の要素をクリアする
			List<string> list = new List<string>();
			for (int i = 1900; i <= System.DateTime.Now.Year; i++) {
				list.Add (i.ToString () + "年");
			}
			dropDownYear.AddOptions(list);  //新しく要素のリストを設定する
			dropDownYear.value = 0;         //デフォルトを設定(0～n-1)
		}

		if (dropDownMonth) {
			dropDownMonth.ClearOptions();    //現在の要素をクリアする
			List<string> list = new List<string>();
			for (int i = 1; i <= 12; i++) {
				list.Add (i.ToString () + "月");
			}
			dropDownMonth.AddOptions(list);  //新しく要素のリストを設定する
			dropDownMonth.value = 0;         //デフォルトを設定(0～n-1)
		}

		if (dropDownDay) {
			updateDropDownDayList();
			dropDownDay.value = 0;         //デフォルトを設定(0～n-1)
		}


		//保存した生年月日が表示されるように
		//データがなければ、現在の日付を表示する
		DateTime d = UserDataManager.Setting.Profile.GetBirthDay () != DateTime.MinValue
			? UserDataManager.Setting.Profile.GetBirthDay ()
			: DateTime.Now;
		dropDownYear.value = d.Year - 1900;
		dropDownMonth.value = d.Month - 1;
		dropDownDay.value = d.Day - 1;
	}

	public override SceneTransitionManager.LoadScene SceneTag {
		get {
			return SceneTransitionManager.LoadScene.Birth;
		}
	}


	//「プロフィール」ボタンが押されると呼び出される
	public void OnProfileButtonTap () {
		SceneTransitionManager.LoadLevel (SceneTransitionManager.LoadScene.Profile);
	}


	// 年のドロップダウンが変更された時に呼び出される
	public void changeYearDropDownValue(int value){
		yearValue = value + 1900;
		changeBirth ();
	}

	// 月のドロップダウンが変更された時に呼び出される
	public void changeMonthDropDownValue(int value){
		monthValue = value + 1;
		changeBirth ();
	}

	// 日のドロップダウンが変更された時に呼び出される
	public void changeDayDropDownValue(int value){
		Debug.Log("value = " + value);  //値を取得（先頭から連番(0～n-1)）
		Debug.Log("text(options) = " + dropDownDay.options[value].text);  //リストからテキストを取得
		dayValue = value + 1;
		changeBirth ();
	}

	// 生年月日が変更された時に呼び出される
	private void changeBirth(){
		DateTime dateTime = makeBirth ();
		UserDataManager.Setting.Profile.SaveBirthDay (dateTime);
		updateDropDownDayList ();
	}

	// ドロップダウンで選択された生年月日からDateTime変数を作って返す
	// ただし、2/30のようなありえない日は調整して（日を減らして）存在する生年月日に調整
	private DateTime makeBirth() {
		try {
			DateTime dateTime = new DateTime(yearValue, monthValue, dayValue);
			return dateTime;
		}
		catch {
			dayValue -= 1;
			DateTime dateTime = makeBirth();
			updateDropDownSelectedDay ();
			return dateTime;
		}
	}

	// 日にちのドロップダウンの候補の更新（年、月によって最大日にちが変わる）
	private void updateDropDownDayList() {

		int maxDay = 30;
		switch (monthValue) {
		case 1:
		case 3:
		case 5:
		case 7:
		case 8:
		case 10:
		case 12:
			maxDay = 31;
			break;
		case 2:
			if (yearValue % 4 == 0) {
				maxDay = 29;
			} else {
				maxDay = 28;
			}
			break;
		}


		dropDownDay.ClearOptions();    //現在の要素をクリアする
		List<string> list = new List<string>();
		for (int i = 1; i <= maxDay; i++) {
			list.Add (i.ToString () + "日");
		}
		dropDownDay.AddOptions(list);  //新しく要素のリストを設定する
	}

	// 日ドロップダウンの選択値を更新
	private void updateDropDownSelectedDay(){
		dropDownDay.value = dayValue - 1;
	}

}
