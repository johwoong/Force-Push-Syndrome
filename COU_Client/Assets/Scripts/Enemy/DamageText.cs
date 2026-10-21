using UnityEngine;
using DG.Tweening;
using System;
using TMPro;

public class DamageText : MonoBehaviour, IPoolable
{
    public enum Style
    {
        FloatUp,
        PopUp
    }
    [SerializeField] private TextMeshPro text;
    [SerializeField] private string sortingLayerName = "Projectile";

    [Header("Motion")]
    [SerializeField] private float riseHeight = 0.8f;
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float randomX = 0.3f;

    // IPoolable : 풀로 돌아가기 직전에 알림 (스포너가 구독해서 사용)
    public event Action<GameObject> OnBeforeReturn;

    private Sequence sequence;
    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
        text.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
    }

    public void Play(int damage, Color color, Style style)
    {
        sequence?.Kill();               // 전에 남아있던 연출 정리
        text.text = damage.ToString();
        text.color = color;
        text.alpha = 1.0f;              // 이전 연출에서 투명해진 것 복구
        transform.localScale = baseScale;

        Vector3 start = transform.position + new Vector3(UnityEngine.Random.Range(-randomX, randomX), 0f, 0f);
        transform.position = start;

        sequence = DOTween.Sequence();
        switch(style)
        {
            // 위로 미끄러지듯 올라오고 사라짐
            case Style.FloatUp:
                sequence.Append(transform.DOMoveY(start.y + riseHeight, duration).SetEase(Ease.OutCubic));
                sequence.Insert(duration * 0.5f, FadeOut(duration * 0.5f));
                break;

            // 커졌다가 떠오르고 사라짐
            case Style.PopUp:
                transform.localScale = baseScale * 0.5f;
                sequence.Append(transform.DOScale(baseScale * 1.3f, duration * 0.2f).SetEase(Ease.OutBack));
                sequence.Append(transform.DOScale(baseScale, duration * 0.15f));
                sequence.Append(transform.DOMoveY(start.y + riseHeight, duration * 0.65f).SetEase(Ease.OutQuad));
                sequence.Join(FadeOut(duration * 0.65f));
                break;
        }

        // 끝나면 풀로 반납
        sequence.OnComplete(ReturnToPool);
    }

    private void ReturnToPool()
    {
        OnBeforeReturn?.Invoke(gameObject);
        GameManager.ObjectPool.ReturnObject(gameObject);
    }

    private Tween FadeOut(float time)
    {
        return DOTween.To(() => text.alpha, a => text.alpha = a, 0f, time);
    }

    // IPoolable: 풀에서 꺼내질 때마다 호출됨
    public void OnSpawn()
    {
        
    }

    // IPoolable: 풀로 돌아갈 때 정리(참조를 끊음)
    public void OnDespawn()
    {
        sequence?.Kill();
    }
    
    private void OnDisable()
    {
        sequence?.Kill();
    }
}
