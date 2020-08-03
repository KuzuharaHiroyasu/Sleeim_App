using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kaimin.Managers;

public class ContactViewControler : ViewControllerBase {

	public override SceneTransitionManager.LoadScene SceneTag
	{
		get
		{
			return SceneTransitionManager.LoadScene.Contact;
		}
	}

	// Use this for initialization
	protected override void Start () {
		base.Start();
	}

	// Update is called once per frame
	void Update () {
		
	}

	/// <summary>
	/// 戻るボタン押下イベントハンドラ
	/// </summary>
	public void OnReturnButtonTap()
	{
		SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Setting);
	}

	public void OnMailContactButtonTap()
	{
		HelpMailLuncher.Lunch();
	}

	public void OnQAButtonTap()
	{
		if(HttpManager.IsInternetAvailable())
		{
			Application.OpenURL(HttpManager.HTTP_BASE_URL + "/Welness/faq/faq.html");
		} else
		{
			StartCoroutine(HttpManager.showDialogMessage("インターネット未接続のため、ページが開けません。"));
		}
	}

}
