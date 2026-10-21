using UnityEngine;

// 움직이지도 공격하지도 않는 상자. 부수면 드롭 테이블에 따라 아이템을 드롭한다.
public class Box : Enemy
{
    protected override void Move() { }

    protected override void Attack() { }

    // 킬 수 집계(OnDied)는 하지 않는다
    protected override void GiveReward()
    {
        GameManager.DropItem.SpawnTable(Type, transform.position);
    }
}
