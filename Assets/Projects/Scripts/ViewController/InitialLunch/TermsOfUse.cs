using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TermsOfUse : ViewControllerBase {

	//[SerializeField]GameObject prehab;
	//[SerializeField]string URL;

	[SerializeField]
    // 画面真ん中のスクロールビュー
	private ScrollRect scrollRect;

	[SerializeField]
	// Footerのトグル
	private Toggle toggle;

	[SerializeField]
	// 次へボタン
	private Button nextButton;


	protected override void Start () {
		base.Start ();
		//prehab.GetComponent<PopUpWebView> ().Url = URL;
	}

	public override SceneTransitionManager.LoadScene SceneTag {
		get {
			return SceneTransitionManager.LoadScene.TermsOfUse;
		}
	}

	////戻るボタンを押した際に実行される
	//public void OnBackButtonTap () {
	//	SceneTransitionManager.LoadLevel (SceneTransitionManager.LoadScene.InitialLunch);
	//}

	#region Components Action (On Value Changed)
	// スクロール
    public void OnScrollChange() {
        // 一番下付近までスクロールした場合、チェック可能にする
        if (this.scrollRect.verticalNormalizedPosition < 0.05f)
        {
			this.toggle.interactable = true;
        }   
    }

	// トグル
	public void OnToggleChange()
	{
		// チェックありならボタン活性、なしなら非活性
		this.nextButton.interactable = this.toggle.isOn;
	}
	#endregion

	#region Components Action (On Click)
	// 次へボタン
	public void OnTapNextButton()
    {
		//規約に同意した事を記録
		UserDataManager.State.SaveAcceptTermOfUse();
		//利用規約画面は初期起動時以外では表示されないため、常にプロフィール画面に遷移する
		SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Profile);
	}
    #endregion
}
