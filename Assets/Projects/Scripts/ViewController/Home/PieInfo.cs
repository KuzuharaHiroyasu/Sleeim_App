using System.Collections;
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
        pieLabel.text = label;
        piePercentValue.text = percent.ToString() + "%";
    }

    public void hidePieInfo()
    {
        pieLabel.text = "";
        piePercentValue.text = "";
    }
}
