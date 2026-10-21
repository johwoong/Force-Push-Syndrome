using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;

// 전투력 표시: 텍스트에 붙이면 장비·진화 변경 시 스스로 갱신
[RequireComponent(typeof(TMP_Text))]
public class CombatPowerView : MonoBehaviour
{
    [SerializeField][TextArea] private string format = "전투력\n{0}";
    [SerializeField] private Color increaseColor = new(0.55f, 0.9f, 0.4f);
    [SerializeField] private Color decreaseColor = new(1f, 0.4f, 0.4f);

    private TMP_Text label;
    private int power = -1;   // 표시 중인 전투력. -1은 아직 숫자를 표시한 적 없음
    private bool isInventorySubscribed;
    private bool isPlayerDataSubscribed;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged += Refresh;
            isInventorySubscribed = true;
        }

        if (GameManager.PlayerData != null)
        {
            GameManager.PlayerData.OnPlayerDataChanged += OnPlayerDataChanged;
            isPlayerDataSubscribed = true;
        }

        Refresh();
    }

    private void OnEnable()
    {
        if (label != null) Refresh();   // 닫혀 있는 동안 바뀐 값 반영
    }

    private void OnDestroy()
    {
        if (isInventorySubscribed && PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged -= Refresh;

        if (isPlayerDataSubscribed && PlayerDataManager.HasInstance)
            GameManager.PlayerData.OnPlayerDataChanged -= OnPlayerDataChanged;
    }

    private void OnPlayerDataChanged(PlayerSaveData _) => Refresh();

    private void Refresh()
    {
        label.DOKill();   // 굴리던 숫자를 멈추면 OnKill에서 원래 색으로 돌아온다

        if (GameManager.PlayerData == null || !GameManager.PlayerData.isPlayerDataLoaded)
        {
            label.text = string.Format(format, "-");
            power = -1;
            return;
        }

        var stat = PlayerStatCalculator.CalculateCurrent();

        if (!stat.HasCombatPowerData)
        {
            label.text = string.Format(format, "-");
            power = -1;
            Debug.LogWarning("[CombatPowerView] 전투력 데이터가 없습니다.", this);
            return;
        }

        // 바뀐 만큼 굴려서 보여주고, 도는 동안 오르면 초록·내리면 빨강으로 표시한다
        var next = stat.CombatPower;
        var baseColor = label.color;
        var count = UITween.Count(label, power, next, FormatPower);
        if (count != null)
        {
            label.color = next > power ? increaseColor : decreaseColor;
            count.OnKill(() => label.color = baseColor);
            UITween.Punch(label.transform);
        }
        power = next;
    }

    // 천 단위 쉼표 (2,325)
    private string FormatPower(int value)
        => string.Format(format, value.ToString("N0", CultureInfo.InvariantCulture));
}