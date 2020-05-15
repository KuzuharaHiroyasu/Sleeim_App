using UnityEngine;
using UnityEngine.UI;

public class BarPercentWeek : BarPercent
{
    public Text pDay;
    public Text pTime;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void drawChartInfoWeek(float percent1, float percent2, float percent3, string date, string sleepTime, int sleepMode, int vibrationStrength)
    {
        drawChartInfo(percent1, percent2, percent3, sleepMode, vibrationStrength, false);

        pDay.text = date;
        pTime.text = sleepTime;
    }

    public void drawChartInfoWeekWhenEmpty()
    {
        drawChartInfoWhenEmpty();

        pDay.text = "";
        pTime.text = "--:--";
    }
}
