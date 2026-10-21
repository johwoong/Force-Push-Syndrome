using System;
using System.Collections.Generic;

// 액티브 스킬이 진화하려면 5레벨이 된 뒤 여기에 적힌 짝 패시브를 하나라도 가지고 있어야 함
// 짝 패시브가 여럿이면 배열에 모두 적으면 되고, 그중 하나만 있어도 진화함. 나중에 데이터 테이블로 옮기기 쉽도록 한 곳에 모아 둠
public static class SkillEvolutionTable
{
    private static readonly Dictionary<long, long[]> _passiveIdsByActiveId = new()
    {
        { GameConstants.TemporaryConstClass.TUMBLER, new long[] { GameConstants.TemporaryConstClass.ENERGY_DRINK } }, // 텀블러 -> 에너지드링크
        { GameConstants.TemporaryConstClass.GLASSES, new long[] { GameConstants.TemporaryConstClass.EYE_DROPS } }, // 안경 -> 안약
        { GameConstants.TemporaryConstClass.MONITOR, new long[] { GameConstants.TemporaryConstClass.SECOND_MONITOR } }, // 모니터 -> 보조 모니터
        { GameConstants.TemporaryConstClass.SPEAKER, new long[] { GameConstants.TemporaryConstClass.EARBUDS } }, // 스피커 -> 이어폰
        { GameConstants.TemporaryConstClass.MOUSE, new long[] { GameConstants.TemporaryConstClass.MOUSE_PAD } }, // 마우스 -> 마우스 패드
        { GameConstants.TemporaryConstClass.KEYBOARD, new long[] { GameConstants.TemporaryConstClass.KEYBOARD_SWITCH } }, // 키보드 -> KeyboardSwitch
        { GameConstants.TemporaryConstClass.CPU, new long[] { GameConstants.TemporaryConstClass.OVERCLOCK } }, // CPU -> 오버클럭
        { GameConstants.TemporaryConstClass.POWER_STRIP, new long[] { GameConstants.TemporaryConstClass.STATIC_SHOCK } }, // 멀티탭 -> 정전기
        { GameConstants.TemporaryConstClass.CTRL_Z, new long[] { GameConstants.TemporaryConstClass.SHIFT_KEY } }, // CtrlZ -> Shift
    };

    // 짝 패시브 ID 목록. 진화 조합이 없는 스킬은 빈 배열
    public static IReadOnlyList<long> GetPassiveIds(long activeSkillId)
    {
        return _passiveIdsByActiveId.TryGetValue(activeSkillId, out long[] passiveIds) ? passiveIds : Array.Empty<long>();
    }
}
