using UnityEngine;
using UnityEngine.UI;

public class EvolutionNodeView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image border;
    [SerializeField] private Image inner;
    [SerializeField] private Image icon;
    [SerializeField] private Color unlockedBorderColor;
    [SerializeField] private Color unlockedInnerColor;
    [SerializeField] private UIColor lockedBorderColor = UIColor.Raised;
    [SerializeField] private UIColor lockedInnerColor = UIColor.Surface;
    [SerializeField] private UIColor lockedIconColor = UIColor.Raised;

    public Button Button => button;

    public void Set(EvolutionNodeInfo node, bool unlocked)
    {
        border.color = unlocked ? unlockedBorderColor : UIPalette.Get(lockedBorderColor);
        inner.color = unlocked ? unlockedInnerColor : UIPalette.Get(lockedInnerColor);

        if (node.icon != null)
            icon.sprite = node.icon;

        icon.color = unlocked ? node.iconColor : UIPalette.Get(lockedIconColor);
    }
}
