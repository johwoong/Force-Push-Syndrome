using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JsonDataManager))]
public class JsonDataManagerEditor : Editor
{
    // 데이터 종류마다 펼침 상태를 분리합니다. 같은 ID가 있어도 서로 영향을 주지 않습니다.
    private readonly HashSet<int> expandedMonsters = new();
    private readonly HashSet<int> expandedMonsterAttacks = new();
    private readonly HashSet<int> expandedWaveEntries = new();
    private readonly HashSet<int> expandedSpawnPatterns = new();
    private readonly HashSet<int> expandedStages = new();
    private readonly HashSet<int> expandedStageRosters = new();
    private readonly HashSet<long> expandedSkills = new();
    private readonly HashSet<DropItemType> expandedDropItems = new();
    private readonly HashSet<MonsterType> expandedDropTables = new();
    private readonly HashSet<long> expandedItems = new();
    private readonly HashSet<long> expandedEquipEffects = new();
    private readonly HashSet<int> expandedItemGradeEffects = new();
    private readonly HashSet<int> expandedAchievements = new();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var manager = (JsonDataManager)target;
        DrawDictionary("몬스터 데이터", manager.MonsterDataDic, expandedMonsters,
            data => data.monsterName, DrawMonster);
        DrawDictionary("몬스터 공격 데이터", manager.MonsterAttackDataDic, expandedMonsterAttacks,
            data => $"몬스터 {data.monsterId} / {data.monsterAttackType}", DrawMonsterAttack);
        DrawDictionary("웨이브 데이터", manager.WaveEntryDataDic, expandedWaveEntries,
            data => $"웨이브 {data.waveId} / 패턴 {data.patternId} / {data.monsterSlot}", DrawWaveEntry);
        DrawDictionary("스폰 패턴 데이터", manager.SpawnPatternDataDic, expandedSpawnPatterns,
            data => $"{data.formation} × {data.spawnCount} / {data.movement}", DrawSpawnPattern);
        DrawDictionary("스테이지 데이터", manager.StageDataDic, expandedStages,
            data => data.stageName, DrawStage);
        DrawDictionary("스테이지 로스터 데이터", manager.StageRosterDic, expandedStageRosters,
            rows => $"슬롯 {rows.Count}개", DrawStageRoster);
        DrawDictionary("스킬 데이터", manager.SkillDataDic, expandedSkills,
            data => data.skillName, DrawSkill);
        DrawDictionary("드롭 아이템 데이터", manager.DropItemDataDic, expandedDropItems,
            data => $"ID {data.dropItemId}", DrawDropItem);
        DrawDictionary("드롭 테이블 데이터", manager.DropTableDic, expandedDropTables,
            rows => $"행 {rows.Count}개", DrawDropTable);
        DrawDictionary("아이템 데이터", manager.ItemDataDic, expandedItems, data => data.itemName, DrawItem);
        DrawDictionary("장비 효과 정의 데이터 (EquipEffect)", manager.EquipEffectDataDic, expandedEquipEffects, data => data.effectDescription ?? string.Empty, DrawEquipEffect);
        DrawItemGradeEffects(manager);
        DrawSingle("계정 상수 데이터", manager.AccountConstData, DrawAccountConst);
        DrawSingle("플레이어 기본 스탯 데이터", manager.PlayerBaseStatData, DrawPlayerBaseStat);
        DrawSingle("장비 상수 데이터", manager.ItemConstData, DrawItemConst);
        DrawDictionary("업적 데이터", manager.AchievementDataDic, expandedAchievements,
            data => $"{data.achievementName} / {data.ConditionType} / {data.TierCount}단계", DrawAchievement);
    }

    private static void DrawDictionary<TKey, T>(string title, IReadOnlyDictionary<TKey, T> datas,
        HashSet<TKey> expandedIds, Func<T, string> getLabel, Action<T> drawData)
        where TKey : IComparable
        where T : class
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        if (datas == null)
        {
            EditorGUILayout.HelpBox($"아직 로드된 {title}가 없습니다.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("등록 수", datas.Count.ToString());

        var ids = new List<TKey>(datas.Keys);
        ids.Sort();

        foreach (TKey id in ids)
        {
            T data = datas[id];
            if (data == null)
            {
                EditorGUILayout.LabelField($"{id}: 데이터 없음");
                continue;
            }

            bool expanded = EditorGUILayout.Foldout(
                expandedIds.Contains(id), $"{id} / {getLabel(data)}", true);

            if (expanded)
                expandedIds.Add(id);
            else
                expandedIds.Remove(id);

            if (!expanded)
                continue;

            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUI.DisabledScope(true))
            {
                // 표시만 하고 반환값을 원본 데이터에 대입하지 않습니다.
                drawData(data);
            }
        }
    }

    // 행이 하나뿐인 테이블
    private static void DrawSingle<T>(string title, T data, Action<T> drawData) where T : class
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        if (data == null)
        {
            EditorGUILayout.HelpBox($"아직 로드된 {title}가 없습니다.", MessageType.Info);
            return;
        }

        using (new EditorGUI.IndentLevelScope())
        using (new EditorGUI.DisabledScope(true))
        {
            // 표시만 하고 반환값을 원본 데이터에 대입하지 않습니다.
            drawData(data);
        }
    }

    private static void DrawMonster(MonsterData data)
    {
        EditorGUILayout.IntField("ID", data.monsterId);
        EditorGUILayout.TextField("이름", data.monsterName ?? string.Empty);
        EditorGUILayout.TextField("종류 문자열", data.monsterType ?? string.Empty);
        EditorGUILayout.EnumPopup("종류", data.Type);
        EditorGUILayout.IntField("최대 체력", data.monsterMaxHealthPoint);
        EditorGUILayout.FloatField("이동 속도", data.monsterMoveSpeed);
        EditorGUILayout.IntField("접촉 피해량", data.monsterContactDamage);
        EditorGUILayout.TextField("에셋 경로", data.monsterAsset ?? string.Empty);
    }

    private static void DrawMonsterAttack(MonsterAttackData data)
    {
        EditorGUILayout.IntField("공격 ID", data.monsterAttackId);
        EditorGUILayout.IntField("몬스터 ID", data.monsterId);
        EditorGUILayout.TextField("공격 종류 문자열", data.monsterAttackType ?? string.Empty);
        EditorGUILayout.EnumPopup("공격 종류", data.AttackType);
        EditorGUILayout.FloatField("쿨다운 (초)", data.monsterAttackCooldown);
        EditorGUILayout.FloatField("공격 시작 거리", data.monsterAttackTriggerRange);
        EditorGUILayout.IntField("피해량", data.monsterAttackDamage);
        EditorGUILayout.IntField("발 수 / 장판 수", data.monsterAttackCount);
        EditorGUILayout.FloatField("산탄 각도", data.monsterAttackAngle);
        EditorGUILayout.FloatField("독 장판 유지 시간 (초)", data.monsterAttackDuration);
    }

    private static void DrawWaveEntry(WaveEntryData data)
    {
        EditorGUILayout.IntField("웨이브 항목 ID", data.waveEntryId);
        EditorGUILayout.IntField("웨이브 ID", data.waveId);
        EditorGUILayout.FloatField("시작 시간 (초)", data.patternStartTime);
        EditorGUILayout.FloatField("반복 지속 시간 (초)", data.patternDuration);
        EditorGUILayout.FloatField("반복 간격 (초)", data.spawnInterval);
        EditorGUILayout.IntField("패턴 ID", data.patternId);
        EditorGUILayout.TextField("몬스터 슬롯", data.monsterSlot ?? string.Empty);
    }

    private static void DrawSpawnPattern(SpawnPatternData data)
    {
        EditorGUILayout.IntField("패턴 ID", data.patternId);
        EditorGUILayout.TextField("배치 형태 문자열", data.formation ?? string.Empty);
        EditorGUILayout.EnumPopup("배치 형태", data.Formation);
        EditorGUILayout.IntField("스폰 마릿수", data.spawnCount);
        EditorGUILayout.FloatField("거리 (0이면 기본값)", data.spawnRadius);
        EditorGUILayout.FloatField("형태 크기 (0이면 기본값)", data.formationSize);
        EditorGUILayout.TextField("이동 문자열", data.movement ?? string.Empty);
        EditorGUILayout.EnumPopup("이동", data.Movement);
    }

    private static void DrawStage(StageData data)
    {
        EditorGUILayout.IntField("스테이지 ID", data.stageId);
        EditorGUILayout.TextField("이름", data.stageName ?? string.Empty);
        EditorGUILayout.IntField("웨이브 ID", data.waveId);
        EditorGUILayout.TextField("설명", data.stageDescription ?? string.Empty);
        EditorGUILayout.TextField("UI 색깔 문자열", data.stageIllustrationColor ?? string.Empty);
        EditorGUILayout.ColorField("UI 색깔 (파싱됨)", data.IllustrationColor);
    }

    private static void DrawStageRoster(List<StageRosterData> rows)
    {
        foreach (StageRosterData row in rows)
        {
            EditorGUILayout.LabelField($"행 {row.stageRosterId}");

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.TextField("슬롯", row.monsterSlot ?? string.Empty);
                EditorGUILayout.IntField("몬스터 ID", row.monsterId);
                EditorGUILayout.FloatField("체력 배율", row.monsterHpScale);
                EditorGUILayout.FloatField("피해 배율", row.monsterDamageScale);
                EditorGUILayout.IntField("패턴 ID (0이면 웨이브 값)", row.patternId);
            }
        }
    }

    private static void DrawSkill(SkillData data)
    {
        EditorGUILayout.LongField("스킬 ID", data.skillId);
        EditorGUILayout.TextField("이름", data.skillName ?? string.Empty);
        EditorGUILayout.EnumPopup("카테고리", data.skillCategory);
        EditorGUILayout.TextField("설명", data.skillDescription ?? string.Empty);
        EditorGUILayout.TextField("타겟 종류 문자열", data.skillTargetType ?? string.Empty);
        EditorGUILayout.EnumPopup("타겟 종류", data.TargetType);
        EditorGUILayout.FloatField("쿨다운 (초)", data.skillCooldown);
        EditorGUILayout.FloatField("속도", data.skillSpeed);
        EditorGUILayout.FloatField("피해량", data.skillDamage);
        EditorGUILayout.FloatField("사거리", data.skillRange);
        EditorGUILayout.TextField("레벨1 설명", data.level1SkillDescription ?? string.Empty);
        EditorGUILayout.TextField("레벨2 설명", data.level2SkillDescription ?? string.Empty);
        EditorGUILayout.TextField("레벨3 설명", data.level3SkillDescription ?? string.Empty);
        EditorGUILayout.TextField("레벨4 설명", data.level4SkillDescription ?? string.Empty);
        EditorGUILayout.TextField("레벨5 설명", data.level5SkillDescription ?? string.Empty);
    }

    private static void DrawDropItem(DropItemData data)
    {
        EditorGUILayout.IntField("드롭 아이템 ID", data.dropItemId);
        EditorGUILayout.TextField("종류 문자열", data.dropItemType ?? string.Empty);
        EditorGUILayout.EnumPopup("종류", data.Type);
        EditorGUILayout.IntField("값", data.value);
    }

    private static void DrawDropTable(List<DropTableEntryData> rows)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            DropTableEntryData row = rows[i];
            EditorGUILayout.LabelField($"행 {i + 1}");

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.IntField("드롭 그룹", row.dropGroup);
                EditorGUILayout.TextField("드롭 종류", row.dropItemType ?? string.Empty);
                EditorGUILayout.IntField("가중치", row.weight);
                EditorGUILayout.IntField("개수", row.count);
            }
        }
    }

    private static void DrawEquipEffect(EquipEffectData data)
    {
        // 효과 기본 정보
        EditorGUILayout.LongField("효과 ID", data.effectId);
        EditorGUILayout.TextField(
            "효과 설명", data.effectDescription ?? string.Empty);

        // 효과 종류
        EditorGUILayout.TextField(
            "카테고리 문자열", data.effectCategory ?? string.Empty);
        EditorGUILayout.EnumPopup("카테고리", data.Category);

        // 79xxx 제외 여부
        EditorGUILayout.Toggle("제외 효과 (79xxx)", data.IsExcluded);

        // Stat형 정보
        EditorGUILayout.TextField(
            "스탯 종류 문자열", data.statType ?? string.Empty);
        EditorGUILayout.EnumPopup("스탯 종류", data.StatType);

        EditorGUILayout.TextField(
            "값 종류 문자열", data.valueType ?? string.Empty);
        EditorGUILayout.EnumPopup("값 종류", data.ValueType);

        EditorGUILayout.FloatField("효과 수치", data.effectValue);

        // Trigger형 정보
        EditorGUILayout.TextField(
            "발동 조건 문자열", data.triggerType ?? string.Empty);
        EditorGUILayout.EnumPopup("발동 조건", data.TriggerType);

        EditorGUILayout.FloatField(
            "발동 확률 (%)", data.triggerChance);
        EditorGUILayout.FloatField(
            "발동 간격 (초)", data.triggerInterval);

        // 발동 시 실행 동작
        EditorGUILayout.TextField(
            "동작 종류 문자열", data.actionType ?? string.Empty);
        EditorGUILayout.EnumPopup("동작 종류", data.ActionType);

        EditorGUILayout.FloatField("동작 수치", data.actionValue);
    }

    private void DrawItemGradeEffects(JsonDataManager manager)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "아이템 등급 효과 연결 데이터 (ItemGradeEffect)",
            EditorStyles.boldLabel);

        IReadOnlyList<ItemGradeEffectData> rows =
            manager.ItemGradeEffectRows;

        if (rows == null)
        {
            EditorGUILayout.HelpBox(
                "아직 로드된 ItemGradeEffect 데이터가 없습니다.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("등록 수", rows.Count.ToString());

        // JSON의 원본 행 순서를 유지한다.
        foreach (ItemGradeEffectData row in rows)
        {
            if (row == null)
            {
                EditorGUILayout.LabelField("데이터가 없는 행");
                continue;
            }

            // 연결된 아이템 정보 조회
            ItemData item = null;

            if (manager.ItemDataDic != null)
            {
                manager.ItemDataDic.TryGetValue(
                    row.itemId, out item);
            }

            string itemName = item != null
                ? item.itemName
                : "아이템 없음";

            // 연결된 효과 정의 조회
            EquipEffectData effect =
                manager.GetEquipEffectDataFromJson(row.effectId);

            string effectDescription = effect != null
                ? effect.effectDescription
                : "효과 정의 없음";

            // 연결 행 ID를 기준으로 펼침 상태 관리
            bool expanded = EditorGUILayout.Foldout(
                expandedItemGradeEffects.Contains(
                    row.itemGradeEffectId),
                $"{row.itemGradeEffectId} / {itemName} " +
                $"/ {row.Grade} / {effectDescription}",
                true);

            if (expanded)
                expandedItemGradeEffects.Add(
                    row.itemGradeEffectId);
            else
                expandedItemGradeEffects.Remove(
                    row.itemGradeEffectId);

            if (!expanded)
                continue;

            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUI.DisabledScope(true))
            {
                // ItemGradeEffect 원본 정보
                EditorGUILayout.IntField(
                    "연결 행 ID", row.itemGradeEffectId);

                EditorGUILayout.LongField(
                    "아이템 ID", row.itemId);

                EditorGUILayout.TextField(
                    "아이템 이름", itemName ?? string.Empty);

                EditorGUILayout.TextField(
                    "등급 문자열", row.grade ?? string.Empty);

                EditorGUILayout.EnumPopup(
                    "해금 등급", row.Grade);

                EditorGUILayout.LongField(
                    "효과 ID", row.effectId);

                // 연결된 EquipEffect 정보
                EditorGUILayout.TextField(
                    "효과 설명",
                    effectDescription ?? string.Empty);

                if (effect != null)
                {
                    EditorGUILayout.EnumPopup(
                        "효과 카테고리", effect.Category);

                    EditorGUILayout.Toggle(
                        "제외 효과 (79xxx)", effect.IsExcluded);
                }
            }
        }
    }

    private static void DrawItem(ItemData data)
    {
        EditorGUILayout.LongField("아이템 ID", data.itemId);
        EditorGUILayout.TextField("이름", data.itemName ?? string.Empty);
        EditorGUILayout.TextField("설명", data.description ?? string.Empty);
        EditorGUILayout.TextField("아이콘 경로", data.iconPath ?? string.Empty);
        EditorGUILayout.TextField("부위 문자열", data.slotType ?? string.Empty);
        EditorGUILayout.EnumPopup("부위", data.SlotType);
        EditorGUILayout.TextField("등급 문자열", data.grade ?? string.Empty);
        EditorGUILayout.EnumPopup("등급", data.Grade);
        EditorGUILayout.IntField("체력 보너스", data.hpBonus);
        EditorGUILayout.IntField("공격력 보너스", data.attackBonus);
        EditorGUILayout.IntField("이동 속도 보너스", data.moveSpeedBonus);
    }
    
    private static void DrawAchievement(AchievementData data)
    {
        EditorGUILayout.IntField("업적 ID", data.achievementId);
        EditorGUILayout.TextField("이름", data.achievementName ?? string.Empty);
        EditorGUILayout.TextField("설명", data.achievementDescription ?? string.Empty);
        EditorGUILayout.TextField("조건 종류 문자열", data.achievementConditionType ?? string.Empty);
        EditorGUILayout.EnumPopup("조건 종류", data.ConditionType);
        EditorGUILayout.LongField("대상 스킬 ID", data.targetSkillId);
        EditorGUILayout.TextField("대상 드롭 아이템 문자열", data.targetDropItemType ?? string.Empty);
        if (data.HasTargetDropItem)
            EditorGUILayout.EnumPopup("대상 드롭 아이템", data.TargetDropItemType);

        for (int i = 0; i < data.TierCount; i++)
        {
            EditorGUILayout.LabelField($"{i + 1}단계");

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.IntField("목표값", data.GetTargetValue(i));
                EditorGUILayout.EnumPopup("보상 종류", data.GetRewardType(i));
                EditorGUILayout.IntField("보상 수량", data.GetRewardAmount(i));
                EditorGUILayout.TextField("표시 설명", data.GetDescription(i));
            }
        }
    }

    private static void DrawAccountConst(AccountConstData data)
    {
        EditorGUILayout.IntField("초기 골드", data.initialGold);
        EditorGUILayout.IntField("초기 보석", data.initialGem);
        EditorGUILayout.IntField("초기 스태미나", data.initialStamina);
        EditorGUILayout.IntField("최대 스태미나", data.maxStamina);
        EditorGUILayout.IntField("기본 필요 경험치", data.accountBaseRequiredExp);
        EditorGUILayout.IntField("필요 경험치 증가량", data.accountRequiredExpIncrement);
        EditorGUILayout.IntField("최대 계정 레벨", data.maxAccountLevel);
        EditorGUILayout.IntField("전투 스태미나 비용", data.battleStaminaCost);
        EditorGUILayout.IntField("스태미나 회복 간격 (초)", data.staminaRecoverySeconds);
        EditorGUILayout.IntField("처치당 경험치", data.accountExpPerKill);
        EditorGUILayout.IntField("초당 경험치", data.accountExpPerSecond);
        EditorGUILayout.IntField("행운열차 최대 골드", data.luckTrainGoldMax);
    }

    private static void DrawPlayerBaseStat(PlayerBaseStatData data)
    {
        EditorGUILayout.IntField("공격력", data.playerBaseAttack);
        EditorGUILayout.IntField("체력", data.playerBaseHp);
        EditorGUILayout.IntField("치명타 피해", data.playerBaseCriticalDamage);
        EditorGUILayout.IntField("치명타 확률", data.playerBaseCriticalChance);
        EditorGUILayout.IntField("스킬 피해", data.playerBaseSkillDamage);
        EditorGUILayout.FloatField("이동 속도", data.playerBaseMoveSpeed);
        EditorGUILayout.FloatField("최대 이동 속도", data.playerBaseMaxMoveSpeed);
        EditorGUILayout.FloatField("루팅 반경", data.playerBaseLootRadius);
    }

    private static void DrawItemConst(ItemConstData data)
    {
        EditorGUILayout.IntField("최대 레벨", data.maxLevel);
        EditorGUILayout.IntField("강화 기본 비용", data.levelUpBaseCost);
        EditorGUILayout.FloatField("레벨당 스탯 증가율", data.statGrowthPerLevel);
        EditorGUILayout.IntField("합성 재료 개수", data.synthesisMaterialCount);

        for (int i = 0; i < data.gradeStatMultiplier.Length; i++)
            EditorGUILayout.FloatField($"등급 배율 ({(ItemGrade)i})", data.gradeStatMultiplier[i]);
    }

    public override bool RequiresConstantRepaint()
    {
        return EditorApplication.isPlaying;
    }
}
