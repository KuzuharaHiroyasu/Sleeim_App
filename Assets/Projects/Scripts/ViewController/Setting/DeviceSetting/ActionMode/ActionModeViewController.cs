using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 動作モード設定画面
/// </summary>
public class ActionModeViewController : ViewControllerBase {

    /// <summary>
    /// 低減モード(いびき)トグル
    /// </summary>
    public Toggle SuppressModeIbikiToggle;

    /// <summary>
    /// 低減モード(いびき+無呼吸)トグル
    /// </summary>
    public Toggle SuppressModeToggle;

    /// <summary>
    /// モニタリングモードトグル
    /// </summary>
    public Toggle MonitoringModeToggle;

    /// <summary>
    /// 低減モード（無呼吸）トグル
    /// </summary>
    public Toggle SuppressModeMukokyuToggle;

    /// <summary>
    /// シーンタグ
    /// </summary>
    /// <value>動作モードタグ</value>
    public override SceneTransitionManager.LoadScene SceneTag {
        get {
            return SceneTransitionManager.LoadScene.ActionMode;
        }
    }

    /// <summary>
    /// シーン開始イベントハンドラ
    /// </summary>
    protected override void Start() {
        base.Start();
        LoadActionModeSetting();
    }

    /// <summary>
    /// 動作モード設定を読み込む
    /// </summary>
    private void LoadActionModeSetting() {
        switch (DeviceSettingViewController.TempDeviceSetting.ActionMode) {
            case ActionMode.SuppressModeIbiki:
                SuppressModeIbikiToggle.isOn = true;
                break;
            case ActionMode.SuppressMode:
                SuppressModeToggle.isOn = true;
                break;
            case ActionMode.MonitoringMode:
                MonitoringModeToggle.isOn = true;
                break;
            case ActionMode.SuppressModeMukokyu:
                SuppressModeMukokyuToggle.isOn = true;
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 戻るボタン押下イベントハンドラ
    /// </summary>
    public void OnReturnButtonTap() {
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.DeviceSetting);
    }

    /// <summary>
    /// 低減モード(いびき)値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void OnSuppressModeIbikiToggleValueChanged(bool isOn)
    {
        if (isOn)
        {
            DeviceSettingViewController.TempDeviceSetting.ActionMode
                = ActionMode.SuppressModeIbiki;
        }
    }

    /// <summary>
    /// 低減モード(いびき+無呼吸)値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void OnSuppressModeToggleValueChanged(bool isOn)
    {
        if (isOn)
        {
            DeviceSettingViewController.TempDeviceSetting.ActionMode
                = ActionMode.SuppressMode;
        }
    }

    /// <summary>
    /// モニタリングモード値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void OnMonitoringModeToggleValueChanged(bool isOn) {
        if (isOn) {
            DeviceSettingViewController.TempDeviceSetting.ActionMode
                = ActionMode.MonitoringMode;
        }
    }

    /// <summary>
    /// 低減モード（無呼吸）値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void OnSuppressModeMukokyuToggleValueChanged(bool isOn)
    {
        if (isOn)
        {
            DeviceSettingViewController.TempDeviceSetting.ActionMode
                = ActionMode.SuppressModeMukokyu;
        }
    }
}
