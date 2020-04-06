using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HelpMailLuncher : MonoBehaviour
{

    static string MAIL_ADRESS = "swanswan@one-a.co.jp";
    static string NEW_LINE = "\n";

    /// <summary>
    /// メーラーを起動し、お問い合わせメールを設定します
    /// </summary>
    public static void Lunch()
    {
        //メールタイトル
        string subject = "お問い合わせ";
        string deviceName = SystemInfo.deviceModel;
        Debug.Log("DeviceName:" + deviceName);
#if UNITY_IOS
        try
        {
            deviceName = Enum.GetName(typeof(UnityEngine.iOS.DeviceGeneration), UnityEngine.iOS.Device.generation);
            switch (deviceName) //DeviceGeneration似ない時はdeviceModelを返却
            {
                case "Unknown":
                case "iPhoneUnknown":
                case "iPadUnknown":
                case "iPodTouchUnknown":
                    deviceName = SystemInfo.deviceModel;
                    break;
            }
        }
        catch(Exception e) //ArgumentNullException,ArgumentException
        {
            Debug.Log(e.Message);
            deviceName = SystemInfo.deviceModel;
        }
        Debug.Log("DeviceName:" + deviceName);
#endif
        //本文
        string body = "";
        body += "■アプリのバージョン" + NEW_LINE;
        body += Application.version + NEW_LINE;
        body += "■ファームウェアのバージョン" + NEW_LINE;
        body += "G1D " + UserDataManager.Device.GetG1DAppVersion() + NEW_LINE;
        body += "■あなたの使っている端末とOS" + NEW_LINE;
        body += deviceName + "/" + SystemInfo.operatingSystem + NEW_LINE;

        body += "■お問い合わせ内容" + NEW_LINE;
        body += NEW_LINE;
        body += "■問題が発生した画面を入力してください（任意）" + NEW_LINE;
        body += NEW_LINE;
        body += "■行った操作を入力してください(任意)" + NEW_LINE;
        body += NEW_LINE;
        body += "■現象や問題点、不明点を入力してください(任意)" + NEW_LINE;
        body += NEW_LINE;
        //エスケープ処理
        body = System.Uri.EscapeDataString(body);
        subject = System.Uri.EscapeDataString(subject);
        //メーラー起動
        Application.OpenURL("mailto:" + MAIL_ADRESS + "?subject=" + subject + "&body=" + body);
    }
}
