using System.Linq;
using UnityEngine;

public class SceneUIRoot : MonoBehaviour
{
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private Canvas screenCanvas;
    [SerializeField] private Canvas popupCanvas;

    private UIManager manager;
    private UIView[] ownedViews;

    private void Awake()
    {
        manager = UIManager.Instance;

        manager.Register(hudCanvas, UILayer.HUD);
        manager.Register(screenCanvas, UILayer.Screen);
        manager.Register(popupCanvas, UILayer.Popup);

        ownedViews = new[] { hudCanvas, screenCanvas, popupCanvas }
            .SelectMany(canvas => canvas.GetComponentsInChildren<UIView>(true))
            .ToArray();
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.Unregister(ownedViews);
    }
}
