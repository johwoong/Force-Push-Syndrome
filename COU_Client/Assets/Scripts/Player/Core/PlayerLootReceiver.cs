using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerLuckTrain))]
public class PlayerLootReceiver : MonoBehaviour, ILootReceiver
{
    private PlayerHealth playerHealth;
    private PlayerStats playerStats;
    private PlayerLuckTrain luckTrain;

    // Potion, Magnet, Bomb을 제외한 아이템 획득 알림 (경험치, 골드, 상자)
    public event Action<DropItemType> OnLooted;
    
    // 종류와 상관없이 모든 아이템 획득 알림 (업적 집계)
    public event Action<DropItemType> OnPickedUp;

    public Vector2 Position => transform.position;
    public float LootRadius => playerStats.LootRadius;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerStats = GetComponent<PlayerStats>();
        luckTrain = GetComponent<PlayerLuckTrain>();
    }

    private void OnEnable()
    {
        GameManager.DropItem.RegisterReceiver(this);
        playerHealth.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        playerHealth.OnDied -= HandleDied;
    }

    public void OnLoot(DropItemType type)
    {
        OnPickedUp?.Invoke(type);
        
        switch (type)
        {
            case DropItemType.Potion:
                // 포션 회복 = (데이터 값 + 포션 회복량) × (100 + 받는 치유량) ÷ 100
                int potionBase = GameManager.JsonData.GetDropItemDataFromJson(DropItemType.Potion).value
                               + (playerStats != null ? playerStats.FinalPotionRecovery : 0);
                int potionHeal = PlayerDamageCalculator.ApplyHealReceived(potionBase, playerStats != null ? playerStats.HealReceived : 0);
                playerHealth.Heal(potionHeal);
                Debug.Log("[PlayerLootReceiver] 포션 회복 " + potionHeal + " (기본 " + potionBase + ", 치유량 +" +
                          (playerStats != null ? playerStats.HealReceived : 0) + "%)");
                break;

            case DropItemType.Magnet:
                GameManager.DropItem.AttractAllExpGems();
                break;

            case DropItemType.Bomb:
                ApplyBomb();
                break;

            case DropItemType.LuckyBox:
                luckTrain.Add();
                break;

            default:
                // ExpGem1~4, Gold1~4, RewardBox 및 이후 추가되는 아이템 → 배틀 매니저 등 구독자가 처리
                OnLooted?.Invoke(type);
                break;
        }
    }

    private void ApplyBomb()
    {
        var spawner = FindFirstObjectByType<MonsterSpawner>();
        if (spawner == null)
        {
            Debug.Log("[PlayerLootReceiver] 폭탄: 씬에 MonsterSpawner가 없어 효과 없음");
            return;
        }

        var targets = new List<Enemy>(spawner.SpawnedEnemies);
        int bombDamage = GameManager.JsonData.GetDropItemDataFromJson(DropItemType.Bomb).value;

        int killed = 0;
        int damaged = 0;

        foreach (var enemy in targets)
        {
            if (enemy == null || enemy.isDead) continue;

            if (enemy.Type == MonsterType.Normal)
            {
                enemy.Kill();   // 폭탄 처치는 스킬 처치로 집계하지 않는다
                killed++;
            }
            else
            {
                enemy.Damaged(bombDamage);
                damaged++;
            }
        }

        Debug.Log("[PlayerLootReceiver] 폭탄: 일반 몬스터 " + killed + "마리 처치, 엘리트·보스 " + damaged + "마리에게 " + bombDamage + " 피해");
    }

    private void HandleDied()
    {
        GameManager.DropItem.UnregisterReceiver(this);
    }

    // For Debug (루팅 범위 표시)
    private void OnDrawGizmos()
    {
        if (playerStats == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, playerStats.LootRadius);
    }

    // 전투 씬 테스트용: 아이템을 먹은 것처럼 효과만 실행
    [ContextMenu("TestBomb")]
    private void TestBomb() => OnLoot(DropItemType.Bomb);

    [ContextMenu("TestMagnet")]
    private void TestMagnet() => OnLoot(DropItemType.Magnet);
}