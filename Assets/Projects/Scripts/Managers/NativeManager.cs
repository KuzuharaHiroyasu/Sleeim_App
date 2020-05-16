using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using Kaimin.Common;
using MiniJSON;
using System.Linq;
using System.Globalization;
using DG.Tweening.Core;

namespace Kaimin.Managers
{
    /// <summary>
    /// ネイティブ機能呼び出しクラス
    /// </summary>
    public class NativeManager : SingletonMonoBehaviour<NativeManager>
    {
        private const string _packageNameMainActivity = "jp.co.onea.sleeim.unityandroidplugin.main.MainActivity";
        private const string _packageNameMyApplication = "jp.co.onea.sleeim.unityandroidplugin.main.MyApplication";
        private const string _packageNameMusicPlayer = "jp.co.onea.sleeim.unityandroidplugin.main.MusicPlayer";
        private const string _packageNameBluetoothActivity = "jp.co.onea.sleeim.unityandroidplugin.main.BluetoothActivity";
		private const string _packageNameNotificationActivity = "jp.co.onea.sleeim.unityandroidplugin.main.NotificationActivity";
        private readonly string _gameObjectName = "Kaimin.Managers.NativeManager (singleton)";

        /// <summary>権限コード(iOSネイティブより取得する)</summary>
        public int PermissionCode { get; private set; }
		/// <summary>通知の許可を求めるメソッドのリザルト取得用</value>
		public int NotificationRequestResultCode { get; private set; }

        /// <summary>
        /// 初期化(パーミッションチェックで使用)
        /// </summary>
        public void Initialize()
        {
            // 初期値設定
            PermissionCode = -1;
        }

        /// <summary>
        /// 権限チェック処理
        /// </summary>
        public void CheckFuncPermission()
        {
#if UNITY_EDITOR
            PermissionCode = 0;
#elif UNITY_ANDROID && !UNITY_EDITOR
                string cbMethodName = ((Action<string>)OnFinishedCheckFuncPermission).Method.Name;
                using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMainActivity))
                {
                    ajo.CallStatic("PermissionCheck", _gameObjectName, cbMethodName);
                }
#elif UNITY_IOS && !UNITY_EDITOR
			//iOSに関しては初期化時に行うパーミッションチェックは不要
#endif
        }

        /// <summary>
		/// CheckPermissionのコールバック用
        /// </summary>
        /// <param name="code">権限情報</param>
        private void OnFinishedCheckFuncPermission(string code)
        {
            // 権限コードをメンバー保持する
            PermissionCode = int.Parse(code);
        }

#if UNITY_IOS
        [DllImport ("__Internal")]
		private static extern void _openBLESetting ();
#endif
        /// <summary>
        /// Bluetooth有効化リクエスト発行
        /// </summary>
        public void BluetoothRequest()
        {
#if UNITY_ANDROID
            string cbMethodName = ((Action<string>)OnFinishedResultBtRequest).Method.Name;
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameBluetoothActivity))
            {
                ajo.CallStatic("BluetoothRequest", _gameObjectName, cbMethodName);
            }
#elif UNITY_IOS
			_openBLESetting ();
#endif
        }

        /// <summary>
        /// Bluetooth有効化リクエスト発行のコールバック用
        /// </summary>
        /// <param name="result">0:BY許可なし 1:BT許可</param>
        private void OnFinishedResultBtRequest(string result)
        {
            // BTリクエスト結果をメンバー保持する
            PermissionCode = Convert.ToInt32(Convert.ToBoolean(result));
        }

        /// <summary>
        /// 通知許可があるかどうか確認します。
        /// </summary>
        public bool NotificationCheck()
        {
#if UNITY_ANDROID
			using (AndroidJavaObject ajo = new AndroidJavaObject (_packageNameMyApplication)) {
				return ajo.CallStatic<Boolean> ("NotificationCheck");
			}
#elif UNITY_IOS
			return BluetoothManager.IsAllowNotification;
#else
            return false;
#endif
        }

#if UNITY_IOS
        [DllImport ("__Internal")]
		private static extern void _openLocalNotificationSetting ();
#endif
        /// <summary>
        /// 通知を許可してもらうために設定画面を開きます。
        /// 設定画面を閉じた際にコールバックが返ります。
        /// NotificationRequestResultCodeで結果を受け取ってください。
        /// 0:許可なし 1:許可
        /// </summary>
        public void NotificationRequest () {
			#if UNITY_ANDROID
			NativeManager.Instance.NotificationRequestResultCode = -1;	//初期化
			string cbMethodName = ((Action<string>)OnFinishedResultNfRequest).Method.Name;
			using (AndroidJavaObject ajo = new AndroidJavaObject (_packageNameNotificationActivity)) {
				ajo.CallStatic ("NotificationRequest", _gameObjectName, cbMethodName);
			}
			#elif UNITY_IOS
			_openLocalNotificationSetting ();
			#endif
		}
			
		/// <summary>
		/// 通知許可を求めるメソッドのおコールバック用
		/// </summary>
		/// <param name="result">0:許可なし 1:許可</param>
		void OnFinishedResultNfRequest (string result) {
			NotificationRequestResultCode = Convert.ToInt32 (Convert.ToBoolean (result));
		}
#if UNITY_IOS
        [DllImport ("__Internal")]
		private static extern void _stopAlarm ();
#endif
        /// <summary>
        /// アラームを停止させる
        /// </summary>
        public void StopAlerm () {
			#if UNITY_ANDROID
			using (AndroidJavaObject ajo = new AndroidJavaObject (_packageNameMyApplication)) {
				ajo.CallStatic ("AlarmStop");
			}
			#elif UNITY_IOS
			_stopAlarm ();
			#endif
		}

#if UNITY_IOS
        [DllImport("__Internal")]
		private static extern Boolean _checkBluetoothPoweredOn();
#endif
        /// <summary>
        /// Bluetooth有効/無効チェック
        /// </summary>
        /// <param name="result">FALSE:無効 TRUE:有効</param>
        public Boolean BluetoothValidCheck()
        {
            Boolean ret=false;
#if UNITY_ANDROID
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ret = ajo.CallStatic<Boolean>("BluetoothValidCheck");
            }
#elif UNITY_IOS
			ret = _checkBluetoothPoweredOn();
#endif
            return ret;
        }

#if UNITY_IOS
        [DllImport("__Internal")]
		private static extern Boolean _checkBluetoothSupported();
#endif
        /// <summary>
        /// BLEサポートチェック
        /// </summary>
        /// <param name="result">FALSE:無効 TRUE:有効</param>
        public Boolean BlesupportCheck()
        {
            Boolean ret = false;

#if UNITY_ANDROID
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMyApplication))
            {
                ret = ajo.CallStatic<Boolean>("BlesupportCheck");
            }
#elif UNITY_IOS
			ret = _checkBluetoothSupported();
#endif
            return ret;
        }


        /// <summary>
        /// 指定した音楽リソースを再生（ループ再生、アラーム(STREAM_ALARM)のボリュームで再生）
        /// </summary>
        /// <param name="path">リソースの保存先(Androidからアクセス可能な場所に限る）</param>
#if UNITY_ANDROID
        public void MusicPlay(string path)
        {
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMusicPlayer))
            {
                ajo.CallStatic("MusicPlay",path);
            }
        }
#endif

        /// <summary>
        /// 音楽の再生を停止
        /// </summary>
#if UNITY_ANDROID
        public void MusicStop()
        {
            using (AndroidJavaObject ajo = new AndroidJavaObject(_packageNameMusicPlayer))
            {
                ajo.CallStatic("MusicStop");
            }
        }
#endif

    }
}
