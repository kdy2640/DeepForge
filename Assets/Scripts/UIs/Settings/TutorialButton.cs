using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialButton : MonoBehaviour
{
    public void Init(CanvasController owner)
    {
        GetComponent<Button>().onClick.AddListener(owner.OpenTutorial);
    }
}
