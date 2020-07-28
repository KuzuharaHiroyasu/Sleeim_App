using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using Kaimin.Managers;

/// 画面遷移を簡単に行えるようにするためのマネージャ
public class SceneTransitionManager : MonoBehaviour {

	static string tabBarSceneName = "TabBar";

	public enum LoadScene {
		Root,				//ルート。実際にシーンとしてはないが判別のために
		Other,				//定位置がないもの。実際にシーンとしてはないが判別のために
		Splash,				//スプラッシュ画面
		InitialLunch,		//利用規約の同意画面
		Home,				//ホーム
		Graph,				//グラフ
		History_Calendar,	//履歴(カレンダー)
		History_List,		//履歴(リスト)
		Setting,			//設定
		DeviceSetting,		// デバイス設定
		ActionMode,		// 動作モード
		SnoreSensitivity,		// いびき感度
		SuppressionStrength,		// 低減強度
		SuppressionOperationMaxTime,		// 低減動作最大継続時間
		AlermSetting,		//アラーム設定
		IbikiAlerm,			//いびきアラーム設定
		Profile,			//プロフィール
		Sex,				//性別選択
		LowBreathAlerm,		//低呼吸アラーム
		Vibration,			//バイブレーション設定
		FeedIn,				//フェードイン
		StopMove,			//体動停止
		CallTime,			//鳴動時間
		CallTimeSetting,	//鳴動時間設定
		DelayTime,			//遅延時間
		DelayTimeSetting,	//遅延時間設定
		DetectSense,		//検知感度設定
		Music,				//音楽
		License,			//ライセンス
		BTConnectPrepare,	//デバイス接続準備
		BTConnect,			//デバイス接続
		PrivacyPolicy,		//プライバシーポリシー
		TermsOfUse,			//利用規約
        GraphCompare,       //比較のグラフ
		Birth,              //生年月日
		Contact             //問い合わせ
	};

	/// <summary>
	/// 指定したシーンに遷移します
	/// </summary>
	public static void LoadLevel (LoadScene scene) {
		//シーン遷移可能なら
		string sceneName;
		bool isUseTab;
		SetSceneProperty (scene, out sceneName, out isUseTab);
		SceneManager.LoadScene (sceneName, LoadSceneMode.Single);
		if (isUseTab)
			SceneManager.LoadScene (tabBarSceneName, LoadSceneMode.Additive);
	}

	/// <summary>
	/// 現在のシーンからその前(親)のシーンへ遷移します
	/// </summary>
	public static void BackScene () {
		//コルーチンをstatic内で使用するためにMonoBehaviourが必要で、呼び出し元のSplashViewControllerから渡して貰おうとしたがGameObjectが永続的に存在しないためnullになる。
		//TouchManagerの画面タッチの流れからこのメソッドが呼ばれるため、そこからMonoBehaviourをもらってくる
		MonoBehaviour mono = naichilab.InputEvents.TouchManager.Instance;
		LoadScene parentScene = GetParentScene (ViewControllerBase.CurrentView.SceneTag);
		if (ViewControllerBase.CurrentView.IsAllowSceneBack ()) {
			if (parentScene == LoadScene.Root) {
				//アプリ終了する。確認ダイアログを挟んで。
				Debug.Log ("Root Scene");
				mono.StartCoroutine (QuitAppFlow (mono));
			} else if (parentScene == LoadScene.Other) {
				//特に何もしない
				Debug.Log ("Other Scene");
			} else {
				LoadLevel (parentScene);
			}
		} else {
			Debug.Log ("DontAllowBackSceneNow...");
		}
	}

	//アプリを終了させる処理の流れ
	static IEnumerator QuitAppFlow (MonoBehaviour mono) {
		bool isQuitApp = false;
		yield return mono.StartCoroutine (AskQuitApp ((bool isQuit) => isQuitApp = isQuit));
		if (isQuitApp) {
            try
            {
                BluetoothManager.Instance.BleDeinitialize();
            } catch (Exception e)
            {

            }
			
			#if UNITY_ANDROID || UNITY_IOS
			Application.Quit ();
			#endif
		}
	}

	//アプリを終了するかどうかユーザーに尋ねる
	static IEnumerator AskQuitApp (Action<bool> onResponse) {
		bool isOk = false;
		bool isCancel = false;
		MessageDialog.Show ("アプリを終了しますか？", true, true, () => isOk = true, () => isCancel = true);
		yield return new WaitUntil (() => isOk || isCancel);
		onResponse (isOk);
	}

	//指定したシーンの親シーンを取得
	//親シーンがない場合はnullを返す
	static LoadScene GetParentScene (LoadScene scene) {
		switch (scene) {
		case LoadScene.Home:
			return LoadScene.Root;
		case LoadScene.Graph:
			return LoadScene.Home;
		case LoadScene.History_Calendar:
			return LoadScene.Home;
		case LoadScene.History_List:
			return LoadScene.Home;
		case LoadScene.Setting:
			return LoadScene.Home;
		case LoadScene.AlermSetting:
			return LoadScene.Setting;
		case LoadScene.IbikiAlerm:
			return LoadScene.AlermSetting;
		case LoadScene.Profile:
			return LoadScene.Setting;
		case LoadScene.Sex:
			return LoadScene.Profile;
		case LoadScene.LowBreathAlerm:
			return LoadScene.AlermSetting;
		case LoadScene.Vibration:
			return LoadScene.AlermSetting;
		case LoadScene.FeedIn:
			return LoadScene.AlermSetting;
		case LoadScene.StopMove:
			return LoadScene.AlermSetting;
		case LoadScene.CallTime:
			return LoadScene.AlermSetting;
		case LoadScene.CallTimeSetting:
			return LoadScene.CallTime;
		case LoadScene.DelayTime:
			return LoadScene.AlermSetting;
		case LoadScene.DelayTimeSetting:
			return LoadScene.DelayTime;
		case LoadScene.DetectSense:
			return LoadScene.IbikiAlerm;
		case LoadScene.Music:
			return LoadScene.AlermSetting;
		case LoadScene.License:
			return LoadScene.Setting;
		case LoadScene.BTConnectPrepare:
			return LoadScene.Setting;
		case LoadScene.BTConnect:
			return LoadScene.Setting;
        case LoadScene.GraphCompare:
			return LoadScene.InitialLunch;
		case LoadScene.PrivacyPolicy:
			return LoadScene.InitialLunch;
		case LoadScene.TermsOfUse:
            int tapFromSetting = PlayerPrefs.GetInt("tapFromSetting", 0);
			return tapFromSetting == 1 ? LoadScene.Setting : LoadScene.InitialLunch;
		case LoadScene.Birth:
			return LoadScene.Profile;
		case LoadScene.Contact:
			return LoadScene.Setting;
		default:
			return LoadScene.Other;
		}
	}

	//シーン名とタブを使用するかどうかを設定
	//ここでシーンの追加や設定の変更を行う
	static void SetSceneProperty (LoadScene scene, out string sceneName, out bool isUseTab) {
		switch (scene) {
		case LoadScene.Splash:
			sceneName = "Splash";
			isUseTab = false;
			break;
		case LoadScene.InitialLunch:
			sceneName = "InitialLunch";
			isUseTab = false;
			break;
		case LoadScene.Home:
			sceneName = "HomeNew";
			isUseTab = true;
			break;
		case LoadScene.Graph:
			sceneName = "Graph";
			isUseTab = true;
			break;
		case LoadScene.History_Calendar:
			sceneName = "History_Calendar";
			isUseTab = true;
			break;
		case LoadScene.History_List:
			sceneName = "History_List";
			isUseTab = true;
			break;
		case LoadScene.Setting:
			sceneName = "Setting";
			isUseTab = true;
			break;
		case LoadScene.DeviceSetting:
			sceneName = "DeviceSetting";
			isUseTab = false;
			break;
		case LoadScene.ActionMode:
			sceneName = "ActionMode";
			isUseTab = false;
			break;
		case LoadScene.SnoreSensitivity:
			sceneName = "SnoreSensitivity";
			isUseTab = false;
			break;
		case LoadScene.SuppressionStrength:
			sceneName = "SuppressionStrength";
			isUseTab = false;
			break;
		case LoadScene.SuppressionOperationMaxTime:
			sceneName = "SuppressionOperationMaxTime";
			isUseTab = false;
			break;
		case LoadScene.AlermSetting:
			sceneName = "AlermSetting";
			isUseTab = false;
			break;
		case LoadScene.IbikiAlerm:
			sceneName = "IbikiAlerm";
			isUseTab = false;
			break;
		case LoadScene.Profile:
			sceneName = "Profile";
			isUseTab = false;
			break;
		case LoadScene.Sex:
			sceneName = "Sex";
			isUseTab = false;
			break;
		case LoadScene.LowBreathAlerm:
			sceneName = "LowBreathAlerm";
			isUseTab = false;
			break;
		case LoadScene.Vibration:
			sceneName = "Vibration";
			isUseTab = false;
			break;
		case LoadScene.FeedIn:
			sceneName = "FeedIn";
			isUseTab = false;
			break;
		case LoadScene.StopMove:
			sceneName = "StopMove";
			isUseTab = false;
			break;
		case LoadScene.CallTime:
			sceneName = "CallTime";
			isUseTab = false;
			break;
		case LoadScene.CallTimeSetting:
			sceneName = "CallTimeSetting";
			isUseTab = false;
			break;
		case LoadScene.DelayTime:
			sceneName = "AlermDelayTime";
			isUseTab = false;
			break;
		case LoadScene.DelayTimeSetting:
			sceneName = "DelayTimeSetting";
			isUseTab = false;
			break;
		case LoadScene.DetectSense:
			sceneName = "IbikiAlerm_DetectSense";
			isUseTab = false;
			break;
		case LoadScene.Music:
			sceneName = "Music";
			isUseTab = false;
			break;
		case LoadScene.License:
			sceneName = "License";
			isUseTab = false;
			break;
		case LoadScene.BTConnectPrepare:
			sceneName = "BTConnectPrepare";
			isUseTab = false;
			break;
        case LoadScene.GraphCompare:
            sceneName = "GraphCompare";
            isUseTab = true;
            break;
        case LoadScene.BTConnect:
			sceneName = "BTConnect";
			isUseTab = false;
			break;
		case LoadScene.PrivacyPolicy:
			sceneName = "PrivacyPolicy";
			isUseTab = false;
			break;
		case LoadScene.TermsOfUse:
			sceneName = "TermsOfUse";
			isUseTab = false;
			break;
		case LoadScene.Birth:
			sceneName = "Birth";
			isUseTab = false;
			break;
		case LoadScene.Contact:
			sceneName = "Contact";
			isUseTab = false;
			break;
		default:
			sceneName = "HomeNew";
			isUseTab = true;
			break;
		}
	}

	/// <summary>
	/// シーンをenumで指定して、名前を取得します
	/// </summary>
	public static string GetSceneName (LoadScene scene) {
		string sceneName;
		bool isUseTab;
		SetSceneProperty (scene, out sceneName, out isUseTab);
		return sceneName;
	}
}
