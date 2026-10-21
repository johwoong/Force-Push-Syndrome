using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PauseWindow : UIView
{
    [SerializeField] private TMP_Text killText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private PauseSkillSlot[] weaponSlots;
    [SerializeField] private PauseSkillSlot[] supportSlots;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button continueButton;
    [FormerlySerializedAs("soundButton")]
    [SerializeField] private Button settingsButton;

    public event Action OnOpened;

    private void Awake()
    {
        homeButton.onClick.AddListener(() => UIManager.Instance.Open<HomePopup>());
        continueButton.onClick.AddListener(Close);
        settingsButton.onClick.AddListener(() => UIManager.Instance.Open<SettingsPopup>());
    }

    public override void Open()
    {
        base.Open();
        if (BattleManager.Instance != null)
            BattleManager.Instance.RequestPause(this);
        OnOpened?.Invoke();
    }

    public override void Close()
    {
        base.Close();
        if (BattleManager.Instance != null)
            BattleManager.Instance.ReleasePause(this);
    }

    public void SetKillCount(int kills)
        => killText.text = kills.ToString();

    public void SetGold(int gold)
        => goldText.text = gold.ToString();

    public void SetWeaponSlots(IReadOnlyList<Sprite> icons, IReadOnlyList<int> grades)
        => SetSlots(weaponSlots, icons, grades);

    public void SetSupportSlots(IReadOnlyList<Sprite> icons, IReadOnlyList<int> grades)
        => SetSlots(supportSlots, icons, grades);

    private static void SetSlots(PauseSkillSlot[] slots, IReadOnlyList<Sprite> icons, IReadOnlyList<int> grades)
    {
        for (var i = 0; i < slots.Length; i++)
        {
            if (i < icons.Count)
                slots[i].Set(icons[i], grades[i]);
            else
                slots[i].Clear();
        }
    }
}
