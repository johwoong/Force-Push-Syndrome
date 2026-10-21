using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect appliedArea;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != appliedArea)
            Apply();
    }

    private void Apply()
    {
        appliedArea = Screen.safeArea;

        var min = appliedArea.min;
        var max = appliedArea.max;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        rectTransform.anchorMin = min;
        rectTransform.anchorMax = max;
    }
}
