using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        Mine_Prepare
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

    private readonly Dictionary<CanvasState, ViewEntry> viewByState = new();
    private ViewEntry currentEntry;
    private Coroutine transitionCoroutine;
    private CanvasState? pendingState;

    public CanvasState? CurrentState { get; private set; }
    public CanvasState? PreviousState { get; private set; }
    public ViewCursorMode CurrentCursorMode { get; private set; }

    private void Awake()
    {
        BuildViewLookup();
    }

    private void Start()
    {
        RequestStateChange(initialState);
    }

    public void RequestStateChange(CanvasState nextState)
    {
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
