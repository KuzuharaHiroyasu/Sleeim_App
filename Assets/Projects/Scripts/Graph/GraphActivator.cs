using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Graph {
	/// <summary>
	/// グラフをアクティブ化・非アクティブ化させるためのクラス
	/// UIのボタンのクリックイベントから呼び出されるよう設定します
	/// </summary>
	public class GraphActivator : MonoBehaviour {

		public Switcher graphTabSwitch;		//タブを切り替えるためのスイッチ
		public List<GameObject> BarGraphElements;
		public List<GameObject> CircleGraphElements;

		public void SetBarGraphActive () {
			//タブの選択状態を記憶しておく
			UserDataManager.Scene.SaveGraphTabType (UserDataManager.Scene.GraphTabType.Time);
			foreach (GameObject element in BarGraphElements) {
				element.SetActive (true);
			}
		}

		public void SetBarGraphDisActive () {
			foreach (GameObject element in BarGraphElements) {
				element.SetActive (false);
			}
		}

		public void SetCircleGraphActive () {
			//タブの選択状態を記憶しておく
			UserDataManager.Scene.SaveGraphTabType (UserDataManager.Scene.GraphTabType.Aggregate);
			foreach (GameObject element in CircleGraphElements) {
				element.SetActive (true);
			}
		}

		public void SetCircleGraphDisActive () {
			foreach (GameObject element in CircleGraphElements) {
				element.SetActive (false);
			}
		}
	}
}
