using UnityEngine;
using UnityEngine.UI;

public sealed class UI_MovementHUD : MonoBehaviour
{
    [SerializeField] private CanvasController viewController;
    [SerializeField] private PlayerMover playerMover;
    [SerializeField] private GameObject content;
    [Header("Slots ordered from bottom to top")]
    [SerializeField] private GameObject[] jumpSlots;
    [SerializeField] private Image[] jumpFills;
    [SerializeField] private GameObject[] dashSlots;
    [SerializeField] private Image[] dashFills;
    [SerializeField] private CanvasGroup dashGroup;
    [SerializeField] private GameObject dashLockedLabel;

    private void Awake()
    {
        content.SetActive(false);
    }

    private void LateUpdate()
    {
        CanvasController.CanvasState? state = viewController.CurrentState;
        bool visible = state == CanvasController.CanvasState.BaseView
            || state == CanvasController.CanvasState.MineView;
        if (content.activeSelf != visible) content.SetActive(visible);
        if (!visible) return;

        for (int i = 0; i < jumpSlots.Length; i++)
        {
            // Show only air jumps; leaving the ground does not spend a visible slot.
            bool unlocked = i < playerMover.MaxJumps - 1;
            if (jumpSlots[i].activeSelf != unlocked) jumpSlots[i].SetActive(unlocked);
            jumpFills[i].fillAmount = i < playerMover.RemainingJumps ? 1f : 0f;
        }

        bool dashLocked = playerMover.MaxDashCharges == 0;
        dashGroup.alpha = dashLocked ? 0.35f : 1f;
        if (dashLockedLabel.activeSelf != dashLocked) dashLockedLabel.SetActive(dashLocked);
        for (int i = 0; i < dashSlots.Length; i++)
        {
            bool unlocked = i < Mathf.Max(1, playerMover.MaxDashCharges);
            if (dashSlots[i].activeSelf != unlocked) dashSlots[i].SetActive(unlocked);
            dashFills[i].fillAmount = i < playerMover.DashCharges ? 1f
                : !dashLocked && i == playerMover.DashCharges ? playerMover.DashRechargeProgress : 0f;
        }
    }
}
