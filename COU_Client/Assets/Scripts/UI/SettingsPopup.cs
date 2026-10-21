using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SettingsPopup : UIPopup
{
    [SerializeField] private Button closeButton;

    [Header("배경음")]
    [SerializeField] private Slider bgmSlider;
    [FormerlySerializedAs("soundButton")]
    [SerializeField] private Button bgmButton;
    [FormerlySerializedAs("muteMark")]
    [SerializeField] private GameObject bgmMuteMark;

    [Header("효과음")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button sfxButton;
    [SerializeField] private GameObject sfxMuteMark;

    [Header("테마")]
    [SerializeField] private Button darkThemeButton;
    [SerializeField] private GameObject darkThemeMark;
    [SerializeField] private Button lightThemeButton;
    [SerializeField] private GameObject lightThemeMark;

    [Header("진동")]
    [SerializeField] private Button vibrationButton;
    [SerializeField] private Image vibrationIcon;
    [SerializeField] private Sprite vibrationOnSprite;
    [SerializeField] private Sprite vibrationOffSprite;

    [SerializeField] private Button logoutButton;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);

        SetupVolumeSlider(bgmSlider, OnBgmSliderChanged);
        SetupVolumeSlider(sfxSlider, OnSfxSliderChanged);
        bgmButton.onClick.AddListener(ToggleBgmMute);
        sfxButton.onClick.AddListener(ToggleSfxMute);
        AddPointerUpSound(sfxSlider); // 효과음 볼륨은 손을 뗄 때 들려줘서 크기를 확인하게 한다

        darkThemeButton.onClick.AddListener(() => SetTheme(UITheme.Dark));
        lightThemeButton.onClick.AddListener(() => SetTheme(UITheme.Light));
        vibrationButton.onClick.AddListener(ToggleVibration);
        logoutButton.onClick.AddListener(Logout);
    }

    public override void Open()
    {
        base.Open();

        // 저장된 값을 보여 준다. 알림 없이 넣어야 열 때 값이 다시 저장되거나 음소거가 풀리지 않는다
        bgmSlider.SetValueWithoutNotify(GameManager.Sound.BgmVolume);
        sfxSlider.SetValueWithoutNotify(GameManager.Sound.SfxVolume);
        RefreshMuteMarks();
        RefreshThemeMarks();
        RefreshVibrationIcon();

        // 배틀 중에는 로그아웃할 수 없으므로 버튼을 숨긴다
        logoutButton.gameObject.SetActive(SceneManager.GetActiveScene().name != GameConstants.SceneNames.BATTLE_SCENE);
    }

    private static void SetupVolumeSlider(Slider slider, UnityEngine.Events.UnityAction<float> onChanged)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.onValueChanged.AddListener(onChanged);
    }

    private static void AddPointerUpSound(Slider slider)
    {
        if (!slider.TryGetComponent(out EventTrigger trigger))
            trigger = slider.gameObject.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        entry.callback.AddListener(_ => GameManager.Sound.PlaySFX(GameConstants.Sfx.CLICK));
        trigger.triggers.Add(entry);
    }

    private void OnBgmSliderChanged(float value)
    {
        GameManager.Sound.BgmVolume = value;
        // 음소거 중에 슬라이더를 움직이면 소리가 나야 하므로 음소거를 푼다
        if (value > 0f && GameManager.Sound.BgmMuted)
            GameManager.Sound.BgmMuted = false;
        RefreshMuteMarks();
    }

    private void OnSfxSliderChanged(float value)
    {
        GameManager.Sound.SfxVolume = value;
        if (value > 0f && GameManager.Sound.SfxMuted)
            GameManager.Sound.SfxMuted = false;
        RefreshMuteMarks();
    }

    private void ToggleBgmMute()
    {
        GameManager.Sound.BgmMuted = !GameManager.Sound.BgmMuted;
        RefreshMuteMarks();
        if (bgmMuteMark.activeSelf)
            UITween.PopIn(bgmMuteMark.transform, 0f, 0f);
    }

    private void ToggleSfxMute()
    {
        GameManager.Sound.SfxMuted = !GameManager.Sound.SfxMuted;
        RefreshMuteMarks();
        if (sfxMuteMark.activeSelf)
            UITween.PopIn(sfxMuteMark.transform, 0f, 0f);
    }

    private void RefreshMuteMarks()
    {
        bgmMuteMark.SetActive(GameManager.Sound.BgmMuted);
        sfxMuteMark.SetActive(GameManager.Sound.SfxMuted);
    }

    private void SetTheme(UITheme theme)
    {
        var changed = UIManager.Instance.Theme != theme;
        UIManager.Instance.SetTheme(theme);
        RefreshThemeMarks();

        if (changed)
            UITween.PopIn((theme == UITheme.Dark ? darkThemeMark : lightThemeMark).transform, 0f, 0f);
    }

    private void RefreshThemeMarks()
    {
        var theme = UIManager.Instance.Theme;
        darkThemeMark.SetActive(theme == UITheme.Dark);
        lightThemeMark.SetActive(theme == UITheme.Light);
    }

    private void ToggleVibration()
    {
        Haptics.Enabled = !Haptics.Enabled;
        RefreshVibrationIcon();
        UITween.PopIn(vibrationIcon.transform, 0f, 0f);

        // 켰을 때 바로 한 번 울려서 진동이 켜졌는지 확인하게 한다 (끄면 Vibrate가 알아서 무시한다)
        Haptics.Vibrate(40);
    }

    private void RefreshVibrationIcon()
        => vibrationIcon.sprite = Haptics.Enabled ? vibrationOnSprite : vibrationOffSprite;

    private void Logout()
    {
        // 서버 데이터는 서버에 이미 저장되어 있으므로 로그아웃 때 따로 저장하지 않는다.
        GameManager.ServerLogin.Logout(); // 토큰, 계정 정보, PlayerDataManager 데이터 삭제
        
        // UIManager는 씬을 넘어 유지되므로, 열려 있는 팝업(설정, 프로필)을 닫고 이동한다.
        UIManager.Instance.CloseAll();

        GameManager.Scene.ChangeScene(GameConstants.SceneNames.LOGIN_SCENE);
    }
}
