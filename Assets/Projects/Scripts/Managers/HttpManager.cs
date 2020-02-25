using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Asyncoroutine;
using UnityEngine.Networking;

namespace Kaimin.Managers
{
    /// <summary>
    /// HTTP管理クラス
    /// </summary>
    /// 
    public class HttpManager
    {
        public const string HTTP_BASE_URL    = "http://down.one-a.co.jp";
        public const string API_UPLOAD_URL   = HTTP_BASE_URL + "/Welness/legal/api/upload.php";   //Params: device_id, file (FileName_FileID.csv)
        public const string API_DOWNLOAD_URL = HTTP_BASE_URL + "/Welness/legal/api/download.php"; //Params: file_path (Ex: /RD8001/Update/G1D/RD8001G1D_Ver000.000.001.016.bin)
        public const string API_RESTORE_URL  = HTTP_BASE_URL + "/Welness/legal/api/restore.php";  //Params: device_id
        public const string API_BACKUP_URL   = HTTP_BASE_URL + "/Welness/legal/api/backup.php";   //Params: device_id
        public const string API_DELETE_URL   = HTTP_BASE_URL + "/Welness/legal/api/delete.php";   //Params: device_id, file_name (FileName_FileID.csv)
        public const string API_DIR_EXIST_URL= HTTP_BASE_URL + "/Welness/legal/api/exist.php";    //Params: directory_path (Ex: /RD8001/Update/H1D, /RD8001/Data/xxx)

        public const int HTTP_TIMEOUT = 10000; //タイムアウト時間(sec)（共通） ※デフォルトは150000

        public const int OK = 1; //ステータス：OK
        public const int NG = 0; //ステータス：NG
        public const int ERROR = -1; //ステータス：ERROR

        public static async Task<bool> UploadFile(string deviceId, long fileId, string filePath, string uploadPath = "")
        {
            using (var client = new HttpClient())
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return false; //throw new ArgumentNullException(nameof(filePath));
                }
                else if (!File.Exists(filePath))
                {
                    return false; //throw new FileNotFoundException($"File [{filePath}] not found");
                }

                MultipartFormDataContent form = new MultipartFormDataContent();

                using (FileStream stream = File.Open(@filePath, FileMode.Open))
                {
                    byte[] result = new byte[stream.Length];
                    await stream.ReadAsync(result, 0, (int)stream.Length);

                    String fileName = Path.GetFileNameWithoutExtension(filePath) + "_" + fileId + ".csv";
                    ByteArrayContent fileContent = new ByteArrayContent(result);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    form.Add(fileContent, "file", fileName);
                    form.Add(new StringContent(deviceId), "device_id");
                    form.Add(new StringContent(uploadPath), "uploadPath");

                    Debug.Log("UploadFileAsync-start(" + fileName + ")");
                    var response = await client.PostAsync($"{API_UPLOAD_URL}/", form);
                    response.EnsureSuccessStatusCode();
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Log("UploadFileAsync-completet(" + fileName + ")");

                    var jsonResult = MiniJSON.Json.Deserialize(responseContent) as Dictionary<string, object>;
                    if (jsonResult.ContainsKey("err_code") && int.Parse(jsonResult["err_code"].ToString()) == 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static async Task<bool> DownloadFile(string saveFilePath, string downloadUrl)
        {
            //saveFilePath = "/storage/emulated/0/Android/data/jp.co.onea.sleeim/files/RD8001G1D_Ver000.000.001.016.bin";
            try
            {
                bool isDownloadComplete = false;
                WebClient client = new WebClient();
                client.DownloadDataAsync(new Uri(downloadUrl));
                //client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
                //client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadDataCompleted);
                client.DownloadDataCompleted += new DownloadDataCompletedEventHandler((object sender, DownloadDataCompletedEventArgs e) =>
                {
                    string dirPath = saveFilePath.Substring(0, saveFilePath.LastIndexOf('/') + 1);
                    CreateDirIfMissing(dirPath);

                    File.WriteAllBytes(saveFilePath, e.Result);

                    isDownloadComplete = true;
                });

                await new WaitUntil(() => isDownloadComplete);

                return true;
            } catch (Exception e)
            {
                return false;
            }
        }

        //fileNameの例:20191226235856_1.csv (FileName_FileId.csv)
        public static async Task<bool> DeleteFile(string deviceId, string fileName)
        {
            using (var client = new HttpClient())
            {
                MultipartFormDataContent form = new MultipartFormDataContent();
                form.Add(new StringContent(deviceId), "device_id");
                form.Add(new StringContent(fileName), "file_name");

                Debug.Log("DeleteFileAPI-start(" + fileName + ")");
                var response = await client.PostAsync($"{API_DELETE_URL}/", form);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.Log("DeleteFileAPI-completet(" + fileName + ")");

                var jsonResult = MiniJSON.Json.Deserialize(responseContent) as Dictionary<string, object>;
                if (jsonResult.ContainsKey("err_code") && int.Parse(jsonResult["err_code"].ToString()) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> DeleteBackupDataByRename(string deviceId)
        {
            using (var client = new HttpClient())
            {
                MultipartFormDataContent form = new MultipartFormDataContent();
                form.Add(new StringContent(deviceId), "device_id");

                Debug.Log("BackupDataAPI-start(" + deviceId + ")");
                var response = await client.PostAsync($"{API_BACKUP_URL}/", form);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.Log("BackupDataAPI-completet(" + deviceId + ")");

                var jsonResult = MiniJSON.Json.Deserialize(responseContent) as Dictionary<string, object>;
                if (jsonResult.ContainsKey("err_code") && int.Parse(jsonResult["err_code"].ToString()) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> IsDirectoryExist(string directoryPath)
        {
            using (var client = new HttpClient())
            {
                MultipartFormDataContent form = new MultipartFormDataContent();
                form.Add(new StringContent(directoryPath), "directory_path");

                Debug.Log("IsDirectoryExistAPI-start(" + directoryPath + ")");
                var response = await client.PostAsync($"{API_DIR_EXIST_URL}/", form);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.Log("IsDirectoryExistAPI-completet(" + directoryPath + ")");

                var jsonResult = MiniJSON.Json.Deserialize(responseContent) as Dictionary<string, object>;
                if (jsonResult.ContainsKey("err_code") && int.Parse(jsonResult["err_code"].ToString()) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsInternetAvailable()
        {
            return UnityEngine.Application.internetReachability != UnityEngine.NetworkReachability.NotReachable;
        }

        public static IEnumerator showDialogMessage(string message)
        {
            bool isOK = false;
            MessageDialog.Show("<size=32>" + message + "</size>", true, false, () => isOK = true);
            yield return new WaitUntil(() => isOK);
        }

        //shortFilePath例：112233445566/yyyyMM/20191226231111.csv
        public static string getDeviceId(string shortFilePath)
        {
            string deviceId = shortFilePath.Substring(0, shortFilePath.IndexOf('/'));
            return deviceId;
        }

        public static IEnumerator UploadUnsendDatasByHttp()
        {
            var dataPath = Kaimin.Common.Utility.GsDataPath();
            var sleepTable = MyDatabase.Instance.GetSleepTable();

            var sleepDatas = sleepTable.SelectAllOrderByAsc(); //DBに登録されたすべてのデータ
            var unSentDatas = sleepDatas.Where(data => data.send_flag == false).ToList(); //サーバーに送信してないすべてのデータ
            
            //データが0件ならアップロードを行わない
            if (unSentDatas.Count == 0)
            {
                yield break;
            }

            UpdateDialog.Show("同期中");
            Screen.sleepTimeout = SleepTimeout.NeverSleep; //スリープしないように設定

            Debug.Log("UploadUnsendDatasByHttp_unsentDataCount:" + unSentDatas.Count);
            var mulitipleUploadDataCount = 10;  //一回でまとめてアップロードするデータ件数
            List<DbSleepData> sendDataStock = new List<DbSleepData>();  //アップロードするデータを貯めておくリスト
                                                                        //ファイルアップロードのためにサーバーと接続
            bool isConnectionSuccess = HttpManager.IsInternetAvailable();
            if (!isConnectionSuccess)
            {
                //サーバーとの接続に失敗すれば
                UpdateDialog.Dismiss();
                //スリープ設定解除
                Screen.sleepTimeout = SleepTimeout.SystemSetting;
                yield break;
            }

            //サーバーに送信してないデータをアップロード
            for (int i = 0; i < unSentDatas.Count; i++)
            {
                var data = unSentDatas[i];
                var uploadPath = data.file_path;                                         //例：1122334455566/yyyyMM/20180827092055.csv
                uploadPath = uploadPath.Substring(0, uploadPath.LastIndexOf('/') + 1);   //例：1122334455566/yyyyMM/

                Debug.Log("data.date:" + data.date + "; data.file_path:" + data.file_path + "; fullPath:" + dataPath + data.file_path);
                if (System.IO.File.Exists(dataPath + data.file_path)) //アップロードするデータが正常か確認する
                {
                    sendDataStock.Add(data);
                }
                else
                {
                    //ファイルが存在してなければ、DBから削除する
                    sleepTable.DeleteFromTable(SleepTable.COL_DATE, data.date);
                }

                bool isStockDataCount = sendDataStock.Count >= mulitipleUploadDataCount;    //送信するデータ個数が一定量(multipleUploadDataCount)に達したかどうか
                bool isLastData = i >= unSentDatas.Count - 1;                               //最後のデータかどうか
                bool isSameDirectoryNextData = false;                                       //現在データと次データのアップロード先が同じであるか
                if (!isLastData)
                {
                    //最後のデータでなければ、次のデータが同じディレクトリのデータであるか確認する。
                    //現在データと比較できるように次データのパスを同じように変換
                    var nextDataDirectory = unSentDatas[i + 1].file_path;                                       //例：1122334455566/yyyyMM/20180827092055.csv
                    nextDataDirectory = nextDataDirectory.Substring(0, nextDataDirectory.LastIndexOf('/') + 1); //例：1122334455566/yyyyMM/

                    //現在データと次データのアップロード先パスを比較
                    isSameDirectoryNextData = uploadPath == nextDataDirectory;
                }

                Debug.Log("isStockDataCount:" + isStockDataCount + ",isLastData:" + isLastData + ",isSameDirectoryNextData:" + isSameDirectoryNextData);
                if (isStockDataCount || isLastData || !isSameDirectoryNextData)
                {
                    //まとめて送信するデータ件数に達したか、最後のデータに到達したらアップロードを行う
                    Debug.Log("UploadData");
                    foreach (var stockedData in sendDataStock)
                    {
                        string filePath = stockedData.file_path;
                        Debug.Log("stockData_path:" + filePath);

                        string deviceId = getDeviceId(filePath);
                        var uploadTask = HttpManager.UploadFile(deviceId, stockedData.file_id, dataPath + filePath);

                        yield return uploadTask.AsCoroutine();
                        //アップロードに成功すれば、アップロードしたファイルのDB送信フラグをtrueに
                        if (uploadTask.Result)
                        {
                            sleepTable.Update(new DbSleepData(stockedData.date, filePath, true)); //例：20180827092055.csv, 1122334455566/yyyyMM/20180827092055.csv
                            Debug.Log("Uploaded " + filePath);
                            sleepTable.DebugPrint();
                        }
                        else
                        {
                            ////アップロードに失敗すれば
                            UpdateDialog.Dismiss();
                            //スリープ設定解除
                            Screen.sleepTimeout = SleepTimeout.SystemSetting;
                            yield break;
                        }
                    }

                    //データのアップロードがひとまとまり完了すれば、次のデータのアップロードへ移る
                    sendDataStock = new List<DbSleepData>();
                }
            }

            Debug.Log("Upload end");

            UpdateDialog.Dismiss();
            //スリープ設定解除
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        /// <summary>
        /// サーバーからHttpで最新のファームウェアのファイル名を取得する
        /// </summary>
        /// <param name="firmwareDirectory">G1D・H1Dのディレクトリパス</param>
        /// <param name="onGetFileName">目的のファイル名を受け取るコールバック</param>
        /// <param name="fileExtension">.mot, .bin</param>
        public static IEnumerator GetLatestFirmwareFileNameByHttp(string firmwareDirectoryPath, Action<string> onGetFileName, string fileExtension, Action<bool> onResponseIsError = null)
        {
            bool directoryExistResult = false;
            if (HttpManager.IsInternetAvailable())
            {
                //HTTPでサーバー上にファームウェアのディレクトリが存在するか確認する
                var isDirExistTask = HttpManager.IsDirectoryExist(firmwareDirectoryPath);
                yield return isDirExistTask.AsCoroutine();

                directoryExistResult = isDirExistTask.Result;
                Debug.Log(directoryExistResult ? "G1D directory is Exist!" : "G1D directory is NotExist...");
            }
            
            if (directoryExistResult)
            {
                //指定したファームウェアディレクトリの名のファイル名をすべて取得する
                var getAllFirmwareFileNameList = FtpManager.GetAllList(firmwareDirectoryPath);
                yield return getAllFirmwareFileNameList.AsCoroutine();
                List<string> firmwareFileNameList = new List<string>();
                if (getAllFirmwareFileNameList.Result != null && getAllFirmwareFileNameList.Result.Count > 0)
                {
                    //取得したものには、ファイル、ディレクトリ、Linkが混在してるためファイルのみを取り出す
                    firmwareFileNameList = getAllFirmwareFileNameList.Result
                        .Where(data => int.Parse(data[0]) == 0) //ファイルのみ通す
                        .Select(data => data[1])        //ファイル名に変換
                        .ToList();

                    //ファイルがあるか確認
                    if (firmwareFileNameList.Count == 0)
                    {
                        onGetFileName(null);
                        if(onResponseIsError != null)
                        {
                            onResponseIsError(false);
                        }
                        Debug.Log("No firmwareFile");
                        yield break;
                    }

                    //ファームウェア以外のファイルをはじく
                    firmwareFileNameList = firmwareFileNameList
                        .Where(fileName => fileName.Contains(fileExtension))
                        .ToList();

                    //ファイルがあるか確認
                    if (firmwareFileNameList.Count == 0)
                    {
                        onGetFileName(null);
                        if (onResponseIsError != null)
                        {
                            onResponseIsError(false);
                        }
                        Debug.Log("No firmwareFile");
                        yield break;
                    }

                    //取得したディレクトリを確認
                    foreach (var fileName in firmwareFileNameList)
                    {
                        Debug.Log("GetFile:" + fileName);
                    }
                }
                else
                {
                    //なにかしらのエラーが発生した場合
                    onGetFileName(null);
                    if (onResponseIsError != null)
                    {
                        onResponseIsError(true);
                    }
                    yield break;
                }

                //ファイル名のリストが取得できれば、その中から最新のものを探す
                var ratestVersionFileIndex = firmwareFileNameList
                    .Select((fileName, index) => new { FileName = fileName, Index = index })
                    .Aggregate((max, current) => (FirmwareFileNameToVersionLong(max.FileName) > FirmwareFileNameToVersionLong(current.FileName) ? max : current))
                    .Index;
                if (onResponseIsError != null)
                {
                    onResponseIsError(false);
                }
                onGetFileName(firmwareFileNameList[ratestVersionFileIndex]);
                yield break;
            }

            if (onResponseIsError != null)
            {
                onResponseIsError(true);
            }
            onGetFileName(null);
        }

        //ファームウェアのアップデートファイルのフォルダ名からバージョン情報を抜き出して比較しやすいように整数型にして返す。
        //その値が大きいものほど新しいバージョン
        public static long FirmwareFileNameToVersionLong(string filePath)
        {
            string versionString = filePath;                                                //例：/Update/G1D/RD8001G1D_Ver000.000.000.004.mot
            versionString = versionString.Substring(0, versionString.LastIndexOf('.'));     //例：/Update/G1D/RD8001G1D_Ver000.000.000.004
            versionString = versionString.Substring(versionString.Length - 15);             //例：000.000.000.004
            versionString = versionString.Replace(".", "");                                 //例：000000000004

            return long.Parse(versionString);
        }

        //「000.000.000.000」の形式のバージョンの文字列から整数値に変換する
        public static long FirmwareVersionStringToLong(string version)
        {
            //形式が違った場合は-1を返す
            if (version.Length != 15)
                return -1;

            //ドットを取り除く
            string versionString = version.Replace(".", "");

            return long.Parse(versionString);
        }

        //サーバーに未アップロードのCsvファイルをFTPでアップロードする
        public static IEnumerator UploadUnsendDatas()
        {
            var dataPath = Kaimin.Common.Utility.GsDataPath();
            var sleepTable = MyDatabase.Instance.GetSleepTable();

            var sleepDatas = sleepTable.SelectAllOrderByAsc(); //DBに登録されたすべてのデータ
            var unSentDatas = sleepDatas.Where(data => data.send_flag == false).ToList(); //サーバーに送信してないすべてのデータ

            //データが0件ならアップロードを行わない
            if (unSentDatas.Count == 0)
            {
                yield break;
            }

            UpdateDialog.Show("同期中");
            Screen.sleepTimeout = SleepTimeout.NeverSleep; //スリープしないように設定

            Debug.Log("UploadUnsendDatas_unsentDataCount:" + unSentDatas.Count);
            var mulitipleUploadDataCount = 10;  //一回でまとめてアップロードするデータ件数
            List<DbSleepData> sendDataStock = new List<DbSleepData>();  //アップロードするデータを貯めておくリスト

            //ファイルアップロードのためにサーバーと接続
            bool isConnectionSuccess = false;
            bool isConnectionComplete = false;
            FtpManager.Connection((bool _success) =>
            {
                isConnectionSuccess = _success;
                isConnectionComplete = true;
            });
            yield return new WaitUntil(() => isConnectionComplete);
            if (!isConnectionSuccess)
            {
                //サーバーとの接続に失敗すれば
                UpdateDialog.Dismiss();
                //スリープ設定解除
                Screen.sleepTimeout = SleepTimeout.SystemSetting;
                yield break;
            }

            //サーバーに送信してないデータをアップロード
            for (int i = 0; i < unSentDatas.Count; i++)
            {
                var data = unSentDatas[i];
                var uploadPath = data.file_path;                                        //例：1122334455566/yyyyMM/20180827092055.csv
                uploadPath = uploadPath.Substring(0, uploadPath.LastIndexOf('/') + 1);  //例：1122334455566/yyyyMM/
                uploadPath = "/Data/" + uploadPath;                                     //例：/Data/1122334455566/yyyyMM/

                Debug.Log("data.date:" + data.date + "; data.file_path:" + data.file_path + "; fullPath:" + dataPath + data.file_path);

                //アップロードするデータが正常か確認する
                if (System.IO.File.Exists(dataPath + data.file_path))
                {
                    sendDataStock.Add(data);
                }
                else
                {
                    //ファイルが存在してなければ、DBから削除する
                    sleepTable.DeleteFromTable(SleepTable.COL_DATE, data.date);
                }

                bool isStockDataCount = sendDataStock.Count >= mulitipleUploadDataCount;    //送信するデータ個数が一定量(multipleUploadDataCount)に達したかどうか
                bool isLastData = i >= unSentDatas.Count - 1;                               //最後のデータかどうか
                bool isSameDirectoryNextData = false;                                       //現在データと次データのアップロード先が同じであるか
                if (!isLastData)
                {
                    //最後のデータでなければ、次のデータが同じディレクトリのデータであるか確認する。
                    //現在データと比較できるように次データのパスを同じように変換
                    var nextDataDirectory = unSentDatas[i + 1].file_path;                                       //例：1122334455566/yyyyMM/20180827092055.csv
                    nextDataDirectory = nextDataDirectory.Substring(0, nextDataDirectory.LastIndexOf('/') + 1); //例：1122334455566/yyyyMM/
                    nextDataDirectory = "/Data/" + nextDataDirectory;                                           //例：/Data/1122334455566/yyyyMM/
                    //現在データと次データのアップロード先パスを比較
                    isSameDirectoryNextData = uploadPath == nextDataDirectory;
                }
                Debug.Log("isStockDataCount:" + isStockDataCount + ",isLastData:" + isLastData + ",isSameDirectoryNextData:" + isSameDirectoryNextData);
                
                if (isStockDataCount || isLastData || !isSameDirectoryNextData)
                {
                    Debug.Log("UploadData");
                    //まとめて送信するデータ件数に達したか、最後のデータに到達したらアップロードを行う
                    //確認
                    foreach (var stockedData in sendDataStock)
                    {
                        Debug.Log("stockData_path:" + stockedData.file_path);
                    }

                    var uploadTask = FtpManager.ManualMulitipleUploadFileAsync(sendDataStock.Select(d => (dataPath + d.file_path)).ToList(), uploadPath);
                    yield return uploadTask.AsCoroutine();
                    Debug.Log(uploadTask.Result);
                    //アップロードに成功すれば、アップロードしたファイルのDB送信フラグをtrueに
                    if (uploadTask.Result)
                    {
                        for (int j = 0; j < sendDataStock.Count; j++)
                        {
                            var dateString = sendDataStock.Select(d => d.date).ToList()[j]; //例：20180827092055.csv
                            var filePath = sendDataStock.Select(d => d.file_path).ToList()[j];//例：1122334455566/yyyyMM/20180827092055.csv
                            sleepTable.Update(new DbSleepData(dateString, filePath, true));
                            Debug.Log("Uploaded.");
                            sleepTable.DebugPrint();
                        }
                        //データのアップロードがひとまとまり完了すれば、次のデータのアップロードへ移る
                        sendDataStock = new List<DbSleepData>();
                    }
                    else
                    {
                        //アップロードに失敗すれば
                        UpdateDialog.Dismiss();
                        //スリープ設定解除
                        Screen.sleepTimeout = SleepTimeout.SystemSetting;
                        yield break;
                    }
                }
            }

            Debug.Log("Upload end");

            //サーバーとの接続を切る
            FtpManager.DisConnect();
            UpdateDialog.Dismiss();
            //スリープ設定解除
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        public static void CreateDirIfMissing(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }
    }
}
