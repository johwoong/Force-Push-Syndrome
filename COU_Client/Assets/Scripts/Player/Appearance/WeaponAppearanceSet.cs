using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "COU/Player/Weapon Appearance Set", fileName = "WeaponAppearanceSet")]
public class WeaponAppearanceSet : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public long itemId;
        public Sprite rightHand;
        public Sprite leftHand;
        public int attackAnimationIndex;
        public float attackHitTime = 0.2f;
    }

    [SerializeField] private List<Entry> entries = new();

    public bool TryGet(long itemId, out Entry entry)
    {
        entry = entries.Find(e => e.itemId == itemId);
        return entry != null;
    }
}
