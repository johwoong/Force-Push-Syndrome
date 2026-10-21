using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoSingleton<UIManager>
{
    private const string ThemeKey = "UITheme";

    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private Image sceneFade;   // 씬이 바뀌면 이 화면이 걷히며 새 씬이 드러난다
    [SerializeField] private LoadingScreen loadingScreen;   // 추가: 씬 이동 로딩창

    private readonly Dictionary<Type, UIView> views = new();
    private readonly List<UIPopup> popups = new();

    private InputAction cancelAction;

    public UITheme Theme { get; private set; }
    public LoadingScreen Loading => loadingScreen; 

    public event Action ThemeChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
        => Instantiate(Resources.Load<UIManager>("UI/UIManager"));

    protected override void Awake()
    {
        base.Awake();

        Theme = (UITheme)PlayerPrefs.GetInt(ThemeKey, (int)UITheme.Dark);
        Register(overlayCanvas, UILayer.Overlay);

        cancelAction = InputSystem.actions.FindAction("UI/Cancel");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var color = sceneFade.color;
        color.a = 1f;
        sceneFade.DOKill();
        sceneFade.color = color;
        sceneFade.DOFade(0f, 0.4f).SetEase(Ease.InQuad).AsUI(sceneFade);
    }

    private void Update()
    {
        if (cancelAction.WasPressedThisFrame())
            CloseTopPopup();
    }

    internal void Register(Canvas canvas, UILayer layer)
    {
        foreach (var view in canvas.GetComponentsInChildren<UIView>(true))
        {
            view.Layer = layer;
            views[view.GetType()] = view;
        }
    }

    internal void Unregister(IEnumerable<UIView> targets)
    {
        foreach (var view in targets)
        {
            if (views.TryGetValue(view.GetType(), out var registered) && registered == view)
                views.Remove(view.GetType());

            if (view is UIPopup popup)
                popups.Remove(popup);
        }
    }

    public T Get<T>() where T : UIView
        => (T)views[typeof(T)];

    public void Open<T>() where T : UIView
        => Get<T>().Open();

    public void Close<T>() where T : UIView
        => Get<T>().Close();

    public void CloseAll()
    {
        foreach (var view in views.Values)
        {
            if (view.Layer != UILayer.HUD && view.IsOpen)
                view.Close();
        }
    }

    public void CloseTopPopup()
    {
        if (popups.Count > 0)
            popups[^1].Close();
    }

    internal void PushPopup(UIPopup popup)
    {
        popups.Remove(popup);
        popups.Add(popup);
    }

    internal void RemovePopup(UIPopup popup)
        => popups.Remove(popup);

    public void SetTheme(UITheme theme)
    {
        Theme = theme;
        PlayerPrefs.SetInt(ThemeKey, (int)theme);
        ThemeChanged?.Invoke();
    }
}
