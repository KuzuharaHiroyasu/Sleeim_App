using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using UnityEngine;
using UnityEngine.Events;

namespace Kaimin.Managers
{
    /// <summary>
    /// FTP管理クラス
    /// </summary>
    /// Copyright（c）2015 JP Trosclair
    /// Released under the MIT license
    /// https://github.com/robinrodricks/FluentFTP/blob/master/LICENSE.TXT
    /// 
    public static class FtpManager
    {
        private const string FTP_IP = "kir352475.kir.jp";
        private const string FTP_USERNAME = "kir352475..welness";
        private const string FTP_PASSWORD = "4GxiX48F";

        private const int FTP_TIMEOUT = 10000; //タイムアウト時間(sec)（共通） ※デフォルトは150000

        private const int OK = 1; //ステータス：OK
        private const int NG = 0; //ステータス：NG
        private const int ERROR = -1; //ステータス：ERROR

        private static Thread thread = null;
        private static bool running;//スレッド実行フラグ
        private static CancellationTokenSource tokenSource;

        private static FtpClient conn = null;
        private static Action<bool> _cbConnect;   //完了時コールバック（接続、切断、リネーム）
        private static Action<int> _cbCheck;   //完了時コールバック（チェック）
        private static Action<bool, List<List<string>>> _cbList;   //完了時コールバック（リスト取得）

        private static List<List<string>> _filelist = new List<List<string>>();
        private static List<string> _Namefilelist = new List<string>();

        private static string[] fileList;
        /// <summary>
        /// 指定したディレクトリのディレクトリ及びファイル名を取得（簡易版）（自動接続/切断）
        /// ※サブディレクトリはチェックしない
        /// </summary>
        /// <param name="path"> //指定ディレクトリ
        /// </param>
        /// <returns>ディレクトリ又はファイルのフルパス（例：/Update/G1D/. , /Update/H1D/xxxxxx.mot)</returns>
        public static async Task<List<string>> GetNameAllList(string path)
        {
            try
            {
                using (conn = new FtpClient())
                {
                    conn.Host = FTP_IP;
                    //conn.DataConnectionType = FtpDataConnectionType.PASV;
                    conn.ConnectTimeout = FTP_TIMEOUT;
                    conn.ReadTimeout = FTP_TIMEOUT;
                    conn.DataConnectionConnectTimeout = FTP_TIMEOUT;
                    conn.DataConnectionReadTimeout = FTP_TIMEOUT;
                    conn.Credentials = new NetworkCredential(FTP_USERNAME, FTP_PASSWORD);
                    //bool confirm = conn.RecursiveList; //LIST -R

                    if (_Namefilelist != null)
                    {
                        _Namefilelist.Clear();
                    }

                    _Namefilelist = await GetNameListAsync(conn, path);
                }
                return _Namefilelist;
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                Debug.LogError(e.Message);
                return null;

            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (FluentFTP.FtpException e) //GetNameListAsync
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                return null;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                return null;
            }
        }

        private static async Task<List<string>> GetNameListAsync(FtpClient client, string path)
        {
            try
            {
                fileList = await client.GetNameListingAsync(path);

                foreach (string s in fileList)
                {
                    _Namefilelist.Add(s);

                }

                return _Namefilelist;
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (IOException e) //Unable to read data from the transport connection: 既存の接続はリモート ホストに強制的に切断されました。
            {
                Debug.LogError(e.Message);
                return null;
            }

            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        /// 指定したディレクトリ以下（子含まない）のディレクトリ及びファイル名を取得（自動接続/切断）
        /// </summary>
        /// <param name="path"> //指定ディレクトリ
        /// </param>
        /// <returns>タイプ(0:ファイル, 1:ディレクトリ, 2：Link), ファイル名(例：xxxxxx.mot), フルパス（例：/Update/H1D/xxxxxx.mot), 更新日時</returns>
        public static async Task<List<List<string>>> GetAllList(string path)
        {
            try
            {
                using (conn = new FtpClient())
                {
                    conn.Host = FTP_IP;
                    //conn.DataConnectionType = FtpDataConnectionType.PASV;
                    conn.ConnectTimeout = FTP_TIMEOUT;
                    conn.ReadTimeout = FTP_TIMEOUT;
                    conn.DataConnectionConnectTimeout = FTP_TIMEOUT;
                    conn.DataConnectionReadTimeout = FTP_TIMEOUT;
                    conn.Credentials = new NetworkCredential(FTP_USERNAME, FTP_PASSWORD);
                    //bool confirm = conn.RecursiveList; //LIST -R

                    if (_filelist != null)
                    {
                        _filelist.Clear();
                    }
                    else
                    {
                        _filelist = new List<List<string>>();
                        _filelist.Clear();
                    }

                    _filelist = await GetListAsync(conn, path);
                }
                return _filelist;
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                Debug.LogError(e.Message);
                return null;

            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (FluentFTP.FtpException e) //GetListAsync
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                return null;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                return null;
            }
        }

        private static async Task<List<List<string>>> GetListAsync(FtpClient client, string path)
        {
            try
            {
                FtpFileSystemObjectType type;
                DateTime time;
                string str;

                foreach (FtpListItem item in await client.GetListingAsync(path, FtpListOption.Auto))
                {
                    switch (item.Type)
                    {
                        case FtpFileSystemObjectType.Directory:
                            // get modified date/time of the file or folder
                            time = client.GetModifiedTime(item.FullName); //ディレクトリの日時は使用しないこと
                            type = item.Type;
                            str = ((int)type).ToString();

                            Debug.Log("Type: " + item.Type);
                            Debug.Log("GetModifiedTime: " + client.GetModifiedTime(item.FullName));
                            Debug.Log("FileName: " + item.Name);
                            Debug.Log("FullName: " + item.FullName);

                            //タイプ(0:ファイル, 1:ディレクトリ, 2：Link), ファイル名, フルパス, 更新日時
                            _filelist.Add(new List<string>(new string[] { str, item.Name, item.FullName, time.ToString() }));

                            //await GetListAsync(client, item.FullName); //サブディレクトリ
                            break;
                        case FtpFileSystemObjectType.File:
                            // get modified date/time of the file or folder
                            time = client.GetModifiedTime(item.FullName);
                            type = item.Type;
                            str = ((int)type).ToString();

                            Debug.Log("Type: " + item.Type);
                            Debug.Log("GetModifiedTime: " + client.GetModifiedTime(item.FullName));
                            Debug.Log("FileName: " + item.Name);
                            Debug.Log("FullName: " + item.FullName);

                            //タイプ(0:ファイル, 1:ディレクトリ, 2：Link), ファイル名, フルパス, 更新日時
                            _filelist.Add(new List<string>(new string[] { str, item.Name, item.FullName, time.ToString() }));
                            break;
                        default:
                            break;
                    }
                }
                return _filelist;
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (IOException e) //Unable to read data from the transport connection: 既存の接続はリモート ホストに強制的に切断されました。
            {
                Debug.LogError(e.Message);
                return null;
            }

            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        /// 指定したディレクトリ以下（子含む）のディレクトリ及びファイル名を取得（自動接続/切断）
        /// </summary>
        /// <param name="path"> //指定ディレクトリ
        /// </param>
        /// <returns>タイプ(0:ファイル, 1:ディレクトリ, 2：Link), ファイル名(例：xxxxxx.mot), フルパス（例：/Update/H1D/xxxxxx.mot), 更新日時</returns>
        public static async Task<List<List<string>>> GetAllListAddSub(string path)
        {
            try
            {
                using (conn = new FtpClient())
                {
                    conn.Host = FTP_IP;
                    //conn.DataConnectionType = FtpDataConnectionType.PASV;
                    conn.ConnectTimeout = FTP_TIMEOUT;
                    conn.ReadTimeout = FTP_TIMEOUT;
                    conn.DataConnectionConnectTimeout = FTP_TIMEOUT;
                    conn.DataConnectionReadTimeout = FTP_TIMEOUT;
                    conn.Credentials = new NetworkCredential(FTP_USERNAME, FTP_PASSWORD);
                    //bool confirm = conn.RecursiveList; //LIST -R

                    if (_filelist != null)
                    {
                        _filelist.Clear();
                    }
                    else
                    {
                        _filelist = new List<List<string>>();
                        _filelist.Clear();
                    }

                    _filelist = await GetListAsyncAddSub(conn, path);
                }
                return _filelist;
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                Debug.LogError(e.Message);
                return null;

            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (FluentFTP.FtpException e) //GetListAsync
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                return null;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                return null;
            }
        }

        private static async Task<List<List<string>>> GetListAsyncAddSub(FtpClient client, string path)
        {
            try
            {
                FtpFileSystemObjectType type;
                DateTime time;
                string str;

                foreach (FtpListItem item in await client.GetListingAsync(path, FtpListOption.Auto))
                {
                    switch (item.Type)
                    {
                        case FtpFileSystemObjectType.Directory:
                            // get modified date/time of the file or folder
                            time = client.GetModifiedTime(item.FullName); //ディレクトリの日時は使用しないこと
                            type = item.Type;
                            str = ((int)type).ToString();

                            Debug.Log("Type: " + item.Type);
                            Debug.Log("GetModifiedTime: " + client.GetModifiedTime(item.FullName));
                            Debug.Log("FileName: " + item.Name);
                            Debug.Log("FullName: " + item.FullName);

                            //タイプ(0:ファイル, 1:ディレクトリ, 2：Link), ファイル名, フルパス, 更新日時
                            _filelist.Add(new List<string>(new string[] { str, item.Name, item.FullName, time.ToString() }));

                            await GetListAsync(client, item.FullName); //サブディレクトリ
                            break;
                        case FtpFileSystemObjectType.File:
                            // get modified date/time of the file or folder
                            time = client.GetModifiedTime(item.FullName);
                            type = item.Type;
                            str = ((int)type).ToString();

                            Debug.Log("Type: " + item.Type);
                            Debug.Log("GetModifiedTime: " + client.GetModifiedTime(item.FullName));
                            Debug.Log("FileName: " + item.Name);
                            Debug.Log("FullName: " + item.FullName);

                            //タイプ(0:ファイル, 1:ディレクトリ, 2：Link), ファイル名, フルパス, 更新日時
                            _filelist.Add(new List<string>(new string[] { str, item.Name, item.FullName, time.ToString() }));
                            break;
                        default:
                            break;
                    }
                }
                return _filelist;
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
                return null;
            }
            catch (IOException e) //Unable to read data from the transport connection: 既存の接続はリモート ホストに強制的に切断されました。
            {
                Debug.LogError(e.Message);
                return null;
            }

            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        /// ファイルのアップロード（シングル）（自動接続/切断）
        /// 保存先は1つのディレクトリを指定
        /// </summary>
        /// <param name="sourceDatas"> //アップロードするファイル（複数対応）
        /// <param name="savedpath"> //アップロード先のパス(FTPサーバー)を指定
        /// <param name="progress"> //ファイルの進捗状況(0-100転送された割合、-1は不明)
        /// </param>
        public static async Task<bool> SingleUploadFileAsync(string sourceDatas, string savedpath, IProgress<double> progress)
        {
            try
            {
                using (conn = new FtpClient())
                {
                    conn.Host = FTP_IP;
                    //conn.DataConnectionType = FtpDataConnectionType.PASV;
                    conn.ConnectTimeout = FTP_TIMEOUT;
                    conn.ReadTimeout = FTP_TIMEOUT;
                    conn.DataConnectionConnectTimeout = FTP_TIMEOUT;
                    conn.DataConnectionReadTimeout = FTP_TIMEOUT;
                    conn.Credentials = new NetworkCredential(FTP_USERNAME, FTP_PASSWORD);

                    if ((UnityEngine.Application.internetReachability != UnityEngine.NetworkReachability.NotReachable))
                    {
                        Debug.Log("ConnectAsync-start");
                        StartThreadConnect();
                        await conn.ConnectAsync();
                        EndThread();
                        Debug.Log("ConnectAsync-end");

                        tokenSource = new CancellationTokenSource();
                        var cancelToken = tokenSource.Token;

                        //エラー時でも途中のファイルは作成されるので注意（上書き時は一旦ファイル削除⇒ファイル作成⇒ファイル作成完了の流れ）
                        //Overwriteでファイル上書き設定
                        //保存先のディレクトリフォルダがなくても作成
                        Debug.Log("UploadFileAsync-start");
                        StartThreadData(tokenSource);
                        await conn.UploadFileAsync(sourceDatas, @savedpath, FtpExists.Overwrite, true, FtpVerify.Delete | FtpVerify.Throw, cancelToken, progress);
                        EndThread();
                        Debug.Log("UploadFileAsync-end");
                    }
                    else
                    {
                        throw new Exception("NetworkReachability.NotReachable");
                    }
                }
                return true;
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                Debug.LogError(e.Message);
                EndThread();
                return false;

            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                Debug.LogError(e.Message);
                EndThread();
                return false;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
                EndThread();
                return false;
            }
            catch (FluentFTP.FtpException e) //UploadFileAsync
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
        }

        /// <summary>
        /// ファイルのアップロード（シングル）（手動接続/切断）
        /// 保存先は1つのディレクトリを指定
        /// </summary>
        /// <param name="sourceDatas"> //アップロードするファイル（複数対応）
        /// <param name="savedpath"> //アップロード先のパス(FTPサーバー)を指定
        /// <param name="progress"> //ファイルの進捗状況(0-100転送された割合、-1は不明)
        /// </param>
        public static async Task<bool> ManualSingleUploadFileAsync(string sourceDatas, string savedpath, IProgress<double> progress)
        {
            try
            {
                if ((conn != null) && ((UnityEngine.Application.internetReachability != UnityEngine.NetworkReachability.NotReachable)))
                {
                    tokenSource = new CancellationTokenSource();
                    var cancelToken = tokenSource.Token;

                    //エラー時でも途中のファイルは作成されるので注意（上書き時は一旦ファイル削除⇒ファイル作成⇒ファイル作成完了の流れ）
                    //Overwriteでファイル上書き設定
                    //保存先のディレクトリフォルダがなくても作成
                    Debug.Log("UploadFileAsync-start");
                    StartThreadData(tokenSource);
                    await conn.UploadFileAsync(sourceDatas, @savedpath, FtpExists.Overwrite, true, FtpVerify.Delete | FtpVerify.Throw, cancelToken, progress);
                    EndThread();
                    Debug.Log("UploadFileAsync-end");

                    return true;
                }
                else
                {
                    Debug.Log("NetworkReachability.NotReachable");

                    if (conn != null)
                    {
                        conn.Dispose();
                    }
                    return false;
                }
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (FluentFTP.FtpException e) //UploadFileAsync
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (Exception e)
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
        }


        /// <summary>
        /// ファイルのアップロード（複数）（自動接続/切断）
        /// 保存先は1つのディレクトリを指定
        /// </summary>
        /// <param name="sourceDatas"> //アップロードするファイル（複数対応）
        /// <param name="savedpath"> //アップロード先のパス(FTPサーバー)を指定
        /// </param>
        public static async Task<bool> MulitipleUploadFileAsync(IEnumerable<string> sourceDatas, string savedpath)
        {
            try
            {
                using (conn = new FtpClient())
                {
                    conn.Host = FTP_IP;
                    //conn.DataConnectionType = FtpDataConnectionType.PASV;
                    conn.ConnectTimeout = FTP_TIMEOUT;
                    conn.ReadTimeout = FTP_TIMEOUT;
                    conn.DataConnectionConnectTimeout = FTP_TIMEOUT;
                    conn.DataConnectionReadTimeout = FTP_TIMEOUT;
                    conn.Credentials = new NetworkCredential(FTP_USERNAME, FTP_PASSWORD);

                    if ((UnityEngine.Application.internetReachability != UnityEngine.NetworkReachability.NotReachable))
                    {
                        Debug.Log("ConnectAsync-start");
                        StartThreadConnect();
                        await conn.ConnectAsync();
                        EndThread();
                        Debug.Log("ConnectAsync-end");

                        tokenSource = new CancellationTokenSource();
                        var cancelToken = tokenSource.Token;

                        //エラー時でも途中のファイルは作成されるので注意（上書き時は一旦ファイル削除⇒ファイル作成⇒ファイル作成完了の流れ）
                        //Overwriteでファイル上書き設定
                        //保存先のディレクトリフォルダがなくても作成
                        Debug.Log("UploadFilesAsync-start");
                        StartThreadData(tokenSource);
                        await conn.UploadFilesAsync(sourceDatas, @savedpath, FtpExists.Overwrite, true, FtpVerify.Delete | FtpVerify.Throw, FtpError.Throw,cancelToken);
                        EndThread();
                        Debug.Log("UploadFilesAsync-end");
                    }
                    else
                    {
                        throw new Exception("NetworkReachability.NotReachable");
                    }
                }
                return true;
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                Debug.LogError(e.Message);
                EndThread();
                return false;
            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                Debug.LogError(e.Message);
                EndThread();
                return false;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
                EndThread();
                return false;
            }
            catch (FluentFTP.FtpException e) //UploadFilesAsync
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
        }

        /// <summary>
        /// ファイルのアップロード（複数）（手動接続/切断）
        /// 保存先は1つのディレクトリを指定
        /// </summary>
        /// <param name="sourceDatas"> //アップロードするファイル（複数対応）
        /// <param name="savedpath"> //アップロード先のパス(FTPサーバー)を指定
        /// </param>
        public static async Task<bool> ManualMulitipleUploadFileAsync(IEnumerable<string> sourceDatas, string savedpath)
        {
            try
            {
                if ((conn != null) && ((UnityEngine.Application.internetReachability != UnityEngine.NetworkReachability.NotReachable)))
                {
                    tokenSource = new CancellationTokenSource();
                    var cancelToken = tokenSource.Token;

                    //エラー時でも途中のファイルは作成されるので注意（上書き時は一旦ファイル削除⇒ファイル作成⇒ファイル作成完了の流れ）
                    //Overwriteでファイル上書き設定
                    //保存先のディレクトリフォルダがなくても作成
                    Debug.Log("UploadFilesAsync-start");
                    StartThreadData(tokenSource);
                    await conn.UploadFilesAsync(sourceDatas, @savedpath, FtpExists.Overwrite, true, FtpVerify.Delete | FtpVerify.Throw, FtpError.Throw,cancelToken);
                    EndThread();
                    Debug.Log("UploadFilesAsync-end");

                    return true;
                }
                else
                {
                    Debug.Log("NetworkReachability.NotReachable");

                    if (conn != null)
                    {
                        conn.Dispose();
                    }
                    return false;
                }
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (FluentFTP.FtpException e) //UploadFilesAsync
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (Exception e)
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
        }


        /// <summary>
        /// ファイルのダウンロード（シングル）（自動接続/切断）
        /// 保存先は1つのディレクトリを指定
        /// </summary>
        /// <param name="savedpath"> //保存先のディレクトリ
        /// <param name="sourceDatas"> //ダウンロードするファイルのパス(FTPサーバー)を指定（複数対応）
        /// <param name="progress"> //ファイルの進捗状況(0-100転送された割合、-1は不明)
        /// </param>
        public static async Task<bool> SingleDownloadFileAsync(string savedpath, string sourceDatas, Progress<double> progress)
        {
            try
            {
                using (conn = new FtpClient())
                {
                    conn.Host = FTP_IP;
                    //conn.DataConnectionType = FtpDataConnectionType.PASV;
                    conn.ConnectTimeout = FTP_TIMEOUT;
                    conn.ReadTimeout = FTP_TIMEOUT;
                    conn.DataConnectionConnectTimeout = FTP_TIMEOUT;
                    conn.DataConnectionReadTimeout = FTP_TIMEOUT;
                    conn.Credentials = new NetworkCredential(FTP_USERNAME, FTP_PASSWORD);

                    if ((UnityEngine.Application.internetReachability != UnityEngine.NetworkReachability.NotReachable))
                    {
                        Debug.Log("ConnectAsync-start");
                        StartThreadConnect();
                        await conn.ConnectAsync();
                        EndThread();
                        Debug.Log("ConnectAsync-end");

                        tokenSource = new CancellationTokenSource();
                        var cancelToken = tokenSource.Token;

                        //エラー時でも途中のファイルは作成されるので注意
                        //trueでファイル上書き設定
                        //保存先のディレクトリフォルダがなくても通常は作成される
                        Debug.Log("DownloadFileAsync-start");
                        StartThreadData(tokenSource);
                        await conn.DownloadFileAsync(@savedpath, sourceDatas, true, FtpVerify.Delete | FtpVerify.Throw,cancelToken, progress);
                        EndThread();
                        Debug.Log("DownloadFileAsync-end");
                    }
                    else
                    {
                        throw new Exception("NetworkReachability.NotReachable");
                    }
                }
                return true;
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                Debug.LogError(e.Message);
                EndThread();
                return false;
            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                Debug.LogError(e.Message);
                EndThread();
                return false;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
                EndThread();
                return false;
            }
            catch (FluentFTP.FtpException e) //DownloadFilesAsync
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (Exception e) //SocketException: Connection reset by peer
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
        }

        /// <summary>
        /// ファイルのダウンロード（シングル）（手動接続/切断）
        /// 保存先は1つのディレクトリを指定
        /// </summary>
        /// <param name="savedpath"> //保存先のディレクトリ
        /// <param name="sourceDatas"> //ダウンロードするファイルのパス(FTPサーバー)を指定（複数対応）
        /// <param name="progress"> //ファイルの進捗状況(0-100転送された割合、-1は不明)
        /// </param>
        public static async Task<bool> ManualSingleDownloadFileAsync(string savedpath, string sourceDatas, Progress<double> progress)
        {
            try
            {
                if ((conn != null) && ((UnityEngine.Application.internetReachability != UnityEngine.NetworkReachability.NotReachable)))
                {
                    tokenSource = new CancellationTokenSource();
                    var cancelToken = tokenSource.Token;

                    //エラー時でも途中のファイルは作成されるので注意
                    //trueでファイル上書き設定
                    //保存先のディレクトリフォルダがなくても通常は作成される
                    Debug.Log("DownloadFileAsync-start");
                    StartThreadData(tokenSource);
                    await conn.DownloadFileAsync(@savedpath, sourceDatas, true, FtpVerify.Delete | FtpVerify.Throw,cancelToken, progress);
                    EndThread();
                    Debug.Log("DownloadFileAsync-end");

                    return true;

                }
                else
                {
                    Debug.Log("NetworkReachability.NotReachable");

                    if (conn != null)
                    {
                        conn.Dispose();
                    }
                    return false;
                }
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (FluentFTP.FtpException e) //DownloadFilesAsync
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (Exception e) //SocketException: Connection reset by peer
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
        }

        /// <summary>
        /// ファイルのダウンロード（複数）（自動接続/切断）
        /// 保存先は1つのディレクトリを指定
        /// </summary>
        /// <param name="savedpath"> //保存先のディレクトリ
        /// <param name="sourceDatas"> //ダウンロードするファイルのパス(FTPサーバー)を指定（複数対応）
        /// </param>
        public static async Task<bool> MulitipleDownloadFileAsync(string savedpath, IEnumerable<string> sourceDatas)
        {
            try
            {
                using (conn = new FtpClient())
                {
                    conn.Host = FTP_IP;
                    //conn.DataConnectionType = FtpDataConnectionType.PASV;
                    conn.ConnectTimeout = FTP_TIMEOUT;
                    conn.ReadTimeout = FTP_TIMEOUT;
                    conn.DataConnectionConnectTimeout = FTP_TIMEOUT;
                    conn.DataConnectionReadTimeout = FTP_TIMEOUT;
                    conn.Credentials = new NetworkCredential(FTP_USERNAME, FTP_PASSWORD);

                    if ((UnityEngine.Application.internetReachability != UnityEngine.NetworkReachability.NotReachable))
                    {
                        Debug.Log("ConnectAsync-start");
                        StartThreadConnect();
                        await conn.ConnectAsync();
                        EndThread();
                        Debug.Log("ConnectAsync-end");

                        tokenSource = new CancellationTokenSource();
                        var cancelToken = tokenSource.Token;

                        //エラー時でも途中のファイルは作成されるので注意
                        //trueでファイル上書き設定
                        //保存先のディレクトリフォルダがなくても通常は作成される
                        Debug.Log("DownloadFilesAsync-start");
                        StartThreadData(tokenSource);
                        await conn.DownloadFilesAsync(@savedpath, sourceDatas, true, FtpVerify.Delete | FtpVerify.Throw, FtpError.Throw, cancelToken);
                        EndThread();
                        Debug.Log("DownloadFilesAsync-end");
                    }
                    else
                    {
                        throw new Exception("NetworkReachability.NotReachable");
                    }
                }
                return true;
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                Debug.LogError(e.Message);
                EndThread();
                return false;

            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                Debug.LogError(e.Message);
                EndThread();
                return false;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
                EndThread();
                return false;
            }
            catch (FluentFTP.FtpException e) //DownloadFilesAsync
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (Exception e) //SocketException: Connection reset by peer
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
        }

        /// <summary>
        /// ファイルのダウンロード（複数）（手動接続/切断）
        /// 保存先は1つのディレクトリを指定
        /// </summary>
        /// <param name="savedpath"> //保存先のディレクトリ
        /// <param name="sourceDatas"> //ダウンロードするファイルのパス(FTPサーバー)を指定（複数対応）
        /// </param>
        public static async Task<bool> ManualMulitipleDownloadFileAsync(string savedpath, IEnumerable<string> sourceDatas)
        {
            try
            {
                if( (conn != null)&&((UnityEngine.Application.internetReachability != UnityEngine.NetworkReachability.NotReachable)))
                {
                    tokenSource = new CancellationTokenSource();
                    var cancelToken = tokenSource.Token;

                    //エラー時でも途中のファイルは作成されるので注意
                    //trueでファイル上書き設定
                    //保存先のディレクトリフォルダがなくても通常は作成される
                    Debug.Log("DownloadFilesAsync-start");
                    StartThreadData(tokenSource);
                    await conn.DownloadFilesAsync(@savedpath, sourceDatas, true, FtpVerify.Delete | FtpVerify.Throw, FtpError.Throw, cancelToken);
                    EndThread();
                    Debug.Log("DownloadFilesAsync-end");

                    return true;
                }
                else
                {
                    Debug.Log("NetworkReachability.NotReachable");

                    if (conn != null)
                    {
                        conn.Dispose();
                    }
                    return false;
                }
            }
            catch (FtpCommandException e) //User cannot log in.
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (TimeoutException e) //Timed out trying to connect!
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (FluentFTP.FtpException e) //DownloadFilesAsync
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
            catch (Exception e) //SocketException: Connection reset by peer
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
                Debug.Log(e.Message);
                EndThread();
                return false;
            }
        }


        /// <summary>
        /// ファイルの存在チェック（自動接続/切断）
        /// </summary>
        /// <param name="path"> //チェックするファイルのパス
        /// <param name="callback"> //完了時コールバック //OK：存在する、NG：存在しない、ERROR：エラー
        /// </param>
        public static void FileExists(string path, Action<int> callback = null)
        {
            _cbCheck = callback;

            try
            {
                conn = new FtpClient();
                conn.Host = FTP_IP;
                //conn.DataConnectionType = FtpDataConnectionType.PASV;
                conn.ConnectTimeout = FTP_TIMEOUT;
                conn.ReadTimeout = FTP_TIMEOUT;
                conn.DataConnectionConnectTimeout = FTP_TIMEOUT;
                conn.DataConnectionReadTimeout = FTP_TIMEOUT;
                conn.Credentials = new NetworkCredential(FTP_USERNAME, FTP_PASSWORD);
                conn.BeginFileExists(path, new AsyncCallback(BeginFileExistsCallback), conn);

            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                conn.Dispose(); //自動接続/切断

                if (_cbCheck != null) //結果を返却
                {
                    Action<int> _callback = _cbCheck;
                    _cbCheck = null;
                    _callback(ERROR); //OK：存在する、NG：存在しない、ERROR：エラー
                }
            }
            catch (Exception e) //SocketException: Connection reset by peer
            {
                conn.Dispose(); //自動接続/切断

                if (_cbCheck != null) //結果を返却
                {
                    Action<int> _callback = _cbCheck;
                    _cbCheck = null;
                    _callback(ERROR); //OK：存在する、NG：存在しない、ERROR：エラー
                }
            }
        }

        static void BeginFileExistsCallback(IAsyncResult ar)
        {
            conn = ar.AsyncState as FtpClient;
            int _result = ERROR;

            try
            {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                bool _exits = conn.EndFileExists(ar);
                Debug.Log(_exits);
                _result = Convert.ToInt32(_exits);
                Debug.Log(_result);
            }
            catch (FtpCommandException e) //User cannot log in. //※ IsConnected:TRUE
            {
                Debug.LogError(e.Message);
            }
            catch (TimeoutException e) //Timed out trying to connect! //※ IsConnected:FALSE
            {
                Debug.LogError(e.Message);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e.Message);
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                conn.Dispose(); //自動接続/切断

                if (_cbCheck != null) //結果を返却
                {
                    Action<int> _callback = _cbCheck;
                    _cbCheck = null;
                    _callback(_result); //OK：存在する、NG：存在しない、ERROR：エラー
                }
            }
        }

        /// <summary>
        /// ディレクトリの存在チェック（自動接続/切断）
        /// </summary>
        /// <param name="path"> //チェックするディレクトリのパス
        /// <param name="callback"> //完了時コールバック //OK：存在する、NG：存在しない、ERROR：エラー
        /// </param>
        public static void DirectoryExists(string path, Action<int> callback = null)
        {
            _cbCheck = callback;

            try
            {
                conn = new FtpClient();
                conn.Host = FTP_IP;
                //conn.DataConnectionType = FtpDataConnectionType.PASV;
                conn.ConnectTimeout = FTP_TIMEOUT;
                conn.ReadTimeout = FTP_TIMEOUT;
                conn.DataConnectionConnectTimeout = FTP_TIMEOUT;
                conn.DataConnectionReadTimeout = FTP_TIMEOUT;
                conn.Credentials = new NetworkCredential(FTP_USERNAME, FTP_PASSWORD);
                conn.BeginDirectoryExists(path, new AsyncCallback(DirectoryExistsCallback), conn);

            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                conn.Dispose(); //自動接続/切断

                if (_cbCheck != null) //結果を返却
                {
                    Action<int> _callback = _cbCheck;
                    _cbCheck = null;
                    _callback(ERROR); //OK：存在する、NG：存在しない、ERROR：エラー
                }
            }
            catch (Exception e) //SocketException: Connection reset by peer
            {
                conn.Dispose(); //自動接続/切断

                if (_cbCheck != null) //結果を返却
                {
                    Action<int> _callback = _cbCheck;
                    _cbCheck = null;
                    _callback(ERROR); //OK：存在する、NG：存在しない、ERROR：エラー
                }
            }
        }

        static void DirectoryExistsCallback(IAsyncResult ar)
        {
            conn = ar.AsyncState as FtpClient;
            int _result = ERROR;

            try
            {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                bool _exits = conn.EndDirectoryExists(ar);
                Debug.Log(_exits);
                _result = Convert.ToInt32(_exits);
                Debug.Log(_result);
            }
            catch (FtpCommandException e) //User cannot log in. //※ IsConnected:TRUE
            {
                Debug.LogError(e.Message);
            }
            catch (TimeoutException e) //Timed out trying to connect! //※ IsConnected:FALSE
            {
                Debug.LogError(e.Message);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e.Message);
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                conn.Dispose(); //自動接続/切断

                if (_cbCheck != null) //結果を返却
                {
                    Action<int> _callback = _cbCheck;
                    _cbCheck = null;
                    _callback(_result); //OK：存在する、NG：存在しない、ERROR：エラー
                }
            }
        }

        /// <summary>
        /// リネーム処理（自動接続/切断）
        /// </summary>
        /// <param name="before"> //変更前のアドレス
        /// <param name="after"> //変更後のアドレス
        /// <param name="callback"> //完了時コールバック（TRUE：成功、FALSE：失敗)
        /// </param>
        public static void Rename(string before, string after, Action<bool> callback = null)
        {
            _cbConnect = callback;

            try
            {
                conn = new FtpClient();
                conn.Host = FTP_IP;
                //conn.DataConnectionType = FtpDataConnectionType.PASV;
                conn.ConnectTimeout = FTP_TIMEOUT;
                conn.ReadTimeout = FTP_TIMEOUT;
                conn.DataConnectionConnectTimeout = FTP_TIMEOUT;
                conn.DataConnectionReadTimeout = FTP_TIMEOUT;
                conn.Credentials = new NetworkCredential(FTP_USERNAME, FTP_PASSWORD);
                conn.BeginRename(before, after,
                    new AsyncCallback(BeginRenameCallback), conn);

            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                conn.Dispose(); //自動接続/切断

                if (_cbConnect != null) //結果を返却
                {
                    Action<bool> _callback = _cbConnect;
                    _cbConnect = null;
                    _callback(false); //TRUE：成功、FALSE：失敗
                }
            }
            catch (Exception e) //SocketException: Connection reset by peer
            {
                conn.Dispose(); //自動接続/切断

                if (_cbConnect != null) //結果を返却
                {
                    Action<bool> _callback = _cbConnect;
                    _cbConnect = null;
                    _callback(false); //TRUE：成功、FALSE：失敗
                }
            }
        }

        static void BeginRenameCallback(IAsyncResult ar)
        {
            conn = ar.AsyncState as FtpClient;
            bool _result = false;

            try
            {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                conn.EndRename(ar);
                _result = true;
            }
            catch (FtpCommandException e) //User cannot log in. //※ IsConnected:TRUE
            {
                Debug.LogError(e.Message);
            }
            catch (TimeoutException e) //Timed out trying to connect! //※ IsConnected:FALSE
            {
                Debug.LogError(e.Message);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e.Message);
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                conn.Dispose(); //自動接続/切断

                if (_cbConnect != null) //結果を返却
                {
                    Action<bool> _callback = _cbConnect;
                    _cbConnect = null;
                    _callback(_result); //TRUE：成功、FALSE：失敗
                }
            }
        }

        /// <summary>
        /// リストを取得（自動接続/切断）
        /// </summary>
        /// <param name="path"> //取得したいフォルダのパス
        /// <param name="callback"> //完了時コールバック Action<bool, List<List<string>>>：結果(TRUE：成功、FALSE：失敗), タイプ(0:ファイル, 1:ディレクトリ, 2：Link), ファイル名, フルパス, 更新日時
        /// </param>
        public static void GetListing(string path, Action<bool, List<List<string>>> callback = null)
        {
            _cbList = callback;

            try
            {
                conn = new FtpClient();
                conn.Host = FTP_IP;
                //conn.DataConnectionType = FtpDataConnectionType.PASV;
                conn.ConnectTimeout = FTP_TIMEOUT;
                conn.ReadTimeout = FTP_TIMEOUT;
                conn.DataConnectionConnectTimeout = FTP_TIMEOUT;
                conn.DataConnectionReadTimeout = FTP_TIMEOUT;
                conn.Credentials = new NetworkCredential(FTP_USERNAME, FTP_PASSWORD);

                if (_filelist != null)
                {
                    _filelist.Clear();
                }
                else
                {
                    _filelist = new List<List<string>>();
                    _filelist.Clear();
                }
                conn.BeginGetListing(path, FtpListOption.Auto, new AsyncCallback(GetListingCallback), conn);

                //conn.BeginGetListing(path, FtpListOption.Recursive, new AsyncCallback(GetListingCallback), conn);
            }
            catch (FtpCommandException e) //The system cannot find the path specified ※指定したパスが存在しない場合
            {
                Debug.LogError(e.Message);

                conn.Dispose(); //自動接続/切断

                if (_cbList != null) //結果を返却
                {
                    Action<bool, List<List<string>>> _callback = _cbList;
                    _cbList = null;
                    _callback(false, null); //TRUE：成功、FALSE：失敗
                }
            }
            catch (Exception e) //SocketException: Connection reset by peer
            {
                conn.Dispose(); //自動接続/切断

                if (_cbList != null) //結果を返却
                {
                    Action<bool, List<List<string>>> _callback = _cbList;
                    _cbList = null;
                    _callback(false, null); //TRUE：成功、FALSE：失敗
                }
            }
        }

        static void GetListingCallback(IAsyncResult ar)
        {
            conn = ar.AsyncState as FtpClient;
            FtpFileSystemObjectType type;
            DateTime time;
            string str;
            bool _result = false;

            try
            {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                foreach (FtpListItem item in conn.EndGetListing(ar))
                {

                    switch (item.Type)
                    {
                        case FtpFileSystemObjectType.Directory:
                            // get modified date/time of the file or folder
                            time = conn.GetModifiedTime(item.FullName);
                            type = item.Type;
                            str = ((int)type).ToString();

                            Debug.Log("Type: " + item.Type);
                            Debug.Log("GetModifiedTime: " + conn.GetModifiedTime(item.FullName));
                            Debug.Log("FileName: " + item.Name);
                            Debug.Log("FullName: " + item.FullName);

                            //タイプ(0:ファイル, 1:ディレクトリ, 2：Link), ファイル名, フルパス, 更新日時
                            _filelist.Add(new List<string>(new string[] { str, item.Name, item.FullName, time.ToString() }));
                            break;

                        case FtpFileSystemObjectType.File:

                            // get modified date/time of the file or folder
                            time = conn.GetModifiedTime(item.FullName);
                            type = item.Type;
                            str = ((int)type).ToString();

                            Debug.Log("Type: " + item.Type);
                            Debug.Log("GetModifiedTime: " + conn.GetModifiedTime(item.FullName));
                            Debug.Log("FileName: " + item.Name);
                            Debug.Log("FullName: " + item.FullName);

                            //タイプ(0:ファイル, 1:ディレクトリ, 2：Link), ファイル名, フルパス, 更新日時
                            _filelist.Add(new List<string>(new string[] { str, item.Name, item.FullName, time.ToString() }));
                            break;
                        default:
                            break;
                    }
                }
                _result = true;

            }
            catch (FtpCommandException e) //User cannot log in. //※ IsConnected:TRUE
            {
                Debug.LogError(e.Message);
            }
            catch (TimeoutException e) //Timed out trying to connect! //※ IsConnected:FALSE
            {
                Debug.LogError(e.Message);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e.Message);
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
            }
            catch (Exception e) //
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                conn.Dispose(); //自動接続/切断

                if (_cbList != null) //結果を返却
                {
                    Action<bool, List<List<string>>> _callback = _cbList;
                    _cbList = null;
                    _callback(_result, _filelist); //TRUE：成功、FALSE：失敗
                }
            }
        }

        /// <summary>
        /// FTPサーバへの接続（手動で接続切断を行う時に使用）
        /// </summary>
        /// <param name="callback"> //完了時コールバック（TRUE：成功、FALSE：失敗)
        /// </param>
        public static void Connection(Action<bool> callback = null)
        {
            _cbConnect = callback;

            conn = new FtpClient();

            conn.Host = FTP_IP;
            //conn.DataConnectionType = FtpDataConnectionType.PASV;
            conn.ConnectTimeout = FTP_TIMEOUT;
            conn.Credentials = new NetworkCredential(FTP_USERNAME, FTP_PASSWORD);
            conn.BeginConnect(new AsyncCallback(ConnectCallback), conn);
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            conn = ar.AsyncState as FtpClient;
            bool _result = false;

            try
            {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                conn.EndConnect(ar);
                _result = true;
            }
            catch (FtpCommandException e) //User cannot log in. //※ IsConnected:TRUE
            {
                Debug.LogError(e.Message);
            }
            catch (TimeoutException e) //Timed out trying to connect! //※ IsConnected:FALSE
            {
                Debug.LogError(e.Message);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e.Message);
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
            }
            catch (Exception e) //
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                if (_result == false) //callbackより前にする
                {
                    conn.Dispose(); //失敗時はDispose
                }
                if (_cbConnect != null) //結果を返却
                {
                    Action<bool> _callback = _cbConnect;
                    _cbConnect = null;
                    _callback(_result); //TRUE：成功、FALSE：失敗
                }

            }
        }

        /// <summary>
        /// FTPサーバの接続を切断（手動で接続切断を行う時に使用）
        /// </summary>
        /// <param name="callback"> //完了時コールバック（TRUE：成功、FALSE：失敗)
        /// </param>
        /// 
        public static void DisConnect(Action<bool> callback = null)
        {
            _cbConnect = callback;

            if (conn != null)
            {
                conn.BeginDisconnect(new AsyncCallback(BeginDisconnectCallback), conn);
            }
            else
            {
                if (_cbConnect != null) //結果を返却
                {
                    _cbConnect(false);
                }
            }
        }

        static void BeginDisconnectCallback(IAsyncResult ar)
        {
            bool _result = false;

            conn = ar.AsyncState as FtpClient;

            try
            {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                conn.EndDisconnect(ar);
                _result = true;
            }
            catch (InvalidOperationException e) //Disconnectではここにはこない
            {
                Debug.LogError(e.Message);
            }
            catch (TimeoutException e) //Timed out trying to read data from the socket stream!
            {
                Console.Write(conn.IsConnected);
                Debug.LogError(e.Message);
            }
            catch (Exception ex) //基本ない
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            { //※ IsConnected:FALSE
                Console.Write(conn.IsConnected);

                if (_result == false) //callbackより前にする
                {
                    if (conn != null)
                    {
                        conn.Dispose(); //失敗時はDispose
                    }
                }
                if (_cbConnect != null) //結果を返却
                {
                    Action<bool> _callback = _cbConnect;
                    _cbConnect = null;
                    _callback(_result); //TRUE：成功、FALSE：失敗
                }
            }
        }

        /// <summary>
        /// ディレクトリの存在チェック（手動）
        /// </summary>
        /// <param name="path"> //チェックするディレクトリのパス
        /// <param name="callback"> //完了時コールバック //OK：存在する、NG：存在しない、ERROR：エラー
        /// </param>
        public static void ManualDirectoryExists(string path, Action<int> callback = null)
        {
            _cbCheck = callback;

            if (conn != null)
            {
                conn.BeginDirectoryExists(path, new AsyncCallback(ManualDirectoryExistsCallback), conn);
            }
            else
            {
                conn.Dispose(); //自動接続/切断
                if (_cbCheck != null) //結果を返却
                {
                    _cbCheck(ERROR); //OK：存在する、NG：存在しない、ERROR：エラー
                }

            }
        }

        static void ManualDirectoryExistsCallback(IAsyncResult ar)
        {
            conn = ar.AsyncState as FtpClient;
            int _result = ERROR;

            try
            {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                bool _exits = conn.EndDirectoryExists(ar);
                Debug.Log(_exits);
                _result = Convert.ToInt32(_exits);
                Debug.Log(_result);
            }
            catch (FtpCommandException e) //User cannot log in. //※ IsConnected:TRUE
            {
                Debug.LogError(e.Message);
            }
            catch (TimeoutException e) //Timed out trying to connect! //※ IsConnected:FALSE
            {
                Debug.LogError(e.Message);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e.Message);
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                if (_cbCheck != null) //結果を返却
                {
                    Action<int> _callback = _cbCheck;
                    _cbCheck = null;
                    _callback(_result); //OK：存在する、NG：存在しない、ERROR：エラー
                }
            }
        }


        /// <summary>
        /// リネーム処理（手動）
        /// </summary>
        /// <param name="before"> //変更前のアドレス
        /// <param name="after"> //変更後のアドレス
        /// <param name="callback"> //完了時コールバック（TRUE：成功、FALSE：失敗)
        /// </param>
        public static void ManualRename(string before, string after, Action<bool> callback = null)
        {
            _cbConnect = callback;

            if (conn != null)
            {
                conn.BeginRename(before, after,
                    new AsyncCallback(ManualBeginRenameCallback), conn);
            }
            else
            {
                conn.Dispose(); //自動接続/切断
                if (_cbConnect != null) //結果を返却
                {
                    Action<bool> _callback = _cbConnect;
                    _cbConnect = null;
                    _callback(false); //TRUE：成功、FALSE：失敗
                }

            }
        }

        static void ManualBeginRenameCallback(IAsyncResult ar)
        {
            conn = ar.AsyncState as FtpClient;
            bool _result = false;

            try
            {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                conn.EndRename(ar);
                _result = true;
            }
            catch (FtpCommandException e) //User cannot log in. //※ IsConnected:TRUE
            {
                Debug.LogError(e.Message);
            }
            catch (TimeoutException e) //Timed out trying to connect! //※ IsConnected:FALSE
            {
                Debug.LogError(e.Message);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e.Message);
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                if (_cbConnect != null) //結果を返却
                {
                    Action<bool> _callback = _cbConnect;
                    _cbConnect = null;
                    _callback(_result); //TRUE：成功、FALSE：失敗
                }
            }
        }

        /// <summary>
        /// リストを取得（手動）
        /// </summary>
        /// <param name="path"> //取得したいフォルダのパス
        /// <param name="callback"> //完了時コールバック Action<bool, List<List<string>>>：結果(TRUE：成功、FALSE：失敗), タイプ(0:ファイル, 1:ディレクトリ, 2：Link), ファイル名, フルパス, 更新日時
        /// </param>
        public static void ManualGetListing(string path, Action<bool, List<List<string>>> callback = null)
        {
            _cbList = callback;

            try
            {
                if (_filelist != null)
                {
                    _filelist.Clear();
                }
                else
                {
                    _filelist = new List<List<string>>();
                    _filelist.Clear();
                }

                if (conn != null)
                {
                    conn.BeginGetListing(path, FtpListOption.Auto, new AsyncCallback(ManualGetListingCallback), conn);
                }
                else
                {
                    conn.Dispose(); //自動接続/切断
                    if (_cbList != null) //結果を返却
                    {
                        Action<bool, List<List<string>>> _callback = _cbList;
                        _cbList = null;
                        _callback(false, null); //TRUE：成功、FALSE：失敗
                    }

                }
            }
            catch (FtpCommandException e) //The system cannot find the path specified ※指定したパスが存在しない場合
            {
                Debug.LogError(e.Message);

                conn.Dispose(); //自動接続/切断

                if (_cbList != null) //結果を返却
                {
                    Action<bool, List<List<string>>> _callback = _cbList;
                    _cbList = null;
                    _callback(false, null); //TRUE：成功、FALSE：失敗
                }
            }
            catch (Exception e) //
            {
                Debug.LogError(e.Message);

                conn.Dispose(); //自動接続/切断

                if (_cbList != null) //結果を返却
                {
                    Action<bool, List<List<string>>> _callback = _cbList;
                    _cbList = null;
                    _callback(false, null); //TRUE：成功、FALSE：失敗
                }
            }
        }

        static void ManualGetListingCallback(IAsyncResult ar)
        {
            conn = ar.AsyncState as FtpClient;
            FtpFileSystemObjectType type;
            DateTime time;
            string str;
            bool _result = false;

            try
            {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                foreach (FtpListItem item in conn.EndGetListing(ar))
                {

                    switch (item.Type)
                    {
                        case FtpFileSystemObjectType.Directory:
                            // get modified date/time of the file or folder
                            time = conn.GetModifiedTime(item.FullName);
                            type = item.Type;
                            str = ((int)type).ToString();

                            Debug.Log("Type: " + item.Type);
                            Debug.Log("GetModifiedTime: " + conn.GetModifiedTime(item.FullName));
                            Debug.Log("FileName: " + item.Name);
                            Debug.Log("FullName: " + item.FullName);

                            //タイプ(0:ファイル, 1:ディレクトリ, 2：Link), ファイル名, フルパス, 更新日時
                            _filelist.Add(new List<string>(new string[] { str, item.Name, item.FullName, time.ToString() }));
                            break;

                        case FtpFileSystemObjectType.File:

                            // get modified date/time of the file or folder
                            time = conn.GetModifiedTime(item.FullName);
                            type = item.Type;
                            str = ((int)type).ToString();

                            Debug.Log("Type: " + item.Type);
                            Debug.Log("GetModifiedTime: " + conn.GetModifiedTime(item.FullName));
                            Debug.Log("FileName: " + item.Name);
                            Debug.Log("FullName: " + item.FullName);

                            //タイプ(0:ファイル, 1:ディレクトリ, 2：Link), ファイル名, フルパス, 更新日時
                            _filelist.Add(new List<string>(new string[] { str, item.Name, item.FullName, time.ToString() }));
                            break;
                        default:
                            break;
                    }
                }
                _result = true;

            }
            catch (FtpCommandException e) //User cannot log in. //※ IsConnected:TRUE
            {
                Debug.LogError(e.Message);
            }
            catch (TimeoutException e) //Timed out trying to connect! //※ IsConnected:FALSE
            {
                Debug.LogError(e.Message);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e.Message);
            }
            catch (SocketException e) //到達できないホストに対してソケット操作を実行しようとしました。
            {
                Debug.LogError(e.Message);
            }
            catch (Exception e) //
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                if (_cbList != null) //結果を返却
                {
                    Action<bool, List<List<string>>> _callback = _cbList;
                    _cbList = null;
                    _callback(_result, _filelist); //TRUE：成功、FALSE：失敗
                }
            }
        }

        //スレッド開始(アップロード/ダウンロード用)
        public static void StartThreadData(CancellationTokenSource token)
        {
            //Debug.Log("StartThreadData");

            if (thread == null)
            {
                thread = new Thread(new ParameterizedThreadStart(threadMainData));
                running = true;//これ重要
                thread.Start(token);
            }
        }

        //スレッド開始(接続用)
        public static void StartThreadConnect()
        {
            //Debug.Log("StartThreadConnect");

            if (thread == null)
            {
                thread = new Thread(new ThreadStart(threadMainConnect));
                running = true;//これ重要
                thread.Start();
            }
        }

        //スレッド終了(共通)
        public static void EndThread()
        {
            //Debug.Log("EndThread");

            if (thread != null)
            {
                running = false;//終了要求
                thread.Join();//実際に終了するのを待つ
                thread = null;
            }
        }

        //スレッドメイン(アップロード/ダウンロード用)
        private static void threadMainData(object token)
        {
            CancellationTokenSource _token = (CancellationTokenSource)token;
            Debug.Log(_token.ToString());

            while (running)
            {
                //スレッドの処理
                if (!conn.IsConnected) //FTPサーバーの接続切れた場合
                {
                    //Debug.Log("conn.IsConnected=false");
                    _token.Cancel();
                    //conn.Dispose();
                    break;
                }

                if (UnityEngine.Application.internetReachability == UnityEngine.NetworkReachability.NotReachable)
                { //ダウンロード中にネットワークOFFとなった場合を想定
                    {
                        // 電波無し
                        //Debug.Log("threadMainData-Cancel");
                        //Debug.Log("threadMainData:connected" + conn.IsConnected.ToString());
                        //Debug.Log("threadMainData:disposed" + conn.IsDisposed.ToString());
                        _token.Cancel();
                        conn.Dispose();
                        break;
                    }
                }
                //Debug.Log("threadMainData");
                //Debug.Log("threadMainData:connected" + conn.IsConnected.ToString());
                //Debug.Log("threadMainData:disposed" + conn.IsDisposed.ToString());
                Thread.Sleep(500);
            }
        }

        //スレッドメイン(接続用)
        private static void threadMainConnect()
        {
            //Debug.Log("threadMainConnect");

            while (running)
            {

                if (UnityEngine.Application.internetReachability == UnityEngine.NetworkReachability.NotReachable)
                {
                    //Debug.Log("threadMainConnect-NotReachable");
                    //Debug.Log("threadMainConnect:connected=" + conn.IsConnected.ToString());
                    ///Debug.Log("threathreadMainConnectdMain:disposed=" + conn.IsDisposed.ToString());
                    conn.Dispose();
                    break;
                }
                else
                {
                    //Debug.Log("threadMainConnect");
                    //Debug.Log("threadMainConnect:connected=" + conn.IsConnected.ToString());
                    //Debug.Log("threadMainConnect:disposed=" + conn.IsDisposed.ToString());
                    Thread.Sleep(500);
                }
            }
        }

    }

}
