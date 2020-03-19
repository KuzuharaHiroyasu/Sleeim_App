using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingDialog : DialogBase
{
    [SerializeField] Text textMessage;
    [SerializeField] Image loadingIcon;

    private RectTransform rectComponent;
    private float rotateSpeed = 150f;

    public static void Show(string message)
    {
        GameObject prehab = CreateDialog("Prehabs/Dialogs/LoadingDialog");
        LoadingDialog dialog = prehab.GetComponent<LoadingDialog>();
        dialog.Init(message);
    }

    public static void ChangeMessage(string message)
    {
        dialogObj.GetComponent<LoadingDialog>().Init(message);
    }

    private void Start()
    {
        rectComponent = loadingIcon.GetComponent<RectTransform>();
    }

    private void Update()
    {
        rectComponent.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }

    private void Init(string message)
    {
        textMessage.text = message;
    }
}
