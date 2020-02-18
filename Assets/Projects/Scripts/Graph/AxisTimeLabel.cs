using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

namespace Graph {
	/// <summary>
	/// グラフのX軸に時間を表示するためのコンポーネント
	/// 一時間単位で目盛り・ラベルを表示します。
	/// </summary>
	public class AxisTimeLabel : MonoBehaviour {

		public RectTransform frame;
		public float width;
		public int fontSize;
		public float markToFontMargin;		//マークとフォントの間隔
		public Color fontColor;
		public Color markColor;
		public Font font;
		public bool isDispStartAndEndTime;	//始点と終点に、それぞれ開始時刻と終了時刻を表示するか
		List<GameObject> markObjList = new List<GameObject>();
		List<GameObject> labelObjList = new List<GameObject>();

		//バーの高さの割合から実際のスケールを返します
		float GetBarHeight (float heightRatio) {
			return frame.rect.height * heightRatio;
		}
		//バーの横幅の割合から実際のスケールを返します
		float GetBarWidth (float widthRatio) {
			return frame.rect.width * widthRatio;
		}

		/// <summary>
		/// 時間のリストから軸の目盛り・ラベルを設定します。
		/// 目盛り・ラベルは一時間間隔で設定されます。
		/// </summary>
		/// <param name="timeList">Time list.</param>
		public void SetAxis (List<DateTime> timeList) {

			ClearMark ();	//上書き可能にするため消しておく
			ClearLabel ();	//上書き可能にするため消しておく

			TimeSpan dispTimeSpan;
			//時間の長さによって、表示する時間間隔を調節します
			//とある時間間隔の時、ラベルの個数が何個になるか
			if (CulcTimeCount (timeList, new TimeSpan (0, 1, 0)) <= 6) {			//一分間隔で6個以下なら
				dispTimeSpan = new TimeSpan (0, 1, 0);
			} else if (CulcTimeCount (timeList, new TimeSpan (0, 5, 0)) <= 6) {		//五分間隔で6個以下なら
				dispTimeSpan = new TimeSpan (0, 5, 0);
			} else if (CulcTimeCount (timeList, new TimeSpan (0, 10, 0)) <= 6) {	//十分間隔で6個以下なら
				dispTimeSpan = new TimeSpan (0, 10, 0);
			} else if (CulcTimeCount (timeList, new TimeSpan (0, 30, 0)) <= 6) {	//三十分間隔で6個以下なら
				dispTimeSpan = new TimeSpan (0, 30, 0);
			} else if (CulcTimeCount (timeList, new TimeSpan (1, 0, 0)) <= 12) {	//一時間間隔で12個以下なら
				dispTimeSpan = new TimeSpan (1, 0, 0);
			} else if (CulcTimeCount (timeList, new TimeSpan (2, 0, 0)) <= 12) {	//二時間間隔で12個以下なら
				dispTimeSpan = new TimeSpan (2, 0, 0);
			} else {
				//最終5時間間隔で設定(ありえないはず)
				dispTimeSpan = new TimeSpan (5, 0, 0);
			}
			List<DateTime> dispTimeList;
			List<string> dispTimeLabelList;
			CreateDispTimeList (timeList, dispTimeSpan, out dispTimeList, out dispTimeLabelList);
			SetMark (dispTimeList, timeList.First (), timeList.Last ());
			SetLabel (dispTimeList, dispTimeLabelList, timeList.First (), timeList.Last ());
		}

		/// <summary>
		/// イビキの時間のリストから軸の目盛り・ラベルを設定します。
		/// 目盛り・ラベルは一時間間隔で設定されます。
		/// </summary>
		/// <param name="timeList">Time list.</param>
		public void SetIbikiAxis (List<DateTime> timeList) {

			ClearMark ();	//上書き可能にするため消しておく
			ClearLabel ();	//上書き可能にするため消しておく

			TimeSpan dispTimeSpan;
			dispTimeSpan = new TimeSpan (0, 10, 0);
			List<DateTime> dispTimeList;
			List<string> dispTimeLabelList;
			isDispStartAndEndTime = true;
			CreateDispTimeList (timeList, dispTimeSpan, out dispTimeList, out dispTimeLabelList);
			isDispStartAndEndTime = false;
			SetMark (dispTimeList, timeList.First (), timeList.Last ());
			SetLabel (dispTimeList, dispTimeLabelList, timeList.First (), timeList.Last ());
		}

		/// <summary>
		/// イビキの時間のリストから軸の目盛り・ラベルを設定します。
		/// 目盛り・ラベルは一時間間隔で設定されます。
		/// </summary>
		/// <param name="timeList">Time list.</param>
		public void SetIbikiMinAxis (List<DateTime> timeList) {

			ClearMark ();	//上書き可能にするため消しておく
			ClearLabel ();	//上書き可能にするため消しておく

			TimeSpan dispTimeSpan;
			if (CulcTimeCount (timeList, new TimeSpan (1, 0, 0)) <= 6) {	//一時間間隔で6個以下なら
				dispTimeSpan = new TimeSpan (1, 0, 0);
			} else if (CulcTimeCount (timeList, new TimeSpan (2, 0, 0)) <= 6) {	//二時間間隔で6個以下なら
				dispTimeSpan = new TimeSpan (2, 0, 0);
			} else {
				//最終5時間間隔で設定(12時間以上睡眠)
				dispTimeSpan = new TimeSpan (5, 0, 0);
			}
			List<DateTime> dispTimeList;
			List<string> dispTimeLabelList;
			isDispStartAndEndTime = true;
			CreateDispTimeList (timeList, dispTimeSpan, out dispTimeList, out dispTimeLabelList, true);
			isDispStartAndEndTime = false;
			SetMark (dispTimeList, timeList.First (), timeList.Last ());
			SetLabel (dispTimeList, dispTimeLabelList, timeList.First (), timeList.Last ());
		}

		public void SetIbikiScroll(float magnification) {
			int markObjCount = 0;
			foreach (GameObject obj in markObjList) {

				RectTransform rectSub = obj.GetComponent<RectTransform>();
				rectSub.localPosition=new Vector3(rectSub.transform.localPosition.x * magnification,rectSub.transform.localPosition.y);
				markObjCount++;
				if (markObjCount == 1) {
					rectSub.localPosition=new Vector3(rectSub.transform.localPosition.x + 2,rectSub.transform.localPosition.y,0);
				}
				else if (markObjCount == markObjList.Count) {
					rectSub.localPosition=new Vector3(rectSub.transform.localPosition.x - 2,rectSub.transform.localPosition.y,0);
				}

			}
			int labelObjCount = 0;
			foreach (GameObject obj in labelObjList) {

				RectTransform rectSub = obj.GetComponent<RectTransform>();
				rectSub.localPosition=new Vector3(rectSub.transform.localPosition.x * magnification,rectSub.transform.localPosition.y);

				labelObjCount++;
				if (labelObjCount == 1) {
					rectSub.localPosition=new Vector3(rectSub.transform.localPosition.x + 40,rectSub.transform.localPosition.y,0);
				}
				else if (labelObjCount == labelObjList.Count) {
					rectSub.localPosition=new Vector3(rectSub.transform.localPosition.x - 40,rectSub.transform.localPosition.y,0);
				}
			}

		}


		/// <summary>
		/// 開始地点は0とし、時間の長さを指定してラベルを設定します。
		/// </summary>
		public void SetAxis (int hour , int min, int sec) {
			DateTime start = new DateTime (2000, 1, 1, 0, 0, 0);
			DateTime end = new DateTime (2000, 1, 1, hour, min, sec);
			List<DateTime> timeList = new List<DateTime> ();
			timeList.Add (start);
			timeList.Add (end);
			SetAxis (timeList);
		}

		//指定した時間間隔の時に、何回合致する時間があるか
		int CulcTimeCount (List<DateTime> timeList, TimeSpan timeSpan) {
			for (int i = 0; i < 1000; i++) {
				DateTime time = timeList.First ().Add (new TimeSpan (timeSpan.Hours * i, timeSpan.Minutes * i, timeSpan.Seconds * i));
				int year = time.Year;
				int month = time.Month;
				int day = time.Day;
				int hour = time.Hour;
				int min = time.Minute;
				int sec = time.Second;

				if (timeSpan.Seconds == 0) {
					if (sec > 0) {
						sec = 0;
						min++;
					}
				} else {
					sec = timeSpan.Seconds * (sec / timeSpan.Seconds) + timeSpan.Seconds * (sec % timeSpan.Seconds > 0 ? 1 : 0);
				}

				if (timeSpan.Minutes == 0) {
					if (min > 0) {
						min = 0;
						hour++;
					}
				} else {
					min = timeSpan.Minutes * (min / timeSpan.Minutes) + timeSpan.Minutes * (min % timeSpan.Minutes > 0 ? 1 : 0);
				}

				if (timeSpan.Hours == 0) {

				} else {
					hour += hour % timeSpan.Hours;
				}
				//時間正規化
				if (sec >= 60) {
					min += sec / 60;
					sec = sec % 60;
				}
				if (min >= 60) {
					hour += min / 60;
					min = min % 60;
				}
				if (hour >= 24) {
					day += hour / 24;
					hour = hour % 24;
				}
				if (day > DateTime.DaysInMonth (year, month)) {
					month++;
					day = 1;
				}
				if (month > 12) {
					year++;
					month = 1;
				}
				time = new DateTime (year, month, day, hour, min, sec);
				if (time.CompareTo (timeList.Last ()) > 0)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// 時間のリストから、指定した時間間隔になるような時間を取り出します。
		/// </summary>
		/// <param name="timeList">入力する時間のリスト</param>
		/// <param name="dispTimeSpan">時間間隔の指定</param>
		/// <param name="resultTimeList">指定した時間間隔で取り出した時間のリスト</param>
		/// <param name="resultLabelList">表示のために時間を文字列にしたもの</param>
		void CreateDispTimeList (List<DateTime> timeList, TimeSpan dispTimeSpan, out List<DateTime> resultTimeList, out List<string> resultLabelList, bool displayMinute = false) {
			List<DateTime> result = new List<DateTime> ();
			List<string> resultLabel = new List<string> ();
			DateTime first = timeList.First();
			DateTime last = timeList.Last ();

			if (isDispStartAndEndTime) {
				//始点と終点に開始時刻と終了時刻を表示するなら
				//開始時刻記載
				result.Add (timeList.First ());
				resultLabel.Add (CSVManager.ConvertTimeToHHMM(timeList.First()));
				//間の時間記載
				//開始時刻、終了時刻より一定の範囲内の時間(は表示が重複しないようにするため、記載しないようにする
				TimeSpan wholeTime = timeList.Last () - timeList.First ();
				TimeSpan curringTime = dispTimeSpan;
				if (last - first > new TimeSpan (curringTime.Hours * 2, curringTime.Minutes * 2, curringTime.Seconds * 2)) {	//全体の時間がマイナスにならないように
					first = first.Add (curringTime);
					last = last.Subtract (curringTime);
				}
			}

			for (int i = 0; i < 100; i++) {
				DateTime time = first.Add (new TimeSpan (dispTimeSpan.Hours * i, dispTimeSpan.Minutes * i, dispTimeSpan.Seconds * i));
				int year = time.Year;
				int month = time.Month;
				int day = time.Day;
				int hour = time.Hour;
				int min = time.Minute;
				int sec = time.Second;

				if (dispTimeSpan.Seconds == 0) {
					if (sec > 0) {
						sec = 0;
						min++;
					}
				} else {
					sec = dispTimeSpan.Seconds * (sec / dispTimeSpan.Seconds) + dispTimeSpan.Seconds * (sec % dispTimeSpan.Seconds > 0 ? 1 : 0);
				}

				if (dispTimeSpan.Minutes == 0) {
					if (min > 0) {
						min = 0;
						hour++;
					}
				} else {
					min = dispTimeSpan.Minutes * (min / dispTimeSpan.Minutes) + dispTimeSpan.Minutes * (min % dispTimeSpan.Minutes > 0 ? 1 : 0);
				}

				if (dispTimeSpan.Hours == 0) {

				} else {
					hour += hour % dispTimeSpan.Hours;
				}
				//時間正規化
				if (sec >= 60) {
					min += sec / 60;
					sec = sec % 60;
				}
				if (min >= 60) {
					hour += min / 60;
					min = min % 60;
				}
				if (hour >= 24) {
					day += hour / 24;
					hour = hour % 24;
				}
				if (day > DateTime.DaysInMonth (year, month)) {
					month++;
					day = 1;
				}
				if (month > 12) {
					year++;
					month = 1;
				}
				time = new DateTime (year, month, day, hour, min, sec);

				if (isDispStartAndEndTime) {
					bool isSameStart = (time - timeList.First ()) < dispTimeSpan;
					bool isSameEnd = (timeList.Last () - time) < dispTimeSpan;
					if (isSameStart || isSameEnd)	//timeSpanが一分など、短い時間間隔になったときに表示桁数の関係上、秒は違うけど分は同じという重複が発生するため回避する
					continue;
				}
				if (time.CompareTo (last) > 0)
					break;	//終了時間を過ぎるなら、終了とみなす
				result.Add (time);

				//表示する時間間隔によって表記を変更する
                if(CSVManager.isInvalidDate(timeList.First()))
                {
                    resultLabel.Add("");
                } else if (dispTimeSpan.Seconds > 0) {
					//秒単位で表記が必要なら
					resultLabel.Add (String.Format ("{0:0}", hour) + ":" + String.Format ("{0:00}", min) + ":" + String.Format ("{0:00}", sec));
				} else if (dispTimeSpan.Minutes > 0 || displayMinute) {
					//分単位で表記が必要なら
					resultLabel.Add (String.Format ("{0:0}", hour) + ":" + String.Format ("{0:00}", min));
				} else {
					//時間単位の表記で良いなら
					resultLabel.Add (String.Format ("{0:0}", hour));
				}
			}

			//表示できる時間が1つもなければ、最後に終了時間を追加する
			bool isNoneDispTime = result.Count == 0;
			if (isDispStartAndEndTime || isNoneDispTime) {
				//終了時刻記載
				result.Add (timeList.Last ());
				resultLabel.Add (CSVManager.ConvertTimeToHHMM(timeList.Last()));
			}

			resultTimeList = result;
			resultLabelList = resultLabel;
		}

		/// <summary>
		/// Sets the mark.
		/// </summary>
		/// <param name="dispTimeList">マークを表示する時間のリスト</param>
		void SetMark (List<DateTime> dispTimeList, DateTime start, DateTime last) {
			foreach (DateTime dispTime in dispTimeList) {
				float posRate = Graph.Time.GetPositionRate (dispTime, start, last);
				Image mark = CreateMark (posRate, width, markColor);
				markObjList.Add (mark.gameObject);
			}
		}

		/// <summary>
		/// Sets the label.
		/// </summary>
		/// <param name="dispTimeList">Dラベルを表示する時間のリスト</param>
		/// <param name="dispTimeLabelList">表示するラベル</param>
		/// <param name="start">Start.</param>
		/// <param name="last">Last.</param>
		void SetLabel (List<DateTime> dispTimeList, List<string> dispTimeLabelList, DateTime start, DateTime last) {
			for (int i = 0; i < dispTimeList.Count; i++) {
				float posRate = Graph.Time.GetPositionRate (dispTimeList[i], start, last);
				if (float.IsNaN (posRate)) {
					continue;
				}
				Text label = CreateLabel (dispTimeLabelList[i], posRate, fontSize, fontColor);
				labelObjList.Add (label.gameObject);
			}
		}

		Image CreateMark (float posRate, float width, Color markColor) {
			var markElement = new GameObject ("mark");
			markElement.transform.parent = frame;
			var image = markElement.AddComponent<Image> ();
			//RectTransform初期化
			image.rectTransform.anchorMax = new Vector2 (1f, 1f);
			image.rectTransform.anchorMin = new Vector2 (0, 0);
			image.rectTransform.offsetMin = new Vector2 (0,0);
			image.rectTransform.offsetMax = new Vector2 (0,0);
			image.rectTransform.localScale = Vector3.one;
			image.rectTransform.localPosition = Vector3.zero;
			//指定の位置・大きさに設定
			image.rectTransform.offsetMin = new Vector2 (GetBarWidth (posRate) - (width / 2f), 0);
			image.rectTransform.offsetMax = new Vector2 (-1f * (GetBarWidth(1f - posRate) - (width / 2f)), 0);
			image.color = markColor;
			return image;
		}

		Text CreateLabel (string labelText, float posRate, int fontSize, Color fontColor) {
			var labelElement = new GameObject ("label");
			labelElement.transform.parent = frame;
			var text = labelElement.AddComponent<Text> ();
			//RectTransform初期化
			text.rectTransform.anchorMax = new Vector2 (0.5f, 0.5f);
			text.rectTransform.anchorMin = new Vector2 (0.5f, 0.5f);
			text.rectTransform.localScale = Vector3.one;
			text.rectTransform.localPosition = Vector3.zero;
			//指定の位置・大きさに設定
			Vector2 posToZero = new Vector2((-1f * frame.rect.width / 2f), (-1f * frame.rect.height / 2f) + (-1f * text.rectTransform.rect.height / 2f) - markToFontMargin);
			text.rectTransform.localPosition = new Vector3 (posToZero.x + GetBarWidth (posRate), posToZero.y, 0);
			text.alignment = TextAnchor.UpperCenter;
			text.fontSize = fontSize;
			text.color = fontColor;
			text.text = labelText;
			text.font = font;
			return text;
		}

		//存在するマークを全て削除します
		void ClearMark () {
			while (markObjList.Count > 0) {
				GameObject obj = markObjList [0];
				markObjList.Remove (obj);
				Destroy (obj);
			}
		}

		void ClearLabel () {
			while (labelObjList.Count > 0) {
				GameObject obj = labelObjList [0];
				labelObjList.Remove (obj);
				Destroy (obj);
			}
		}
	}
}