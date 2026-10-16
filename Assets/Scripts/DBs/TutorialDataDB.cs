using System.Collections.Generic;
using UnityEngine;

public static class TutorialDataDB
{
    private static readonly Dictionary<CanvasController.CanvasState, TutorialDataSO> tutorialDataMap = new();

    static TutorialDataDB()
    {
        foreach (TutorialDataSO data in Resources.LoadAll<TutorialDataSO>("SOs/TutorialDatas"))
            tutorialDataMap.Add(data.State, data);
    }

    public static TutorialDataSO GetData(CanvasController.CanvasState state)
    {
        return tutorialDataMap[state];
    }
}
