using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewControllerBase : MonoBehaviour {

	static ViewControllerBase currentView;	//現在表示中のView

	public static ViewControllerBase CurrentView {
		get {
			return currentView;
		}
	}

	protected virtual void Start () {
		currentView = this;
	}

	//シーンを判別するためのタグを設定
	public virtual SceneTransitionManager.LoadScene SceneTag {
		get {
			return SceneTransitionManager.LoadScene.Other;
		}
	}

	/// <summary>
	/// バックキーで戻っても良いかどうか
	/// </summary>
	/// <param name="to">遷移先のシーン</param>
	public virtual bool IsAllowSceneBack () {
		//ダイアログ表示中であれば、遷移しないように
		return !DialogBase.IsDisp ();
	}

    public bool isTapFromHome()
    {
        int tapFromHome = PlayerPrefs.GetInt("tapFromHome", 0);
        if (tapFromHome == 1)
        {
            PlayerPrefs.SetInt("tapFromHome", 0); //Reset
            return true;
        }

        return false;
    }

    public bool isTapFromSetting()
    {
        int tapFromSetting = PlayerPrefs.GetInt("tapFromSetting", 0);
        if (tapFromSetting == 1)
        {
            PlayerPrefs.SetInt("tapFromSetting", 0); //Reset
            return true;
        }

        return false;
    }
}
