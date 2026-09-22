using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates and initializes the six Hub views once, then reuses them for state transitions.
/// </summary>
public sealed class HubCanvasController : MonoBehaviour
{
    public enum HubCanvasState
    {
        HubView,
        Shop_Order,
        Shop_Deal,
        Smith_Reinforce,
        Smith_Forge,
        Mine_Prepare
    }

    [Serializable]
    private sealed class ViewEntry
    {
        [SerializeField] private HubCanvasState state;
        [SerializeField] private UI_Base prefab;

        public HubCanvasState State => state;
        public UI_Base Instance { get; private set; }

        public void Initialize(HubCanvasController owner)
        {
            Instance = Instantiate(prefab, owner.transform, false);
            Instance.name = prefab.name;
            Instance.gameObject.SetActive(false);
            Instance.Init(owner);
        }
    }

    [SerializeField] private HubCanvasState initialState = HubCanvasState.HubView;
    [SerializeField] private List<ViewEntry> views = new();

    private readonly Dictionary<HubCanvasState, ViewEntry> viewByState = new();
    private ViewEntry currentEntry;
    private Coroutine transitionCoroutine;
    private HubCanvasState? pendingState;

    public HubCanvasState? CurrentState { get; private set; }
    public HubCanvasState? PreviousState { get; private set; }

    private void Awake()
    {
        BuildViewLookup();
    }

    private void Start()
    {
        RequestStateChange(initialState);
    }

    public void RequestStateChange(HubCanvasState nextState)
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
            HubCanvasState nextState = pendingState.Value;
            pendingState = null;
            yield return ChangeState(nextState);
        }

        transitionCoroutine = null;
    }

    private IEnumerator ChangeState(HubCanvasState nextState)
    {
        ViewEntry nextEntry = viewByState[nextState];

        if (currentEntry != null)
            yield return currentEntry.Instance.Hide();

        currentEntry = nextEntry;
        PreviousState = CurrentState;
        CurrentState = nextState;
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
