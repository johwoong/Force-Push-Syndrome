using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EvolutionLevelMark : MonoBehaviour
{
    [SerializeField] private Image inner;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Color reachedColor;
    [SerializeField] private Color unreachedColor;

    public void Set(int level, bool reached)
    {
        levelText.text = level.ToString();
        inner.color = reached ? reachedColor : unreachedColor;
    }
}
