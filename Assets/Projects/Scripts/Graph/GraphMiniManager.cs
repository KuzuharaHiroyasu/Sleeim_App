using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// UI処理のクラスを使用する宣言
using UnityEngine.UI;

public class GraphMiniManager : MonoBehaviour {

	public Graph.IbikiGraph ibikiGraph;
	public Graph.BreathGraph breathGraph;

	public Image buttonImage;
	public Sprite buttonImageActive;
	public Sprite buttonImageDisable;

	public GraphBigManager plusButtonManager;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void onTapGraphMiniButton() {

		if (!ibikiGraph.IsScroll ()) {
			return;
		}


		float sleepingTime = ibikiGraph.sleepingTime ();
		ibikiGraph.ReSizeMin ();
		breathGraph.sleepingTime = sleepingTime;
		breathGraph.ResizeMin ();
		ibikiGraph.setScroll (false);
		plusButtonManager.setActive ();
		setDisActive ();
	}

	public void setActive() {
		buttonImage.sprite = buttonImageActive;
	}

	public void setDisActive() {
		buttonImage.sprite = buttonImageDisable;
	}
}
