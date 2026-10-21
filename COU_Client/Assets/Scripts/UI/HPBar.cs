using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    [SerializeField] private Image fill;

    // 줄어들면(피격) 바가 한 번 튕긴다
    public void SetRatio(float ratio)
    {
        if (ratio < fill.fillAmount)
            UITween.Punch(transform, 0.15f);
        UITween.Fill(fill, ratio, false, 0.2f);
    }
}
