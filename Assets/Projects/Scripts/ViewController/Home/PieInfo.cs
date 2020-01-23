﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PieInfo : MonoBehaviour {

    // Use this for initialization
    public Text pieLabel;
    public Text piePercentValue;

	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void drawPieInfo(string label, double percent)
    {
        //Default 
        pieLabel.text = "";
        piePercentValue.text = "";

        if (percent >= 10) {
            pieLabel.text = label;
            piePercentValue.text = percent.ToString() + "%";
        } else if (percent >= 5) {
            pieLabel.text = label;
            piePercentValue.text = "";
        } else if (percent >= 3.5) {
            if(label == "快眠") {
                pieLabel.text = "快";
                piePercentValue.text = "眠";
            } else if (label == "いびき") {
                pieLabel.text = "いび";
                piePercentValue.text = "き";
            } else if (label == "無呼吸") {
                pieLabel.text = "無呼";
                piePercentValue.text = "吸";
            } else if (label == "不明") {
                pieLabel.text = "不";
                piePercentValue.text = "明";
            }
        }
    }

    public void hidePieInfo()
    {
        pieLabel.text = "";
        piePercentValue.text = "";
    }
}
