using UnityEngine;
// UI処理のクラスを使用する宣言
using UnityEngine.UI;

public class GraphZoomManager : MonoBehaviour {

	public Graph.IbikiGraph ibikiGraph;
	public Graph.BreathGraph breathGraph;

	public Image buttonImage;
	public Sprite buttonImageBig;
	public Sprite buttonImageMini;

	public bool isTapBigButton = true;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void onTapGraphZoomButton()
	{
		float sleepingTime = ibikiGraph.sleepingTime();
		if (isTapBigButton)
		{
			if (ibikiGraph.IsScroll())
			{
				return;
			}
			
			ibikiGraph.ReSizeBig();
			breathGraph.sleepingTime = sleepingTime;
			breathGraph.ResizeBig();
			ibikiGraph.setScroll(true);

			isTapBigButton = false;
			buttonImage.sprite = buttonImageMini;
		} 
		else  //Tap Minus Button
		{
			if (!ibikiGraph.IsScroll())
			{
				return;
			}

			ibikiGraph.ReSizeMin();
			breathGraph.sleepingTime = sleepingTime;
			breathGraph.ResizeMin();
			ibikiGraph.setScroll(false);

			isTapBigButton = true;
			buttonImage.sprite = buttonImageBig;
		}
	}

	public void setBigButtonAsDefault()
	{
		isTapBigButton = true;
		buttonImage.sprite = buttonImageBig;
	}
}
