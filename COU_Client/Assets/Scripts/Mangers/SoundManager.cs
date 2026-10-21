using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGM / 효과음 재생 + 볼륨·음소거 저장.
///
/// 클립은 Resources 폴더에서 이름으로 불러온다.
///   Assets/Resources/Sound/BGM/Main.mp3   → GameManager.Sound.PlayBGM("Main");
///   Assets/Resources/Sound/SFX/Click.wav  → GameManager.Sound.PlaySFX("Click");
/// 하위 폴더도 그대로 쓸 수 있다. (Sound/SFX/UI/Click → "UI/Click")
/// 볼륨/음소거는 PlayerPrefs에 저장되어 다음 실행 때 복원된다.
/// </summary>
public class SoundManager : MonoSingleton<SoundManager>
{
    private const string KEY_BGM_VOLUME = "Sound.BgmVolume";
    private const string KEY_SFX_VOLUME = "Sound.SfxVolume";
    private const string KEY_BGM_MUTED = "Sound.BgmMuted";
    private const string KEY_SFX_MUTED = "Sound.SfxMuted";
    private const string KEY_MUTED = "Sound.Muted"; // 예전 전체 음소거 키. 새 키가 없을 때만 이어받는다

    private const int SFX_POOL_INITIAL = 8;
    private const int SFX_POOL_MAX = 24;
    private const int SFX_LOOP_MAX = 8; // 동시에 돌릴 수 있는 루프 효과음 수. 일반 효과음 풀(SFX_POOL_MAX)과는 따로 쓴다
    private const float SFX_MIN_INTERVAL = 0.04f; // 같은 클립이 한 프레임에 겹쳐 터지는 걸 방지
    private const float DEFAULT_BGM_FADE = 1f;

    // BGM은 두 채널을 번갈아 쓰며 크로스페이드한다.
    private class BgmChannel
    {
        public AudioSource Source;
        public string Name;
        public float Level;  // 페이드 배율 0~1
        public float Target; // 목표 배율
        public float Speed;  // 초당 변화량
    }

    private readonly Dictionary<string, AudioClip> bgmCache = new();
    private readonly Dictionary<string, AudioClip> sfxCache = new();
    private readonly HashSet<string> missing = new(); // 없는 클립은 경고 한 번만, 이후엔 다시 찾지 않는다
    private readonly List<AudioSource> sfxPool = new();
    private readonly Dictionary<AudioClip, float> lastSfxTime = new();

    // 루프 효과음 한 채널. Token이 0이면 비어 있고, 쓸 때마다 새 번호를 받는다 (끝난 소리를 가리키는 오래된 핸들을 걸러내기 위함)
    private class SfxLoop
    {
        public AudioSource Source;
        public int Token;
        public float VolumeScale;
        public bool IsPaused;
    }

    private readonly List<SfxLoop> sfxLoops = new();
    private int nextLoopToken = 1;

    private BgmChannel[] bgmChannels;
    private int currentBgm;

    private float bgmVolume = 1f;
    private float sfxVolume = 1f;
    private bool bgmMuted;
    private bool sfxMuted;

    /// <summary>BGM 볼륨 (0~1). 저장됨.</summary>
    public float BgmVolume
    {
        get => bgmVolume;
        set { bgmVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KEY_BGM_VOLUME, bgmVolume); }
    }

    /// <summary>효과음 볼륨 (0~1). 저장됨.</summary>
    public float SfxVolume
    {
        get => sfxVolume;
        set { sfxVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KEY_SFX_VOLUME, sfxVolume); }
    }

    /// <summary>BGM만 음소거. 볼륨 값은 그대로 두고 소리만 끈다. 저장됨.</summary>
    public bool BgmMuted
    {
        get => bgmMuted;
        set
        {
            if (bgmMuted == value) return;
            bgmMuted = value;
            PlayerPrefs.SetInt(KEY_BGM_MUTED, bgmMuted ? 1 : 0);
            PlayerPrefs.Save();
            OnMuteChanged?.Invoke(IsMuted);
        }
    }

    /// <summary>효과음만 음소거. 볼륨 값은 그대로 두고 소리만 끈다. 저장됨.</summary>
    public bool SfxMuted
    {
        get => sfxMuted;
        set
        {
            if (sfxMuted == value) return;
            sfxMuted = value;
            PlayerPrefs.SetInt(KEY_SFX_MUTED, sfxMuted ? 1 : 0);
            PlayerPrefs.Save();
            OnMuteChanged?.Invoke(IsMuted);
        }
    }

    /// <summary>BGM과 효과음이 모두 음소거일 때 true. 설정하면 둘을 함께 바꾼다.</summary>
    public bool IsMuted
    {
        get => bgmMuted && sfxMuted;
        set
        {
            if (bgmMuted == value && sfxMuted == value) return;
            bgmMuted = value;
            sfxMuted = value;
            PlayerPrefs.SetInt(KEY_BGM_MUTED, value ? 1 : 0);
            PlayerPrefs.SetInt(KEY_SFX_MUTED, value ? 1 : 0);
            PlayerPrefs.Save();
            OnMuteChanged?.Invoke(IsMuted);
        }
    }

    public string CurrentBgmName => bgmChannels?[currentBgm].Target > 0f ? bgmChannels[currentBgm].Name : null;

    public event Action<bool> OnMuteChanged;

    // 첫 씬이 로드되면 저장된 음소거 상태를 바로 적용한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() => _ = Instance;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return; // 중복 인스턴스는 base에서 파괴 예약됨

        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);

        bgmVolume = PlayerPrefs.GetFloat(KEY_BGM_VOLUME, 1f);
        sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 1f);
        var legacyMuted = PlayerPrefs.GetInt(KEY_MUTED, 0);
        bgmMuted = PlayerPrefs.GetInt(KEY_BGM_MUTED, legacyMuted) == 1;
        sfxMuted = PlayerPrefs.GetInt(KEY_SFX_MUTED, legacyMuted) == 1;
        AudioListener.volume = 1f; // 음소거는 BGM/효과음 각자 처리한다

        bgmChannels = new BgmChannel[2];
        for (var i = 0; i < bgmChannels.Length; i++)
        {
            var source = CreateSource($"BGM_{i}");
            source.loop = true;
            bgmChannels[i] = new BgmChannel { Source = source };
        }

        for (var i = 0; i < SFX_POOL_INITIAL; i++)
            sfxPool.Add(CreateSource($"SFX_{i}"));

        gameObject.AddComponent<ButtonClickSound>(); // 모든 버튼 클릭음
    }

    private AudioSource CreateSource(string objName)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D
        return source;
    }

    private void Update()
    {
        if (bgmChannels == null) return; 

        var dt = Time.unscaledDeltaTime;
        foreach (var ch in bgmChannels)
        {
            if (ch.Level != ch.Target)
                ch.Level = Mathf.MoveTowards(ch.Level, ch.Target, ch.Speed * dt);

            if (ch.Source.isPlaying)
            {
                ch.Source.volume = bgmMuted ? 0f : ch.Level * bgmVolume;
                if (ch.Level <= 0f && ch.Target <= 0f)
                    ch.Source.Stop();
            }
        }

        UpdateSfxLoops();
    }

    // ───────────────────────── BGM ─────────────────────────

    /// <summary>Resources/Sound/BGM/{name} 을 크로스페이드로 재생. 같은 곡이 이미 재생 중이면 무시.</summary>
    public void PlayBGM(string name, float fadeDuration = DEFAULT_BGM_FADE)
    {
        var current = bgmChannels[currentBgm];
        if (current.Name == name && current.Target > 0f) return;

        var clip = LoadClip(bgmCache, GameConstants.Paths.BGM_PATH, name);
        if (clip == null) return;

        FadeTo(current, 0f, fadeDuration);

        currentBgm = (currentBgm + 1) % bgmChannels.Length;
        var next = bgmChannels[currentBgm];
        next.Name = name;
        next.Source.clip = clip;
        next.Source.volume = 0f;
        next.Level = 0f;
        next.Source.Play();
        FadeTo(next, 1f, fadeDuration);
    }

    /// <summary>BGM을 페이드아웃하며 정지. fadeDuration이 0이면 즉시 정지.</summary>
    public void StopBGM(float fadeDuration = DEFAULT_BGM_FADE)
    {
        foreach (var ch in bgmChannels)
        {
            FadeTo(ch, 0f, fadeDuration);
            if (fadeDuration <= 0f) ch.Source.Stop(); // 즉시 정지
        }
    }

    private static void FadeTo(BgmChannel ch, float target, float duration)
    {
        ch.Target = target;
        ch.Speed = duration <= 0f ? float.MaxValue : 1f / duration;
        if (duration <= 0f) ch.Level = target;
    }

    // ───────────────────────── SFX ─────────────────────────

    /// <summary>Resources/Sound/SFX/{name} 효과음 재생.</summary>
    public void PlaySFX(string name, float volumeScale = 1f, float pitch = 1f)
    {
        var clip = LoadClip(sfxCache, GameConstants.Paths.SFX_PATH, name);
        PlaySFX(clip, volumeScale, pitch);
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (clip == null || sfxMuted) return;

        var now = Time.unscaledTime;
        if (lastSfxTime.TryGetValue(clip, out var last) && now - last < SFX_MIN_INTERVAL) return;
        lastSfxTime[clip] = now;

        var source = GetFreeSfxSource();
        if (source == null) return; // 풀이 가득 차면 이번 소리는 생략

        source.clip = clip;
        source.volume = Mathf.Clamp01(volumeScale) * sfxVolume;
        source.pitch = pitch;
        source.Play();
    }

    /// <summary>자주 쓰는 효과음을 미리 메모리에 올려 첫 재생 끊김을 줄인다.</summary>
    public void PreloadSFX(params string[] names)
    {
        foreach (var n in names)
            LoadClip(sfxCache, GameConstants.Paths.SFX_PATH, n);
    }

    /// <summary>재생 중인 효과음을 모두 즉시 정지한다. (씬을 떠날 때 긴 효과음이 다음 씬으로 이어지지 않게)</summary>
    public void StopAllSFX()
    {
        foreach (var source in sfxPool)
        {
            if (source == null || !source.isPlaying) continue;
            source.Stop();
            source.clip = null;
        }

        StopAllLoopSFX();
    }

    // ───────────────────────── SFX 루프 ─────────────────────────

    /// <summary>
    /// 효과음을 반복 재생한다. 돌려받은 핸들로 피치를 바꾸거나 멈춘다. 채널이 가득 차면 None(IsValid가 false)을 돌려준다.
    /// 볼륨과 음소거는 재생 중에도 설정을 따라가고, Time.timeScale이 0이면 일시정지한다.
    /// </summary>
    public SfxLoopHandle PlayLoopSFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (clip == null) return SfxLoopHandle.None;

        var loop = GetFreeSfxLoop();
        if (loop == null) return SfxLoopHandle.None;

        loop.Token = nextLoopToken++;
        loop.VolumeScale = Mathf.Clamp01(volumeScale);
        loop.IsPaused = false;

        var source = loop.Source;
        source.clip = clip;
        source.loop = true;
        source.pitch = pitch;
        source.volume = GetLoopVolume(loop);
        source.Play();

        return new SfxLoopHandle(sfxLoops.IndexOf(loop), loop.Token);
    }

    /// <summary>재생 중인 루프의 피치를 바꾼다. 이미 끝났거나 다른 소리가 쓰는 채널이면 무시한다.</summary>
    public void SetLoopPitch(SfxLoopHandle handle, float pitch)
    {
        if (TryGetLoop(handle, out var loop))
            loop.Source.pitch = pitch;
    }

    /// <summary>루프를 멈추고 채널을 돌려준다. 이미 끝난 핸들이면 무시한다.</summary>
    public void StopLoopSFX(SfxLoopHandle handle)
    {
        if (TryGetLoop(handle, out var loop))
            ReleaseLoop(loop);
    }

    private void StopAllLoopSFX()
    {
        foreach (var loop in sfxLoops)
            if (loop.Token != 0) ReleaseLoop(loop);
    }

    private bool TryGetLoop(SfxLoopHandle handle, out SfxLoop loop)
    {
        loop = null;
        if (!handle.IsValid || handle.Slot >= sfxLoops.Count) return false;

        var candidate = sfxLoops[handle.Slot];
        if (candidate.Token != handle.Token) return false; // 이미 끝났거나 다른 소리가 쓰는 채널

        loop = candidate;
        return true;
    }

    private static void ReleaseLoop(SfxLoop loop)
    {
        loop.Source.Stop();
        loop.Source.clip = null;
        loop.Token = 0;
    }

    private SfxLoop GetFreeSfxLoop()
    {
        foreach (var loop in sfxLoops)
            if (loop.Token == 0) return loop;

        if (sfxLoops.Count >= SFX_LOOP_MAX) return null;

        var added = new SfxLoop { Source = CreateSource($"SFXLoop_{sfxLoops.Count}") };
        sfxLoops.Add(added);
        return added;
    }

    private float GetLoopVolume(SfxLoop loop) => sfxMuted ? 0f : loop.VolumeScale * sfxVolume;

    // 재생 중인 루프에 현재 볼륨/음소거를 반영하고, 게임이 멈춰 있는 동안(Time.timeScale 0)은 일시정지한다.
    private void UpdateSfxLoops()
    {
        var gamePaused = Time.timeScale <= 0f;

        foreach (var loop in sfxLoops)
        {
            if (loop.Token == 0) continue;

            loop.Source.volume = GetLoopVolume(loop);

            if (gamePaused && !loop.IsPaused)
            {
                loop.Source.Pause();
                loop.IsPaused = true;
            }
            else if (!gamePaused && loop.IsPaused)
            {
                loop.Source.UnPause();
                loop.IsPaused = false;
            }
        }
    }

    private AudioSource GetFreeSfxSource()
    {
        foreach (var s in sfxPool)
            if (!s.isPlaying) return s;

        if (sfxPool.Count >= SFX_POOL_MAX) return null;

        var added = CreateSource($"SFX_{sfxPool.Count}");
        sfxPool.Add(added);
        return added;
    }

    // ───────────────────────── 공통 ─────────────────────────

    public void ToggleMute() => IsMuted = !IsMuted;

    private AudioClip LoadClip(Dictionary<string, AudioClip> cache, string folder, string name, bool warn = true)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (cache.TryGetValue(name, out var cached)) return cached;

        var path = folder + name;
        if (missing.Contains(path)) return null; // 이미 없다고 확인된 클립은 매번 Resources를 뒤지지 않는다

        var clip = Resources.Load<AudioClip>(path);
        if (clip == null)
        {
            missing.Add(path);
            if (warn)
                Debug.LogWarning($"[SoundManager] 클립을 찾을 수 없음: Resources/{path}");
            return null;
        }

        cache[name] = clip;
        return clip;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) PlayerPrefs.Save(); // 볼륨 슬라이더 변경분 저장
    }

    protected override void OnDestroy()
    {
        PlayerPrefs.Save();
        base.OnDestroy();
    }
}

/// <summary>SoundManager.PlayLoopSFX가 돌려주는 루프 효과음 조작권. 피치 변경과 정지에 쓴다. default나 None은 유효하지 않은 핸들이다.</summary>
public readonly struct SfxLoopHandle
{
    public static readonly SfxLoopHandle None = default;

    public readonly int Slot;
    public readonly int Token;

    public SfxLoopHandle(int slot, int token)
    {
        Slot = slot;
        Token = token;
    }

    public bool IsValid => Token != 0;
}
