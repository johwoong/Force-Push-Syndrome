using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.Serialization;

public class VirtualJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [InputControl(layout = "Vector2")]
    [FormerlySerializedAs("controlPath")]
    [SerializeField] private string m_ControlPath = "<Gamepad>/leftStick";
    [SerializeField] private RectTransform stickBase;
    [SerializeField] private RectTransform knob;
    [SerializeField] private float radius = 165f;

    private Vector2 homePosition;

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }

    private void Awake()
    {
        homePosition = stickBase.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, eventData.position, eventData.pressEventCamera, out var point);
        knob.DOKill();
        knob.anchoredPosition = Vector2.zero;
        stickBase.localPosition = point;
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            stickBase, eventData.position, eventData.pressEventCamera, out var offset);
        offset = Vector2.ClampMagnitude(offset, radius);
        knob.anchoredPosition = offset;
        SendValueToControl(offset.normalized);
    }

    // 손을 떼면 노브가 가운데로 튕기듯 돌아온다
    public void OnPointerUp(PointerEventData eventData)
    {
        knob.DOAnchorPos(Vector2.zero, 0.15f).SetEase(Ease.OutBack).AsUI(knob);
        stickBase.anchoredPosition = homePosition;
        SendValueToControl(Vector2.zero);
    }
}
