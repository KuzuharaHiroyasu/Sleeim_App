using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrivacyPolicyViewController : ViewControllerBase {
	//http://down.one-a.co.jp/Welness/legal/privacy_policy.html

	protected override void Start () {
		base.Start ();
	}

	public override SceneTransitionManager.LoadScene SceneTag {
		get {
			return SceneTransitionManager.LoadScene.PrivacyPolicy;
		}
	}

	//戻るボタンをタップした際に呼び出される
	public void OnBackButtonTap () {
		SceneTransitionManager.LoadLevel (SceneTransitionManager.LoadScene.Home);
	}
}
