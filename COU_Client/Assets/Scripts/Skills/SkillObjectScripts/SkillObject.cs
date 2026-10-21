using System;
using UnityEngine;

public abstract class SkillObject<TInitData> : SkillObjectBase, ISkillPoolable
{
    public event Action<GameObject> OnBeforeReturn;
    protected SkillStats stats;

    public void Init(TInitData data)
    {
        OnInit(data);
    }

    protected abstract void OnInit(TInitData data);

    // 활성화되기 전
    protected abstract void OnLaunch();

    // 활성화 되고 나서
    public virtual void OnSpawn()
    {
        PlaySfx();
    }

    public virtual void OnDespawn()
    {
        // SkillObjectPool.Instance.Return에 호출
    }

    protected virtual void ReturnToPool()
    {
        OnDespawn();// SkillObjectPool.Instance.Return에 호출
        OnBeforeReturn?.Invoke(gameObject);
        SkillObjectPool.Instance.Return(gameObject);
    }
}
