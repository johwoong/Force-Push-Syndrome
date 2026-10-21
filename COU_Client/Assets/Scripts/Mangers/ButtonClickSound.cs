using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 화면을 누른 자리에 활성 Button이 있으면 클릭음을 낸다.
/// 버튼마다 컴포넌트를 붙이지 않아도 되고, 나중에 생성되는 팝업의 버튼도 자동으로 포함된다.
/// SoundManager가 자동으로 붙인다.
/// </summary>
public class ButtonClickSound : MonoBehaviour
{
    private readonly List<RaycastResult> results = new();
    private PointerEventData pointerData;

    private void Update()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null || !TryGetPressPosition(out var position)) return;

        pointerData ??= new PointerEventData(eventSystem);
        pointerData.position = position;

        results.Clear();
        eventSystem.RaycastAll(pointerData, results);
        if (results.Count == 0) return;

        // 가장 위에 보이는 UI가 버튼(또는 버튼의 자식)일 때만
        var top = results[0].gameObject;
        var button = top.GetComponentInParent<Button>();
        if (button == null || !button.IsInteractable()) return;

        // 하단 탭바 버튼은 클릭음 없이 화면만 전환한다
        if (top.GetComponentInParent<TabBar>() != null) return;

        GameManager.Sound.PlaySFX(GameConstants.Sfx.CLICK);
    }

    private static bool TryGetPressPosition(out Vector2 position)
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            position = touch.primaryTouch.position.ReadValue();
            return true;
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            position = mouse.position.ReadValue();
            return true;
        }

        position = default;
        return false;
    }
}
