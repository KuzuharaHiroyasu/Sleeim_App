using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using naichilab.InputEvents;
using Kaimin.Managers;
using MiniJSON;
using Asyncoroutine;
using System.IO;
using System.Linq;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SplashViewController : ViewControllerBase
{

    [SerializeField] Animator feedBlankAnimator;    //フェードイン・アウト演出用画像のAnimator
    [SerializeField] float feedTime;                //フェードインにかける時間(アウトも同じ)
    [SerializeField] float feedWaitTime;			//フェードイン完了からフェードアウト開始までの待機時間

    void Awake()
    {
        //Android版ステータスバー表示
        ApplicationChrome.dimmed = false;
        ApplicationChrome.statusBarState = ApplicationChrome.States.TranslucentOverContent;
        // Makes the status bar and navigation bar invisible (animated)
        ApplicationChrome.navigationBarState = ApplicationChrome.States.Hidden;

        //iOSはPlayerSettingsでステータスバーの設定を実施
    }

    protected override void Start()
    {
        base.Start();
        StartCoroutine(Flow());

        //ナビゲーションバーのタッチイベントを取得できるように
        TouchManager.Instance.NavigationAction += (object sender, CustomInputEventArgs e) =>
        {
            //バックボタンが押されたらシーンを戻るように
            if (e.Input.IsNavigationBackDown)
            {
                SceneTransitionManager.BackScene();
                Debug.Log("Scene Back");
            }
        };
    }

    public override SceneTransitionManager.LoadScene SceneTag
    {
        get
        {
            return SceneTransitionManager.LoadScene.Splash;
        }
    }

    //フェードイン開始
    //ブランドロゴが徐々に見える
    void StartFeedIn()
    {
        feedBlankAnimator.SetBool("isIn", true);
        feedBlankAnimator.SetBool("isOut", false);
    }

    //フェードアウト開始
    //ブランドロゴが徐々に隠れる
    void StartFeedOut()
    {
        feedBlankAnimator.SetBool("isOut", true);
        feedBlankAnimator.SetBool("isIn", false);
    }

    //フェードインが完了してブランドロゴが完全に表示されているかどうか
    bool IsCompleteFeedIn()
    {
        var stateInfo = feedBlankAnimator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("IsIn");
    }

    //フェードアウトが完了してブランドロゴが完全に隠れたかどうか
    bool IsCompleteFeedOut()
    {
        var stateInfo = feedBlankAnimator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("IsOut");
    }

    IEnumerator Flow()
    {
        //パーミッションチェック
        yield return StartCoroutine(PermissionCheck());
        //デバイスとの接続状況初期化 アプリ起動時は必ず切断されてる
        UserDataManager.State.SaveDeviceConnectState(false);
        //ロゴ表示
        yield return StartCoroutine(DispBrandLogo());
        //初期化
        yield return StartCoroutine(InitializeFlow());

        if (IsInitialLunch())
        {
            //初期設定
            //利用規約に同意していなければ、利用規約表示
            if (!IsAcceptPrivacyPolicy())
            {
                PlayerPrefs.SetInt("tapFromSetting", 0);
                SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.TermsOfUse);
                yield break;
            }
            //プロフィール設定をしていなければ、プロフィール表示
            if (!IsDoneProfileSetting())
            {
                SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Profile);
                yield break;
            }
        }
        //データの復元
        if (UserDataManager.State.isDoneDevicePareing())
        {
            //ペアリングが完了してるなら、復元確認を行う
            if (UserDataManager.State.isNessesaryRestore())
            {
                //データの復元が必要であれば、復元処理を行う
                bool isCompleteRestore = false;
                if (UserDataManager.State.GetRestoreDataCount() == 0)
                {
                    //初回復元の場合
                    FtpFunction.RestoreData(this, () => isCompleteRestore = true);
                    yield return new WaitUntil(() => isCompleteRestore);
                }
                else
                {
                    //復元再開の場合
                    FtpFunction.ReRestoreData(this, () => isCompleteRestore = true);
                    yield return new WaitUntil(() => isCompleteRestore);
                }
            }
        }
        if (!IsInitialLunch())
        {
            //未アップロードのCsvファイルが存在すれば、アップロードする
            yield return StartCoroutine(HttpManager.UploadUnsendDatasByHttp());
        }
        //FTPサーバーにファームウェアの最新バージョンが存在するか確認する
        if (UserDataManager.State.isDoneDevicePareing())    //ペアリング済みであれば
            yield return StartCoroutine(FarmwareVersionCheckFlow());
        //ホームへ
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Home);
    }

    //ブランドロゴを表示する演出
    IEnumerator DispBrandLogo()
    {
        //ブランドロゴを表示する演出開始
        StartFeedIn();
        //ブランドロゴのフェードインが完了するまで待機
        yield return new WaitUntil(() => IsCompleteFeedIn());
        //フェードインが完了すれば、ロゴを少しの間見せるために待機
        yield return new WaitForSeconds(feedWaitTime);
        //フェードアウト開始
        StartFeedOut();
        //フェードアウトが完了するまで待機
        yield return new WaitUntil(() => IsCompleteFeedOut());
    }

    //初期化処理の流れ
    IEnumerator InitializeFlow()
    {
        Debug.Log("Initialize");
#if UNITY_IOS
        //Documents配下をバックアップ非対象に設定(NativeのInitialize時にも設定している）
        UnityEngine.iOS.Device.SetNoBackupFlag(Application.persistentDataPath);
#endif
        //ストリーミングアセットから音楽フォルダコピー
        yield return StartCoroutine(StreamingAssetsFileCopy());
        //DataBase初期化
        yield return StartCoroutine(MyDatabase.Init(this));
        //Bluetooth初期化
        bool isInitializeComplete = false;
        BluetoothManager.Instance.Initialize(() => isInitializeComplete = true);
        yield return new WaitUntil(() => isInitializeComplete); //初期化完了待ち
    }

    //デバイスのファームウェアが最新のものかどうか確認する処理の流れ
    //ファームウェアが最新でなくても更新までは行わない
    IEnumerator FarmwareVersionCheckFlow()
    {
        Debug.Log("FarmwareVersionCheck");
        UpdateDialog.Show("同期中");
        //TODO:G1Dのファームウェアの更新があるかどうか調べる
        // long h1dVersionInDevice = FarmwareVersionStringToLong (UserDataManager.Device.GetH1DAppVersion ());
        //Ftpサーバーから最新のファームウェアのファイル名を取得
        string ratestH1dFileName = "";
        yield return StartCoroutine(HttpManager.GetLatestFirmwareFileNameByHttp("/RD8001/Update/H1D", (string fileName) => ratestH1dFileName = fileName, ".mot"));
        if (ratestH1dFileName == null)
        {
            //FTPサーバーにファイルが存在しなかった、もしくはエラーが発生したら
            UpdateDialog.Dismiss();
            yield break;
        }
        Debug.Log("Ratest H1D Farmware is " + ratestH1dFileName);

        //long h1dVersionRatest = HttpManager.FirmwareFileNameToVersionLong("/Update/H1D/" + ratestH1dFileName);
        // TODO:デバイスのファームウェアバージョンと、最新のファームウェアバージョンを比較する
        // bool isExistH1DRatestFarmware = h1dVersionRatest > h1dVersionInDevice;
        // TODO:デバイスのファームウェアバージョンと最新のファームウェアバージョンに差があるか設定
        // UserDataManager.Device.SaveIsExistFarmwareVersionDiff (isExistH1DRatestFarmware);
        // TODO:アイコンに反映する
        // if (isExistH1DRatestFarmware)
        if (false)
            DeviceStateManager.Instance.OnFirmwareUpdateNecessary();
        else
            DeviceStateManager.Instance.OnFirmwareUpdateNonNecessary();
        UpdateDialog.Dismiss();
    }

    /// <summary>
    /// ストリーミングアセットのファイルコピー
    /// </summary>
    IEnumerator StreamingAssetsFileCopy()
    {
        //初回ファイル作成
        //ディレクトリチェック
        string temp_path = Kaimin.Common.Utility.MusicTemplatePath();
        if (!Directory.Exists(temp_path))
        {
            //フォルダ作成
            Directory.CreateDirectory(temp_path);
        }
        //共通スナップコピー
        for (int i = 0; i < 6; i++)
        {
#if UNITY_IOS
            string tmp = "/alarm" + (i + 1).ToString ("00") + ".mp3";
//#elif UNITY_ANDROID && !UNITY_EDITOR
#else
            string tmp = "/alarm" + (i + 1).ToString("00") + ".ogg";
#endif
            string dstPath = temp_path + tmp;
            if (!File.Exists(dstPath))
            {
                string srcPath = Application.streamingAssetsPath + "/Musics" + tmp;
#if UNITY_ANDROID && !UNITY_EDITOR
                WWW www = new WWW (srcPath);
                while (!www.isDone) {
                    yield return null;
                }
                File.WriteAllBytes (dstPath, www.bytes);
#else
                File.Copy(srcPath, dstPath);
#endif
            }
        }

        string[] files = new string[] {
             "20191226235856.csv", "20191229032047.csv", "20191230011224.csv", "20200201234716.csv", "20200205235856.csv",
        };
        for (int i = 0; i < files.Length; i++)
        {

            string tmp = "/" + files[i];
            string dstPath = temp_path + tmp;
            if (!File.Exists(dstPath))
            {
                string srcPath = Application.streamingAssetsPath + "/Musics" + tmp;
#if UNITY_ANDROID && !UNITY_EDITOR
			WWW www = new WWW (srcPath);
			while (!www.isDone) {
				yield return null;
			}
			File.WriteAllBytes (dstPath, www.bytes);
#else
                File.Copy(srcPath, dstPath);
#endif
            }
        }

        yield break;
    }

    ////bluetoothに対応している端末かどうか確認する
    //IEnumerator BluetoothSupportCheck () {
    //	NativeManager.Instance.Initialize ();
    //	bool isSupport = NativeManager.Instance.BlesupportCheck ();
    //	if (!isSupport) {
    //		bool isOk = false;
    //		MessageDialog.Show ("この端末はBluetoothをサポートしていません。", true, false, () => isOk = true, null, "アプリ終了");
    //		yield return new WaitUntil (() => isOk);
    //	}
    //	yield return null;
    //}

    //bluetoothが有効になっているかどうか確認する
    IEnumerator BluetoothActiveCheck()
    {
        NativeManager.Instance.Initialize();
        bool isActive = NativeManager.Instance.BluetoothValidCheck();
        if (!isActive)
        {
            NativeManager.Instance.BluetoothRequest();
            bool isAllow = false;
#if UNITY_ANDROID
            yield return new WaitUntil(() => NativeManager.Instance.PermissionCode > 0);
            isAllow = NativeManager.Instance.PermissionCode == 1;
#elif UNITY_IOS
            isAllow = false;	//iOSの場合、ユーザーの選択が受け取れなかったため、拒否された前提で進める
#endif
            if (!isAllow)
            {
                //Todo：許可がない場合の処理を確認する
                Debug.Log("Bluetooth is NotActive...");
            }
        }
        //ネイティブが必要？いったん保留
        //Bluetoothが無効の場合は、無効の旨を表示し、システム設定の変更を促す
        yield return null;
    }

    //パーミッションの許可を求める処理の流れ
    IEnumerator PermissionCheck()
    {
        //必須(Dangerous)パーミッションのチェック
#if UNITY_ANDROID
        NativeManager.Instance.Initialize();
        NativeManager.Instance.CheckFuncPermission();
        yield return new WaitUntil(() => NativeManager.Instance.PermissionCode != -1);
        bool isOKPermission = NativeManager.Instance.PermissionCode == 0;   //0より大きい:許可なし 0:許可
        if (isOKPermission)
        {
            Debug.Log("Permission All OK.");
        }
        else
        {
            bool isOK = false;
            MessageDialog.Show("「設定」から権限を付与して\nアプリを再起動して下さい", true, false, () => isOK = true);
            yield return new WaitUntil(() => isOK);
            ShutDownApp();
        }
#endif
        yield return null;
    }

    //アプリ終了
    void ShutDownApp()
    {
        Debug.Log("Shut down App.");
        BluetoothManager.Instance.BleDeinitialize();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;    //停止するだけ
#elif UNITY_ANDROID || UNITY_IOS
        Application.Quit ();
#endif
    }

    //初期起動であるか
    bool IsInitialLunch()
    {
        return UserDataManager.State.isInitialLunch();  //ホームを見てない
    }

    //利用規約に同意しているか
    bool IsAcceptPrivacyPolicy()
    {
        return UserDataManager.State.isAcceptTermOfUse();
    }

    //プロフィール設定が完了しているか
    bool IsDoneProfileSetting()
    {
        return UserDataManager.Setting.Profile.isCompleteSetting();
    }

    //デバイスのペアリングをしているか
    bool IsDoneDevicePareing()
    {
        return UserDataManager.State.isDoneDevicePareing();
    }
}
