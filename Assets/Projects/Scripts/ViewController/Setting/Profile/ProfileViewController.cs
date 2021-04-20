using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using Object = UnityEngine.Object;

public class ProfileViewController : ViewControllerBase {

	[SerializeField]Button backButton = null;					//戻るボタン
	[SerializeField]Button completeButton = null;				//完了ボタン
	[SerializeField]InputField nickNameText = null;				//ニックネーム
	[SerializeField]Text sexText = null;						//性別
	[SerializeField]Text birthDayText = null;					//生年月日
	[SerializeField]Button bodyLengthButton = null;				//身長を設定するためのボタン
	[SerializeField]Text bodyLengthText = null;					//身長の値を設定するテキストフィールド
	[SerializeField]Button weightButton = null;					//体重を設定するためのボタン
	[SerializeField]Text weightText = null;						//体重の値を設定するテキストフィールド
	[SerializeField]Text idealSleepTimeStartText = null;		//理想の睡眠時間の開始時刻
	[SerializeField]Text idealSleepTimeEndText = null;			//理想の睡眠時間の終了時刻


	protected override void Start () {
		base.Start ();
		InitNickName ();
		InitSex ();
		InitBirthDay ();
		InitBodyLength ();
		InitWeight ();
		InitIdealSleepTime ();
		UpdateHeaderItem ();
	}

	public override SceneTransitionManager.LoadScene SceneTag {
		get {
			return SceneTransitionManager.LoadScene.Profile;
		}
	}

	public override bool IsAllowSceneBack () {
		//初回起動時に表示されるプロフィール画面ではバックボタンを使えないようにする
		if (DialogBase.IsDisp ()) {
			//ダイアログ表示中であれば、戻れない
			return false;
		} else {
			//１．初期起動時、スプラッシュから遷移してきた場合
			if (UserDataManager.State.isInitialLunch ()) {
				return false;
			}
			//２．メニューの設定から遷移してきた場合
			else {
				//戻るボタンを押したとき
				if (IsSettingAllItem ()) {
					//すべての項目が設定されていれば
					return true;
				} else {
					//設定されてない項目がある場合ダイアログを表示する
					StartCoroutine (TellExistUnSetItem ());
					return false;
				}
			}
		}
	}

	//ヘッダーのボタン表示を状況によって変更する
	void UpdateHeaderItem () {
		//１．初期起動時、スプラッシュから遷移してきた場合
		if (UserDataManager.State.isInitialLunch ()) {
			//戻るボタン非表示・完了ボタン表示
			//すべての項目が入力されるまで完了ボタンが押せないように
			completeButton.gameObject.SetActive (true);
			backButton.gameObject.SetActive (false);
		}
		//２．メニューの設定から遷移してきた場合
		else {
			//戻るボタン表示・完了ボタン非表示
			//すべての項目が入力されるまで戻るボタンが押せないように
			backButton.gameObject.SetActive (true);
			completeButton.gameObject.SetActive (false);
		}
	}

	//すべての項目が設定されたかどうか
	bool IsSettingAllItem () {
		return IsSetNickname () && IsSetSex ();
	}

	//設定項目の値が変更された際に呼び出される
	//実際には値が変化したかどうかより、項目が変更された可能性があれば呼び出される
	void OnChangeSetting () {
		//すべての項目が入力されたときに完了ボタンを表示するため、値が変化した際に確認する
		UpdateHeaderItem ();
	}

	//ニックネームが設定されているか
	bool IsSetNickname () {
		return UserDataManager.Setting.Profile.GetNickName ().Count () > 0;
	}
	//性別が設定されているか
	bool IsSetSex () {
		return UserDataManager.Setting.Profile.GetSex () != UserDataManager.Setting.Profile.Sex.Unknown;
	}

	void InitNickName () {
		//保存したニックネームが表示されるように
		nickNameText.text = UserDataManager.Setting.Profile.GetNickName ();
		//ニックネームが入力されると保存されるように
		nickNameText.onValueChanged.AddListener ((string name) => {
			Debug.Log (name);
			UserDataManager.Setting.Profile.SaveNickName (name);
			OnChangeSetting ();
		});
		//特殊文字をはじくように
		nickNameText.onValidateInput += ((string text, int charIndex, char addedChar) => {
			if (char.IsSurrogate (addedChar)) {
				addedChar = '�';
			}
			return addedChar;
		});
	}

	void InitSex () {
		//保存した性別を表示する
		string text = "";
		switch (UserDataManager.Setting.Profile.GetSex ()) {
		case UserDataManager.Setting.Profile.Sex.Male:
			text = "男";
			break;
		case UserDataManager.Setting.Profile.Sex.Female:
			text = "女";
			break;
		default:
			text = "未選択";
			break;
		}
		sexText.text = text;
	}

	void InitBirthDay () {
		//保存した生年月日が表示されるように
		//データがなければ、現在の日付を表示する
		DateTime d = UserDataManager.Setting.Profile.GetBirthDay () != DateTime.MinValue
			? UserDataManager.Setting.Profile.GetBirthDay ()
			: DateTime.Now;
		string dateTimeString = d.Year.ToString () + "/" + d.Month.ToString ("00") + "/" + d.Day.ToString ("00");
		birthDayText.text = dateTimeString;
	}

	void InitBodyLength () {
		//アプリ内に保存した身長の値をテキストに反映させる
		bodyLengthText.text = UserDataManager.Setting.Profile.GetBodyLength ().ToString ("0.0") + "cm";
	}

	void InitWeight () {
		//アプリ内に保存した体重の値をテキストに反映させる
		weightText.text = UserDataManager.Setting.Profile.GetWeight ().ToString ("0.0") + "kg";
	}

	//生年月日のボタンが押されたときに日付ピッカーを表示する
	public void OnBirthDayButtonTap (Object button) {


		#if UNITY_ANDROID
		SceneTransitionManager.LoadLevel (SceneTransitionManager.LoadScene.Birth);
		return;
		#endif
		DateTime initDate = UserDataManager.Setting.Profile.GetBirthDay ();
		NativePicker.Instance.ShowDatePicker (GetScreenRect (button as GameObject), initDate, (long val) => {
			//日付が変更された
			var date = NativePicker.ConvertToDateTime (val);
			//表示を更新する
			string dateTimeString = date.Year.ToString () + "/" + date.Month.ToString ("00") + "/" + date.Day.ToString ("00");
			birthDayText.text = dateTimeString;
			//日付を生年月日として保存する
			UserDataManager.Setting.Profile.SaveBirthDay (date);
			OnChangeSetting ();
		}, () => {
			//日付の変更がキャンセルされた
			//変更を元に戻す
			UserDataManager.Setting.Profile.SaveBirthDay (initDate);
			OnChangeSetting ();
		});
	}

	Rect GetScreenRect (GameObject gameObject) {
		RectTransform transform = gameObject.GetComponent<RectTransform>();
		Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
		Rect rect = new Rect(transform.position.x, Screen.height - transform.position.y, size.x, size.y);
		rect.x -= (transform.pivot.x * size.x);
		rect.y -= ((1.0f - transform.pivot.y) * size.y);
		return rect;
	}

	//身長ボタンをタップした際に実行される
	public void OnBodyLengthButtonTap () {
		//ピッカーを表示して身長を設定させる
		string title = "身長設定";
		string unit = "cm";
		float maxValue = 220.9f;
		float minValue = 130f;
		float stepValue = 0.1f;
		float currentValue = UserDataManager.Setting.Profile.GetBodyLength ();
		var vs = new SelectValueDialogParamSet (
			SelectValueDialogParamSet.DISPLAY_TYPE.Numeric,
			title,
			unit,
			maxValue,
			minValue,
			stepValue,
			currentValue);
		SelectValueDialog.Show (vs, (SelectValueDialog.ButtonItem status, float value, GameObject dialog) => {
			if (status == SelectValueDialog.ButtonItem.OK) {
				//結果をテキストに反映
				bodyLengthText.text = value.ToString ("0.0") + unit;
				//アプリ内保存
				UserDataManager.Setting.Profile.SaveBodyLength (value);
				OnChangeSetting ();
			} else {
				//なにもしない
			}
		});
	}

	//体重ボタンをタップした際に実行される
	public void OnWeightButtonTap () {
		//ピッカーを表示して体重を設定させる
		string title = "体重設定";
		string unit = "kg";
		float maxValue = 200.9f;
		float minValue = 30f;
		float stepValue = 0.1f;
		float currentValue = UserDataManager.Setting.Profile.GetWeight ();
		var vs = new SelectValueDialogParamSet (
			SelectValueDialogParamSet.DISPLAY_TYPE.Numeric,
			title,
			unit,
			maxValue,
			minValue,
			stepValue,
			currentValue);
		SelectValueDialog.Show (vs, (SelectValueDialog.ButtonItem status, float value, GameObject dialog) => {
			if (status == SelectValueDialog.ButtonItem.OK) {
				//結果をテキストに反映
				weightText.text = value.ToString ("0.0") + unit;
				//アプリ内保存
				UserDataManager.Setting.Profile.SaveWeight (value);
				OnChangeSetting ();
			} else {
				//なにもしない
			}
		});
	}

	//理想の睡眠時間初期化
	void InitIdealSleepTime () {
		//保存したデータがあれば表示する
		if (UserDataManager.Setting.Profile.GetIdealSleepStartTime () == DateTime.MinValue)
			return;		//データが保存されてなければなにもしない
		var startDate = UserDataManager.Setting.Profile.GetIdealSleepStartTime ();
		var endDate = UserDataManager.Setting.Profile.GetIdealSleepEndTime ();
		var startDateText = startDate.Hour.ToString ("00") + ":" + startDate.Minute.ToString ("00");
		var endDateText = endDate.Hour.ToString ("00") + ":" + endDate.Minute.ToString ("00");
		idealSleepTimeStartText.text = startDateText;
		idealSleepTimeEndText.text = endDateText;
	}

	//「＜」ボタンが押された時に実行される
	//設定画面から遷移してきたときのみ押される
	public void OnBackButtonTap () {
		//遷移元シーンに戻る
		if (IsSettingAllItem ()) {
			//メニューの設定から遷移してきたため、設定画面に戻る
			SceneTransitionManager.LoadLevel (isTapFromHome() ? SceneTransitionManager.LoadScene.Home : SceneTransitionManager.LoadScene.Setting);
		} else {
			//設定されてない項目がある場合ダイアログを表示する
			StartCoroutine (TellExistUnSetItem ());
		}
	}

	//「完了」ボタンが押された時に呼び出される
	//初期起動時のプロフィール表示の際にのみ押される
	public void OnCompleteButtonTap () {
		//プロフィール設定を完了した事を記録します
		if (IsSettingAllItem ()) {
			UserDataManager.Setting.Profile.SaveCompleteSetting ();
			//すべての項目の設定を保存します
			SaveAllField ();
			//デバイスのペアリング準備画面に遷移する
			//SceneTransitionManager.LoadLevel (SceneTransitionManager.LoadScene.BTConnectPrepare);

			//Bluetooth接続画面へ遷移
			SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.BTConnect);
		} else {
			//設定されてない項目がある場合ダイアログを表示する
			StartCoroutine (TellExistUnSetItem ());
		}
	}

	//設定されてない項目がある事をユーザーに伝える
	IEnumerator TellExistUnSetItem () {
		bool isOK = false;
		MessageDialog.Show (
			"<size=32>設定されていない項目があります。</size>",
			true,
			false,
			() => isOK = true);
		yield return new WaitUntil (() => isOK);
	}

	//「性別」ボタンが押された時に呼び出される
	public void OnSexButtonTap () {
		SceneTransitionManager.LoadLevel (SceneTransitionManager.LoadScene.Sex);
	}

	//ニックネーム他、すべての項目のデータを保存します
	void SaveAllField () {
		UserDataManager.Setting.Profile.SaveNickName (nickNameText.text);
	}

	//理想の睡眠時間の開始時間を設定します
	public void OnIdealSleepTimeStartButtonTap (Object button) {
		var initDate = UserDataManager.Setting.Profile.GetIdealSleepStartTime ();
		NativePicker.Instance.ShowTimePicker (GetScreenRect (button as GameObject), NativePicker.DateTimeForTime (initDate.Hour, initDate.Minute, 0), (long val) => {
			//時刻が変更された
			//表示を更新する
			var date = NativePicker.ConvertToDateTime (val);
			string dateTimeString = date.Hour.ToString ("00") + ":" + date.Minute.ToString ("00");
			idealSleepTimeStartText.text = dateTimeString;
			//日付を保存する
			UserDataManager.Setting.Profile.SaveIdealSleepStartTime (date);
			OnChangeSetting ();
		}, () => {
			//時刻の変更がキャンセルされた
			//保存した日付をもとに戻す
			UserDataManager.Setting.Profile.SaveIdealSleepStartTime (initDate);
			OnChangeSetting ();
		});
	}

	//理想の睡眠時間の終了時間を設定します
	public void OnIdealSleeptimeendButtonTap (Object button) {
		var initDate = UserDataManager.Setting.Profile.GetIdealSleepEndTime ();
		NativePicker.Instance.ShowTimePicker (GetScreenRect (button as GameObject), NativePicker.DateTimeForTime (initDate.Hour, initDate.Minute, 0), (long val) => {
			//時刻が変更された
			//表示を更新する
			var date = NativePicker.ConvertToDateTime (val);
			string dateTimeString = date.Hour.ToString ("00") + ":" + date.Minute.ToString ("00");
			idealSleepTimeEndText.text = dateTimeString;
			//日付を保存する
			UserDataManager.Setting.Profile.SaveIdealSleepEndTime (date);
			OnChangeSetting ();
		}, () => {
			//時刻の変更がキャンセルされた
			//保存した日付をもとに戻す
			UserDataManager.Setting.Profile.SaveIdealSleepEndTime (initDate);
			OnChangeSetting ();
		});
	}
}
