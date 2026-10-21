using UnityEngine;
using UnityEngine.UI;

// 로비 배경 패턴: Repeat 텍스처를 깐 RawImage의 uvRect를 밀어 무늬가 대각선으로 끝없이 흐르게 한다
[RequireComponent(typeof(RawImage))]
public class ScrollingPatternBackground : MonoBehaviour
{
    [SerializeField] private float tileSize = 400f;                     // 타일 한 장의 캔버스 크기
    [SerializeField] private Vector2 speed = new Vector2(0.05f, 0.05f); // 초당 이동량 (타일 비율, +면 오른쪽 위)
    [SerializeField] private float darkAlpha = 0.05f;   // 테마별 무늬 진하기 (리니어 블렌딩이라 밝은 배경은 훨씬 높아야 비슷하게 보인다)
    [SerializeField] private float lightAlpha = 0.3f;

    private RawImage image;
    private RectTransform rect;
    private UIManager manager;

    private void Awake()
    {
        image = GetComponent<RawImage>();
        rect = (RectTransform)transform;
    }

    private void OnEnable()
    {
        manager = UIManager.Instance;
        manager.ThemeChanged += ApplyAlpha;
        ApplyAlpha();
    }

    private void OnDisable()
    {
        if (manager != null)
            manager.ThemeChanged -= ApplyAlpha;
    }

    // 색(RGB)은 ThemedGraphic이 팔레트로 칠하고, 여기서는 알파만 테마에 맞춘다
    private void ApplyAlpha()
    {
        var color = image.color;
        color.a = manager.Theme == UITheme.Light ? lightAlpha : darkAlpha;
        image.color = color;
    }

    private void Update()
    {
        Rect uv = image.uvRect;
        uv.size = rect.rect.size / tileSize;
        uv.position = new Vector2(
            Mathf.Repeat(uv.x - speed.x * Time.deltaTime, 1f),
            Mathf.Repeat(uv.y - speed.y * Time.deltaTime, 1f));
        image.uvRect = uv;
    }
}
