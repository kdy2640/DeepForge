using UnityEngine;

public sealed class InteractZone : MonoBehaviour
{
    [SerializeField] private CanvasController canvasController;
    [SerializeField] private CanvasController.CanvasState targetState;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        canvasController.RequestStateChange(targetState);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        canvasController.RequestStateChange(CanvasController.CanvasState.BaseView);
    }
}
