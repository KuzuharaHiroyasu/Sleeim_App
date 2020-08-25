using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Graph {
	/// <summary>
	/// 各グラフに設定するデータを切り替え可能にするためのクラス
	/// </summary>
	public class GraphDataActivater : MonoBehaviour {

		//public GraphItem InputData; //データの変更通知を発行するクラス
		//public IbikiGraph ibikiGraph;
		//public BreathGraph breathGraph;

		void Awake () {
			/*
			InputData.OnGraphDataChange.Subscribe (_ => {
				//グラフに表示するデータが変更された際に実行される
				//データが更新されたら、選択中のデータの表示を更新する
				ibikiGraph.SetActive();
				breathGraph.SetActive();
			});
			*/
		}
	}
}
