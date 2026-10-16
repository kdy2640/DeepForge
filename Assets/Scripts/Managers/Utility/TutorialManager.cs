using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private readonly Dictionary<CanvasController.CanvasState, bool> tutorialProgressionMap = new();

    private void Awake()
    {
        Initialize();
    }

    public bool GetTutorialProgressed(CanvasController.CanvasState state)
    {
        return tutorialProgressionMap[state];
    }

    public void ResolveTutorial(CanvasController.CanvasState state)
    {
        tutorialProgressionMap[state] = true;
    }

    public void Initialize()
    {
        tutorialProgressionMap.Clear();
        foreach (CanvasController.CanvasState state in Enum.GetValues(typeof(CanvasController.CanvasState)))
            tutorialProgressionMap.Add(state, false);
    }

    public List<TutorialSaveData> CreateTutorialSaveData()
    {
        List<TutorialSaveData> saveData = new();

        foreach (KeyValuePair<CanvasController.CanvasState, bool> pair in tutorialProgressionMap)
            saveData.Add(new TutorialSaveData(pair.Key.ToString(), pair.Value));

        return saveData;
    }

    public void LoadTutorialSaveData(List<TutorialSaveData> saveData)
    {
        Initialize();

        if (saveData == null)
            return;

        foreach (TutorialSaveData savedState in saveData)
        {
            if (savedState == null
                || !Enum.TryParse(savedState.id, out CanvasController.CanvasState state)
                || !tutorialProgressionMap.ContainsKey(state))
                continue;

            tutorialProgressionMap[state] = savedState.flag;
        }
    }

    public void ResetTutorialSaveData()
    {
        Initialize();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Log Tutorial Progress")]
    private void LogTutorialProgress()
    {
        foreach (KeyValuePair<CanvasController.CanvasState, bool> pair in tutorialProgressionMap)
            Debug.Log($"{pair.Key}: {pair.Value}");
    }
#endif
}
