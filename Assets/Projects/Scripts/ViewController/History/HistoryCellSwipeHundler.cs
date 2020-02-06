using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class HistoryCellSwipeHundler : SwipeHandler {

	public GameObject mainObject;
	public GameObject vanishObject;

	bool displayVanishButton = false;

	override public void OnEndDrag(PointerEventData eventData)
	{
		// eventData.delta の値が期待と異なったので算出
		Vector2 delta = eventData.position - eventData.pressPosition;


		// 閾値チェック（ロングタップ）
		if (delta.x*delta.x+delta.y*delta.y < longtaphold) {

//			ChangeDisplayVanishState ();
			return;
		}
		// 閾値チェック
		if (Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) < threshold) {
			return;
		}

		// 縦横チェック
		if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) {
			// 横方向
			if (delta.x > 0) {
				// 左から右
				HideVanishButton();
			} else {
				// 右から左
				DisplayVanishButton ();
			}
		} else {
			// 縦方向
			if (delta.y > 0) {
				// 下から上
			} else {
				// 上から下
			}
		}
	}

	bool pointerDownFlag = false;
	override public void OnPointerDown (PointerEventData eventData)
	{   
        /*
		pointerDownFlag = true;
		StartCoroutine ("DelayLongPress");
        */
	}

	override public void OnPointerUp (PointerEventData eventData)
	{
        /*
		pointerDownFlag = false;
		// 長押し判定開始前に離したorスクロールした場合
		if (!isLongPressed) {
			StopCoroutine ("DelayLongPress");       //コルーチン停止
			return;
		}
        */
	}

	public float intervalInit = 1.0f;
	bool isLongPressed = false;

	// intervalInit秒後に長押し判定開始
	IEnumerator DelayLongPress(){
		isLongPressed = false;
		yield return new WaitForSeconds (intervalInit);
		isLongPressed = true;
		if (pointerDownFlag) {
			ChangeDisplayVanishState ();
		}
	}


	public void ChangeDisplayVanishState() {
		if (displayVanishButton) {
			HideVanishButton ();
		} else {
			DisplayVanishButton ();
		}
	}

	void DisplayVanishButton() {

		if (displayVanishButton) {
			return;
		}

		Vector3 position = mainObject.transform.localPosition;
		mainObject.transform.localPosition = new Vector3(position.x-30,position.y);

		Vector3 position2 = vanishObject.transform.localPosition;
		vanishObject.transform.localPosition = new Vector3(position2.x-30, position2.y);

        vanishObject.SetActive(true);
        displayVanishButton = true;
	}

	void HideVanishButton() {

		if (!displayVanishButton) {
			return;
		}

		Vector3 position = mainObject.transform.localPosition;
		mainObject.transform.localPosition = new Vector3(position.x+30, position.y);

		Vector3 position2 = vanishObject.transform.localPosition;
		vanishObject.transform.localPosition = new Vector3(position2.x+30, position2.y);

        vanishObject.SetActive(false);
        displayVanishButton = false;
	}
}
