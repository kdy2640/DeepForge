using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InteractZone : MonoBehaviour
{
    [SerializeField] private CanvasController canvasController;
    [SerializeField] private CanvasController.CanvasState targetState;
    [SerializeField] private Transform promptCanvas;
    [SerializeField] private GameObject keyPrompt;
    [SerializeField] private Camera viewCamera;

    private bool isPlayerInside;

    private void Awake()
    {
        keyPrompt.SetActive(false);
    }

    private void Update()
    {
        if (isPlayerInside && canvasController.CanInteract && Keyboard.current != null
            && Keyboard.current.eKey.wasPressedThisFrame)
            canvasController.RequestStateChange(targetState);
    }

    private void LateUpdate()
    {
        promptCanvas.rotation = viewCamera.transform.rotation;
        keyPrompt.SetActive(isPlayerInside && canvasController.CanInteract);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        isPlayerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        isPlayerInside = false;
        keyPrompt.SetActive(false);
        canvasController.RequestStateChange(CanvasController.CanvasState.BaseView);
    }
}
