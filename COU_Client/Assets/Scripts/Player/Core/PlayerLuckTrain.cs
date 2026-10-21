using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SkillController))]
public class PlayerLuckTrain : MonoBehaviour
{
    private const int SlotCount = 16;
    private const int FiveChancePercent = 10;    // 5개 10%
    private const int ThreeChancePercent = 20;   // 3개 20% (나머지 70%는 1개)

    [SerializeField][Min(0)] private int goldMin = 100;   // 임시 값 (최대값은 AccountConst.luckTrainGoldMax, 서버 골드 상한과 같은 값)

    [Header("Test")]
    // 0이면 확률대로, 1/3/5면 해당 개수로
    [SerializeField] private int testForcedCount = 0;

    private SkillController skillController;

    private LuckTrainWindow window;
    private SkillSelectWindow selectWindow;
    private BattleResultWindow resultWindow;

    private int pending;
    private bool isShowing;
    private int rewardGold;

    private readonly long[] slotSkillIds = new long[SlotCount];
    private readonly List<long> selectedSkillIds = new();

    public int Pending => pending;
    public bool IsShowing => isShowing;

    private void Awake()
    {
        skillController = GetComponent<SkillController>();
    }

    private void OnDestroy()
    {
        if (window != null)
            window.OnFinished -= HandleFinished;
    }

    // 행운상자 획득
    public void Add()
    {
        pending++;
        Debug.Log("[PlayerLuckTrain] 행운상자 획득, 대기: " + pending);
    }

    private void Update()
    {
        if (isShowing)
        {
            if (IsBattleEnded())
                Abort();
            return;
        }

        if (pending == 0) return;

        if (!ResolveWindows())
        {
            Debug.LogWarning("[PlayerLuckTrain] 행운열차 창을 찾을 수 없어 대기분을 버립니다. (전투 씬이 아닌 경우 정상)");
            pending = 0;
            return;
        }

        if (IsBattleEnded())
        {
            pending = 0;
            return;
        }

        if (selectWindow != null && selectWindow.IsOpen) return;

        ShowNext();
    }

    private void ShowNext()
    {
        pending--;

        var candidates = new List<SkillBase>();
        foreach (var skill in skillController.ActiveSkills)
        {
            if (skill.Level < SkillBase.MaxLevel)
                candidates.Add(skill);
        }

        if (candidates.Count == 0)
        {
            Debug.Log("[PlayerLuckTrain] 등급을 올릴 스킬이 없어 행운열차를 건너뜁니다.");
            return;
        }

        // 중복 허용
        for (int i = 0; i < SlotCount; i++)
        {
            long skillId = candidates[Random.Range(0, candidates.Count)].SkillId;
            slotSkillIds[i] = skillId;
            window.SetSlot(i, Resources.Load<Sprite>(GameManager.JsonData.GetSkillDataFromJson(skillId).IconPath));
        }

        int[] selected = DrawSelection(candidates);

        selectedSkillIds.Clear();
        var previewLevels = new Dictionary<long, int>();

        for (int i = 0; i < selected.Length; i++)
        {
            long skillId = slotSkillIds[selected[i]];
            selectedSkillIds.Add(skillId);

            if (!previewLevels.TryGetValue(skillId, out int level))
                level = FindLevel(skillId);

            level++;
            previewLevels[skillId] = level;

            var data = GameManager.JsonData.GetSkillDataFromJson(skillId);
            window.SetReward(i, Resources.Load<Sprite>(data.IconPath), data.skillName, level, "Lv." + level);
        }

        int goldMax = GameManager.JsonData.AccountConstData.luckTrainGoldMax;
        rewardGold = Random.Range(goldMin, goldMax + 1) * selected.Length;

        BattleManager.Instance?.RequestPause(this);
        window.Show(selected, selected.Length, rewardGold);
        isShowing = true;
    }


    private int[] DrawSelection(List<SkillBase> candidates)
    {
        int rolled = RollCount();
        int[] result = null;

        if (rolled == 5)
            result = TryDrawFive(BuildRemain(candidates));

        if (result == null && rolled >= 3)
            result = TryDrawSpread(3, BuildRemain(candidates));

        if (result == null)
            result = TryDrawSpread(1, BuildRemain(candidates));

        Debug.Log("[PlayerLuckTrain] 추첨: " + rolled + "개" +
                  (result.Length != rolled ? " → " + result.Length + "개 (강등)" : "") +
                  " / 칸: " + string.Join(", ", result));

        return result;
    }

    private int RollCount()
    {
        if (testForcedCount == 1 || testForcedCount == 3 || testForcedCount == 5)
            return testForcedCount;

        int roll = Random.Range(0, 100);
        if (roll < FiveChancePercent) return 5;
        if (roll < FiveChancePercent + ThreeChancePercent) return 3;
        return 1;
    }

    private static Dictionary<long, int> BuildRemain(List<SkillBase> candidates)
    {
        var remain = new Dictionary<long, int>();
        foreach (var skill in candidates)
            remain[skill.SkillId] = SkillBase.MaxLevel - skill.Level;
        return remain;
    }

    private int[] TryDrawFive(Dictionary<long, int> remain)
    {
        var validStarts = new List<int>();

        for (int start = 0; start < SlotCount; start++)
        {
            var counts = new Dictionary<long, int>();
            bool valid = true;

            for (int k = 0; k < 5; k++)
            {
                long skillId = slotSkillIds[(start + k) % SlotCount];
                counts.TryGetValue(skillId, out int count);
                counts[skillId] = ++count;

                if (count > remain[skillId])
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
                validStarts.Add(start);
        }

        if (validStarts.Count == 0) return null;

        int s = validStarts[Random.Range(0, validStarts.Count)];
        var result = new int[5];
        for (int k = 0; k < 5; k++)
            result[k] = (s + k) % SlotCount;

        return result;
    }

    private int[] TryDrawSpread(int count, Dictionary<long, int> remain)
    {
        var order = new int[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            order[i] = i;

        for (int i = SlotCount - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        var picked = new List<int>();

        foreach (int index in order)
        {
            if (picked.Count == count) break;
            if (IsAdjacentToAny(index, picked)) continue;

            long skillId = slotSkillIds[index];
            if (remain[skillId] <= 0) continue;

            picked.Add(index);
            remain[skillId]--;
        }

        if (picked.Count < count) return null;

        picked.Sort();
        return picked.ToArray();
    }

    private static bool IsAdjacentToAny(int index, List<int> picked)
    {
        int next = (index + 1) % SlotCount;
        int prev = (index + SlotCount - 1) % SlotCount;

        foreach (int p in picked)
        {
            if (p == next || p == prev) return true;
        }
        return false;
    }

    // ===== 결과 적용·정리 =====

    private void HandleFinished()
    {
        // 행운열차 레벨업은 연마 업적에 넣지 않는다 (BattleManager의 skillUpgrades 집계를 거치지 않음)
        foreach (long skillId in selectedSkillIds)
        {
            bool result = skillController.LevelUpSkill(skillId);
            Debug.Log("[PlayerLuckTrain] 스킬 " + skillId + " 등급 상승: " + (result ? "성공" : "실패"));
        }

        BattleManager.Instance?.AddGold(rewardGold);
        BattleManager.Instance?.AddLuckTrain();

        BattleManager.Instance?.ReleasePause(this);
        isShowing = false;
    }

    private void Abort()
    {
        if (window != null && window.IsOpen)
            window.Close();

        BattleManager.Instance?.ReleasePause(this);
        isShowing = false;
        pending = 0;
        Debug.Log("[PlayerLuckTrain] 전투 종료로 행운열차 정리");
    }

    private bool IsBattleEnded()
        => resultWindow != null && resultWindow.IsOpen;

    private int FindLevel(long skillId)
    {
        foreach (var skill in skillController.ActiveSkills)
        {
            if (skill.SkillId == skillId) return skill.Level;
        }
        return 0;
    }

    private bool ResolveWindows()
    {
        if (window != null) return true;

        window = TryGetView<LuckTrainWindow>();
        if (window == null) return false;

        selectWindow = TryGetView<SkillSelectWindow>();
        resultWindow = TryGetView<BattleResultWindow>();
        window.OnFinished += HandleFinished;

        return true;
    }

    private static T TryGetView<T>() where T : UIView
    {
        try
        {
            return UIManager.Instance.Get<T>();
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    [ContextMenu("TestLuckyBox")]
    private void TestLuckyBox() => Add();
}