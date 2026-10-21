using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;
using UnityEngine.Networking;
using UnityEngine.U2D;
using UnityEngine.UIElements;
using static Unity.Collections.AllocatorManager;
using static UnityEditor.UIElements.ToolbarMenu;
using static UnityEngine.Rendering.DebugUI;
using static UnityEngine.Rendering.DebugUI.Table;


public class JsonDataManager : MonoSingleton<JsonDataManager>
{
    #region Fields

    private Dictionary<int, MonsterData> monsterDataDic;
    public IReadOnlyDictionary<int, MonsterData> MonsterDataDic => monsterDataDic;

    private Dictionary<int, MonsterAttackData> monsterAttackDataDic;
    public IReadOnlyDictionary<int, MonsterAttackData> MonsterAttackDataDic => monsterAttackDataDic;

    private Dictionary<int, List<MonsterAttackData>> monsterAttacksByMonsterId = new();

    private static readonly List<MonsterAttackData> EmptyAttacks = new();

    private Dictionary<int, WaveEntryData> waveEntryDataDic;
    public IReadOnlyDictionary<int, WaveEntryData> WaveEntryDataDic => waveEntryDataDic;

    private Dictionary<int, SpawnPatternData> spawnPatternDataDic;
    public IReadOnlyDictionary<int, SpawnPatternData> SpawnPatternDataDic => spawnPatternDataDic;

    private Dictionary<MonsterType, List<DropTableEntryData>> dropTableDic;
    public IReadOnlyDictionary<MonsterType, List<DropTableEntryData>> DropTableDic => dropTableDic;

    private Dictionary<int, StageData> stageDataDic;
    public IReadOnlyDictionary<int, StageData> StageDataDic => stageDataDic;

    private Dictionary<int, List<StageRosterData>> stageRosterDic;
    public IReadOnlyDictionary<int, List<StageRosterData>> StageRosterDic => stageRosterDic;

    private Dictionary<long, SkillData> skillDataDic;
    public IReadOnlyDictionary<long, SkillData> SkillDataDic => skillDataDic;

    // 스킬 ID별 레벨 추가 효과 (레벨 오름차순)
    private Dictionary<long, List<SkillLevelData>> skillLevelDic;
    public IReadOnlyDictionary<long, List<SkillLevelData>> SkillLevelDic => skillLevelDic;

    private static readonly List<SkillLevelData> EmptySkillLevels = new();

    private Dictionary<DropItemType, DropItemData> dropItemDataDic;
    public IReadOnlyDictionary<DropItemType, DropItemData> DropItemDataDic => dropItemDataDic;

    private AccountConstData accountConstData;
    public AccountConstData AccountConstData => accountConstData;

    private Dictionary<long, ItemData> itemDataDic;
    public IReadOnlyDictionary<long, ItemData> ItemDataDic => itemDataDic;

    private Dictionary<long, EquipEffectData> equipEffectDataDic;
    public IReadOnlyDictionary<long, EquipEffectData> EquipEffectDataDic => equipEffectDataDic;

    private List<ItemGradeEffectData> itemGradeEffectRows;
    public IReadOnlyList<ItemGradeEffectData> ItemGradeEffectRows => itemGradeEffectRows;

    private Dictionary<long, Dictionary<ItemGrade, List<EquipEffectData>>> itemGradeEffectsByItemId = new();

    private static readonly IReadOnlyList<EquipEffectData> EmptyEquipEffects = Array.Empty<EquipEffectData>();

    private PlayerBaseStatData playerBaseStatData;
    public PlayerBaseStatData PlayerBaseStatData => playerBaseStatData;

    private ItemConstData itemConstData;
    public ItemConstData ItemConstData => itemConstData;

    private EvolutionData evolutionData;
    public EvolutionData EvolutionData => evolutionData;
    
    private Dictionary<int, AchievementData> achievementDataDic;
    public IReadOnlyDictionary<int, AchievementData> AchievementDataDic => achievementDataDic;
    

    private CombatPowerData combatPowerData;
    public CombatPowerData CombatPowerData => combatPowerData;
    #endregion

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this) return;

        monsterDataDic = LoadTable(GameConstants.Paths.MonsterData_Json_Path, ParseMonsterData);
        monsterAttackDataDic = LoadTable(GameConstants.Paths.MonsterAttackData_Json_Path, ParseMonsterAttackData);
        waveEntryDataDic = LoadTable(GameConstants.Paths.WaveData_Json_Path, ParseWaveData);
        spawnPatternDataDic = LoadTable(GameConstants.Paths.SpawnPatternData_Json_Path, ParseSpawnPatternData);
        stageDataDic = LoadTable(GameConstants.Paths.StageData_Json_Path, ParseStageData);
        stageRosterDic = LoadTable(GameConstants.Paths.StageRosterData_Json_Path, ParseStageRosterData);
        skillDataDic = LoadTable(GameConstants.Paths.SkillData_Json_Path, ParseSkillData);
        skillLevelDic = LoadTable(GameConstants.Paths.SkillLevelData_Json_Path, ParseSkillLevelData);
        dropItemDataDic = LoadTable(GameConstants.Paths.DropItemData_Json_Path, ParseDropItemData);
        dropTableDic = LoadTable(GameConstants.Paths.DropTableData_Json_Path, ParseDropTableData);
        accountConstData = LoadTable(GameConstants.Paths.AccountConstData_Json_Path, ParseAccountConstData);
        itemDataDic = LoadTable(GameConstants.Paths.ItemData_Json_Path, ParseItemData);
        equipEffectDataDic = LoadTable(GameConstants.Paths.EquipEffectData_Json_Path, ParseEquipEffectData);
        itemGradeEffectRows = LoadTable(GameConstants.Paths.ItemGradeEffectData_Json_Path, ParseItemGradeEffectData);
        playerBaseStatData = LoadTable(GameConstants.Paths.PlayerBaseStatData_Json_Path, ParsePlayerBaseStatData);
        playerBaseStatData = LoadTable(GameConstants.Paths.PlayerBaseStatData_Json_Path, ParsePlayerBaseStatData);
        itemConstData = LoadTable(GameConstants.Paths.ItemConstData_Json_Path, ParseItemConstData);
        evolutionData = LoadTable(GameConstants.Paths.EvolutionData_Json_Path, ParseEvolutionData);
        achievementDataDic = LoadTable(GameConstants.Paths.AchievementData_Json_Path, ParseAchievementData);
        combatPowerData = LoadTable(GameConstants.Paths.CombatPowerData_Json_Path, ParseCombatPowerData);

        try
        {
            itemGradeEffectsByItemId = BuildItemGradeEffectIndex(itemDataDic, equipEffectDataDic, itemGradeEffectRows);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"장비 등급 효과 인덱스 생성 실패: {exception.Message}");
        }

        RebuildMonsterAttackIndex();
    }

    // 로그인 전에 서버 정적 데이터를 확인한다. 요청이 실패하면 가진 데이터를 유지한다
    // onComplete: 앱 업데이트가 필요하면 true
    public IEnumerator FetchStaticData(Action<bool> onComplete)
    {
        string path = GameConstants.Server.STATIC_DATA_API;
        string savedVersion = LoadUsableSavedVersion();
        if (savedVersion != null)
            path += "?version=" + UnityWebRequest.EscapeURL(savedVersion);

        bool appUpdateRequired = false;

        // 인증이 필요 없는 API라 토큰을 붙이지 않는다
        yield return ApiClient.Get<JObject>(path, result =>
        {
            // 204면 가진 데이터가 최신이다
            if (!result.IsSuccess)
                Debug.LogWarning($"정적 데이터 요청 실패, 가진 데이터를 유지합니다: {result.Describe()}");
            else if (result.StatusCode == 200)
                appUpdateRequired = ApplyStaticDataResponse(result.Data);
        }, auth: false);

        onComplete?.Invoke(appUpdateRequired);
    }

    // 200 응답을 적용한다. 버전 첫째 자리가 빌드 값과 다르면 받은 데이터를 쓰지 않고 true(앱 업데이트 필요)를 반환한다
    private bool ApplyStaticDataResponse(JObject body)
    {
        try
        {
            string version = body.Value<string>("version");

            if (!TryGetMajorVersion(version, out int major))
                throw new InvalidOperationException($"버전 형식이 올바르지 않습니다: {version}");

            if (major != GameConstants.Server.STATIC_DATA_MAJOR_VERSION)
                return true;

            JObject tables = body["tables"] as JObject
                ?? throw new InvalidOperationException("tables가 없습니다.");

            // 모두 파싱한 뒤 저장하고 한 번에 교체한다. 응답에 없는 테이블(null)은 가진 데이터를 유지한다
            var monsters = ParseResponseTable(tables, GameConstants.Paths.MonsterData_Json_Path, ParseMonsterData);
            var monsterAttacks = ParseResponseTable(tables, GameConstants.Paths.MonsterAttackData_Json_Path, ParseMonsterAttackData);
            var waveEntries = ParseResponseTable(tables, GameConstants.Paths.WaveData_Json_Path, ParseWaveData);
            var spawnPatterns = ParseResponseTable(tables, GameConstants.Paths.SpawnPatternData_Json_Path, ParseSpawnPatternData);
            var stages = ParseResponseTable(tables, GameConstants.Paths.StageData_Json_Path, ParseStageData);
            var stageRosters = ParseResponseTable(tables, GameConstants.Paths.StageRosterData_Json_Path, ParseStageRosterData);
            var skills = ParseResponseTable(tables, GameConstants.Paths.SkillData_Json_Path, ParseSkillData);
            var skillLevels = ParseResponseTable(tables, GameConstants.Paths.SkillLevelData_Json_Path, ParseSkillLevelData);
            var dropItems = ParseResponseTable(tables, GameConstants.Paths.DropItemData_Json_Path, ParseDropItemData);
            var dropTables = ParseResponseTable(tables, GameConstants.Paths.DropTableData_Json_Path, ParseDropTableData);
            var accountConst = ParseResponseTable(tables, GameConstants.Paths.AccountConstData_Json_Path, ParseAccountConstData);
            var items = ParseResponseTable(tables, GameConstants.Paths.ItemData_Json_Path, ParseItemData);
            var equipEffects = ParseResponseTable(tables, GameConstants.Paths.EquipEffectData_Json_Path, ParseEquipEffectData);
            var itemGradeEffects = ParseResponseTable(tables, GameConstants.Paths.ItemGradeEffectData_Json_Path, ParseItemGradeEffectData);
            var playerBaseStat = ParseResponseTable(tables, GameConstants.Paths.PlayerBaseStatData_Json_Path, ParsePlayerBaseStatData);
            var itemConst = ParseResponseTable(tables, GameConstants.Paths.ItemConstData_Json_Path, ParseItemConstData);
            var evolution = ParseResponseTable(tables, GameConstants.Paths.EvolutionData_Json_Path, ParseEvolutionData);
            var achievements = ParseResponseTable(tables, GameConstants.Paths.AchievementData_Json_Path, ParseAchievementData);
            var combatPower = ParseResponseTable(tables, GameConstants.Paths.CombatPowerData_Json_Path, ParseCombatPowerData);
            var gradeEffectIndex = BuildItemGradeEffectIndex(items ?? itemDataDic, equipEffects ?? equipEffectDataDic, itemGradeEffects ?? itemGradeEffectRows);

            SaveLoadHelper.SaveStaticData(version, tables);

            monsterDataDic = monsters ?? monsterDataDic;
            monsterAttackDataDic = monsterAttacks ?? monsterAttackDataDic;
            waveEntryDataDic = waveEntries ?? waveEntryDataDic;
            spawnPatternDataDic = spawnPatterns ?? spawnPatternDataDic;
            stageDataDic = stages ?? stageDataDic;
            stageRosterDic = stageRosters ?? stageRosterDic;
            skillDataDic = skills ?? skillDataDic;
            skillLevelDic = skillLevels ?? skillLevelDic;
            dropItemDataDic = dropItems ?? dropItemDataDic;
            dropTableDic = dropTables ?? dropTableDic;
            accountConstData = accountConst ?? accountConstData;
            itemDataDic = items ?? itemDataDic;
            playerBaseStatData = playerBaseStat ?? playerBaseStatData;
            itemConstData = itemConst ?? itemConstData;
            equipEffectDataDic = equipEffects ?? equipEffectDataDic;
            itemGradeEffectRows = itemGradeEffects ?? itemGradeEffectRows;
            itemGradeEffectsByItemId = gradeEffectIndex;
            evolutionData = evolution ?? evolutionData;
            achievementDataDic = achievements ?? achievementDataDic;
            combatPowerData = combatPower ?? combatPowerData;

            if (monsterAttacks != null)
                RebuildMonsterAttackIndex();

            Debug.Log($"정적 데이터를 {version}(으)로 갱신했습니다.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"정적 데이터 갱신 실패, 가진 데이터를 유지합니다: {exception.Message}");
        }

        return false;
    }

    // 응답에 테이블이 없으면 null. 파싱에 실패하면 테이블 이름을 붙여 예외를 던진다
    private static T ParseResponseTable<T>(JObject tables, string resourcePath, Func<JObject, T> parse) where T : class
    {
        string tableName = Path.GetFileName(resourcePath);
        JToken table = tables[tableName];
        if (table == null)
            return null;

        try
        {
            return parse((JObject)table);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"{tableName}: {exception.Message}", exception);
        }
    }

    // 저장본 버전. 없거나 첫째 자리가 빌드 값과 다르면(앱 업데이트 직후) 저장본을 쓰지 않으므로 null
    private static string LoadUsableSavedVersion()
    {
        string version = SaveLoadHelper.LoadStaticDataVersion();
        return TryGetMajorVersion(version, out int major) && major == GameConstants.Server.STATIC_DATA_MAJOR_VERSION
            ? version
            : null;
    }

    // 버전 문자열의 첫째 자리. 자리는 점으로 나뉜다
    private static bool TryGetMajorVersion(string version, out int major)
    {
        major = 0;
        return !string.IsNullOrEmpty(version) && int.TryParse(version.Split('.')[0], out major);
    }


    // 장비 효과 데이터 규칙 검증
    private static void ValidateEquipEffect(EquipEffectData data)
    {
        // NaN, Infinity 방지
        if (float.IsNaN(data.effectValue)
            || float.IsInfinity(data.effectValue)
            || float.IsNaN(data.triggerChance)
            || float.IsInfinity(data.triggerChance)
            || float.IsNaN(data.triggerInterval)
            || float.IsInfinity(data.triggerInterval)
            || float.IsNaN(data.actionValue)
            || float.IsInfinity(data.actionValue))
        {
            throw new InvalidOperationException(
                $"EquipEffect {data.effectId}: 잘못된 숫자입니다.");
        }

        // 79xxx: 제외 효과
        if (data.IsExcluded)
        {
            if (data.Category != EquipEffectCategory.None)
                throw new InvalidOperationException(
                    $"EquipEffect {data.effectId}: " +
                    "제외 효과는 Category가 None이어야 합니다.");

            return;
        }

        // 제외 효과가 아닌데 Category가 None인 경우
        if (data.Category == EquipEffectCategory.None)
        {
            throw new InvalidOperationException(
                $"EquipEffect {data.effectId}: " +
                "유효한 효과 카테고리가 필요합니다.");
        }

        // Stat형: statType과 valueType 조합 검사
        if (data.Category == EquipEffectCategory.Stat)
        {
            bool percent =
                data.StatType == EquipEffectStatType.HP
                || data.StatType == EquipEffectStatType.ATTACK
                || data.StatType == EquipEffectStatType.MOVE_SPEED;

            bool add =
                data.StatType == EquipEffectStatType.DEFENSE
                || data.StatType == EquipEffectStatType.CRITICAL_CHANCE
                || data.StatType == EquipEffectStatType.CRITICAL_DAMAGE
                || data.StatType == EquipEffectStatType.DODGE_CHANCE
                || data.StatType == EquipEffectStatType.DAMAGE_REDUCTION
                || data.StatType == EquipEffectStatType.HEAL_RECEIVED;

            if (!(percent || add)
                || (percent && data.ValueType
                    != EquipEffectValueType.Percent)
                || (add && data.ValueType
                    != EquipEffectValueType.Add))
            {
                throw new InvalidOperationException(
                    $"EquipEffect {data.effectId}: " +
                    "잘못된 statType/valueType 조합입니다.");
            }

            // HP, ATTACK, DEFENSE는 정수만 허용
            if ((data.StatType == EquipEffectStatType.HP
                || data.StatType == EquipEffectStatType.ATTACK
                || data.StatType == EquipEffectStatType.DEFENSE)
                && data.effectValue != Math.Truncate(data.effectValue))
            {
                throw new InvalidOperationException(
                    $"EquipEffect {data.effectId}: " +
                    "HP/ATTACK/DEFENSE 값은 정수여야 합니다.");
            }
        }

        // Trigger형: 발동 조건과 실행 동작 필수
        else if (data.Category == EquipEffectCategory.Trigger)
        {
            if (data.TriggerType == EquipEffectTriggerType.None
                || data.ActionType == EquipEffectActionType.None)
            {
                throw new InvalidOperationException(
                    $"EquipEffect {data.effectId}: " +
                    "Trigger 설정이 비어 있습니다.");
            }
        }

        // Buff, Conditional은 이번 단계에서 계산하지 않음
    }


    // itemId -> grade -> EquipEffectData 목록 생성
    private static Dictionary<long, Dictionary<ItemGrade, 
        List<EquipEffectData>>> BuildItemGradeEffectIndex(Dictionary<long, ItemData> items, Dictionary<long, EquipEffectData> effects, List<ItemGradeEffectData> rows)
    {
        if (items == null || effects == null || rows == null)
        {
            throw new InvalidOperationException(
                "Item, EquipEffect 또는 ItemGradeEffect가 로드되지 않았습니다.");
        }

        var index = new Dictionary<long,
            Dictionary<ItemGrade, List<EquipEffectData>>>();

        foreach (ItemGradeEffectData row in rows)
        {
            // Item 테이블 참조 검사
            if (!items.ContainsKey(row.itemId))
            {
                throw new InvalidOperationException(
                    $"ItemGradeEffect {row.itemGradeEffectId}: " +
                    $"존재하지 않는 itemId {row.itemId}");
            }

            // EquipEffect 테이블 참조 검사
            if (!effects.TryGetValue(row.effectId, out var effect))
            {
                throw new InvalidOperationException(
                    $"ItemGradeEffect {row.itemGradeEffectId}: " +
                    $"존재하지 않는 effectId {row.effectId}");
            }

            // 아이템 인덱스 생성
            if (!index.TryGetValue(row.itemId, out var grades))
            {
                grades = new Dictionary<ItemGrade,
                    List<EquipEffectData>>();

                index.Add(row.itemId, grades);
            }

            // 등급별 인덱스 생성
            if (!grades.TryGetValue(row.Grade, out var gradeEffects))
            {
                gradeEffects = new List<EquipEffectData>();
                grades.Add(row.Grade, gradeEffects);
            }

            // 원본 순서대로 효과 추가
            gradeEffects.Add(effect);
        }

        return index;
    }


    #region GetMethod

    public MonsterData GetMonsterDataFromJson(int monsterId)
    {
        if (monsterDataDic == null)
        {
            Debug.LogError("몬스터 데이터가 초기화되지 않았습니다.");
            return null;
        }

        if (monsterDataDic.TryGetValue(monsterId, out MonsterData data))
            return data;

        Debug.LogWarning($"등록되지 않은 몬스터 ID: {monsterId}");
        return null;
    }

    // 공격이 없는 몬스터는 빈 목록(null 체크 불필요)
    public IReadOnlyList<MonsterAttackData> GetMonsterAttacks(int monsterId)
        => monsterAttacksByMonsterId.TryGetValue(monsterId, out var list) ? list : EmptyAttacks;

    // monsterAttackDataDic을 monsterId별로 묶는다. 원본이 바뀌는 모든 곳에서 호출해야 한다
    private void RebuildMonsterAttackIndex()
    {
        var index = new Dictionary<int, List<MonsterAttackData>>();
        if (monsterAttackDataDic != null)
        {
            foreach (MonsterAttackData data in monsterAttackDataDic.Values)
            {
                if (!index.TryGetValue(data.monsterId, out var list))
                {
                    list = new List<MonsterAttackData>();
                    index.Add(data.monsterId, list);
                }
                list.Add(data);
            }
        }
        monsterAttacksByMonsterId = index;
    }

    // 개별 행의 고유 ID로 조회합니다. 실패 시 null을 반환합니다.
    public SpawnPatternData GetSpawnPatternDataFromJson(int patternId)
    {
        if (spawnPatternDataDic == null)
        {
            Debug.LogError("SpawnPattern 데이터가 초기화되지 않았습니다.");
            return null;
        }

        if (spawnPatternDataDic.TryGetValue(patternId, out SpawnPatternData data))
            return data;

        Debug.LogWarning($"등록되지 않은 SpawnPattern ID: {patternId}");
        return null;
    }

    // 몬스터 역할로 조회합니다. 행은 dropGroup 오름차순입니다. 실패 시 null을 반환합니다.
    public IReadOnlyList<DropTableEntryData> GetDropTableFromJson(MonsterType type)
    {
        if (dropTableDic == null)
        {
            Debug.LogError("DropTable 데이터가 초기화되지 않았습니다.");
            return null;
        }

        if (dropTableDic.TryGetValue(type, out List<DropTableEntryData> entries))
            return entries;

        Debug.LogWarning($"등록되지 않은 DropTable 역할: {type}");
        return null;
    }

    // 개별 행의 고유 ID로 조회합니다. 실패 시 null을 반환합니다.
    public StageData GetStageDataFromJson(int stageId)
    {
        if (stageDataDic == null)
        {
            Debug.LogError("Stage 데이터가 초기화되지 않았습니다.");
            return null;
        }

        if (stageDataDic.TryGetValue(stageId, out StageData data))
            return data;

        Debug.LogWarning($"등록되지 않은 Stage ID: {stageId}");
        return null;
    }

    // 스테이지 ID로 그 스테이지의 로스터 행들을 조회합니다. 실패 시 null을 반환합니다.
    public IReadOnlyList<StageRosterData> GetStageRosterFromJson(int stageId)
    {
        if (stageRosterDic == null)
        {
            Debug.LogError("StageRoster 데이터가 초기화되지 않았습니다.");
            return null;
        }

        if (stageRosterDic.TryGetValue(stageId, out List<StageRosterData> rows))
            return rows;

        Debug.LogWarning($"등록되지 않은 StageRoster 스테이지 ID: {stageId}");
        return null;
    }

    public SkillData GetSkillDataFromJson(long skillId)
    {
        if (skillDataDic == null)
        {
            Debug.LogError("Skill 데이터가 초기화되지 않았습니다.");
            return null;
        }

        if (skillDataDic.TryGetValue(skillId, out SkillData data))
            return data;

        Debug.LogWarning($"등록되지 않은 Skill ID: {skillId}");
        return null;
    }

    // 스킬 ID로 그 스킬의 레벨 추가 효과를 조회합니다. 효과가 없는 스킬은 빈 목록(null 체크 불필요)
    public IReadOnlyList<SkillLevelData> GetSkillLevelsFromJson(long skillId)
    {
        if (skillLevelDic == null)
            return EmptySkillLevels;

        return skillLevelDic.TryGetValue(skillId, out List<SkillLevelData> rows) ? rows : EmptySkillLevels;
    }

    public DropItemData GetDropItemDataFromJson(DropItemType type)
    {
        if (dropItemDataDic == null)
        {
            Debug.LogError("DropItem 데이터가 초기화되지 않았습니다.");
            return null;
        }

        if (dropItemDataDic.TryGetValue(type, out DropItemData data))
            return data;

        Debug.LogWarning($"등록되지 않은 DropItem 종류: {type}");
        return null;
    }


    // effectId로 장비 효과 정의 조회
    // 존재하지 않는 ID는 null 반환
    public EquipEffectData GetEquipEffectDataFromJson(long effectId)
    {
        if (equipEffectDataDic == null)
            return null;

        return equipEffectDataDic.TryGetValue(
            effectId, out var effect) ? effect : null;
    }

    // 아이템의 특정 등급에서 새롭게 해금되는 효과 조회
    // 등급 누적 계산은 C2에서 처리
    public IReadOnlyList<EquipEffectData> GetItemGradeEffectsFromJson(long itemId, ItemGrade grade)
    {
        if (itemGradeEffectsByItemId.TryGetValue(
                itemId, out var grades)
            && grades.TryGetValue(grade, out var effects))
        {
            return effects;
        }

        return EmptyEquipEffects;
    }


    public ItemData GetItemDataFromJson(long itemId)
    {
        if (itemDataDic == null)
        {
            Debug.LogError("아이템 데이터가 초기화되지 않았습니다.");
            return null;
        }

        if (itemDataDic.TryGetValue(itemId, out ItemData data))
            return data;

        Debug.LogWarning($"등록되지 않은 아이템 ID: {itemId}");
        return null;
    }
    
    public AchievementData GetAchievementDataFromJson(int achievementId)
    {
        if (achievementDataDic == null)
        {
            Debug.LogError("Achievement 데이터가 초기화되지 않았습니다.");
            return null;
        }

        if (achievementDataDic.TryGetValue(achievementId, out AchievementData data))
            return data;

        Debug.LogWarning($"등록되지 않은 Achievement ID: {achievementId}");
        return null;
    }
    
    #endregion

    #region LoadData

    // 서버 테이블 원문. 쓸 수 있는 저장본이 있으면 저장본, 없으면 빌드 사본(Resources)이다. 둘 다 없으면 null
    public static string ReadTableText(string resourcePath)
    {
        if (LoadUsableSavedVersion() != null)
        {
            string saved = SaveLoadHelper.LoadStaticDataTable(Path.GetFileName(resourcePath));
            if (saved != null)
                return saved;
        }

        TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);
        return jsonFile != null ? jsonFile.text : null;
    }

    // 시작할 때 테이블 하나를 읽는다. 실패하면 로그만 남기고 null을 반환한다
    private static T LoadTable<T>(string resourcePath, Func<JObject, T> parse) where T : class
    {
        try
        {
            string text = ReadTableText(resourcePath)
                ?? throw new InvalidOperationException("JSON 파일이 없습니다: " + resourcePath);

            return parse(JObject.Parse(text));
        }
        catch (Exception exception)
        {
            Debug.LogError($"{Path.GetFileName(resourcePath)} 데이터 로드 실패: {exception.Message}");
            return null;
        }
    }

    private static Dictionary<int, MonsterData> ParseMonsterData(JObject root)
    {
        JArray rows = root["datas"] as JArray;

        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException("몬스터 데이터 목록이 비어 있습니다.");

        var loadedDatas = new Dictionary<int, MonsterData>();
        foreach (JToken row in rows)
        {
            if (!(row is JObject))
            {
                throw new InvalidOperationException("몬스터 데이터 항목이 객체 형식이 아닙니다.");
            }

            MonsterData data = row.ToObject<MonsterData>();

            if (data == null || data.monsterId <= 0)
            {
                throw new InvalidOperationException("몬스터 데이터 또는 ID가 올바르지 않습니다.");
            }

            if (loadedDatas.ContainsKey(data.monsterId))
            {
                throw new InvalidOperationException($"중복된 몬스터 ID: {data.monsterId}");
            }

            data.OnLoaded();
            loadedDatas.Add(data.monsterId, data);
        }

        return loadedDatas;
    }

    private static Dictionary<int, MonsterAttackData> ParseMonsterAttackData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException("MonsterAttack 데이터 목록이 비어 있습니다.");

        var loadedDatas = new Dictionary<int, MonsterAttackData>();
        foreach (JToken row in rows)
        {
            if (!(row is JObject))
                throw new InvalidOperationException("MonsterAttack 데이터 항목이 객체 형식이 아닙니다.");

            MonsterAttackData data = row.ToObject<MonsterAttackData>();
            if (data == null || data.monsterAttackId <= 0)
                throw new InvalidOperationException("MonsterAttack 데이터 또는 ID가 올바르지 않습니다.");

            if (loadedDatas.ContainsKey(data.monsterAttackId))
                throw new InvalidOperationException($"중복된 MonsterAttack ID: {data.monsterAttackId}");

            if (!data.OnLoaded())
                throw new InvalidOperationException($"MonsterAttack {data.monsterAttackId}의 monsterAttackType \"{data.monsterAttackType}\"을(를) 알 수 없습니다.");

            loadedDatas.Add(data.monsterAttackId, data);
        }

        return loadedDatas;
    }

    private static Dictionary<int, WaveEntryData> ParseWaveData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException("Wave 데이터 목록이 비어 있습니다.");

        var loadedDatas = new Dictionary<int, WaveEntryData>();
        foreach (JToken row in rows)
        {
            if (!(row is JObject))
                throw new InvalidOperationException("Wave 데이터 항목이 객체 형식이 아닙니다.");

            WaveEntryData data = row.ToObject<WaveEntryData>();
            if (data == null || data.waveEntryId <= 0)
                throw new InvalidOperationException("Wave 데이터 또는 ID가 올바르지 않습니다.");

            if (loadedDatas.ContainsKey(data.waveEntryId))
                throw new InvalidOperationException($"중복된 Wave 항목 ID: {data.waveEntryId}");

            if (data.patternStartTime < 0f)
                throw new InvalidOperationException($"Wave {data.waveEntryId}의 patternStartTime은 0 이상이어야 합니다.");

            if (data.patternDuration < 0f)
                throw new InvalidOperationException($"Wave {data.waveEntryId}의 patternDuration은 0 이상이어야 합니다.");

            if (data.patternDuration > 0f && data.spawnInterval <= 0f)
                throw new InvalidOperationException($"Wave {data.waveEntryId}의 spawnInterval은 patternDuration이 0보다 크면 0보다 커야 합니다.");

            if (string.IsNullOrEmpty(data.monsterSlot))
                throw new InvalidOperationException($"Wave {data.waveEntryId}의 monsterSlot이 비어 있습니다.");

            loadedDatas.Add(data.waveEntryId, data);
        }

        return loadedDatas;
    }

    private static Dictionary<int, SpawnPatternData> ParseSpawnPatternData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException("SpawnPattern 데이터 목록이 비어 있습니다.");

        var loadedDatas = new Dictionary<int, SpawnPatternData>();
        foreach (JToken row in rows)
        {
            if (!(row is JObject))
                throw new InvalidOperationException("SpawnPattern 데이터 항목이 객체 형식이 아닙니다.");

            SpawnPatternData data = row.ToObject<SpawnPatternData>();
            if (data == null || data.patternId <= 0)
                throw new InvalidOperationException("SpawnPattern 데이터 또는 ID가 올바르지 않습니다.");

            if (loadedDatas.ContainsKey(data.patternId))
                throw new InvalidOperationException($"중복된 SpawnPattern ID: {data.patternId}");

            if (data.spawnCount < 1)
                throw new InvalidOperationException($"SpawnPattern {data.patternId}의 spawnCount는 1 이상이어야 합니다.");

            if (data.spawnRadius < 0f)
                throw new InvalidOperationException($"SpawnPattern {data.patternId}의 spawnRadius는 0 이상이어야 합니다.");

            if (data.formationSize < 0f)
                throw new InvalidOperationException($"SpawnPattern {data.patternId}의 formationSize는 0 이상이어야 합니다.");

            if (!data.OnLoaded())
                throw new InvalidOperationException($"SpawnPattern {data.patternId}의 formation \"{data.formation}\" 또는 movement \"{data.movement}\"을(를) 알 수 없습니다.");

            loadedDatas.Add(data.patternId, data);
        }

        return loadedDatas;
    }

    private static Dictionary<int, StageData> ParseStageData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException("Stage 데이터 목록이 비어 있습니다.");

        var loadedDatas = new Dictionary<int, StageData>();
        foreach (JToken row in rows)
        {
            if (!(row is JObject))
                throw new InvalidOperationException("Stage 데이터 항목이 객체 형식이 아닙니다.");

            StageData data = row.ToObject<StageData>();
            if (data == null || data.stageId <= 0)
                throw new InvalidOperationException("Stage 데이터 또는 ID가 올바르지 않습니다.");

            if (loadedDatas.ContainsKey(data.stageId))
                throw new InvalidOperationException($"중복된 Stage ID: {data.stageId}");

            data.OnLoaded();
            loadedDatas.Add(data.stageId, data);
        }

        return loadedDatas;
    }

    private static Dictionary<int, List<StageRosterData>> ParseStageRosterData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException("StageRoster 데이터 목록이 비어 있습니다.");

        var loadedDatas = new Dictionary<int, List<StageRosterData>>();
        var loadedIds = new HashSet<int>();
        foreach (JToken row in rows)
        {
            if (!(row is JObject))
                throw new InvalidOperationException("StageRoster 데이터 항목이 객체 형식이 아닙니다.");

            StageRosterData data = row.ToObject<StageRosterData>();
            if (data == null || data.stageRosterId <= 0)
                throw new InvalidOperationException("StageRoster 데이터 또는 ID가 올바르지 않습니다.");

            if (!loadedIds.Add(data.stageRosterId))
                throw new InvalidOperationException($"중복된 StageRoster ID: {data.stageRosterId}");

            if (data.stageId <= 0)
                throw new InvalidOperationException($"StageRoster {data.stageRosterId}의 stageId는 0보다 커야 합니다.");

            if (data.monsterId <= 0)
                throw new InvalidOperationException($"StageRoster {data.stageRosterId}의 monsterId는 0보다 커야 합니다.");

            if (string.IsNullOrEmpty(data.monsterSlot))
                throw new InvalidOperationException($"StageRoster {data.stageRosterId}의 monsterSlot이 비어 있습니다.");

            if (data.monsterHpScale <= 0f)
                throw new InvalidOperationException($"StageRoster {data.stageRosterId}의 monsterHpScale은 0보다 커야 합니다.");

            if (data.monsterDamageScale <= 0f)
                throw new InvalidOperationException($"StageRoster {data.stageRosterId}의 monsterDamageScale은 0보다 커야 합니다.");

            if (data.patternId < 0)
                throw new InvalidOperationException($"StageRoster {data.stageRosterId}의 patternId는 0 이상이어야 합니다.");

            if (!loadedDatas.TryGetValue(data.stageId, out List<StageRosterData> stageRows))
            {
                stageRows = new List<StageRosterData>();
                loadedDatas.Add(data.stageId, stageRows);
            }

            if (stageRows.Exists(r => r.monsterSlot == data.monsterSlot))
                throw new InvalidOperationException($"Stage {data.stageId}의 monsterSlot \"{data.monsterSlot}\"이(가) 중복됩니다.");

            stageRows.Add(data);
        }

        return loadedDatas;
    }

    private static Dictionary<long, SkillData> ParseSkillData(JObject root)
    {
        JArray rows = root["datas"] as JArray;

        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException("Skill 데이터 목록이 비어 있습니다.");

        var loadedDatas = new Dictionary<long, SkillData>();

        foreach (JToken row in rows)
        {
            if (!(row is JObject))
                throw new InvalidOperationException("Skill 데이터 항목이 객체 형식이 아닙니다.");

            SkillData data = row.ToObject<SkillData>();

            if (data == null || data.skillId <= 0)
                throw new InvalidOperationException("Skill 데이터 또는 ID가 올바르지 않습니다.");

            if (loadedDatas.ContainsKey(data.skillId))
                throw new InvalidOperationException($"중복된 Skill ID: {data.skillId}");

            // Newtonsoft 역직렬화 후 Unity 콜백을 직접 호출하여
            // skillTargetType 문자열을 TargetType 열거형으로 변환합니다.
            data.OnAfterDeserialize();

            loadedDatas.Add(data.skillId, data);
        }

        return loadedDatas;
    }

    // 레벨 추가 효과가 없는 스킬이 있으므로 행이 비어 있어도 됨. 스킬 ID마다 레벨당 행은 하나
    private static Dictionary<long, List<SkillLevelData>> ParseSkillLevelData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null)
            throw new InvalidOperationException("SkillLevel 데이터 목록이 없습니다.");

        var loadedDatas = new Dictionary<long, List<SkillLevelData>>();
        foreach (JToken row in rows)
        {
            if (!(row is JObject))
                throw new InvalidOperationException("SkillLevel 데이터 항목이 객체 형식이 아닙니다.");

            SkillLevelData data = row.ToObject<SkillLevelData>();
            if (data == null || data.skillId <= 0)
                throw new InvalidOperationException("SkillLevel 데이터 또는 skillId가 올바르지 않습니다.");

            if (data.skillLevel < 1 || data.skillLevel > SkillBase.MaxLevel)
                throw new InvalidOperationException($"SkillLevel {data.skillId}의 skillLevel은 1~{SkillBase.MaxLevel}이어야 합니다: {data.skillLevel}");

            if (!data.OnLoaded())
                throw new InvalidOperationException($"SkillLevel {data.skillId} 레벨 {data.skillLevel}의 effect1Type \"{data.effect1Type}\" 또는 effect2Type \"{data.effect2Type}\"을(를) 알 수 없습니다.");

            if (!loadedDatas.TryGetValue(data.skillId, out List<SkillLevelData> skillRows))
            {
                skillRows = new List<SkillLevelData>();
                loadedDatas.Add(data.skillId, skillRows);
            }

            if (skillRows.Exists(r => r.skillLevel == data.skillLevel))
                throw new InvalidOperationException($"SkillLevel {data.skillId}의 레벨 {data.skillLevel}이(가) 중복됩니다.");

            skillRows.Add(data);
        }

        foreach (List<SkillLevelData> skillRows in loadedDatas.Values)
            skillRows.Sort((a, b) => a.skillLevel.CompareTo(b.skillLevel));

        return loadedDatas;
    }

    private static Dictionary<DropItemType, DropItemData> ParseDropItemData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException("DropItem 데이터 목록이 비어 있습니다.");

        var loadedDatas = new Dictionary<DropItemType, DropItemData>();
        var loadedIds = new HashSet<int>();
        foreach (JToken row in rows)
        {
            if (!(row is JObject))
                throw new InvalidOperationException("DropItem 데이터 항목이 객체 형식이 아닙니다.");

            DropItemData data = row.ToObject<DropItemData>();
            if (data == null || data.dropItemId <= 0)
                throw new InvalidOperationException("DropItem 데이터 또는 ID가 올바르지 않습니다.");

            if (!loadedIds.Add(data.dropItemId))
                throw new InvalidOperationException($"중복된 DropItem ID: {data.dropItemId}");

            if (!data.OnLoaded())
                throw new InvalidOperationException($"DropItem {data.dropItemId}의 dropItemType \"{data.dropItemType}\"을(를) 알 수 없습니다.");

            if (loadedDatas.ContainsKey(data.Type))
                throw new InvalidOperationException($"중복된 DropItem 종류: {data.Type}");

            loadedDatas.Add(data.Type, data);
        }

        return loadedDatas;
    }

    private static Dictionary<MonsterType, List<DropTableEntryData>> ParseDropTableData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException("DropTable 데이터 목록이 비어 있습니다.");

        var loadedDatas = new Dictionary<MonsterType, List<DropTableEntryData>>();
        foreach (JToken row in rows)
        {
            if (!(row is JObject))
                throw new InvalidOperationException("DropTable 데이터 항목이 객체 형식이 아닙니다.");

            DropTableEntryData data = row.ToObject<DropTableEntryData>();
            if (data == null)
                throw new InvalidOperationException("DropTable 데이터가 올바르지 않습니다.");

            if (data.dropGroup < 0)
                throw new InvalidOperationException($"DropTable {data.monsterType}의 dropGroup은 0 이상이어야 합니다.");

            if (data.weight < 1)
                throw new InvalidOperationException($"DropTable {data.monsterType}의 weight는 1 이상이어야 합니다.");

            if (data.count < 1)
                throw new InvalidOperationException($"DropTable {data.monsterType}의 count는 1 이상이어야 합니다.");

            if (!data.OnLoaded())
                throw new InvalidOperationException($"DropTable의 monsterType \"{data.monsterType}\" 또는 dropItemType \"{data.dropItemType}\"을(를) 알 수 없습니다.");

            if (!loadedDatas.TryGetValue(data.MonsterType, out List<DropTableEntryData> entries))
            {
                entries = new List<DropTableEntryData>();
                loadedDatas.Add(data.MonsterType, entries);
            }
            entries.Add(data);
        }

        foreach (List<DropTableEntryData> entries in loadedDatas.Values)
            entries.Sort((a, b) => a.dropGroup.CompareTo(b.dropGroup));

        return loadedDatas;
    }

    private static AccountConstData ParseAccountConstData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null || rows.Count != 1)
            throw new InvalidOperationException("AccountConst 데이터는 행이 하나여야 합니다.");

        if (!(rows[0] is JObject))
            throw new InvalidOperationException("AccountConst 데이터 항목이 객체 형식이 아닙니다.");

        return rows[0].ToObject<AccountConstData>();
    }

    private static Dictionary<long, ItemData> ParseItemData(JObject root)
    {
        JArray rows = root["datas"] as JArray;

        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException("아이템 데이터 목록이 비어 있습니다.");

        var loadedItems = new Dictionary<long, ItemData>();
        foreach (JToken row in rows)
        {
            if (!(row is JObject))
            {
                throw new InvalidOperationException("아이템 데이터 항목이 객체 형식이 아닙니다.");
            }

            ItemData data = row.ToObject<ItemData>();

            if (data == null || data.itemId <= 0)
            {
                throw new InvalidOperationException("아이템 데이터 또는 ID가 올바르지 않습니다.");
            }

            if (loadedItems.ContainsKey(data.itemId))
            {
                throw new InvalidOperationException($"중복된 아이템 ID: {data.itemId}");
            }

            data.OnLoaded();
            loadedItems.Add(data.itemId, data);
        }

        return loadedItems;
    }


    private static Dictionary<long, EquipEffectData> ParseEquipEffectData(JObject root)
    {
        JArray rows = root["datas"] as JArray;

        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException(
                "EquipEffect 데이터 목록이 비어 있습니다.");

        var loaded = new Dictionary<long, EquipEffectData>();

        foreach (JToken row in rows)
        {
            if (!(row is JObject))
                throw new InvalidOperationException(
                    "EquipEffect 행이 객체 형식이 아닙니다.");

            EquipEffectData data = row.ToObject<EquipEffectData>();

            if (data == null || data.effectId < 70000
                || data.effectId > 79999)
            {
                throw new InvalidOperationException(
                    "EquipEffect ID는 7xxxx 형식이어야 합니다.");
            }

            if (loaded.ContainsKey(data.effectId))
                throw new InvalidOperationException(
                    $"중복된 EquipEffect ID: {data.effectId}");

            data.OnLoaded();

            ValidateEquipEffect(data);

            loaded.Add(data.effectId, data);
        }

        return loaded;
    }


    private static PlayerBaseStatData ParsePlayerBaseStatData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null || rows.Count != 1)
            throw new InvalidOperationException("PlayerBaseStat 데이터는 행이 하나여야 합니다.");

        if (!(rows[0] is JObject))
            throw new InvalidOperationException("PlayerBaseStat 데이터 항목이 객체 형식이 아닙니다.");

        PlayerBaseStatData data = rows[0].ToObject<PlayerBaseStatData>();
        data.Sanitize();
        return data;
    }

    private static ItemConstData ParseItemConstData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null || rows.Count != 1)
            throw new InvalidOperationException("ItemConst 데이터는 행이 하나여야 합니다.");

        if (!(rows[0] is JObject))
            throw new InvalidOperationException("ItemConst 데이터 항목이 객체 형식이 아닙니다.");

        ItemConstData data = rows[0].ToObject<ItemConstData>();

        // ItemLevelConfig.GetGradeMultiplier가 (int)grade로 인덱싱한다
        int gradeCount = Enum.GetValues(typeof(ItemGrade)).Length;
        if (data.gradeStatMultiplier == null || data.gradeStatMultiplier.Length != gradeCount)
            throw new InvalidOperationException($"ItemConst의 gradeStatMultiplier는 {gradeCount}개(ItemGrade 순서)여야 합니다.");

        return data;
    }


    // ItemGradeEffect.json 파싱
    private static List<ItemGradeEffectData> ParseItemGradeEffectData(JObject root)
    {
        JArray rows = root["datas"] as JArray;

        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException(
                "ItemGradeEffect 데이터 목록이 비어 있습니다.");

        var loaded = new List<ItemGradeEffectData>(rows.Count);
        var usedIds = new HashSet<int>();

        foreach (JToken row in rows)
        {
            if (!(row is JObject))
                throw new InvalidOperationException(
                    "ItemGradeEffect 행이 객체 형식이 아닙니다.");

            ItemGradeEffectData data =
                row.ToObject<ItemGradeEffectData>();

            if (data == null
                || data.itemGradeEffectId <= 0
                || data.itemId <= 0
                || data.effectId <= 0)
            {
                throw new InvalidOperationException(
                    "ItemGradeEffect의 ID 값이 올바르지 않습니다.");
            }

            if (!usedIds.Add(data.itemGradeEffectId))
            {
                throw new InvalidOperationException(
                    $"중복된 ItemGradeEffect ID: " +
                    $"{data.itemGradeEffectId}");
            }

            // C1-2에서 구현한 등급 enum 변환
            data.OnLoaded();

            // 시트 행 순서 유지
            loaded.Add(data);
        }

        return loaded;
    }


    private static EvolutionData ParseEvolutionData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null || rows.Count != 1)
            throw new InvalidOperationException("Evolution 데이터는 행이 하나여야 합니다.");

        if (!(rows[0] is JObject))
            throw new InvalidOperationException("Evolution 데이터 항목이 객체 형식이 아닙니다.");

        EvolutionData data = rows[0].ToObject<EvolutionData>();
        data.Validate();
        return data;
    }
    
    private static Dictionary<int, AchievementData> ParseAchievementData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException("Achievement 데이터 목록이 비어 있습니다.");

        var loadedDatas = new Dictionary<int, AchievementData>();
        foreach (JToken row in rows)
        {
            if (!(row is JObject))
                throw new InvalidOperationException("Achievement 데이터 항목이 객체 형식이 아닙니다.");

            AchievementData data = row.ToObject<AchievementData>();
            if (data == null || data.achievementId <= 0)
                throw new InvalidOperationException("Achievement 데이터 또는 ID가 올바르지 않습니다.");

            if (loadedDatas.ContainsKey(data.achievementId))
                throw new InvalidOperationException($"중복된 Achievement ID: {data.achievementId}");

            data.OnLoaded();
            loadedDatas.Add(data.achievementId, data);
        }

        return loadedDatas;
    }
    

    private static CombatPowerData ParseCombatPowerData(JObject root)
    {
        JArray rows = root["datas"] as JArray;
        if (rows == null || rows.Count != 1)
            throw new InvalidOperationException("CombatPower 데이터는 행이 하나여야 합니다.");

        if (!(rows[0] is JObject))
            throw new InvalidOperationException("CombatPower 데이터 항목이 객체 형식이 아닙니다.");

        CombatPowerData data = rows[0].ToObject<CombatPowerData>();
        data.Validate();
        return data;
    }
    #endregion
}
