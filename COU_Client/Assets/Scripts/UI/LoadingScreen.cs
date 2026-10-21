using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 씬 이동 로딩 화면. UIManager 프리팹의 OverlayCanvas 안 LoadingScreen 오브젝트에 붙인다 (씬이 바뀌어도 유지)
// 가운데에 플레이어가 제자리에서 달리고, 아래에 "Loading..." 점 애니메이션이 돈다
// UIView를 상속하지 않는다: CloseAll()에 같이 닫히면 안 되기 때문
[RequireComponent(typeof(CanvasGroup))]
public class LoadingScreen : MonoBehaviour
{
    private const string LoadingLabel = "Loading";
    private const float DotInterval = 0.4f;
    private const float FadeDuration = 0.25f;
    
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private RawImage runnerImage;          // 가운데 캐릭터 표시
    [SerializeField] private PlayerPreview runnerPrefab;    // Prefabs/Player/PlayerPreview
    [SerializeField] private Vector3 runnerSpawnPosition = new(-10000f, 0f, 0f);   // 메인 카메라·장비창 미리보기(10000)와 겹치지 않는 먼 위치
    
    private CanvasGroup canvasGroup;
    private PlayerPreview runner;
    private Coroutine dotLoop;
    private float shownAt;

    public bool IsShown { get; private set; }
    public float ShownSeconds => Time.unscaledTime - shownAt;
    
    // 로그인 -> 데이터 로드 -> 씬 이동처럼 이어서 불려도 처음 한 번만 연다
    public void Show()
    {
        if (IsShown) return;
        IsShown = true;
        shownAt = Time.unscaledTime;
        
        gameObject.SetActive(true);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.DOKill();
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, FadeDuration).AsUI(canvasGroup);

        StartRunner();
        
        if (dotLoop != null) StopCoroutine(dotLoop);
        dotLoop = StartCoroutine(DotLoop());
    }
    
    public void Hide()
    {
        if (!IsShown) return;
        IsShown = false;

        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, FadeDuration).AsUI(canvasGroup).OnComplete(() =>
        {
            if (dotLoop != null)
            {
                StopCoroutine(dotLoop);
                dotLoop = null;
            }

            // 캐릭터 오브젝트는 끄지 않고 카메라만 끈다
            // (오브젝트를 끄면 Animator가 초기화되어 다음 로딩 때 달리지 않을 수 있음)
            if (runner != null)
                runner.SetVisible(false);

            gameObject.SetActive(false);
        });
    }
    
    private void StartRunner()
    {
        if (runnerPrefab == null) return;

        if (runner == null)
        {
            runner = Instantiate(runnerPrefab, runnerSpawnPosition, Quaternion.identity);
            DontDestroyOnLoad(runner.gameObject);
            runnerImage.texture = runner.Texture;
        }

        // 장착 무기가 있으면 들고 달린다 (로그인 직후에는 인벤토리가 없어 맨손)
        var weapon = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.GetEquipped(EquipSlotType.Weapon)
            : null;

        runner.SetWeapon(weapon != null ? weapon.itemId : (long?)null);   // SetWeapon이 Idle을 재생하므로
        runner.PlayMove();                                                // 그 다음 Move → 매번 처음부터 달린다
        runner.SetVisible(true);
    }

    // Loading → Loading. → Loading.. → Loading... 반복
    private IEnumerator DotLoop()
    {
        var wait = new WaitForSecondsRealtime(DotInterval);
        int dots = 0;

        while (true)
        {
            loadingText.text = LoadingLabel + new string('.', dots);
            dots = (dots + 1) % 4;
            yield return wait;
        }
    }
}
