using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeHandler : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler {
	public float longtaphold = 20.0f;
	public float threshold = 100.0f;

	public void OnDrag(PointerEventData eventData)
	{
		
	}


	virtual public void OnEndDrag(PointerEventData eventData)
	{
		// eventData.delta の値が期待と異なったので算出
		Vector2 delta = eventData.position - eventData.pressPosition;

		// 閾値チェック
		if (Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) < threshold) {
			return;
		}

		// 縦横チェック
		if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) {
			// 横方向
			if (delta.x > 0) {
				// 左から右
			} else {
				// 右から左
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

	virtual public void OnPointerDown (PointerEventData eventData)
	{                               
	}


	virtual public void OnPointerUp (PointerEventData eventData){
	}
}