using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 低減動作最大継続時間画面管理クラス
/// </summary>
public class SuppressionOperationMaxTimeViewController : DeviceSettingViewController
{

    /// <summary>
    /// 5分トグル
    /// </summary>
    public Toggle FiveMinToggle;

    /// <summary>
    /// 10分トグル
    /// </summary>
    public Toggle TenMinToggle;

    /// <summary>
    /// 設定しないトグル
    /// </summary>
    public Toggle NoSettingsToggle;

    /// <summary>
    /// シーンタグ
    /// </summary>
    /// <value>低減動作最大継続時間タグ</value>
    public override SceneTransitionManager.LoadScene SceneTag {
        get {
            return SceneTransitionManager.LoadScene.SuppressionOperationMaxTime;
        }
    }

    /// <summary>
    /// シーン開始イベントハンドラ
    /// </summary>
    protected override void Start() {
        base.Start();
        LoadSuppressionOperationMaxTimeSetting();
    }

    /// <summary>
    /// 低減動作最大継続時間設定を読み込む
    /// </summary>
    private void LoadSuppressionOperationMaxTimeSetting() {
        switch (DeviceSettingViewController.TempDeviceSetting.SuppressionOperationMaxTime) {
            case SuppressionOperationMaxTime.FiveMin:
                FiveMinToggle.isOn = true;
                break;
            case SuppressionOperationMaxTime.TenMin:
                TenMinToggle.isOn = true;
                break;
            case SuppressionOperationMaxTime.NoSettings:
                NoSettingsToggle.isOn = true;
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
    /// ５分トグル値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void On5MinToggleValueChanged(bool isOn) {
        if (isOn) {
            DeviceSettingViewController.TempDeviceSetting.SuppressionOperationMaxTime
                = SuppressionOperationMaxTime.FiveMin;
        }
    }

    /// <summary>
    /// 10分トグル値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void On10MinToggleValueChanged(bool isOn) {
        if (isOn) {
            DeviceSettingViewController.TempDeviceSetting.SuppressionOperationMaxTime
                = SuppressionOperationMaxTime.TenMin;
        }
    }

    /// <summary>
    /// 設定なしトグル値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void OnNoSettingsToggleValueChanged(bool isOn) {
        if (isOn) {
            DeviceSettingViewController.TempDeviceSetting.SuppressionOperationMaxTime
                = SuppressionOperationMaxTime.NoSettings;
        }
    }
}
