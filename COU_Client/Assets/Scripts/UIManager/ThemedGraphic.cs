using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class ThemedGraphic : MonoBehaviour
{
    [SerializeField] private UIColor color;

    private Graphic graphic;
    private UIManager manager;

    private void Awake()
        => graphic = GetComponent<Graphic>();

    private void OnEnable()
    {
        manager = UIManager.Instance;
        manager.ThemeChanged += Apply;
        Apply();
    }

    private void OnDisable()
    {
        if (manager != null)
            manager.ThemeChanged -= Apply;
    }

    private void Apply()
    {
        var themed = UIPalette.Get(color);
        themed.a = graphic.color.a;
        graphic.color = themed;
    }
}
