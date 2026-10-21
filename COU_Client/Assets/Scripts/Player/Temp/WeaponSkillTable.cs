using System.Collections.Generic;

// 임시: 장비 데이터(ItemData)에 시작 스킬 필드가 추가되면 교체 후 삭제
public static class WeaponSkillTable
{
    public const long NoSkill = 0;

    // 무기 아이템 ID → 시작 스킬 ID
    private static readonly Dictionary<long, long> itemToSkill = new()
    {
        { 6001, GameConstants.TemporaryConstClass.KEYBOARD },   // 낡은 검 → Katana
        { 6002, GameConstants.TemporaryConstClass.MOUSE },   // 연발 권총 → Revolver
        { 6003, GameConstants.TemporaryConstClass.MONITOR },   // 그림자 쿠나이 → Shuriken
    };

    public static long GetSkillByItem(long itemId)
        => itemToSkill.TryGetValue(itemId, out long skillId) ? skillId : NoSkill;
}