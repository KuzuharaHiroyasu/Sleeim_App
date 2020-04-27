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
    private Button backButton;

    [SerializeField]
    private GameObject footer;
    [SerializeField]
    private GameObject footerNone;

    [SerializeField]
	// Footerのトグル
	private Toggle toggle;

	[SerializeField]
	// 次へボタン
	private Button nextButton;

	[SerializeField]
	// 利用規約同意テキスト
	private Text termsText;


	protected override void Start () {
		base.Start ();

        int tapFromSetting = PlayerPrefs.GetInt("tapFromSetting", 0);
        if (tapFromSetting == 1)
        {
            backButton.gameObject.SetActive(true);
            footer.SetActive(false);
            footerNone.SetActive(false);
        } else //First time
        {
            backButton.gameObject.SetActive(false);
            footer.SetActive(true);
            footerNone.SetActive(false);
        }
    }

	public override SceneTransitionManager.LoadScene SceneTag {
		get {
			return SceneTransitionManager.LoadScene.TermsOfUse;
		}
	}

	//戻るボタンを押した際に実行される
	public void OnBackButtonTap () {
        if(isTapFromSetting())
        {
            SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Setting);
        }
	}

	#region Components Action (On Value Changed)
	// スクロール
    private void OnScrollChange() {
        // 一番下付近までスクロールした場合、チェック可能にする
        if (this.scrollRect.verticalNormalizedPosition < 0.05f)
        {
            // Toggel活性
			this.toggle.interactable = true;
            // TextColorを白に
			this.termsText.color = Color.white;
        }   
    }

	// トグル
	private void OnToggleChange()
	{
		// チェックありならボタン活性、なしなら非活性
		this.nextButton.interactable = this.toggle.isOn;
	}
	#endregion

	#region Components Action (On Click)
	// 次へボタン
	private void OnTapNextButton()
    {
		//規約に同意した事を記録
		UserDataManager.State.SaveAcceptTermOfUse();
		//利用規約画面は初期起動時以外では表示されないため、常にプロフィール画面に遷移する
		SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Profile);
	}
    #endregion
}
