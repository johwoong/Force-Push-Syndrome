using System;
using UnityEngine;

// 장비 등급 효과의 주기 회복: 정해진 간격마다 최종 최대 체력의 N% 회복 (받는 치유량 보정)
// 효과가 없으면(간격 0) 아무것도 하지 않는다
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerIntervalHeal : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private PlayerStats playerStats;

    private float healPercent;
    private float interval;
    private float timer;
    private bool isActive;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerStats = GetComponent<PlayerStats>();
    }

    private void OnEnable() => playerHealth.OnDied += HandleDied;
    private void OnDisable() => playerHealth.OnDied -= HandleDied;

    private void Start()
    {
        healPercent = playerStats.IntervalHealPercent;
        interval = playerStats.IntervalHealSeconds;
        isActive = healPercent > 0f && interval > 0f;

        if (isActive)
            Debug.Log("[PlayerIntervalHeal] " + interval + "초마다 최대 체력의 " + healPercent + "% 회복");
    }

    private void Update()
    {
        if (!isActive) return;

        // 일시정지(Time.timeScale = 0) 중에는 deltaTime이 0이라 멈춤
        timer += Time.deltaTime;
        if (timer < interval) return;
        timer -= interval;

        if (playerHealth.IsDead) return;
        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth) return;   // 가득 차 있으면 건너뜀

        // 최대 체력 × % ÷ 100 버림 → 받는 치유량 보정 → 최소 1
        int baseHeal = (int)Math.Floor((decimal)playerHealth.MaxHealth * (decimal)healPercent / 100m);
        int heal = PlayerDamageCalculator.ApplyHealReceived(baseHeal, playerStats.HealReceived);

        playerHealth.Heal(heal);
        Debug.Log("[PlayerIntervalHeal] 회복 " + heal + " (기본 " + baseHeal + ", 치유량 +" + playerStats.HealReceived + "%)");
    }

    private void HandleDied() => isActive = false;
}