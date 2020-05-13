using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 低減強度画面管理クラス
/// </summary>
public class SuppressionStrengthViewController : DeviceSettingViewController
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
    /// 徐々に強トグル
    /// </summary>
    public Toggle HighGradToggle;

    /// <summary>
    /// シーンタグ
    /// </summary>
    /// <value>低減強度タグ</value>
    public override SceneTransitionManager.LoadScene SceneTag {
        get {
            return SceneTransitionManager.LoadScene.SuppressionStrength;
        }
    }

    /// <summary>
    /// シーン開始イベントハンドラ
    /// </summary>
    protected override void Start() {
        base.Start();
        LoadSuppressionStrengthSetting();
    }

    /// <summary>
    /// 低減強度設定を読み込む
    /// </summary>
    private void LoadSuppressionStrengthSetting() {
        switch (DeviceSettingViewController.TempDeviceSetting.SuppressionStrength) {
            case SuppressionStrength.Low:
                LowToggle.isOn = true;
                break;
            case SuppressionStrength.Mid:
                MidToggle.isOn = true;
                break;
            case SuppressionStrength.High:
                HighToggle.isOn = true;
                break;
            case SuppressionStrength.HighGrad:
                HighGradToggle.isOn = true;
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
    /// 弱トグル値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void OnLowToggleValueChanged(bool isOn) {
        if (isOn) {
            StartVibrationConfirmCoroutine(SuppressionStrength.Low);
        }
    }

    /// <summary>
    /// 中トグル値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void OnMidToggleValueChanged(bool isOn) {
        if (isOn) {
            StartVibrationConfirmCoroutine(SuppressionStrength.Mid);
        }
    }

    /// <summary>
    /// 強トグル値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void OnHighToggleValueChanged(bool isOn) {
        if (isOn) {
            StartVibrationConfirmCoroutine(SuppressionStrength.High);
        }
    }


    /// <summary>
    /// 徐々に強トグル値変化イベントハンドラ
    /// </summary>
    /// <param name="isOn"></param>
    public void OnHighGradToggleValueChanged(bool isOn)
    {
        if (isOn)
        {
            StartVibrationConfirmCoroutine(SuppressionStrength.HighGrad);
        }
    }

    public void StartVibrationConfirmCoroutine(SuppressionStrength suppressionStrength)
    {
        if (DeviceSettingViewController.TempDeviceSetting.SuppressionStrength != suppressionStrength)
        {
            DeviceSettingViewController.TempDeviceSetting.SuppressionStrength = suppressionStrength;
            StartCoroutine(ConfirmVibrationCoroutine(suppressionStrength));
        }
    }

    /// <summary>
    /// バイブレーション確認するコルーチン
    /// </summary>
    /// <returns></returns>
    private IEnumerator ConfirmVibrationCoroutine(SuppressionStrength suppressionStrength) {
        Debug.Log("ConfirmVibration: start ConfirmVibrationCoroutine");
        yield return StartCoroutine(SendCommandToDeviceCoroutine(
            DeviceSetting.CommandCodeVibrationConfirm,
            (bool isSuccess) => {
                if (isSuccess) {
                    SaveDeviceSetting();
                } else {
                    StartCoroutine(ShowMessageDialogCoroutine("バイブレーション確認に失敗しました。"));
                }
            }, 5f)
        );
    }
}
