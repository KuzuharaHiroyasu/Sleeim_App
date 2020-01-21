using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// デバイス設定クラス
/// </summary>
public class DeviceSetting {

    /// <summary>
    /// BLEコマンドコード
    /// </summary>
    public static readonly byte CommandCode = 0xC6;

    /// <summary>
    /// バイブレーション確認コマンドコード
    /// </summary>
    public static readonly byte CommandCodeVibrationConfirm = 0xC7;

    /// <summary>
    /// バイブレーション停止コマンドコード
    /// </summary>
    public static readonly byte CommandCodeVibrationStop = 0xC8;

    /// <summary>
    /// 動作モード
    /// </summary>
    public ActionMode ActionMode { get; set; } = ActionMode.SuppressModeIbiki;



	/// <summary>
	/// 動作モード 文字列
	/// </summary>
	public string ActionModeText
	{
		get
		{
			string ret = "-";
			switch (ActionMode)
			{
			case ActionMode.SuppressModeIbiki:
				ret = "抑制モード(いびき)";
				break;
			case ActionMode.SuppressMode:
				ret = "抑制モード(いびき+無呼吸)";
				break;
			case ActionMode.MonitoringMode:
				ret = "モニタリング";
				break;
			case ActionMode.SuppressModeMukokyu:
				ret = "抑制モード（無呼吸）";
				break;
			default:
				// 何もしない
				break;
			}
			return ret;
		}
	}


    /// <summary>
    /// いびき感度
    /// </summary>
    public SnoreSensitivity SnoreSensitivity { get; set; }
        = SnoreSensitivity.Mid;

    /// <summary>
    /// 抑制強度
    /// </summary>
    public SuppressionStrength SuppressionStrength { get; set; }
        = SuppressionStrength.Mid;

    /// <summary>
    /// 抑制動作最大継続時間
    /// </summary>
    public SuppressionOperationMaxTime SuppressionOperationMaxTime { get; set; }
        = SuppressionOperationMaxTime.TenMin;

    /// <summary>
    /// 抑制開始時間(～分)
    /// </summary>
    public SuppressionStartTime SuppressionStartTime { get; set; }
        = SuppressionStartTime.Default;

    /// <summary>
    /// デバイス設定コマンド
    /// </summary>
    public byte[] Command {
        get {
            return new byte[] {
                CommandCode,
                (byte)ActionMode,
                (byte)SnoreSensitivity,
                (byte)SuppressionStrength,
                (byte)SuppressionOperationMaxTime,
                (byte)SuppressionStartTime
            };
        }
    }

    /// <summary>
    /// バイブレーション確認コマンド
    /// </summary>
    public byte[] CommandVibrationConfirm
    {
        get
        {
            return new byte[] {
                CommandCodeVibrationConfirm,
                (byte)SuppressionStrength,
            };
        }
    }

    /// <summary>
    /// バイブレーション停止コマンド
    /// </summary>
    public byte[] CommandVibrationStop
    {
        get
        {
            return new byte[] { 
                CommandCodeVibrationStop
            };
        }
    }
}

/// <summary>
/// 動作モード定数
/// </summary>
public enum ActionMode : byte {
    SuppressModeIbiki = 0,
    SuppressMode,
    MonitoringMode,
    SuppressModeMukokyu,
}

/// <summary>
/// いびき感度
/// </summary>
public enum SnoreSensitivity : byte {
    Low = 0,
    Mid,
    High
}

/// <summary>
/// 抑制強度
/// </summary>
public enum SuppressionStrength : byte {
    Low = 0,
    Mid,
    High,
    HighGrad
}

/// <summary>
/// 抑制動作最大継続時間
/// </summary>
public enum SuppressionOperationMaxTime : byte {
    FiveMin = 0,
    TenMin,
    NoSettings
}

/// <summary>
/// 抑制開始時間(～分)
/// </summar
public enum SuppressionStartTime : byte
{
    Min = 0,
    Max = 59,
    Default = 20
}
