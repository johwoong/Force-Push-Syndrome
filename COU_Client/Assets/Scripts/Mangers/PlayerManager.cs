using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerVisual))]
[RequireComponent(typeof(PlayerLootReceiver))]
[RequireComponent(typeof(SkillController))]
[RequireComponent(typeof(PlayerLuckTrain))]
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public PlayerMovement Movement { get; private set; }
    public PlayerHealth Health { get; private set; }
    public PlayerStats Stats { get; private set; }
    public PlayerVisual Visual { get; private set; }
    public PlayerLootReceiver Loot { get; private set; }
    public SkillController Skills { get; private set; }
    public PlayerLuckTrain LuckTrain { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerManager] 씬에 PlayerManager가 두 개 이상 있습니다.", this);
            return;
        }

        Instance = this;

        Movement = GetComponent<PlayerMovement>();
        Health = GetComponent<PlayerHealth>();
        Stats = GetComponent<PlayerStats>();
        Visual = GetComponent<PlayerVisual>();
        Loot = GetComponent<PlayerLootReceiver>();
        Skills = GetComponent<SkillController>();
        LuckTrain = GetComponent<PlayerLuckTrain>();
    }

    private void Start()
    {
        if (Instance != this) return;
        EquipStartingSkill();
    }

    private void OnEnable()
    {
        if (Health != null)
            Health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (Health != null)
            Health.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (Skills != null)
            Skills.enabled = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void EquipStartingSkill()
    {
        long skillId = Stats.StartingSkillId;

        if (skillId == WeaponSkillTable.NoSkill)
        {
            Debug.Log("[PlayerManager] 시작 스킬 없음 (장착 무기 없음 또는 매핑 없음)");
            return;
        }

        foreach (var skill in Skills.ActiveSkills)
        {
            if (skill.SkillId == skillId)
            {
                Debug.LogWarning("[PlayerManager] 시작 스킬 " + skillId + "이 이미 등록되어 있습니다. 배틀 매니저의 시작 스킬 하드코딩이 남아 있는지 확인하세요.");
                return;
            }
        }

        bool result = Skills.EquipSkill(skillId);
        Debug.Log("[PlayerManager] 시작 스킬 " + skillId + " 등록: " + (result ? "성공" : "실패"));

        if (result && Visual != null)
        {
            var weaponSkill = System.Linq.Enumerable.FirstOrDefault(Skills.ActiveSkills, s => s.SkillId == skillId);
            if (weaponSkill != null)
                Visual.ApplyWeaponSkillTiming(weaponSkill);
        }
    }
}