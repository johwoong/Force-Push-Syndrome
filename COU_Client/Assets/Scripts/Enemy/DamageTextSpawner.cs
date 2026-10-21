using UnityEngine;

public class DamageTextSpawner : MonoSingleton<DamageTextSpawner>
{
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private DamageText.Style style = DamageText.Style.FloatUp;
    [SerializeField] private Color normalColor = Color.darkRed;

    public void Show(int damage,Vector3 worldPosition)
    {
        if (damageTextPrefab == null || damage <= 0) { return; }

        GameObject obj = GameManager.ObjectPool.GetObject(damageTextPrefab, worldPosition, Quaternion.identity);
        obj.GetComponent<DamageText>().Play(damage, normalColor, style);
    }
}
