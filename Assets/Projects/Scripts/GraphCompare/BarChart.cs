using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BarChart : MonoBehaviour {

    public int width = 40;
    public int height = 180;

    public Image barChart;
    public Image barHolder1; //Fumei
    public Image barHolder2; //Mukokyu
    public Image barHolder3; //Ibiki
    public Image barHolder4; //Kaimin

    // Use this for initialization
    void Start () {
        barChart.rectTransform.sizeDelta = new Vector2(width, height);
        barChart.color = new Color32(177, 255, 142, 0); //Set border of bar chart as blue
    }
	
	// Update is called once per frame
	void Update () {

    }

    public void changeWidth(int newWith)
    {
        width = newWith;
        barChart.rectTransform.sizeDelta = new Vector2(width, height);
    }

    public void drawBarChart(float percentKaimin, float percentIbiki, float percentMukokyu)
    {
        int barWidth = width - 2;
        int barHeight = height;

        barHolder4.rectTransform.sizeDelta = new Vector2(barWidth, percentKaimin * barHeight);
        barHolder3.rectTransform.sizeDelta = new Vector2(barWidth, percentIbiki * barHeight);
        barHolder2.rectTransform.sizeDelta = new Vector2(barWidth, percentMukokyu * barHeight);
        barHolder1.rectTransform.sizeDelta = new Vector2(barWidth, (1 - percentKaimin - percentIbiki - percentMukokyu) * barHeight);
    }

    public void drawBarChartWhenEmpty()
    {
        barHolder1.color = Color.clear;
        barHolder2.color = Color.clear;
        barHolder3.color = Color.clear;
        barHolder4.color = Color.clear;
    }
}
