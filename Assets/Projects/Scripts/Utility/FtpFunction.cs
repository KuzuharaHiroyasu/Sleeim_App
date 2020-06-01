using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Kaimin.Managers;
using Asyncoroutine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// このアプリで使用するFTP通信の機能を簡単に使用できるようにまとめたクラス
/// </summary>
public class FtpFunction {

	/// <summary>
	/// FTPサーバーと通信して、前回データがあればデータを復元する一連の処理を実行します。
	/// </summary>
	/// <param name="mono">コルーチン実行のために必要となるMonobehaviourを継承したインスタンス</param>
	/// <param name="onComplete">終了時に呼び出されるコールバック</param>
	public static void RestoreData (MonoBehaviour mono, Action onComplete) {
		mono.StartCoroutine (RestoreDataFlow (mono, onComplete));
	}

	/// <summary>
	/// 復元の途中から再開するときの一連の処理を実行します
	/// </summary>
	/// <param name="mono">コルーチン実行のために必要となるMonobehaviourを継承したインスタンス</param>
	/// <param name="onComplete">終了時に呼び出されるコールバック</param>
	public static void ReRestoreData (MonoBehaviour mono, Action onComplete) {
		mono.StartCoroutine (ReRestoreDataFlow (mono, onComplete));
	}

	//前回データの復元処理の流れ
	//復元を途中から再開する場合(復元するかどうかの確認が必要ない場合)に使用する
	static IEnumerator ReRestoreDataFlow (MonoBehaviour mono, Action onComplete) {
		//同期中の表示
		UpdateDialog.Show ("同期中");
		//FTPサーバーに前回のデータが存在しているか確認する。
		var deviceAdress = UserDataManager.Device.GetPareringDeviceAdress ();
		Debug.Log ("deviceAdress:" + deviceAdress);
		int getPriviousDataResult = -1;	//前回データがあるかどうかの通信結果を格納する
		yield return mono.StartCoroutine (IsExistPriviousDataInServer (deviceAdress, (int result) => getPriviousDataResult = result));
		UpdateDialog.Dismiss ();
		if (getPriviousDataResult == 1) {
			//前回のデータが存在していれば、復元を再開する
			yield return mono.StartCoroutine (RestoreData (mono, deviceAdress));
		} else if (getPriviousDataResult == 0) {
			//前回データが存在していなければ、以降復元確認は不要と設定
			UserDataManager.State.SaveNessesaryRestore (false);	
		} else {
			//通信エラーが発生し、前回データの有無が確認できなければ
			//復元に失敗した事をユーザーに伝える
			yield return mono.StartCoroutine (TellFailedRestoreData ());
			//ダイアログを表示して、アプリを終了する
			#if UNITY_ANDROID
			//Androidの場合は、復元に失敗すればアプリを終了するか再接続するか確認する
			bool isShutDownApp = false;
			yield return mono.StartCoroutine (AskShutDownApp ((bool _isShutDownApp) => isShutDownApp = _isShutDownApp));
			if (isShutDownApp) {
				//アプリを終了
				Application.Quit ();
			} else {
				//終了しないなら、復元処理を再度行う
				yield return mono.StartCoroutine (RestoreData (mono, deviceAdress));
			}
			#elif UNITY_IOS
			//iOSの場合は、アプリの終了が行えないため確認はせずに再接続する
			yield return mono.StartCoroutine (RestoreData (mono, deviceAdress));
			#endif
		}
		onComplete ();
	}

	//以前の睡眠データを取得する処理の流れ
	static IEnumerator RestoreDataFlow (MonoBehaviour mono, Action onComplete) {
		//同期中の表示
		UpdateDialog.Show ("同期中");
		//FTPサーバーに前回のデータが存在しているか確認する。
		var deviceAdress = UserDataManager.Device.GetPareringDeviceAdress ();
		Debug.Log ("deviceAdress:" + deviceAdress);
		int getPriviousDataResult = -1;	//前回データがあるかどうかの通信結果を格納する
		yield return mono.StartCoroutine (IsExistPriviousDataInServer (deviceAdress, (int result) => getPriviousDataResult = result));
		UpdateDialog.Dismiss ();
		if (getPriviousDataResult == 1) {
			//前回のデータが存在していれば、ユーザにデータを復元するかどうか尋ねる
			bool isRestoreData = false;
			yield return mono.StartCoroutine (AskRestorePriviousData (mono, (bool _isRestoreData) => isRestoreData = _isRestoreData));
			if (isRestoreData) {
				//ユーザがデータの復元に同意すれば
				yield return mono.StartCoroutine (RestoreData (mono, deviceAdress));
			} else {
				//復元に同意しなかったなら
				UpdateDialog.Show ("同期中");

                //バックアップデータの削除をする
                var isDeleteTask = HttpManager.DeleteBackupDataByRename(deviceAdress);
                yield return isDeleteTask.AsCoroutine();
                if (isDeleteTask.Result) {
					//バックアップデータの削除に成功すれば
					//以降復元しないように設定
					UserDataManager.State.SaveNessesaryRestore (false);
					UpdateDialog.Dismiss ();
					//削除完了ダイアログを表示する
					yield return mono.StartCoroutine (TellCompleteBackupDataDelete ());
				} else {
					//バックアップデータの削除に失敗すれば
					UpdateDialog.Dismiss ();
					bool useNoBackup = false;
					yield return mono.StartCoroutine (AskNoBackupUse ((bool _useNoBackup) => useNoBackup = _useNoBackup));
					if (useNoBackup) {
						//バックアップの確認を断念し、このままアプリを使用するなら
						//以降復元確認は不要と設定
						UserDataManager.State.SaveNessesaryRestore (false);
					} else {
						//バックアップデータの確認をするなら
						//再帰処理
						yield return mono.StartCoroutine (RestoreDataFlow (mono, onComplete));
					}
				}
			}
		} else if (getPriviousDataResult == 0) {
			//前回データが存在していなければ、以降復元確認は不要と設定
			UserDataManager.State.SaveNessesaryRestore (false);	
		} else {
			//通信エラーが発生し、前回データの有無が確認できなければ
			bool useNoBackup = false;
			yield return mono.StartCoroutine (AskNoBackupUse ((bool _useNoBackup) => useNoBackup = _useNoBackup));
			if (useNoBackup) {
				//バックアップの確認を断念し、このままアプリを使用するなら
				//以降復元確認は不要と設定
				UserDataManager.State.SaveNessesaryRestore (false);
			} else {
				//バックアップデータの確認をするなら
				//再帰処理
				yield return mono.StartCoroutine (RestoreDataFlow (mono, onComplete));
			}
		}
		onComplete ();
	}

	//前回のデータを取得し、DBに登録する
	public static IEnumerator RestoreData (MonoBehaviour mono, string deviceAdress) {
		Debug.Log ("RestoreData");
		//同期中の表示
		UpdateDialog.Show ("同期中");
		//サーバーにある前回データ全てのファイルパスを取得する
		List<string> filePathList = new List<string> ();
		yield return mono.StartCoroutine (HttpManager.GetPriviousDataPathList (deviceAdress, (List<string> _filePathList) => filePathList = _filePathList));
		UpdateDialog.Dismiss ();
		if (filePathList != null) {
			//前回データのファイルパスが取得できれば
			int restoreDataCount = UserDataManager.State.GetRestoreDataCount ();	//既に復元済みのデータ件数
			yield return mono.StartCoroutine (DownloadServerDatasToRegistDBByHttp(mono, filePathList, restoreDataCount));
		} else {
			//何らかのエラーが発生すれば
			//復元に失敗した事をユーザーに伝える
			yield return mono.StartCoroutine (TellFailedRestoreData ());
			//ダイアログを表示して、アプリを終了する
			#if UNITY_ANDROID
			//Androidの場合は、復元に失敗すればアプリを終了するか再接続するか確認する
			bool isShutDownApp = false;
			yield return mono.StartCoroutine (AskShutDownApp ((bool _isShutDownApp) => isShutDownApp = _isShutDownApp));
			if (isShutDownApp) {
				//アプリを終了
				Application.Quit ();
			} else {
				//終了しないなら、復元処理を再度行う
				yield return mono.StartCoroutine (RestoreData (mono, deviceAdress));
			}
			#elif UNITY_IOS
			//iOSの場合は、アプリの終了が行えないため確認はせずに再接続する
			yield return mono.StartCoroutine (RestoreData (mono, deviceAdress));
			#endif
		}
		yield return null;
	}

    //指定したリストでDBに未登録のものを返す
    //注意: /RD8001/Data/112233445566/yyyyMMdd/20180827092055_1.csvのようなファイルパス専用
    static List<string> GetNoDBRegistDataList (List<string> dataList) {
		var sleepTable = MyDatabase.Instance.GetSleepTable ();
		List<string> result = new List<string> ();
		for (int i = 0; i < dataList.Count; i++) {
            var filePath = dataList[i]; //例：/RD8001/Data/112233445566/yyyyMMdd/20180827092055_1.csv
            Debug.Log ("file:" + filePath); 

            //ファイルパスから日付に変換
            var date = Kaimin.Common.Utility.getDateFromDownloadCsvFilePath(filePath); //例：20180827092055

            //DBに同じデータが存在しているか確認する
            var data = sleepTable.SelectFromColumn("date", date);
			if (data == null) {
				result.Add (filePath);
			}
		}

		return result;
	}

	//入力したファイルパスのリストからディレクトリによってリストを分割する
	//注意；/Data/112233445566/yyyyMMdd/20180827092055.csvのようなファイルパス専用
	static List<List<string>> DivideDataListByDirectory (List<string> dataList) {
		//追加した複数件のデータで年月が違うものがあれば、ダウンロード先のフォルダを分けるために複数のリストに分解する
		List<List<string>> divideDataList = new List<List<string>> ();
		List<string> sameDirectoryDataList = new List<string> ();
		string currentDirectoryName = "";
		for (int j = 0; j < dataList.Count; j++) {
			string data = dataList [j];
			//年月を見て、違うディレクトリに保存するデータであれば分ける
			//ファイルパスから年月のデータ部分を取り出す
			string dateString = data;
			dateString = dateString.Substring (0, dateString.LastIndexOf ('/'));
			dateString = dateString.Substring (dateString.LastIndexOf ('/') + 1);
			//一件目は否応なしに追加
			if (sameDirectoryDataList.Count == 0) {
				sameDirectoryDataList.Add (data);
			} else {
				//二件目以降であれば、同じディレクトリかどうか判断する
				//保存先のディレクトリが同じであるか判断する
				if (currentDirectoryName == "" || dateString == currentDirectoryName) {
					//同じディレクトリに保存するため、リストに追加
					sameDirectoryDataList.Add (data);
				} else {
					//違うディレクトリに保存
					//前のディレクトリのデータを保存
					divideDataList.Add (sameDirectoryDataList);
					sameDirectoryDataList = new List<string> ();
					sameDirectoryDataList.Add (data);
				}
			}
			currentDirectoryName = dateString;
			Debug.Log ("CurrentDirectoryName:" + currentDirectoryName);
			//もし最後のデータに到達すれば、もうひと階層上のリストに追加する
			bool isLastData = j >= dataList.Count - 1;
			if (isLastData) {
				divideDataList.Add (sameDirectoryDataList);
				sameDirectoryDataList = new List<string> ();
			}
		}
		return divideDataList;
	}

    /// <summary>
	/// Httpでサーバーに存在する指定のパスのファイルを取得して、DBに登録する
	/// </summary>
	/// <returns>The server datas to regist D.</returns>
	/// <param name="mono">Mono.</param>
	/// <param name="filePathList">File path list.</param>
	/// <param name="restoreCount">(復元再開時に使用)復元済みのデータ件数</param> 
	static IEnumerator DownloadServerDatasToRegistDBByHttp(MonoBehaviour mono, List<string> filePathList, int restoreCount = 0)
    {
        Debug.Log("DownloadServerDatasToRegistDB");
        //スリープしないようにする
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        //指定されたファイルパスとDBとを照合してDBに未登録のデータを探し出す
        List<string> noDBRegistDataList = GetNoDBRegistDataList(filePathList);

        var sleepTable = MyDatabase.Instance.GetSleepTable();

        //進捗ダイアログ表示
        ProgressDialog.Show("データを復元しています。", noDBRegistDataList.Count + restoreCount, restoreCount);
        int downloadCompleteDataSum = restoreCount; //ダウンロード完了したデータ件数
        
        if (!HttpManager.IsInternetAvailable())
        {
            //サーバーとの接続に失敗すれば、ダウンロードに失敗したら、スリープ設定解除
            Screen.sleepTimeout = SleepTimeout.SystemSetting;

            //進捗ダイアログ終了
            ProgressDialog.Dismiss();

            //復元に失敗した事をユーザーに伝える
            yield return mono.StartCoroutine(TellFailedRestoreData());
            AskReDownloadData(mono, filePathList, downloadCompleteDataSum);

            yield break;
        }

        foreach(var filePath in noDBRegistDataList)
        {
            //filePathの例: /RD8001/Data/112233445566/202002/20180827092055_1.csv
            var date = Kaimin.Common.Utility.getDateFromDownloadCsvFilePath(filePath); //例：20180827092055

            var dataDirectoryPath = filePath.Replace("/RD8001/Data/", "");
            dataDirectoryPath = dataDirectoryPath.Substring(0, dataDirectoryPath.LastIndexOf('/') + 1); //例：112233445566/yyyyMMdd/
            //念のためん、先頭にスラッシュがついてれば、削除する
            if (dataDirectoryPath.IndexOf('/') == 0)
                dataDirectoryPath = dataDirectoryPath.Substring(1);

            var dataPath = dataDirectoryPath + date + ".csv";               //例：112233445566/yyyyMM/20180827092055.csv
            string saveFilePath = Kaimin.Common.Utility.GsDataPath() + dataPath;

            string downloadUrl = HttpManager.API_DOWNLOAD_URL + "?file_path=" + filePath;
            var downloadTask = HttpManager.DownloadFile(saveFilePath, downloadUrl);
            yield return downloadTask.AsCoroutine();
            Debug.Log(downloadTask.Result ? "Download Success!" : "Download Failed...");

            //ダウンロードに成功すれば、ダウンロードしたデータをDBに登録する
            if (downloadTask.Result)
            {
                sleepTable.Update(new DbSleepData(date, dataPath, true));
                Debug.Log("Insert " + dataPath + " to DB.");

                //データが正常に保存されているか確認する
                Debug.Log("isExistFile:" + System.IO.File.Exists(saveFilePath));

                //進捗ダイアログ更新
                downloadCompleteDataSum++;
                //復元した件数を記録
                UserDataManager.State.SaveRestoreDataCount(downloadCompleteDataSum);
                ProgressDialog.UpdateProgress(downloadCompleteDataSum);
            }
            else
            {
                //ダウンロードに失敗したら

                //スリープ設定解除
                Screen.sleepTimeout = SleepTimeout.SystemSetting;
                //進捗ダイアログ終了
                ProgressDialog.Dismiss();

                //復元に失敗した事をユーザーに伝える
                yield return mono.StartCoroutine(TellFailedRestoreData());
                AskReDownloadData(mono, filePathList, downloadCompleteDataSum);

                yield break;
            }
        }

        //復元が成功した場合のみここまで到達
        Debug.Log("RestoreCompleted!!!!!");

        //DB確認
        Debug.Log("StartCheckDBData----------------------------");
        var dbFilePathList = Kaimin.Common.Utility.GetAllFiles(Kaimin.Common.Utility.GsDataPath(), "*.csv");
        foreach (var filePath in dbFilePathList)
        {
            Debug.Log("FilePath:" + filePath);
        }
        Debug.Log("EndCheckDBData----------------------------");

        //スリープ設定解除
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
        //これ以降復元しないように設定
        UserDataManager.State.SaveNessesaryRestore(false);
        //進捗ダイアログ終了
        ProgressDialog.Dismiss();
        //復元完了した事をユーザーに伝える
        yield return mono.StartCoroutine(TellCompleteDataRestore());
    }

    static IEnumerator AskReDownloadData(MonoBehaviour mono, List<string> filePathList, int downloadCompleteDataSum)
    {
        #if UNITY_ANDROID
            bool isShutDownApp = false;
            yield return mono.StartCoroutine(AskShutDownApp((bool _isShutDownApp) => isShutDownApp = _isShutDownApp));
            if (isShutDownApp)
            {
                //アプリを終了
                Application.Quit();
            }
            else
            {
                //終了しないなら、復元処理を再度行う
                yield return mono.StartCoroutine(DownloadServerDatasToRegistDBByHttp(mono, filePathList, downloadCompleteDataSum));
            }
        #elif UNITY_IOS
			//iOSの場合は、アプリの終了が行えないため確認はせずに再接続する
			yield return mono.StartCoroutine(DownloadServerDatasToRegistDBByHttp(mono, filePathList, downloadCompleteDataSum));
        #endif

		    yield return null;
    }

    /// <summary>
    /// サーバーに存在する指定のパスのファイルを取得して、DBに登録する
    /// </summary>
    /// <returns>The server datas to regist D.</returns>
    /// <param name="mono">Mono.</param>
    /// <param name="filePathList">File path list.</param>
    /// <param name="restoreCount">(復元再開時に使用)復元済みのデータ件数</param> 
    static IEnumerator DownloadServerDatasToRegistDB (MonoBehaviour mono, List<string> filePathList, int restoreCount = 0) {
		Debug.Log ("DownloadServerDatasToRegistDB");
		//スリープしないようにする
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		//指定されたファイルパスとDBとを照合してDBに未登録のデータを探し出す
		List<string> noDBRegistDataList = GetNoDBRegistDataList (filePathList);
		//ファイルのディレクトリによってリストを分割する
		List<List<string>> downloadDataListDivideDirectory = DivideDataListByDirectory (noDBRegistDataList);
		var sleepTable = MyDatabase.Instance.GetSleepTable ();

		//進捗ダイアログ表示
		ProgressDialog.Show ("データを復元しています。", noDBRegistDataList.Count + restoreCount, restoreCount);
		int downloadCompleteDataSum = restoreCount;	//ダウンロード完了したデータ件数
		//int multipleDownLoadDataCount = 5;	//一回でまとめてダウンロードする最大件数
		int multipleDownLoadDataCount = 1;	//一回でまとめてダウンロードする最大件数
		//ファイルアップロードのためにサーバーと接続
		bool isConnectionSuccess = false;
		bool isConnectionComplete = false;
		FtpManager.Connection ((bool _success) => {
			isConnectionSuccess = _success;
			isConnectionComplete = true;
		});
		yield return new WaitUntil (() => isConnectionComplete);
		if (!isConnectionSuccess) {
			//サーバーとの接続に失敗すれば
			//ダウンロードに失敗したら
			//スリープ設定解除
			Screen.sleepTimeout = SleepTimeout.SystemSetting;
			//進捗ダイアログ終了
			ProgressDialog.Dismiss ();
			//復元に失敗した事をユーザーに伝える
			yield return mono.StartCoroutine (TellFailedRestoreData ());
			#if UNITY_ANDROID
			bool isShutDownApp = false;
			yield return mono.StartCoroutine (AskShutDownApp ((bool _isShutDownApp) => isShutDownApp = _isShutDownApp));
			if (isShutDownApp) {
				//アプリを終了
				Application.Quit ();
			} else {
				//終了しないなら、復元処理を再度行う
				yield return mono.StartCoroutine (DownloadServerDatasToRegistDB (mono, filePathList, downloadCompleteDataSum));
			}
			#elif UNITY_IOS
			//iOSの場合は、アプリの終了が行えないため確認はせずに再接続する
			yield return mono.StartCoroutine(DownloadServerDatasToRegistDB(mono, filePathList, downloadCompleteDataSum));
			#endif
			yield break;
		}
		//ディレクトリごとに区切ってダウンロードを行う
		foreach (List<string> downloadDataListInDirectory in downloadDataListDivideDirectory) {
			//1つのディレクトリに存在するデータリストを一回でダウンロードする最大件数で更に区切って小分けにダウンロードを行う
			for (int i = 0; i < Mathf.CeilToInt ((float)downloadDataListInDirectory.Count / (float)multipleDownLoadDataCount); i++) {
				int dataCount = (downloadDataListInDirectory.Count - (multipleDownLoadDataCount * i)) >= multipleDownLoadDataCount
					? multipleDownLoadDataCount
					: downloadDataListInDirectory.Count % multipleDownLoadDataCount;
				List<string> downloadDataList = downloadDataListInDirectory.Skip (i * multipleDownLoadDataCount).Take (dataCount).ToList ();

				//保存先のパス(Android端末内)	
				var filePath = downloadDataList.First ();
				var dataDirectoryPath = filePath.Substring (filePath.IndexOf ('/') + 1);
				if (dataDirectoryPath.Contains ("Data"))		//パスの形式が先頭にスラッシュがついてるかついてないか心配なので、一応
					dataDirectoryPath = dataDirectoryPath.Substring (dataDirectoryPath.IndexOf ('/') + 1);
				dataDirectoryPath = dataDirectoryPath.Substring (0, dataDirectoryPath.LastIndexOf ('/') + 1);				//例：/112233445566/yyyyMMdd/
				if (dataDirectoryPath.Contains ("csv")) 	//念のため
					dataDirectoryPath = dataDirectoryPath.Substring (0, dataDirectoryPath.LastIndexOf ('/') + 1);
				//先頭にスラッシュがついてれば、削除する
				if (dataDirectoryPath.IndexOf ('/') == 0) 
					dataDirectoryPath = dataDirectoryPath.Substring (1);
				
				var fullDataDirectoryPath = Kaimin.Common.Utility.GsDataPath () + dataDirectoryPath;
				Debug.Log ("fullDataDirectoryPath:" + fullDataDirectoryPath);
				var downloadTask = FtpManager.ManualSingleDownloadFileAsync (fullDataDirectoryPath + Path.GetFileName(downloadDataList[0]), downloadDataList[0], null);
				yield return downloadTask.AsCoroutine ();
				//ダウンロードに成功すれば、ダウンロードしたデータをDBに登録する
				Debug.Log ("DataDownload:" + downloadTask.Result);
				if (downloadTask.Result) {
					//foreach (string downloadData in downloadDataList) {
						var date = downloadDataList[0];							//例：/Data/112233445566/yyyyMMdd/20180827092055.csv
						date = date.Substring (date.LastIndexOf ('/') + 1);	//例：20180827092055.csv
						date = date.Substring (0, date.LastIndexOf ('.'));	//例：20180827092055
						var dataPath = dataDirectoryPath + date + ".csv";
						sleepTable.Update (new DbSleepData (date, dataPath, true));
						Debug.Log ("Insert " + dataPath + " to DB.");
						//データが正常に保存されているか確認する
						Debug.Log ("isExistFile:" + System.IO.File.Exists (Kaimin.Common.Utility.GsDataPath () + dataPath));
						//進捗ダイアログ更新
						downloadCompleteDataSum++;
						//復元した件数を記録
						UserDataManager.State.SaveRestoreDataCount (downloadCompleteDataSum);
						ProgressDialog.UpdateProgress (downloadCompleteDataSum);
					//}
					Debug.Log ("Download Success!");
				} else {
					//ダウンロードに失敗したら
					//スリープ設定解除
					Screen.sleepTimeout = SleepTimeout.SystemSetting;
					//進捗ダイアログ終了
					ProgressDialog.Dismiss ();
					//復元に失敗した事をユーザーに伝える
					yield return mono.StartCoroutine (TellFailedRestoreData ());
					#if UNITY_ANDROID
					bool isShutDownApp = false;
					yield return mono.StartCoroutine (AskShutDownApp ((bool _isShutDownApp) => isShutDownApp = _isShutDownApp));
					if (isShutDownApp) {
						//アプリを終了
						Application.Quit ();
					} else {
						//終了しないなら、復元処理を再度行う
						yield return mono.StartCoroutine (DownloadServerDatasToRegistDB (mono, filePathList, downloadCompleteDataSum));
					}
					#elif UNITY_IOS
					//iOSの場合は、アプリの終了が行えないため確認はせずに再接続する
					yield return mono.StartCoroutine(DownloadServerDatasToRegistDB(mono, filePathList, downloadCompleteDataSum));
					#endif
					yield break;
				}
			}
		}
		//サーバーとの接続を切る
		FtpManager.DisConnect ();
		//復元が成功した場合のみここまで到達
		Debug.Log ("RestoreCompleted!!!!!");
		//DB確認
		Debug.Log ("StartCheckDBData----------------------------");
		var dbFilePathList = Kaimin.Common.Utility.GetAllFiles(Kaimin.Common.Utility.GsDataPath(), "*.csv");
		foreach (var filePath in dbFilePathList) {
			Debug.Log ("FilePath:" + filePath);
		}
		Debug.Log ("EndCheckDBData----------------------------");
		//スリープ設定解除
		Screen.sleepTimeout = SleepTimeout.SystemSetting;
		//これ以降復元しないように設定
		UserDataManager.State.SaveNessesaryRestore (false);
		//進捗ダイアログ終了
		ProgressDialog.Dismiss ();
		//復元完了した事をユーザーに伝える
		yield return mono.StartCoroutine (TellCompleteDataRestore ());
	}

	//アプリを終了する事をユーザーに伝える
	static IEnumerator AskShutDownApp (Action<bool> onResponse) {
		bool isOK = false;
		bool isCancel = false;
		MessageDialog.Show ("アプリを終了しますか？", true, true, () => isOK = true, () => isCancel = true, "はい", "いいえ");
		yield return new WaitUntil (() => isOK || isCancel);
		onResponse (isOK);
	}

	//データの復元を完了した事をユーザーに伝える
	static IEnumerator TellCompleteDataRestore () {
		bool isOK = false;
		MessageDialog.Show ("データの復元が完了しました。", true, false, () => isOK = true);
		yield return new WaitUntil (() => isOK);
	}

	/// <summary>
	/// データの復元に失敗した事を伝えるダイアログを表示する
	/// </summary>
	/// <param name="onResponse">結果を受け取るためのコールバック</param>
	static IEnumerator TellFailedRestoreData () {
		bool isOK = false;
		MessageDialog.Show ("データの復元に失敗しました。\nネットワーク接続を確認の上、再度お試しください。", true, false, () => isOK = true);
		yield return new WaitUntil (() => isOK);
	}

	/// <summary>
	/// 通信エラーでデータの復元が行えなかった際に、バックアップ確認を行わずにアプリを使用するかユーザに確認する
	/// </summary>
	/// <returns>The no backup use.</returns>
	static IEnumerator AskNoBackupUse (Action<bool> onAnswer) {
		bool isOk = false;
		bool isCancel = false;
		MessageDialog.Show (
			"<size=32>サーバーへ接続できませんでした。\nこのままバックアップ確認を行わずに、アプリを使用しますか。\n<color=red>※「はい」を選択すると、後で\nバックアップ確認は行えません。</color></size>",
			true,
			true,
			() => isOk = true,
			() => isCancel = true,
			"はい",
			"いいえ");
		yield return new WaitUntil (() => isOk || isCancel);
		onAnswer (isOk);
	}

	/// <summary>
	/// ユーザにサーバーに存在する前回のデータを復元するかどうか尋ねる
	/// </summary>
	/// <param name="onAnswer">OKかCancelかユーザが選択した時に呼び出されるコールバック</param>
	static IEnumerator AskRestorePriviousData (MonoBehaviour mono, Action<bool> onAnswer) {
		bool isOk = false;
		bool isCancel = false;
		MessageDialog.Show (
			"サーバーに前回のデータが存在します。\n前回データを復元しますか。\n\n※「はい」を選択時は、Wi-Fi環境でのダウンロードを推奨します。",
			true,
			true,
			() => isOk = true,
			() => isCancel = true,
			"はい",
			"いいえ");
		yield return new WaitUntil (() => isOk || isCancel);
		if (isOk) {
			onAnswer (true);
		} else {
			//データの復元を選択すれば、念のためもう一度確認する
			bool isReallyCancel = false;
			yield return mono.StartCoroutine (AskRegisterNewAccount ((bool _isReallyCancel) => isReallyCancel = _isReallyCancel));
			if (isReallyCancel) {
				//本当に復元をキャンセルするなら
				onAnswer (false);
			} else {
				//ユーザーがデータの復元を望むなら、再度復元するか尋ねる
				yield return mono.StartCoroutine (AskRestorePriviousData (mono, onAnswer));
			}
		}
	}

	//新規アカウントで登録し、前回データの復元ができなくなることをユーザに確認する
	static IEnumerator AskRegisterNewAccount (Action<bool> onAnswer) {
		bool isOk = false;
		bool isCancel = false;
		MessageDialog.Show (
			"<size=28>新規アカウントとして登録を行います。\n前回データの復元は行えなくなります\nが、本当によろしいですか？</size>",
			true,
			true,
			() => isOk = true,
			() => isCancel = true,
			"はい",
			"いいえ");
		yield return new WaitUntil (() => isOk || isCancel);
		onAnswer (isOk);
	}

	/// <summary>
	/// Httpでサーバーに前回データが存在しているかどうか
	/// </summary>
	/// <param name="deviceAdress">接続しているデバイスのアドレス(MACアドレス)</param> 
	/// <param name="onReceivResult">通信結果を返すコールバック 1:有り 0:無し -1:エラー</param>
	static IEnumerator IsExistPriviousDataInServer (string deviceAdress, Action<int> onReceivResult) {
        //サーバー上にデバイスアドレスと一致するディレクトリが存在するかどうか調べる
        int result = -1;

        if(HttpManager.IsInternetAvailable())
        {
            var isDirExistTask = HttpManager.IsDirectoryExist("/RD8001/Data/" + deviceAdress, 1);
            yield return isDirExistTask.AsCoroutine();

            result = isDirExistTask.Result ? 1 : 0;
        }
        
		Debug.Log (result == 1 ? "Privious data is Exist!" : "Privious data is NotExist...");
		onReceivResult (result);
	}

	/// <summary>
	/// FTPサーバーから前回データのファイルパスを全て取得する
	/// </summary>
	/// <param name="deviceAdress">接続しているデバイスのアドレス(MACアドレス)</param>
	/// <param name="onGetDataPathList">データのパスリスト取得完了時に呼び出されるコールバック。取得失敗時はnull</param> 
	static IEnumerator GetPriviousDataPathList (string deviceAdress, Action<List<string>> onGetDataPathList) {
		Debug.Log ("GetPriviousDataPathList");
		//スリープしないように設定
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		bool isConnectComplete = false;
		bool isConnectSuccess = false;
		FtpManager.Connection ((bool isSuccess) => {
			isConnectSuccess = isSuccess;
			isConnectComplete = true;
		});
		yield return new WaitUntil (() => isConnectComplete);
		if (!isConnectSuccess) {
			//接続失敗
			Debug.Log ("Connection Failed...");
			onGetDataPathList (null);
			//スリープ設定解除
			Screen.sleepTimeout = SleepTimeout.SystemSetting;
			yield break;
		}
		Debug.Log ("Connection Success");
		//GetListingではサブディレクトリまで取得する事ができないためCSVファイル取得するのに2回に分けてアクセスする必要がある。
		var firstPath = "/Data/" + deviceAdress;
		var isDirectoryGetListingSuccess = false;
		var isDirectoryGetListingComplete = false;
		List<List<string>> getDirectoryList = new List<List<string>> ();
		Debug.Log ("GetDirectoryFilePath:" + firstPath);
		FtpManager.ManualGetListing (firstPath, (bool _isSuccess, List<List<string>> _getPathList) => {
			isDirectoryGetListingSuccess = _isSuccess;
			getDirectoryList = _getPathList;
			isDirectoryGetListingComplete = true;
		});
		yield return new WaitUntil (() => isDirectoryGetListingComplete);
		if (!isDirectoryGetListingSuccess) {
			//取得失敗
			Debug.Log ("DirectoryGetListingFailed...");
			onGetDataPathList (null);
			//スリープ設定解除
			Screen.sleepTimeout = SleepTimeout.SystemSetting;
			yield break;
		}
		Debug.Log ("DirectoryGetListingSuccess");
		//確認用
		List<string> correctDirectoryFullPathList = new List<string> ();
		foreach (List<string> directoryInfo in getDirectoryList) {
			//取得したものにファイル、Linkが混じってる可能性があるためディレクトリのみにする
			if (int.Parse (directoryInfo [0]) == 1) {	//タイプがディレクトリなら
				correctDirectoryFullPathList.Add (directoryInfo [2]);	//フルパスを追加
				Debug.Log (directoryInfo [2] + " is directory");
			}
		}
		//取得したディレクトリから、その中のCSVファイルを取得しにいく。
		List<string> correctFileFullPathList = new List<string> ();
		foreach (string directoryFullPath in correctDirectoryFullPathList) {
			Debug.Log ("GetFilesIn:" + directoryFullPath);
			var isFileGetListingSuccess = false;
			var isFileGetListingComplete = false;
			var getFileList = new List<List<string>> ();
			FtpManager.ManualGetListing (directoryFullPath, (bool _isSuccess, List<List<string>> _getPathList) => {
				isFileGetListingSuccess = _isSuccess;
				getFileList = _getPathList;
				isFileGetListingComplete = true;
			});
			yield return new WaitUntil (() => isFileGetListingComplete);
			if (!isFileGetListingSuccess) {
				//取得失敗
				Debug.Log ("FileGetFailed:" + directoryFullPath);
				onGetDataPathList (null);
				//スリープ設定解除
				Screen.sleepTimeout = SleepTimeout.SystemSetting;
				yield break;
			}
			Debug.Log ("File Count is " + getFileList.Count);
			foreach (List<string> fileInfo in getFileList) {
				//取得したものにディレクトリ、Linkが混じってる可能性があるためファイルのみにする
				if (int.Parse (fileInfo [0]) == 0) {	//タイプがファイルなら
					correctFileFullPathList.Add (fileInfo [2]);	//フルパスを追加
					Debug.Log (fileInfo [2] + " is file");
				}
			}
		}
		//指定したパス以下のすべてのファイルが取得できたため、切断
		bool isDisConnectComplete = false;
		bool isDisConnectSuccess = false;
		FtpManager.DisConnect ((bool isSuccess) => {
			isDisConnectSuccess = isSuccess;
			isDisConnectComplete = true;
		});
		yield return new WaitUntil (() => isDisConnectComplete);
		if (!isDisConnectSuccess) {
			//切断失敗
			Debug.Log ("Disconnect Failed...");
			onGetDataPathList (null);
			//スリープ設定解除
			Screen.sleepTimeout = SleepTimeout.SystemSetting;
			yield break;
		}
		//確認用
		foreach (string resultPath in correctFileFullPathList) {
			Debug.Log (resultPath);
		}
		onGetDataPathList (correctFileFullPathList);
		//スリープ設定解除
		Screen.sleepTimeout = SleepTimeout.SystemSetting;
	}

	/// <summary>
	/// FTPサーバーに存在するバックアップデータを削除する一連の処理を実行します。
	/// (実際には削除ではなく復元できないようにリネームするだけです)
	/// <param name="mono">コルーチンを実行するのに必要なMonoBehaviourを継承したインスタンス</param>
	/// <param name="onFinish">削除処理が終了した時に呼び出されるコールバック</param>
	/// </summary>
	public static void DeleteBackupData (MonoBehaviour mono, Action onFinish = null) {
		string deviceAdress = UserDataManager.Device.GetPareringDeviceAdress ();
		mono.StartCoroutine (RenameBackupDataFlow (mono, onFinish));
	}
		
	/// <summary>
	/// バックアップデータを復元できないようリネームする処理の流れ
	/// </summary>
	/// <param name="mono">コルーチンを実行するのに必要なMonoBehaviourを継承したインスタンス</param>
	/// <param name="onFinish">リネーム処理が終了した時に呼び出されるコールバック</param>
	static IEnumerator RenameBackupDataFlow (MonoBehaviour mono, Action onFinish) {
		string deviceAdress = UserDataManager.Device.GetPareringDeviceAdress ();
		int getPriviousDataResult = -1;	//前回データがあるかどうかの通信結果を格納する
		UpdateDialog.Show ("同期中");
		yield return mono.StartCoroutine (IsExistPriviousDataInServer (deviceAdress, (int result) => getPriviousDataResult = result));
		UpdateDialog.Dismiss ();
		switch (getPriviousDataResult) {
		case 1:		//前回データが存在すれば
			bool isDelete = false;
			yield return mono.StartCoroutine (AskDeleteBackupData (mono, (bool _isDelete) => isDelete = _isDelete));
			if (isDelete) {
                //バックアップデータの削除をする
                UpdateDialog.Show ("同期中");

                var isDeleteTask = HttpManager.DeleteBackupDataByRename(deviceAdress);
                yield return isDeleteTask.AsCoroutine();
                if (!isDeleteTask.Result) {
					//バックアップデータの削除に失敗すれば
					//データの削除に失敗した事を伝えるダイアログを表示する
					yield return mono.StartCoroutine (TellFailedDeleteBackupData ());
					Debug.Log ("Failed GetBackupData...");
					if (onFinish != null)
						onFinish ();
					yield break;
				}
				UpdateDialog.Dismiss ();
				//削除完了ダイアログを表示する
				yield return mono.StartCoroutine (TellCompleteBackupDataDelete ());
			}
			break;
		case 0:		//前回データがなければ
			//削除するデータがない事を伝えるダイアログを表示する
			yield return mono.StartCoroutine (TellNotFoundBackupData ());
			Debug.Log ("Not found BackupData...");
			break;
		default:	//前回データの取得に失敗すれば
			//データの削除に失敗した事を伝えるダイアログを表示する
			yield return mono.StartCoroutine (TellFailedDeleteBackupData ());
			Debug.Log ("Failed GetBackupData...");
			break;
		}
		if (onFinish != null)
			onFinish ();
	}

	//バックアップデータの削除が完了した事をユーザーに伝えるダイアログを表示する
	static IEnumerator TellCompleteBackupDataDelete () {
		bool isOk = false;
		MessageDialog.Show ("バックアップデータの削除が\n完了しました。", true, false, () => isOk = true);
		yield return new WaitUntil (() => isOk);
	}

	//バクアップデータが無い事をユーザーに伝えるダイアログを表示する
	static IEnumerator TellNotFoundBackupData () {
		bool isOk = false;
		MessageDialog.Show ("削除対象のバックアップデータはありません。", true, false, () => isOk = true);
		yield return new WaitUntil (() => isOk);
	}

	//バックアップデータの削除に失敗した事をユーザーに伝えるダイアログを表示する
	static IEnumerator TellFailedDeleteBackupData () {
		bool isOk = false;
		MessageDialog.Show ("バックアップデータの削除に失敗しました。\nネットワーク接続を確認の上、再度お試しください。", true, false, () => isOk = true);
		yield return new WaitUntil (() => isOk);
	}
		
	/// <summary>
	/// サーバーにバックアップデータがあった場合に、データを削除するかユーザーに尋ねる
	/// </summary>
	/// <param name="onResponse">onResponse:ユーザーが返答したときに削除するかしないかを返すコールバック</param>
	static IEnumerator AskDeleteBackupData (MonoBehaviour mono, Action<bool> onResponse) {
		bool isOk = false;
		bool isCancel = false;
		MessageDialog.Show (
			"サーバーにバックアップデータが存在します。\nデータを削除しますか。", 
			true, 
			true, 
			() => isOk = true, 
			() => isCancel = true,
			"はい",
			"いいえ");
		yield return new WaitUntil (() => isOk || isCancel);
		if (isOk) {
			yield return mono.StartCoroutine (AskDeleteBackupDataAgain (onResponse));
		} else {
			onResponse (false);
		}
	}

	//本当にデータを削除するか再三ユーザーに確認する
	static IEnumerator AskDeleteBackupDataAgain (Action<bool> onResponse) {
		bool isOk = false;
		bool isCancel = false;
		MessageDialog.Show (
			"バックアップデータの削除を行うと、データの復元ができなくなりますが、本当によろしいですか？", 
			true, 
			true, 
			() => isOk = true, 
			() => isCancel = true,
			"はい",
			"いいえ");
		yield return new WaitUntil (() => isOk || isCancel);
		onResponse (isOk);
	}

	//バックアップデータをリネームした回数を取得する
	static IEnumerator GetBackupRenameCount (MonoBehaviour mono, string deviceAdress, Action<int> onGetCount) {
		//whileと同義。100回まで確認すれば十分かなと
		for (int renameCount = 0; renameCount < 100; renameCount++) {
			string directoryName = "";
			if (renameCount == 0) {
				//一回もリネームされてないならdeviceAdressそのままのディレクトリがサーバー上に存在するか確認
				directoryName = deviceAdress;
			} else {
				//一回以上リネームされていれば_deviceAdress_renameCountのディレクトリがサーバー上に存在するか確認
				directoryName = "_" + deviceAdress + "_" + renameCount.ToString ();
			}
			bool isExistDirectory = false;
			yield return mono.StartCoroutine (IsExistPriviousDataInServer (directoryName, (int result) => isExistDirectory = (result == 1)));
			if (!isExistDirectory) {
				//ディレクトリが存在しなければ
				onGetCount (renameCount - 1);
				yield break;
			}
		}
		yield return null;
	}

	//バックアップデータをリネームする
	static IEnumerator RenameBackupData (MonoBehaviour mono, string deviceAdress, Action<bool> onResponse) {
		int renameCount = 0;
		yield return mono.StartCoroutine (GetBackupRenameCount (mono, deviceAdress, (int _renameCount) => renameCount = _renameCount));
		string directoryName = deviceAdress;
		string renamedDirectoryName = "_" + deviceAdress + "_" + (renameCount + 1).ToString ();
		bool isRenameComplete = false;
		bool isRenameSuccess = false;
		FtpManager.Rename ("/Data/" + directoryName, "/Data/" + renamedDirectoryName, (bool isSuccess) => {
			isRenameComplete = true;
			isRenameSuccess = isSuccess;
		});
		yield return new WaitUntil (() => isRenameComplete);
		onResponse (isRenameSuccess);
	}
}
