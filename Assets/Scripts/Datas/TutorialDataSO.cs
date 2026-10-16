using UnityEngine;

[CreateAssetMenu(menuName = "Game/TutorialDataSO")]
public sealed class TutorialDataSO : ScriptableObject
{
    [SerializeField] private CanvasController.CanvasState state;
    [SerializeField] private string title;
    [SerializeField, TextArea(5, 12)] private string content;

    public CanvasController.CanvasState State => state;
    public string Title => title;
    public string Content => content;
}
