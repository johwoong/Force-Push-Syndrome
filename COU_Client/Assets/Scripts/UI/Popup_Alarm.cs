using System;
using TMPro;
using UnityEngine;

public class Popup_Alarm : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tmp_AlarmContext;
    [SerializeField] private RectTransform panel;

    private Action onConfirm;

    public void Show(string message, Action onConfirm = null)
    {
        tmp_AlarmContext.text = message;
        this.onConfirm = onConfirm;

        if (gameObject.activeSelf) return;

        gameObject.SetActive(true);
        UITween.PopIn(panel, 0f, 0.9f);
    }

    public void OnClickConfirm()
    {
        Action callback = onConfirm;

        Close();
        callback?.Invoke();
    }

    public void Close()
    {
        onConfirm = null;
        gameObject.SetActive(false);
    }
}
