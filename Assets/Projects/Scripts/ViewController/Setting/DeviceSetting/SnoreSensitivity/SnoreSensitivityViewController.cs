using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// いびき感度画面管理クラス
/// </summary>
public class SnoreSensitivityViewController : DeviceSettingViewController
{

    /// <summary>
    /// 弱トグル
    /// </summary>
    public Toggle LowToggle;

    /// <summary>
    /// 中トグル
    /// </summary>
    public Toggle MidToggle;

    /// <summary>
    /// 強トグル
    /// </summary>
    public Toggle HighToggle;

    /// <summary>
    /// シーンタグ
    /// </summary>
    /// <value>いびき感度タグ</value>
    public override SceneTransitionManager.LoadScene SceneTag {
        get {
            return SceneTransitionManager.LoadScene.SnoreSensitivity;
        }
    }

    /// <summary>
    /// シーン開始イベントハンドラ
    /// </summary>
    protected override void Start() {
        base.Start();
        LoadSnoreSensitivitySetting();
    }

    /// <summary>
    /// いびき感度設定を読み込む
    /// </summary>
    private void LoadSnoreSensitivitySetting() {
        switch (DeviceSettingViewController.TempDeviceSetting.SnoreSensitivity) {
            case SnoreSensitivity.Low:
                LowToggle.isOn = true;
                break;
            case SnoreSensitivity.Mid:
                MidToggle.isOn = true;
                break;
            case SnoreSensitivity.High:
                HighToggle.isOn = true;
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 保存ボタン押下イベントハンドラ
    /// </summary>
    public void OnSaveButtonTap()
    {
        base.OnSaveButtonTap();
    }

    /// <summary>
    /// 戻るボタン押下イベントハンドラ
    /// </summary>
    public void OnReturnButtonTap() {
        //SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.DeviceSetting);
        StartCoroutine(base.BackButtonCoroutineForChildClass());
    }

    /// <summary>
    /// 弱トグル値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void OnLowToggleValueChanged(bool isOn) {
        if (isOn) {
            DeviceSettingViewController.TempDeviceSetting.SnoreSensitivity
                = SnoreSensitivity.Low;
        }
    }

    /// <summary>
    /// 中トグル値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void OnMidToggleValueChanged(bool isOn) {
        if (isOn) {
            DeviceSettingViewController.TempDeviceSetting.SnoreSensitivity
                = SnoreSensitivity.Mid;
        }
    }

    /// <summary>
    /// 強トグル値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void OnHighToggleValueChanged(bool isOn) {
        if (isOn) {
            DeviceSettingViewController.TempDeviceSetting.SnoreSensitivity
                = SnoreSensitivity.High;
        }
    }
}
