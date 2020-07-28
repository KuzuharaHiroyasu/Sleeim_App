using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Unityの標準のセーブ機能を使って、データを簡単に保存・取得するためのクラス
/// </summary>
public static class UserDataManager {

    /// <summary>
    /// デバイス起動状態保存・読込クラス
    /// </summary>
    public static class DeviceActivationStatus
    {
        /// <summary>
        /// データ保存先を示すキー
        /// </summary>
        private readonly static string Key = "DeviceActivationStatus";

        /// <summary>
        /// デバイス起動状態デフォルト値
        /// </summary>
        /// <returns></returns>
        private readonly static int defaultValue = (int)DeviceActivationStatusType.App;

        /// <summary>
        /// デバイス起動状態を保存する
        /// </summary>
        /// <param name="status">デバイス起動状態種別</param>
        public static void Save(DeviceActivationStatusType status)
        {
            PlayerPrefs.SetInt(Key, (int)status);
        }

        /// <summary>
        /// デバイス起動状態を保存する
        /// </summary>
        /// <returns></returns>
        public static DeviceActivationStatusType Load()
        {
            return (DeviceActivationStatusType)PlayerPrefs.GetInt(Key, defaultValue);
        }
    }

    public static class State {

        static string isAcceptTermOfUseKey = "KAIMIN_STATE_ISACCEPTTERMOFUSE";					//利用規約に同意しているかどうか
        static string isCompleteInitialSettingKey = "KAIMIN_STATE_ISCOMPLETEINITIALSETTING";	//初期設定を完了しているかどうか
        static string isLoadedHomeSceneKey = "KAIMIN_STATE_ISLOADEDHOMESCENE";					//ホーム画面を一度でもロードしたかどうかDoneD
        static string isDoneDevicePareingKey = "KAIMIN_STATE_ISDONEDEVICEPAREING";				//デバイスのペアリングをしているかどうか
        static string isConnectingDeviceKey = "KAIMIN_STATE_ISCONNECTINGDEVICE";				//デバイスと接続中かどうか
        static string recoveryInfoKey = "KAIMIN_STATE_RECOVERYINFO";							//復元情報
        static string dataReceptionTimeKey = "KAIMIN_STATE_DATARECEPTIONTIME";					//機器からデータを最後に受信した時刻
        static string isNessesaryRestoreKey = "KAIMIN_STATE_ISNESSESARYRESTORE";				//復元が必要かどうか
        static string restoreDataCountKey = "KAIMIN_STATE_RESTOREDATACOUNT";					//復元済みのデータ件数

        /// <summary>
        /// 初回起動かどうか
        /// </summary>
        public static bool isInitialLunch () {
            //ホーム画面を一度も表示してなければ初回起動
            return !isLoadedHomeScene ();
        }

        /// <summary>
        /// ホーム画面を一度でもロードしているかどうか
        /// </summary>
        public static bool isLoadedHomeScene () {
            //1ならロードした、それ以外ならロードしてないとみなす
            return PlayerPrefs.GetInt (isLoadedHomeSceneKey, 0) == 1;
        }

        /// <summary>
        /// ホーム画面をロードした事を記録する
        /// </summary>
        public static void SaveLoadHomeScene () {
            //1をロードしたとして記録する
            PlayerPrefs.SetInt (isLoadedHomeSceneKey, 1);
        }

        /// <summary>
        /// デバイスのペアリングをしたかどうか
        /// </summary>
        public static bool isDoneDevicePareing () {
            //1ならペアリングした、それ以外ならしてないとみなす
            return PlayerPrefs.GetInt (isDoneDevicePareingKey, 0) == 1;
        }

        /// <summary>
        /// デバイスのペアリングをした事を記録する
        /// </summary>
        public static void SaveDoneDevicePareing () {
            //1をペアリングしたとして記録する
            PlayerPrefs.SetInt (isDoneDevicePareingKey, 1);
        }

        /// <summary>
        /// デバイスとのペアリングを切ります
        /// </summary>
        public static void ClearDeviceParering () {
            //ペアリングをしてない事に設定
            PlayerPrefs.SetInt (isDoneDevicePareingKey, 0);
            //ペアリング中のアドレスを初期化
            Device.SavePareringDeviceAdress ("");
            //ペアリング中のデバイス名を初期化
            Device.SavePareringDeviceName ("Sleeim");
            //ペアリング中のデバイスの機器時刻を初期化
            Device.SavePareringDeviceTime (DateTime.MinValue);
            //ペアリング中のデバイスのUUIDを初期化
            Device.SavePareringDeviceUUID ("");
        }

        //※PlayerPrefsで永続的に保存する必要はないが、管理しやすいためここで持っておく。
        /// <summary>
        /// デバイスと接続中かどうか
        /// </summary>
        public static bool isConnectingDevice () {
            //1なら接続中、それ以外ならしてないとみなす
            return PlayerPrefs.GetInt (isConnectingDeviceKey, 0) == 1;
        }

        //※PlayerPrefsで永続的に保存する必要はないが、管理しやすいためここで持っておく。
        /// <summary>
        /// デバイスとの接続状況を記録します
        /// </summary>
        public static void SaveDeviceConnectState (bool isConnect) {
            //1を接続中として記録する
            PlayerPrefs.SetInt (isConnectingDeviceKey, isConnect ? 1 : 0);
        }

        /// <summary>
        /// ユーザが利用規約に同意しているかどうか
        /// </summary>
        public static bool isAcceptTermOfUse () {
            //1なら同意した、それ以外なら同意してないとみなす
            return PlayerPrefs.GetInt (isAcceptTermOfUseKey, 0) == 1;
        }
        /// <summary>
        /// ユーザが利用規約に同意したことを記録します
        /// </summary>
        public static void SaveAcceptTermOfUse () {
            //1を同意したとして記録する
            PlayerPrefs.SetInt (isAcceptTermOfUseKey, 1);
        }

        /// <summary>
        /// ユーザが初期設定を完了しているかどうか
        /// </summary>
        public static bool isCompleteInitialSetting () {
            //1なら完了した、それ以外なら完了していないとみなす
            return PlayerPrefs.GetInt (isCompleteInitialSettingKey, 0) == 1;
        }

        /// <summary>
        /// ユーザが初期設定を完了したことを記録します
        /// </summary>
        public static void SaveCompleteInitialSetting () {
            //1を完了したとして記録
            PlayerPrefs.SetInt (isCompleteInitialSettingKey, 1);
        }

        /// <summary>
        /// 復元情報
        /// </summary>
        public enum RecoveryInfo {
            None,		//復元を行わない
            Running,	//復元中
            Complete	//復元完了
        }

        /// <summary>
        /// 復元情報を取得します
        /// 復元を行わない/復元中/復元完了
        /// </summary>
        public static RecoveryInfo GetRecoveryInfo () {
            //保存された数値から、RecoveryInfoの項目に変換して返す
            return (RecoveryInfo) Enum.ToObject (typeof(RecoveryInfo), PlayerPrefs.GetInt (recoveryInfoKey, 0));
        }

        /// <summary>
        /// 復元情報を記録します
        /// </summary>
        public static void SaveRecoveryInfo (RecoveryInfo info) {
            //列挙型を数値に変換して保存する
            PlayerPrefs.SetInt (recoveryInfoKey, (int) info);
        }

        /// <summary>
        /// 保存された、機器から最後にデータを受信した時刻を取得します
        /// </summary>
        public static DateTime GetDataReceptionTime () {
            string dataReceptionTimeString = PlayerPrefs.GetString (dataReceptionTimeKey, "");
            if (dataReceptionTimeString == "")
                return DateTime.MinValue;
            return DateTime.Parse (dataReceptionTimeString);
        }
        /// <summary>
        /// 機器からデータを取得した時刻を保存します
        /// </summary>
        public static void SaveDataReceptionTime (DateTime value) {
            string dataReceptionTime = value.Year + "/" + value.Month + "/" + value.Day + " " + value.Hour + ":" + value.Minute + ":" + value.Second;	//DateTimeに変換できる形式に
            PlayerPrefs.SetString (dataReceptionTimeKey, dataReceptionTime);
        }

        /// <summary>
        /// 復元が必要かどうか
        /// </summary>
        public static bool isNessesaryRestore () {
            //復元が必要であれば1、必要なければ0
            return PlayerPrefs.GetInt (isNessesaryRestoreKey, 1) == 1;	//初期値は復元が必要に設定
        }

        /// <summary>
        /// 復元が必要かどうかを記録する
        /// </summary>
        public static void SaveNessesaryRestore (bool isNessesary) {
            //復元が必要であれば、1を。必要なければ0を記録
            PlayerPrefs.SetInt (isNessesaryRestoreKey, isNessesary ? 1 : 0);
        }

        /// <summary>
        /// 復元済みのデータ件数を取得します
        /// </summary>
        public static int GetRestoreDataCount () {
            return PlayerPrefs.GetInt (restoreDataCountKey, 0);
        }

        /// <summary>
        /// 復元済みのデータ件数を記録します
        /// </summary>
        public static void SaveRestoreDataCount (int dataCount) {
            PlayerPrefs.SetInt (restoreDataCountKey, dataCount);
        }
    }

    public static class Scene {

        static string historyDateKey = "KAIMIN_SCENE_HISTORY_VIEW_DATE";	//履歴画面で最後に見た日付
        static string graphDateKey = "KAIMIN_SCENE_GRAPH_DATE";			//グラフ画面に遷移したときに開く日付
        static string graphTabSelectKey = "KAIMIN_SCENE_GRAPH_TAB_SELECT";	//グラフの表示を時間と集計どちらにしているか
        static string graphDataTabSelectKey = "KAIMIN_SCENE_GRAPH_DATA_TAB_SELECT";	//グラフのデータ表示を呼吸・いびき・睡眠のどれにしているか

        /// <summary>
        /// 保存された、履歴画面で最後に見た日付を取得します
        /// </summary>
        public static DateTime GetHistoryDate () {
            string historyDateString = PlayerPrefs.GetString (historyDateKey, "");
            if (historyDateString == "")
                return DateTime.MinValue;
            return DateTime.Parse (historyDateString);
        }
        /// <summary>
        /// 履歴画面で最後に見た日付を保存します
        /// </summary>
        public static void SaveHistoryDate (DateTime value) {
            string dateTime = value.Year + "/" + value.Month + "/" + value.Day + " " + value.Hour + ":" + value.Minute + ":" + value.Second;	//DateTimeに変換できる形式に
            PlayerPrefs.SetString (historyDateKey, dateTime);
        }

        /// <summary>
        /// 保存された、グラフ画面で開く日付を取得します
        /// 保存されてない場合はDateTime.MinValueを返します
        /// </summary>
        public static DateTime GetGraphDate () {
            string graphDateString = PlayerPrefs.GetString (graphDateKey, "");
            if (graphDateString == "")
                return DateTime.MinValue;
            return DateTime.Parse (graphDateString);
        }

        /// <summary>
        /// グラフ画面で開く日付を保存します
        /// ヘッダーの睡眠開始時刻を保存(CSVのファイル名と合致するかで判断するため)
        /// </summary>
        public static void SaveGraphDate (DateTime value) {
            string dateTime = value.Year + "/" + value.Month + "/" + value.Day + " " + value.Hour + ":" + value.Minute + ":" + value.Second;	//DateTimeに変換できる形式に
            PlayerPrefs.SetString (graphDateKey, dateTime);
        }

        public static void ResetGraphDate()
        {
            PlayerPrefs.SetString(graphDateKey, "");
        }

        /// <summary>
        /// グラフ画面を開いた時の日付指定を解除します
        /// </summary>
        public static void ClearGraphDateSetting () {
            PlayerPrefs.DeleteKey (graphDateKey);
        }

        public enum GraphTabType {
            Time,		//時間
            Aggregate	//集計
        }

        /// <summary>
        /// グラフ画面で以前に開いていたグラフのタイプ(時間・集計)を取得します
        /// </summary>
        public static GraphTabType GetGraphTabType () {
            return (GraphTabType) Enum.ToObject (typeof (GraphTabType), PlayerPrefs.GetInt (graphTabSelectKey, 0));
        }

        /// <summary>
        /// グラフ画面で開いているグラフのタイプ(時間・集計)を記憶します
        /// </summary>
        public static void SaveGraphTabType (GraphTabType tabType) {
            PlayerPrefs.SetInt (graphTabSelectKey, (int) tabType);
        }

        /// <summary>
        /// グラフ画面で開いているグラフのタイプ(時間・集計)の記憶していたデータを初期化します
        /// </summary>
        public static void InitGraphTabSave () {
            PlayerPrefs.SetInt (graphTabSelectKey, (int) GraphTabType.Time);
        }

        public enum GraphDataTabType {
            Breath,		//呼吸
            Ibiki,		//いびき
            Sleep		//睡眠
        }

        /// <summary>
        /// グラフ画面で以前に開いていたグラフのデータタイプ(呼吸・いびき・睡眠)を取得します
        /// </summary>
        public static GraphDataTabType GetGraphDataTabType () {
            return (GraphDataTabType) Enum.ToObject (typeof (GraphDataTabType), PlayerPrefs.GetInt (graphDataTabSelectKey, 0));
        }

        /// <summary>
        /// グラフ画面で開いているグラフのデータタイプ(呼吸・いびき・睡眠)を記憶します
        /// </summary>
        public static void SaveGraphDataTabType (GraphDataTabType tabType) {
            PlayerPrefs.SetInt (graphDataTabSelectKey, (int) tabType);
        }

        /// <summary>
        /// グラフ画面で開いているグラフのデータタイプ(呼吸・いびき・睡眠)の記憶していたデータを初期化します
        /// </summary>
        public static void InitGraphDataTabSave () {
            PlayerPrefs.SetInt (graphDataTabSelectKey, (int) GraphDataTabType.Breath);
        }
    }

    public static class Setting {

        public static class Profile {

            static string isCompleteSettingKey = "KAIMIN_SETTING_PROFILE_ISCOMPLETESETTING";	//プロフィール設定を完了しているか
            static string nickNameKey = "KAIMIN_SETTING_PROFILE_NICKNAME";						//ニックネーム
            static string sexKey = "KAIMIN_SETTING_PROFILE_SEX";								//性別
            static string birthDayKey = "KAIMIN_SETTING_PROFILE_BIRTHDAY";						//誕生日
            static string bodyLengthKey = "KAIMIN_SETTING_PROFILE_BODYLENGTH";					//身長
            static string weightKey = "KAIMIN_SETTING_PROFILE_WEIGHT";							//体重
            static string idealSleepStartTimeKey = "KAIMIN_SETTING_PROFILE_IDEALSLEEPSTARTTIME";			//理想の睡眠時間(開始時刻)
            static string idealSleepEndTimeKey = "KAIMIN_SETTING_PROFILE_IDEALSLEEPENDTIME";				//理想の睡眠時間(終了時刻)


            static int bodyLengthFactor = 100;	//身長の小数点以下のデータを保持できるようにするための係数。大きいほど精度は高くなる
            static int weightFactor = 100;		//体重の小数点以下のデータを保持できるようにするための係数。大きいほど精度は高くなる

            /// <summary>
            /// プロフィール設定を完了しているかどうか
            /// </summary>
            public static bool isCompleteSetting () {
                //1なら設定完了、それ以外なら未完了とみなす
                return PlayerPrefs.GetInt (isCompleteSettingKey, 0) == 1;
            }

            /// <summary>
            /// プロフィール設定を完了した事を記録する
            /// </summary>
            public static void SaveCompleteSetting () {
                //1を設定完了とする
                PlayerPrefs.SetInt (isCompleteSettingKey, 1);
            }

            /// <summary>
            /// 保存されたニックネームを取得します
            /// </summary>
            public static string GetNickName () {
                return PlayerPrefs.GetString (nickNameKey, "");
            }
            /// <summary>
            /// ニックネームを保存します
            /// </summary>
            public static void SaveNickName (string value) {
                PlayerPrefs.SetString (nickNameKey, value);
            }

            public enum Sex {
                Unknown,	//未選択
                Male,		//男性
                Female		//女性
            }

            /// <summary>
            /// 設定された性別を取得します
            /// </summary>
            public static Sex GetSex () {
                //保存された数値から、Sexの項目に変換して返す
                return (Sex) Enum.ToObject (typeof(Sex), PlayerPrefs.GetInt (sexKey, 0));
            }

            /// <summary>
            /// 性別の設定を記録します
            /// </summary>
            public static void SaveSex (Sex sex) {
                //列挙型を数値に変換して保存する
                PlayerPrefs.SetInt (sexKey, (int) sex);
            }

            /// <summary>
            /// 保存された誕生日を取得します
            /// データが記録されてない場合はDateTime.MinValueを返します
            /// </summary>
            public static DateTime GetBirthDay () {
                string birthDayString = PlayerPrefs.GetString (birthDayKey, new DateTime (1970, 1, 1).ToString ());
                return DateTime.Parse (birthDayString);
            }

            /// <summary>
            /// 誕生日を保存します
            /// </summary>
            public static void SaveBirthDay (DateTime value) {
                string birthDay = value.Year + "/" + value.Month + "/" + value.Day;	//DateTimeに変換できる形式に
                PlayerPrefs.SetString (birthDayKey, birthDay);
            }

            /// <summary>
            /// 保存された身長を取得します
            /// </summary>
            public static float GetBodyLength () {
                //少数第一までの値が数値選択ダイアログから保存されてきて、それを係数を介してint型に変換しまたfloat型に戻された値
                var value = (float)PlayerPrefs.GetInt (bodyLengthKey, 170 * bodyLengthFactor) / (float)bodyLengthFactor;
                //元の値が少数第一だったため、再度変換する
                return (float)Math.Round (value, 1, MidpointRounding.AwayFromZero);
            }

            /// <summary>
            /// 身長を保存します
            /// </summary>
            public static void SaveBodyLength (float value) {
                //数値選択ダイアログから少数第一までの状態で値が設定されてくる 例：165.7
                int saveValue = (int)(value * bodyLengthFactor);	//後から小数点以下を復元できるように、桁を大きくしておく
                PlayerPrefs.SetInt (bodyLengthKey, saveValue);
            }

            /// <summary>
            /// 保存された体重を取得します
            /// </summary>
            public static float GetWeight () {
                //少数第一までの値が数値選択ダイアログから保存されてきて、それを係数を介してint型に変換しまたfloat型に戻された値
                var value = (float)PlayerPrefs.GetInt (weightKey, 60 * weightFactor) / (float)weightFactor;
                //元の値が少数第一だったため、再度変換する
                return (float)Math.Round (value, 1, MidpointRounding.AwayFromZero);
            }

            /// <summary>
            /// 体重を保存します
            /// </summary>
            public static void SaveWeight (float value) {
                //数値選択ダイアログから少数第一までの状態で値が設定されてくる 例：60.2
                int saveValue = (int)(value * weightFactor);	//後から小数点以下を復元できるように、桁を大きくしておく
                PlayerPrefs.SetInt (weightKey, saveValue);
            }

            /// <summary>
            /// 保存された理想の睡眠時間(開始時刻)を取得します
            /// データが保存されてなければ1970/01/01/00/00/00を返します
            /// </summary>
            public static DateTime GetIdealSleepStartTime () {
                string sleepTimeString = PlayerPrefs.GetString (idealSleepStartTimeKey, "");
                return sleepTimeString != ""
                    ? DateTime.Parse (sleepTimeString)
                    : new DateTime (1970, 1, 1, 0, 0, 0);
            }
            /// <summary>
            /// 理想の睡眠時間(開始時刻)を保存します
            /// </summary>
            public static void SaveIdealSleepStartTime (DateTime value) {
                string sleepTime = value.Year + "/" + value.Month + "/" + value.Day + " " + value.Hour + ":" + value.Minute + ":" + value.Second;	//DateTimeに変換できる形式に
                PlayerPrefs.SetString (idealSleepStartTimeKey, sleepTime);
            }

            /// <summary>
            /// 保存された理想の睡眠時間(終了時刻)を取得します
            /// データが保存されてなければ1970/01/01/00/00/00を返します
            /// </summary>
            public static DateTime GetIdealSleepEndTime () {
                string sleepTimeString = PlayerPrefs.GetString (idealSleepEndTimeKey, "");
                return sleepTimeString != ""
                    ? DateTime.Parse (sleepTimeString)
                    : new DateTime (1970, 1, 1, 0, 0, 0);
            }

            /// <summary>
            /// 理想の睡眠時間(終了時刻)を保存します
            /// </summary>
            public static void SaveIdealSleepEndTime (DateTime value) {
                string sleepTime = value.Year + "/" + value.Month + "/" + value.Day + " " + value.Hour + ":" + value.Minute + ":" + value.Second;	//DateTimeに変換できる形式に
                PlayerPrefs.SetString (idealSleepEndTimeKey, sleepTime);
            }
        }

        public static class Alerm {

            static string isEnableKey = "KAIMIN_SETTING_ALERM_ISENABLE";	//アラームが有効か無効か
            static string callTimeKey = "KAIMIN_SETTING_ALERM_CALLTIME";	//鳴動時間
            static string delayTimeKey = "KAIMIN_SETTING_ALERM_DELAYTIME_TIMESETTING";	//アラーム遅延時間の設定時間
            static string selectAlermKey = "KAIMIN_SETTING_SELECT_ALERM";	//選択中のアラーム

            /// <summary>
            /// 保存されたアラームの有効・無効設定を取得します
            /// </summary>
            public static bool isEnable () {
                //0が無効、1が有効として変換する
                return PlayerPrefs.GetInt (isEnableKey, 0) == 1;
            }

            /// <summary>
            /// アラームの有効・無効設定を保存します
            /// </summary>
            public static void SaveIsEnable (bool isEnable) {
                PlayerPrefs.SetInt (isEnableKey, isEnable ? 1 : 0);
            }


            public enum CallTime {
                None,		//設定しない
                Sec5,		//5秒
                Sec10,		//10秒
                Sec15,		//15秒
                Sec30		//30秒
            }

            /// <summary>
            /// 設定された鳴動時間を取得します
            /// </summary>
            public static CallTime GetCallTime () {
                //保存された数値から、Timeの項目に変換して返す
                return (CallTime) Enum.ToObject (typeof(CallTime), PlayerPrefs.GetInt (callTimeKey, 0));
            }

            /// <summary>
            /// 鳴動時間の設定を記録します
            /// </summary>
            public static void SaveCallTime (CallTime time) {
                //列挙型を数値に変換して保存する
                PlayerPrefs.SetInt (callTimeKey, (int) time);
            }


            //アラーム遅延時間の選択項目
            public enum DelayTime {
                None,	//なし
                Sec10,	//10秒
                Sec20,	//20秒
                Sec30	//30秒
            }

            /// <summary>
            /// 設定されたアラーム遅延時間を取得します
            /// </summary>
            public static DelayTime GetDelayTime () {
                //保存された数値から、DelayTimeの項目に変換して返す
                return (DelayTime) Enum.ToObject (typeof(DelayTime), PlayerPrefs.GetInt (delayTimeKey, 0));
            }

            /// <summary>
            /// アラーム遅延時間の設定を記録します
            /// </summary>
            public static void SaveDelayTime (DelayTime sense) {
                //列挙型を数値に変換して保存する
                PlayerPrefs.SetInt (delayTimeKey, (int) sense);
            }

            /// <summary>
            /// 選択中のアラームの番号を取得します
            /// </summary>
            /// <returns>The select alerm index.</returns>
            public static int GetSelectAlermIndex () {
                return PlayerPrefs.GetInt (selectAlermKey, 0);
            }

            /// <summary>
            /// 選択したアラームの番号を記録します
            /// </summary>
            /// <param name="index">Index.</param>
            public static void SaveSelectAlermIndex (int index) {
                PlayerPrefs.SetInt (selectAlermKey, index);
            }

            public static class IbikiAlerm {

                static string isEnableKey = "SETTING_ALERM_IBIKIALERM_ISENABLE";
                static string detectSense = "SETTING_ALERM_IBIKIALERM_DETECTSENSE";

                /// <summary>
                /// 保存されたいびきアラームの有効・無効設定を取得します
                /// </summary>
                public static bool isEnable () {
                    //0が無効、1が有効として変換する
                    return PlayerPrefs.GetInt (isEnableKey, 1) == 1;
                }

                /// <summary>
                /// アラームの有効・無効設定を保存します
                /// </summary>
                public static void SaveIsEnable (bool isEnable) {
                    PlayerPrefs.SetInt (isEnableKey, isEnable ? 1 : 0);
                }

                //いびき検知感度
                public enum DetectSense {
                    Normal,		//普通
                    Large		//大き目
                }

                /// <summary>
                /// 設定された検知感度を取得します
                /// </summary>
                public static DetectSense GetDetectSense () {
                    //保存された数値から、DetectSenseの項目に変換して返す
                    return (DetectSense) Enum.ToObject (typeof(DetectSense), PlayerPrefs.GetInt (detectSense, 0));
                }

                /// <summary>
                /// 検知感度の設定を記録します
                /// </summary>
                public static void SaveDetectSense (DetectSense sense) {
                    //列挙型を数値に変換して保存する
                    PlayerPrefs.SetInt (detectSense, (int) sense);
                }
            }

            public static class LowBreathAlerm {

                static string isEnableKey = "SETTING_ALERM_LOWBREATHALERM_ISENABLE";

                /// <summary>
                /// 保存された低呼吸アラームの有効・無効設定を取得します
                /// </summary>
                public static bool isEnable () {
                    //0が無効、1が有効として変換する
                    return PlayerPrefs.GetInt (isEnableKey, 1) == 1;
                }

                /// <summary>
                /// 低呼吸アラームの有効・無効設定を保存します
                /// </summary>
                public static void SaveIsEnable (bool isEnable) {
                    PlayerPrefs.SetInt (isEnableKey, isEnable ? 1 : 0);
                }
            }

            public static class StopMoveAlerm {

                static string isEnableKey = "SETTING_ALERM_STOPMOVEALERM_ISENABLE";

                /// <summary>
                /// 保存された低呼吸アラームの有効・無効設定を取得します
                /// </summary>
                public static bool isEnable () {
                    //0が無効、1が有効として変換する
                    return PlayerPrefs.GetInt (isEnableKey, 0) == 1;
                }

                /// <summary>
                /// 低呼吸アラームの有効・無効設定を保存します
                /// </summary>
                public static void SaveIsEnable (bool isEnable) {
                    PlayerPrefs.SetInt (isEnableKey, isEnable ? 1 : 0);
                }
            }

            public static class Vibration {

                static string isEnableKey = "SETTING_VIBRATION_ISENABLE";		//バイブレーションのON/OFF

                /// <summary>
                /// 保存されたバイブレーションの有効・無効設定を取得します
                /// </summary>
                public static bool isEnable () {
                    //0が無効、1が有効として変換する
                    return PlayerPrefs.GetInt (isEnableKey, 1) == 1;
                }

                /// <summary>
                /// バイブレーションの有効・無効設定を保存します
                /// </summary>
                public static void SaveIsEnable (bool isEnable) {
                    PlayerPrefs.SetInt (isEnableKey, isEnable ? 1 : 0);
                }
            }

            public static class FeedIn {

                static string isEnableKey = "SETTING_FEEDIN_ISENABLE";		//フェードインのON/OFF

                /// <summary>
                /// 保存されたフェードインの有効・無効設定を取得します
                /// </summary>
                public static bool isEnable () {
                    //0が無効、1が有効として変換する
                    return PlayerPrefs.GetInt (isEnableKey, 1) == 1;
                }

                /// <summary>
                /// フェードインの有効・無効設定を保存します
                /// </summary>
                public static void SaveIsEnable (bool isEnable) {
                    PlayerPrefs.SetInt (isEnableKey, isEnable ? 1 : 0);
                }
            }
        }

        /// <summary>
        /// デバイス設定を永続的に保存・読込するクラス
        /// </summary>
        public static class DeviceSettingData {

            /// <summary>
            /// デバイス設定を保存する
            /// </summary>
            public static void Save(DeviceSetting deviceSettig) {
                ActionModeData.Save(deviceSettig.ActionMode);
                SnoreSensitivityData.Save(deviceSettig.SnoreSensitivity);
                SuppressionStrengthData.Save(deviceSettig.SuppressionStrength);
                SuppressionOperationMaxTimeData.Save(deviceSettig.SuppressionOperationMaxTime);
                SuppressionStartTimeData.Save(deviceSettig.SuppressionStartTime);
            }

            /// <summary>
            /// デバイス設定を読み込む
            /// </summary>
            /// <returns>デバイス設定</returns>
            public static DeviceSetting Load() {
                var actionMode = ActionModeData.Load();
                var snoreSensitivity = SnoreSensitivityData.Load();
                var suppressionStrength = SuppressionStrengthData.Load();
                var suppressionOperationMaxTime = SuppressionOperationMaxTimeData.Load();
                var suppressionStartTime = SuppressionStartTimeData.Load();
                return new DeviceSetting() {
                    ActionMode = actionMode,
                    SnoreSensitivity = snoreSensitivity,
                    SuppressionStrength = suppressionStrength,
                    SuppressionOperationMaxTime = suppressionOperationMaxTime,
                    SuppressionStartTime = suppressionStartTime
                };
            }

            /// <summary>
            /// 動作モード設定を保存・読込するクラス
            /// </summary>
            public static class ActionModeData {

                /// <summary>
                /// データ保存先を示すキー
                /// </summary>
                private readonly static string Key = "ActionMode";

                /// <summary>
                /// 動作モード設定を保存する
                /// </summary>
                /// <param name="actionMode">動作モード</param>
                public static void Save(ActionMode actionMode) {
                    PlayerPrefs.SetInt(Key, (int)actionMode);
                }

                /// <summary>
                /// 動作モードを読み込む
                /// </summary>
                /// <returns>動作モード</returns>
                public static ActionMode Load() {
                    const int defaultValue = (int)ActionMode.SuppressModeIbiki;
                    return (ActionMode)PlayerPrefs.GetInt(Key, defaultValue);
                }
            }

            /// <summary>
            /// いびき感度設定を保存・読込するクラス
            /// </summary>
            public static class SnoreSensitivityData {

                /// <summary>
                /// いびき感度保存先を示すキー
                /// </summary>
                private readonly static string Key = "SnoreSensitivity";

                /// <summary>
                /// いびき感度設定を保存する
                /// </summary>
                /// <param name="snoreSensitivity">いびき感度</param>
                public static void Save(SnoreSensitivity snoreSensitivity) {
                    PlayerPrefs.SetInt(Key, (int)snoreSensitivity);
                }

                /// <summary>
                /// いびき感度設定を読み込む
                /// </summary>
                /// <returns>いびき感度</returns>
                public static SnoreSensitivity Load() {
                    const int defaultValue = (int)SnoreSensitivity.Mid;
                    return (SnoreSensitivity)PlayerPrefs.GetInt(Key, defaultValue);
                }
            }

            /// <summary>
            /// 低減強度設定を保存・読込するクラス
            /// </summary>
            public static class SuppressionStrengthData {

                /// <summary>
                /// 低減強度保存先を示すキー
                /// </summary>
                private readonly static string Key = "SuppressionStrength";

                /// <summary>
                /// 低減強度設定を保存する
                /// </summary>
                /// <param name="suppressionStrength">低減強度</param>
                public static void Save(SuppressionStrength suppressionStrength) {
                    PlayerPrefs.SetInt(Key, (int)suppressionStrength);
                }

                /// <summary>
                /// 低減強度を読み込む
                /// </summary>
                /// <returns>低減強度</returns>
                public static SuppressionStrength Load() {
                    const int defaultValue = (int)SuppressionStrength.Mid;
                    return (SuppressionStrength)PlayerPrefs.GetInt(
                        Key,
                        defaultValue);
                }
            }

            /// <summary>
            /// 低減動作最大継続時間を保存・読込するクラス
            /// </summary>
            public static class SuppressionOperationMaxTimeData {

                /// <summary>
                /// データの保存先を示すキー
                /// </summary>
                private readonly static string Key = "SuppressionOperationMaxTime";

                /// <summary>
                /// 低減動作最大継続時間設定を保存する
                /// </summary>
                /// <param name="suppressionOperationMaxTime">低減動作最大継続時間</param>
                public static void Save(SuppressionOperationMaxTime suppressionOperationMaxTime) {
                    PlayerPrefs.SetInt(Key, (int)suppressionOperationMaxTime);
                }

                /// <summary>
                /// 低減動作最大継続時間を読み込む
                /// </summary>
                /// <returns>低減動作最大継続時間</returns>
                public static SuppressionOperationMaxTime Load() {
                    const int defaultValue = (int)SuppressionOperationMaxTime.TenMin;
                    return (SuppressionOperationMaxTime)PlayerPrefs.GetInt(
                        Key,
                        defaultValue);
                }
            }

            /// <summary>
            /// 低減開始時間を保存・読込するクラス
            /// </summary>
            public static class SuppressionStartTimeData
            {

                /// <summary>
                /// データの保存先を示すキー
                /// </summary>
                private readonly static string Key = "SuppressionStartTime";

                /// <summary>
                /// 低減開始時間設定を保存する
                /// </summary>
                /// <param name="suppressionStartTime">低減開始時間</param>
                public static void Save(SuppressionStartTime suppressionStartTime)
                {
                    PlayerPrefs.SetInt(Key, (int)suppressionStartTime);
                }

                /// <summary>
                /// 低減開始時間を読み込む
                /// </summary>
                /// <returns>低減開始時間</returns>
                public static SuppressionStartTime Load()
                {
                    return (SuppressionStartTime)PlayerPrefs.GetInt(Key, (int)SuppressionStartTime.Default);
                }
            }
        }
    }

    public static class Device {

        static string pareringBLEAdress = "KAIMIN_SETTING_DEVICE_BLEADRESS";		//ペアリングしているBLEアドレス
        static string pareringDeviceName = "KAIMIN_SETTING_DEVICE_NAME";			//ペアリングしているデバイスのデバイス名
        static string pareringDeviceAdress = "KAIMIN_SETTING_DEVICE_ADRESS";		//ペアリングしているデバイスのデバイスアドレス(マックアドレス)
        static string pareringDeviceUuid = "KAIMIN_SETTING_DEVICE_INDEX";			//ペアリングしているデバイスのUUID(iOSでのみ接続の際に必要)
        static string pareringDeviceTime = "KAIMIN_SETTING_DEVICE_TIME";			//ペアリングしているデバイスの時刻
        static string g1dAppVersionKey = "KAIMIN_SETTING_G1DAPPVERSION";			//G1Dアプリファームウェアバージョン
        static string h1dAppVersionKey = "KAIMIN_SETTING_H1DAPPVERSION";			//H1Dアプリファームウェアバージョン
        static string h1dBootVersionKey = "KAIMIN_SETTING_H1DBOOTVERSION";			//H1Dブートファームウェアバージョン
        static string batteryState = "KAIMIN_SETTING_BATTERYSTATE";					//バッテリー残量
        static string farmwareVersionDiffKey = "KAIMIN_FARMWAREVERSIONDIFF";		//デバイスのファームウェアバージョンと最新のファームウェアバージョンに差があるかどうか

        /// <summary>
        /// ぺアリング中のデバイスのBLEアドレスを取得します
        /// </summary>
        public static string GetPareringBLEAdress () {
            return PlayerPrefs.GetString (pareringBLEAdress, "");
        }

        /// <summary>
        /// ペアリング中のデバイスのBLEアドレスを保存します
        /// </summary>
        public static void SavePareringBLEAdress (string bleAdress) {
            PlayerPrefs.SetString (pareringBLEAdress, bleAdress);
        }

        /// <summary>
        /// ペアリング中のデバイスのデバイス名を取得します
        /// </summary>
        public static string GetPareringDeviceName () {
            return PlayerPrefs.GetString (pareringDeviceName, "Sleeim");
        }

        /// <summary>
        /// ペアリング中のデバイスのデバイス名を保存します
        /// </summary>
        public static void SavePareringDeviceName (string deviceName) {
            PlayerPrefs.SetString (pareringDeviceName, deviceName);
        }

        /// <summary>
        /// ペアリング中のデバイスのマックアドレスを取得します
        /// </summary>
        public static string GetPareringDeviceAdress () {
            return PlayerPrefs.GetString (pareringDeviceAdress, "112233445566");
        }

        public static string GetLastDeviceAdress()
        {
            return PlayerPrefs.GetString(pareringDeviceAdress, "");
        }

        /// <summary>
        /// ペアリング中のデバイスのマックアドレスを保存します
        /// </summary>
        public static void SavePareringDeviceAdress (string deviceAdress) {
            PlayerPrefs.SetString (pareringDeviceAdress, deviceAdress);
        }

        /// <summary>
        /// ペアリング中のデバイスのUUIDを取得します
        /// iOSでペアリング中のデバイスに接続する際に使用します
        /// </summary>
        /// <returns>The parering device index.</returns>
        public static string GetPareringDeviceUUID () {
            return PlayerPrefs.GetString (pareringDeviceUuid, "");
        }

        /// <summary>
        /// ペアリングが完了したデバイスのUUIDを保存します
        /// iOSのみで使用します
        /// </summary>
        /// <param name="uuid">UUID.</param>
        public static void SavePareringDeviceUUID (string uuid) {
            PlayerPrefs.SetString (pareringDeviceUuid, uuid);
        }

        /// <summary>
        /// ペアリング中のデバイスの機器時刻を取得します
        /// </summary>
        public static DateTime GetPareringDeviceTime () {
            string deviceTimeString = PlayerPrefs.GetString (pareringDeviceTime, "");
            if (deviceTimeString == "")
                return DateTime.MinValue;
            return DateTime.Parse (deviceTimeString);
        }

        /// <summary>
        /// ペアリング中のデバイスの機器時刻を保存します
        /// </summary>
        public static void SavePareringDeviceTime (DateTime value) {
            string deviceTimeString = value.Year + "/" + value.Month + "/" + value.Day + " " + value.Hour + ":" + value.Minute + ":" + value.Second;	//DateTimeに変換できる形式に
            PlayerPrefs.SetString (pareringDeviceTime, deviceTimeString);
        }

        /// <summary>
        /// G1Dアプリファームウェアバージョンを取得します
        /// </summary>
        public static string GetG1DAppVersion () {
            return PlayerPrefs.GetString (g1dAppVersionKey, "---");
        }

        /// <summary>
        /// G1Dアプリファームウェアバージョンを保存します
        /// </summary>
        public static void SaveG1dAppVersion (string version) {
            PlayerPrefs.SetString (g1dAppVersionKey, version);
        }

        /// <summary>
        /// H1Dボートファームウェアバージョンを取得します
        /// </summary>
        public static string GetH1DBootVersion () {
            return PlayerPrefs.GetString (h1dBootVersionKey, "---");
        }

        /// <summary>
        /// H1Dブートファームウェアバージョンを保存します
        /// </summary>
        public static void SaveH1dBootVersion (string version) {
            PlayerPrefs.SetString (h1dBootVersionKey, version);
        }

        /// <summary>
        /// H1Dアプリファームウェアバージョンを取得します
        /// </summary>
        public static string GetH1DAppVersion () {
            return PlayerPrefs.GetString (h1dAppVersionKey, "---");
        }

        /// <summary>
        /// H1Dアプリファームウェアバージョンを保存します
        /// </summary>
        public static void SaveH1dAppVersion (string version) {
            PlayerPrefs.SetString (h1dAppVersionKey, version);
        }


        /// <summary>
        /// バッテリー残量を取得します
        /// </summary>
        public static int GetBatteryState () {
            return PlayerPrefs.GetInt (batteryState, 3);	//デフォルトをセンシング開始できない充電量(ハテナマーク)に設定
        }

        /// <summary>
        /// バッテリー残量を保存します
        /// </summary>
        public static void SaveBatteryState (int state) {
            PlayerPrefs.SetInt (batteryState ,state);
        }

        /// <summary>
        /// デバイスのファームウェアバージョンと最新のファームウェアバージョンに差があるか返します
        /// </summary>
        public static bool IsExistFirmwareVersionDiff () {
            return PlayerPrefs.GetInt (farmwareVersionDiffKey, 0) == 1;
        }

        /// <summary>
        /// デバイスのファームウェアバージョンと最新のファームウェアバージョンに差があるか記録します
        /// </summary>
        /// <param name="isExist">If set to <c>true</c> is exist.</param>
        public static void SaveIsExistFirmwareVersionDiff (bool isExist) {
            PlayerPrefs.SetInt (farmwareVersionDiffKey, isExist ? 1 : 0);
        }
    }
}
