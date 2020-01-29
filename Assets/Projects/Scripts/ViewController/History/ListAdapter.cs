using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// リストビューの要素オブジェクトを指定の場所に放り込むだけ
/// </summary>
public class ListAdapter : MonoBehaviour {

	public Transform listParent;

	/// <summary>
	/// リストビューに要素を追加します
	/// </summary>
	public void SetElementToList (GameObject elementObj) {
		elementObj.transform.SetParent (listParent);
		//UIの位置・大きさ初期化
		var elementTransform = elementObj.GetComponent<RectTransform> ();
		elementTransform.localScale = Vector3.one;
	}

	/// <summary>
	/// リストビューに現在入っている要素を全て削除します
	/// </summary>
	public void ClearAllElement () {
		foreach (Transform elementTransform in listParent.transform) {
			//全ての子オブジェクトに対して処理を行う
			Destroy (elementTransform.gameObject);
		}
	}
}
