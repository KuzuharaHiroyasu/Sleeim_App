public class ChartInfo
{
    public string fileName;
    public string date;
    public System.DateTime realDateTime;
    public System.DateTime startSleepTime;
    public System.DateTime endSleepTime;
    public string sleepTime;

    public int sleepMode;
    public int vibrationStrength;

    public float pKaiMin = 0;
    public float pIbiki = 0;
    public float pMukokyu = 0;
    public float pFumei = 0;
}

public enum SleepMode
{
    Suppress = 1,
    Monitor = 2,
}

//SuppressionStrengthと同じ
public enum VibrationStrength
{
    Weak = 0,   //弱
    Medium = 1, //中
    Strong = 2, //強
    Multi = 3,  //徐々に強く
}