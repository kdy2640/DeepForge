using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Creates and initializes views once, then reuses them for state transitions.
/// </summary>
public sealed class CanvasController : MonoBehaviour
{
    public enum ViewCursorMode
    {
        Player,
        Free
    }

    public enum CanvasState
    {
        BaseView,
        Shop_Order,
        Shop_Deal,
        Smith_Equipment,
        Smith_Forge,
        Mine_Prepare,
        MineView
    }

    [Serializable]
    private sealed class ViewEntry
    {
        [SerializeField] private CanvasState state;
        [SerializeField] private UI_Base prefab;
        [SerializeField] private ViewCursorMode cursorMode;

        public CanvasState State => state;
        public ViewCursorMode CursorMode => cursorMode;
        public UI_Base Instance { get; private set; }

        public void Initialize(CanvasController owner)
        {
            Instance = Instantiate(prefab, owner.transform, false);
            Instance.name = prefab.name;
            Instance.gameObject.SetActive(false);
            Instance.Init(owner);
        }
    }

    [SerializeField] private CanvasState initialState = CanvasState.BaseView;
    [SerializeField] private List<ViewEntry> views = new();
    [SerializeField] private PlayerMiner playerMiner;
    [SerializeField] private BaseCamp baseCamp;
    [SerializeField] private SettingsPopup settingsPopup;

    private readonly Dictionary<CanvasState, ViewEntry> viewByState = new();
    private ViewEntry currentEntry;
    private Coroutine transitionCoroutine;
    private CanvasState? pendingState;
    private InputManager inputManager;
    private bool settingsOpen;
    private bool settingsClosing;
    private float previousTimeScale;

    public CanvasState? CurrentState { get; private set; }
    public CanvasState? PreviousState { get; private set; }
    public ViewCursorMode CurrentCursorMode { get; private set; }

    private void Awake()
    {
        inputManager = GameManager.Instance.InputManager;
        settingsPopup.Init(this);
        BuildViewLookup();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;
        if (settingsOpen)
            CloseSettings();
        else if (CurrentState == CanvasState.BaseView || CurrentState == CanvasState.MineView)
            OpenSettings();
        else if (CurrentState.HasValue && transitionCoroutine == null)
            RequestStateChange(CanvasState.BaseView);
    }

    public void OpenSettings()
    {
        if (settingsOpen || transitionCoroutine != null || GameManager.Instance.SceneController.IsChangingScene)
            return;
        if (CurrentState != CanvasState.BaseView && CurrentState != CanvasState.MineView)
            return;

        settingsOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        inputManager.SetGameplayInputBlocked(true);
        playerMiner.SetViewCursorMode(ViewCursorMode.Free);
        settingsPopup.Open();
    }

    public void CloseSettings()
    {
        if (!settingsOpen || settingsClosing || settingsPopup.IsBusy)
            return;
        StartCoroutine(CloseSettingsRoutine());
    }

    public IEnumerator CloseSettingsRoutine()
    {
        settingsClosing = true;
        yield return settingsPopup.Close();
        settingsOpen = false;
        settingsClosing = false;
        Time.timeScale = previousTimeScale;
        inputManager.SetGameplayInputBlocked(false);
        playerMiner.SetViewCursorMode(CurrentCursorMode);
    }

    private void OnDestroy()
    {
        if (!settingsOpen) return;
        Time.timeScale = previousTimeScale;
        inputManager.SetGameplayInputBlocked(false);
    }

    private void Start()
    {
        RequestStateChange(initialState);
    }

    public void RequestStateChange(CanvasState nextState)
    {
        if (settingsOpen) return;

        if (nextState == CanvasState.BaseView || nextState == CanvasState.MineView)
            nextState = baseCamp.IsPlayerInside ? CanvasState.BaseView : CanvasState.MineView;

        if (CurrentState == nextState && pendingState == null)
            return;

        pendingState = nextState;
        if (transitionCoroutine == null)
            transitionCoroutine = StartCoroutine(ProcessStateRequests());
    }

    private IEnumerator ProcessStateRequests()
    {
        while (pendingState.HasValue)
        {
            CanvasState nextState = pendingState.Value;
            pendingState = null;
            yield return ChangeState(nextState);
        }

        transitionCoroutine = null;
    }

    private IEnumerator ChangeState(CanvasState nextState)
    {
        ViewEntry nextEntry = viewByState[nextState];

        if (currentEntry != null)
            yield return currentEntry.Instance.Hide();

        currentEntry = nextEntry;
        PreviousState = CurrentState;
        CurrentState = nextState;
        CurrentCursorMode = nextEntry.CursorMode;
        playerMiner.SetViewCursorMode(CurrentCursorMode);
        yield return currentEntry.Instance.Show();
    }

    private void BuildViewLookup()
    {
        viewByState.Clear();
        foreach (ViewEntry entry in views)
        {
            viewByState.Add(entry.State, entry);
            entry.Initialize(this);
        }
    }
}
