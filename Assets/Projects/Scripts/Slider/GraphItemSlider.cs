﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Events;
using System.Collections.Generic;
using System;

[DisallowMultipleComponent]
[RequireComponent(typeof(ScrollRect), typeof(CanvasGroup))]
public class GraphItemSlider : UIBehaviour, IDragHandler, IEndDragHandler
{
    [SerializeField] public int startingIndex = 0;
    [SerializeField] public bool wrapAround = false;
    [SerializeField] public float lerpTimeMilliSeconds = 200f;
    [SerializeField] public float triggerPercent = 5f;
    [Range(0f, 10f)] public float triggerAcceleration = 1f;

    public class OnLerpCompleteEvent : UnityEvent { }
    public OnLerpCompleteEvent onLerpComplete;
    public class OnReleaseEvent : UnityEvent<int> { }
    public OnReleaseEvent onRelease;

    public int actualIndex;
    public int cellIndex;
    ScrollRect scrollRect;
    CanvasGroup canvasGroup;
    RectTransform content;
    Vector2 cellSize;
    bool indexChangeTriggered = false;
    bool isLerping = false;
    float lerpStartedAt;
    Vector2 releasedPosition;
    Vector2 targetPosition;

    public Graph.GraphDataSource controllerDelegate = null;
    public List<GraphItem>  graphItems = null;
    public List<String>  filePaths = null;
    public List<List<SleepData>> sleepDatas = null;
    public List<SleepHeaderData> sleepHeaderDatas = null;

    protected override void Awake()
    {
        base.Awake();
        actualIndex = startingIndex;
        cellIndex = startingIndex;
        this.onLerpComplete = new OnLerpCompleteEvent();
        this.onRelease = new OnReleaseEvent();
        this.scrollRect = GetComponent<ScrollRect>();
        this.canvasGroup = GetComponent<CanvasGroup>();
        this.content = scrollRect.content;
        this.cellSize = content.GetComponent<GridLayoutGroup>().cellSize;
        content.anchoredPosition = new Vector2(-cellSize.x * cellIndex, content.anchoredPosition.y);
        int count = LayoutElementCount();
        SetContentSize(count);

        graphItems = new List<GraphItem>();
        filePaths = new List<String>();
        sleepDatas = new List<List<SleepData>>();
        sleepHeaderDatas = new List<SleepHeaderData>();

        if (startingIndex < count)
        {
            MoveToIndex(startingIndex);
        }
    }

    void LateUpdate()
    {
        if (isLerping)
        {
            LerpToElement();
            if (ShouldStopLerping())
            {
                isLerping = false;
                canvasGroup.blocksRaycasts = true;
                onLerpComplete.Invoke();
                onLerpComplete.RemoveListener(WrapElementAround);
            }
        }
    }


    public void PushGraphItemData(String filePath)
    {
        graphItems.Add(null);
        filePaths.Add(filePath);
        sleepDatas.Add(null);
        sleepHeaderDatas.Add(null);
    }

    public void PushGraphItemLayout(GraphItem graphItem, int i)
    {
        graphItems[i] = graphItem;
        LayoutElement layoutElementPrefab = graphItem.GetComponent<LayoutElement>();
        PushLayoutElement(layoutElementPrefab);
    }

    public void PushLayoutElement(LayoutElement element)
    {
        element.transform.SetParent(content.transform, false);
        SetContentSize(LayoutElementCount());
    }


    public void PopLayoutElement(int currentStartIdex)
    {
        LayoutElement[] elements = content.GetComponentsInChildren<LayoutElement>();
        RemoveLayoutElement(elements.Length - 1, currentStartIdex);
    }

    public void RemoveLayoutElement(int cellIndex, int currentStartIdex = 0)
    {
        RemoveGraphItemLayout(cellIndex);
        RemoveGraphItemData(cellIndex + currentStartIdex);
    }

    public void RemoveGraphItemLayout(int index)
    {
        LayoutElement[] elements = content.GetComponentsInChildren<LayoutElement>();
        if (index >= 0 && index < elements.Length && elements.Length > 0)
        {
            Destroy(elements[index].gameObject);
            SetContentSize(LayoutElementCount() - 1);
            if (cellIndex == CalculateMaxIndex())
            {
                cellIndex -= 1;
            }
        }
    }

    public void RemoveGraphItemData(int index)
    {
        if (index >= 0 && index < filePaths.Count && filePaths.Count > 0)
        {
            graphItems.RemoveAt(index);
            filePaths.RemoveAt(index);
            sleepDatas.RemoveAt(index);
            sleepHeaderDatas.RemoveAt(index);
        }
    }

    public void RemoveDefaultLayoutElement()
    {
        LayoutElement[] elements = content.GetComponentsInChildren<LayoutElement>();
        if (0 < elements.Length)
        {
            Destroy(elements[0].gameObject);
            SetContentSize(LayoutElementCount() - 1);
            if (cellIndex == CalculateMaxIndex())
            {
                cellIndex -= 1;
            }
        }
    }

    public void UnshiftLayoutElement(LayoutElement element)
    {
        cellIndex += 1;
        element.transform.SetParent(content.transform, false);
        element.transform.SetAsFirstSibling();
        SetContentSize(LayoutElementCount());
        content.anchoredPosition = new Vector2(content.anchoredPosition.x - cellSize.x, content.anchoredPosition.y);
    }

    public void ShiftLayoutElement()
    {
        Destroy(GetComponentInChildren<LayoutElement>().gameObject);
        SetContentSize(LayoutElementCount() - 1);
        cellIndex -= 1;
        content.anchoredPosition = new Vector2(content.anchoredPosition.x + cellSize.x, content.anchoredPosition.y);
    }

    public int LayoutElementCount()
    {
        return content.GetComponentsInChildren<LayoutElement>(false)
            .Count(e => e.transform.parent == content);
    }

    public int CurrentIndex
    {
        get
        {
            int count = LayoutElementCount();
            int mod = actualIndex % count;
            return mod >= 0 ? mod : count + mod;
        }
    }

    public void OnDrag(PointerEventData data)
    {
        float dx = data.delta.x;
        float dt = Time.deltaTime * 1000f;
        float acceleration = Mathf.Abs(dx / dt);
        if (acceleration > triggerAcceleration && !float.IsPositiveInfinity(acceleration))
        {
            indexChangeTriggered = true;
        }
    }

    public void OnEndDrag(PointerEventData data)
    {
        if (IndexShouldChangeFromDrag(data))
        {
            int direction = (data.pressPosition.x - data.position.x) > 0f ? 1 : -1;
            SnapToIndex(cellIndex + direction * CalculateScrollingAmount(data), direction == 1);
        }
        else
        {
            StartLerping();
        }
    }

    public int CalculateScrollingAmount(PointerEventData data)
    {
        var offset = scrollRect.content.anchoredPosition.x + cellIndex * cellSize.x;
        var normalizedOffset = Mathf.Abs(offset / cellSize.x);
        var skipping = (int)Mathf.Floor(normalizedOffset);
        if (skipping == 0)
            return 1;
        if ((normalizedOffset - skipping) * 100f > triggerPercent)
        {
            return 1; //skipping + 1;
        }
        else
        {
            return skipping;
        }
    }

    public void SnapToNext()
    {
        SnapToIndex(cellIndex + 1, true);
    }

    public void SnapToPrev()
    {
        SnapToIndex(cellIndex - 1, false);
    }

    public void SnapToIndex(int newCellIndex, bool isToNext = true)
    {
        int maxIndex = CalculateMaxIndex();
        if (wrapAround && maxIndex > 0)
        {
            actualIndex += newCellIndex - cellIndex;
            cellIndex = newCellIndex;
            onLerpComplete.AddListener(WrapElementAround);
        }
        else
        {
            newCellIndex = Mathf.Clamp(newCellIndex, 0, maxIndex);
            actualIndex += newCellIndex - cellIndex;
            cellIndex = newCellIndex;
        }

        onRelease.Invoke(cellIndex);
        StartLerping();

        if (controllerDelegate != null)
        {
            this.controllerDelegate.UpdateGraphItem(cellIndex, isToNext);
        }
    }

    public void MoveToIndex(int newCellIndex)
    {
        int maxIndex = CalculateMaxIndex();
        if (newCellIndex >= 0 && newCellIndex <= maxIndex)
        {
            actualIndex += newCellIndex - cellIndex;
            cellIndex = newCellIndex;
        }

        onRelease.Invoke(cellIndex);
        content.anchoredPosition = CalculateTargetPoisition(cellIndex);
    }

    void StartLerping()
    {
        releasedPosition = content.anchoredPosition;
        targetPosition = CalculateTargetPoisition(cellIndex);
        lerpStartedAt = Time.time;
        canvasGroup.blocksRaycasts = false;
        isLerping = true;
    }

    int CalculateMaxIndex()
    {
        int cellPerFrame = Mathf.FloorToInt(scrollRect.GetComponent<RectTransform>().rect.size.x / cellSize.x);
        return LayoutElementCount() - cellPerFrame;
    }

    bool IndexShouldChangeFromDrag(PointerEventData data)
    {
        // acceleration was above threshold
        if (indexChangeTriggered)
        {
            indexChangeTriggered = false;
            return true;
        }
        // dragged beyond trigger threshold
        var offset = scrollRect.content.anchoredPosition.x + cellIndex * cellSize.x;
        var normalizedOffset = Mathf.Abs(offset / cellSize.x);
        return normalizedOffset * 100f > triggerPercent;
    }

    void LerpToElement()
    {
        float t = (Time.time - lerpStartedAt) * 1000f / lerpTimeMilliSeconds;
        float newX = Mathf.Lerp(releasedPosition.x, targetPosition.x, t);
        content.anchoredPosition = new Vector2(newX, content.anchoredPosition.y);
    }

    void WrapElementAround()
    {
        if (cellIndex <= 0)
        {
            var elements = content.GetComponentsInChildren<LayoutElement>();
            elements[elements.Length - 1].transform.SetAsFirstSibling();
            cellIndex += 1;
            content.anchoredPosition = new Vector2(content.anchoredPosition.x - cellSize.x, content.anchoredPosition.y);
        }
        else if (cellIndex >= CalculateMaxIndex())
        {
            var element = content.GetComponentInChildren<LayoutElement>();
            element.transform.SetAsLastSibling();
            cellIndex -= 1;
            content.anchoredPosition = new Vector2(content.anchoredPosition.x + cellSize.x, content.anchoredPosition.y);
        }
    }

    void SetContentSize(int elementCount)
    {
        content.sizeDelta = new Vector2(cellSize.x * elementCount, content.rect.height);
    }

    Vector2 CalculateTargetPoisition(int index)
    {
        return new Vector2(-cellSize.x * index, content.anchoredPosition.y);
    }

    bool ShouldStopLerping()
    {
        return Mathf.Abs(content.anchoredPosition.x - targetPosition.x) < 0.1f;
    }
}
