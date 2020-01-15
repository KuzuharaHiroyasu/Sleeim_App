using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using naichilab.InputEvents;

namespace Graph {
	public class GraphActionManager : MonoBehaviour {

		public GraphDataSource graphDataSource;

		void OnEnable () {
			//タッチマネージャーのイベントリスナを設定
//			TouchManager.Instance.FlickComplete += OnFlickComplete;
		}

		void OnDisable () {
			//後処理
//			TouchManager.Instance.FlickComplete -= OnFlickComplete;
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
				ToLeft ();
			else if (e.Direction == FlickEventArgs.Direction4.Right)
				ToRight ();
		}

		//右を選択
		void ToRight () {
			graphDataSource.ChangeBackDate ();
		}
		//左を選択
		void ToLeft () {
			graphDataSource.ChangeNextDate ();
		}
	}
}