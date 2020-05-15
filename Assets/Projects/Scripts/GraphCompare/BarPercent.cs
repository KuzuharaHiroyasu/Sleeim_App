using UnityEngine;
using UnityEngine.UI;

public class BarPercent : MonoBehaviour {

    public Button btnIcon;
    public Text pkaiMin;
    public Text pIbiki;
    public Text pMukokyu;
    public Text pFumei;

	// Use this for initialization
	void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void drawChartInfo(float percent1, float percent2, float percent3, int sleepMode, int vibrationStrength = -1, bool showPercent = true)
    {
        double p1 = System.Math.Round((double)(percent1 * 100), 1);
        double p2 = System.Math.Round((double)(percent2 * 100), 1);
        double p3 = System.Math.Round((double)(percent3 * 100), 1);
        double p4 = 100 - p1 - p2 - p3;
        p4 = p4 < 0.1 ? 0 : System.Math.Round(p4, 1);

        string suffix = showPercent ? " %" : "";

        pkaiMin.text  = p1 + suffix;
        pIbiki.text   = p2 + suffix;
        pMukokyu.text = p3 + suffix;
        pFumei.text   = p4 + suffix;

        if(vibrationStrength >= 0)
        {
            string icName = "ic_mode_monitor"; //Default
            if (sleepMode != (int)SleepMode.Monitor)
            {
                icName = "ic_mode_suppress";
                if (vibrationStrength == (int)VibrationStrength.Weak)
                {
                    icName = "ic_mode_suppress_weak";
                }
                else if (vibrationStrength == (int)VibrationStrength.Strong)
                {
                    icName = "ic_mode_suppress_strong";
                }
                else if (vibrationStrength == (int)VibrationStrength.Multi)
                {
                    icName = "ic_mode_suppress_multi";
                }
            }
            
            btnIcon.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/" + icName);
        }
    }

    public void drawChartInfoWhenEmpty() 
    {
        pkaiMin.text = "-";
        pIbiki.text = "-";
        pMukokyu.text = "-";
        pFumei.text = "-";

        btnIcon.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/ic_mode_none"); ;
        btnIcon.GetComponentInChildren<Text>().text = "-";
        btnIcon.transform.localScale = new Vector3(1, 1, 1);
    }
}
