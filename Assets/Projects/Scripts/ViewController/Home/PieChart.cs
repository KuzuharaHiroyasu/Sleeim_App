using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Kaimin.Managers;
using UnityEngine.UI;

public class PieChart : MonoBehaviour {

	public Image piePrefab;
	public PieInfo pieInfo;
	public Text sleepTimeText = null; //睡眠時間
	public Text sleepDateText = null; //睡眠日付 (選択したグラフの日付)
	public GameObject circleOuter;
	public GameObject pieChart;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
