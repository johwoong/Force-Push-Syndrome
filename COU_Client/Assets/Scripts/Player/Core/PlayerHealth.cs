using System;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private HPBar hpBar;

    private PlayerStats playerStats;

    private int maxHealth;
    public int MaxHealth => maxHealth;

    [SerializeField][Min(1)] private int maxLives = 2;
    public int MaxLives => maxLives;

    [SerializeField][Min(0f)] private float invulnerableTime = 0.5f;
    private float invulnerableEndTime;
    public bool IsInvulnerable => Time.time < invulnerableEndTime;

    public int CurrentHealth { get; private set; }
    public int CurrentLives { get; private set; }

    public event Action OnDied;

    public bool IsDead => CurrentLives <= 0;

    public event Action OnDamaged;
    public event Action OnDodged;          // 회피 성공 (피해 0, 피격 모션 없음)
    public event Action OnDamageNegated;   // 피해 무효 발동 (피해 0, 피격 모션 없음)
    public event Action<float> OnInvulnerableStarted;

    [Header("Test")]
    [SerializeField] private int testDamage = 50;
    [SerializeField] private int testHeal = 20;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        CurrentLives = maxLives;
    }

    private void Start()
    {
        maxHealth = playerStats.FinalHp;
        CurrentHealth = maxHealth;
        UpdateHpBar();
    }

    public void GetDamage(int damage)
    {
        if (damage <= 0 || IsDead) return;
        if (IsInvulnerable)
        {
            Debug.Log("무적 중 피해무시");
            return;
        }

        // 회피 → 피해 무효 → 방어력 → 받는 피해 감소 → 최소 1 (모든 받는 피해가 여기로 들어옴)
        var received = PlayerDamageCalculator.CalculateReceived(damage, playerStats);

        if (received.isDodged)
        {
            Debug.Log("회피: 피해 " + damage + " 무시");
            OnDodged?.Invoke();
            return;
        }

        if (received.isNegated)
        {
            Debug.Log("피해 무효: 피해 " + damage + " 무시");
            OnDamageNegated?.Invoke();
            return;
        }

        int finalDamage = received.damage;

        OnDamaged?.Invoke();

        if (CurrentHealth - finalDamage <= 0)
        {
            CurrentHealth = 0;
            DecreaseLife();
        }
        else
            CurrentHealth -= finalDamage;

        UpdateHpBar();
        Debug.Log("피해 " + damage + " → " + finalDamage +
                  " (방어력 " + playerStats.FinalDefense + ", 감소 " + playerStats.DamageReduction + "%)" +
                  ", 현재 체력 : " + CurrentHealth + ", 현재 목숨 : " + CurrentLives);
    }

    public void Heal(int heal)
    {
        if (heal <= 0 || IsDead) return;

        if (CurrentHealth + heal >= maxHealth)
            CurrentHealth = maxHealth;
        else
            CurrentHealth += heal;

        UpdateHpBar();
        Debug.Log("현재 체력 : " + CurrentHealth);
    }

    private void UpdateHpBar()
        => hpBar.SetRatio(maxHealth > 0 ? (float)CurrentHealth / maxHealth : 0f);

    private void DecreaseLife()
    {
        CurrentLives--;

        if (IsDead)
        {
            Debug.Log("플레이어 사망");
            OnDied?.Invoke();
        }
        else
        {
            CurrentHealth = maxHealth;
            invulnerableEndTime = Time.time + invulnerableTime;
            OnInvulnerableStarted?.Invoke(invulnerableTime);
        }
    }

    public void IncreaseLife()
    {
        if (CurrentLives >= maxLives || IsDead)
            return;

        CurrentLives++;
        Debug.Log("현재 목숨 : " + CurrentLives);
    }

    // For Debug (무적일 때 플레이어 주위에 원 표시)
    private void OnDrawGizmos()
    {
        if (IsInvulnerable)
            Gizmos.DrawWireSphere(transform.position, 1);
    }

    [ContextMenu("TestDamage")]
    private void TestGetDamage()
    {
        GetDamage(testDamage);
    }

    [ContextMenu("TestHeal")]
    private void TestHeal()
    {
        Heal(testHeal);
    }

    [ContextMenu("TestIncreaseLife")]
    private void TestIncreaseLife()
    {
        IncreaseLife();
    }

    [ContextMenu("Test/받는 피해 판정 100회 (피해 100, 체력 변화 없음)")]
    private void TestReceivedDamage()
    {
        int dodged = 0, negated = 0, hits = 0, total = 0;
        for (int i = 0; i < 100; i++)
        {
            var r = PlayerDamageCalculator.CalculateReceived(100, playerStats);
            if (r.isDodged) dodged++;
            else if (r.isNegated) negated++;
            else { hits++; total += r.damage; }
        }

        Debug.Log("[PlayerHealth] 회피 " + dodged + "회, 무효 " + negated + "회, 피격 " + hits + "회" +
                  (hits > 0 ? " (피격 1회 피해 " + (total / hits) + ")" : "") +
                  " / 회피 " + playerStats.DodgeChance + "%, 무효 " + playerStats.NegateDamageChance + "%" +
                  ", 방어력 " + playerStats.FinalDefense + ", 감소 " + playerStats.DamageReduction + "%");
    }
}