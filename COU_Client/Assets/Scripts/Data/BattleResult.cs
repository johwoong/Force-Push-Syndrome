using System.Collections.Generic;

public static class BattleResult
{
    // 씬 전환 후 메인 씬에서 보상상자 팝업으로 보여줄 직전 전투의 보상 장비 (inventoryList에는 이미 들어가 있다)
    public static List<InventoryData> Rewards { get; set; }
}
