using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

// 몬스터 "외형"만 담당하는 컴포넌트
public class EnemyVisual : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;          // 외형 프리팹을 붙일 곳
    [FormerlySerializedAs("legacyRenderer")]                // 프리팹 파일에 "legacyRenderer"로 저장된 값을 이 필드로 읽어 옴
    [SerializeField] private SpriteRenderer spriteRenderer; 
    [SerializeField] private bool assetFacesLeft = false;   // 에셋 원본이 왼쪽을 보면 체크
    [SerializeField] private int maxCachedVisuals = 3;      // 이 오브젝트가 들고 있을 외형 수 상한
    [SerializeField] private string sortingLayerName = "Character"; // 레이어 설정

    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    // 모든 몬스터가 함께 쓰는 static 캐시
    private static readonly Dictionary<string, GameObject> prefabCache = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => prefabCache.Clear();

    // 같은 외형을 다시 입을 때 Instantiate하지 않고 켜기만 함
    private readonly Dictionary<string, GameObject> owned = new();

    private GameObject current;                 // 지금 입고 있는 외형
    private Animator animator;
    private SpriteRenderer[] renderers = System.Array.Empty<SpriteRenderer>();
    private Color[] originalColors = System.Array.Empty<Color>();
    private Vector3 baseScale;

    public bool HasAnimator => animator != null;

    private void Awake()
    {
        if (visualRoot == null) visualRoot = transform;
        baseScale = visualRoot.localScale;
    }

    // Enemy.Init에서 호출((경로가 프리팹이면 프리팹/아니면 스프라이트로 처리)
    public void Apply(string assetPath)
    {
        if (current != null) current.SetActive(false);
        current = null;
        animator = null;

        GameObject prefab = LoadPrefab(assetPath);
        if (prefab != null)
        {
            if (!owned.TryGetValue(assetPath, out current))
            {
                // 여러 외형을 계속 쌓아 두면 메모리가 늘어나므로 상한을 넘으면 비움
                if (owned.Count >= maxCachedVisuals) ClearOwned();

                current = Instantiate(prefab, visualRoot);
                current.transform.localPosition = Vector3.zero;
                owned[assetPath] = current;

                SetupInstance(current);
            }

            current.SetActive(true);
            animator = current.GetComponent<Animator>();
            renderers = current.GetComponentsInChildren<SpriteRenderer>(true);
            if (spriteRenderer != null) spriteRenderer.enabled = false;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            Sprite sprite = Resources.Load<Sprite>(assetPath);
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
            else
            {
                Debug.LogWarning($"[EnemyVisual] 외형 에셋을 찾을 수 없습니다: {assetPath}");
            }
            renderers = new[] { spriteRenderer };
        }
        else
        {
            Debug.LogWarning($"[EnemyVisual] {name}: spriteRenderer가 연결되지 않아 외형을 적용할 수 없습니다 ({assetPath})", this);
        }

        CacheColors();
        visualRoot.localScale = baseScale;
        Play(WalkHash);
    }

    private void SetupInstance(GameObject instance)
    {
        // 정렬: 몬스터 전체를 Character 레이어의 한 덩어리로
        if (instance.TryGetComponent(out SortingGroup group))
        {
            group.sortingLayerName = sortingLayerName;
            group.sortingOrder = 0;   // 같은 레이어 안에서는 Y축 정렬로
        }

        // 몬스터가 다시 스폰될 때 유령으로 보이는 버그 수정
        if (instance.TryGetComponent(out Animator anim))
        {
            anim.writeDefaultValuesOnDisable = true;
        }
    }

    public void PlayWalk() => Play(WalkHash);
    public void PlayHit() => Play(HitHash);
    public void PlayDead() => Play(DeadHash);

    // 좌우반전
    public void FaceTo(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) { return; }

        bool lookLeft = dirX < 0f;
        Vector3 scale = baseScale;
        scale.x = Mathf.Abs(baseScale.x) * (lookLeft == assetFacesLeft ? 1f : -1f);
        visualRoot.localScale = scale;
    }

    // 보스 예고 깜빡임용: 조각 전부에 같은 색을 섞음(t: 0=원래 색, 1=경고 색)
    public void SetTint(Color warning, float t)
    {
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = Color.Lerp(originalColors[i], warning, t);
    }

    public void RestoreTint()
    {
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = originalColors[i];
    }

    private void Play(int hash)
    {
        if (animator != null) animator.Play(hash, 0, 0f);   // 0f: 처음부터 재생
    }

    private void CacheColors()
    {
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) originalColors[i] = renderers[i].color;
    }

    private void ClearOwned()
    {
        foreach (GameObject visual in owned.Values) Destroy(visual);
        owned.Clear();
    }

    private static GameObject LoadPrefab(string path)
    {
        if (string.IsNullOrEmpty(path)) { return null; }
        if (!prefabCache.TryGetValue(path, out GameObject prefab))
        {
            prefab = Resources.Load<GameObject>(path);   // 스프라이트 경로면 null
            prefabCache[path] = prefab;                  // null도 저장 → 다시 찾지 않음
        }
        return prefab;
    }
}
