using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using DG.Tweening;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerMiner : MonoBehaviour
{
    bool isMiningMode = true;
     
    [SerializeField]
    [FormerlySerializedAs("Anchor")]
    private GameObject anchor;
    [SerializeField] private MiningSetting settings = new MiningSetting();
    [Header("강화 적용 채굴 설정")]
    [SerializeField] private MiningSetting appliedMiningSettings = new MiningSetting();
    private UpgradeManager upgradeManager;
    [SerializeField] private int equipmentId;

    [SerializeField] private TerrainManager terrainManager;
    [Header("채굴 타격 효과")]
    [SerializeField] private ParticleSystem miningChips;
    [SerializeField] private AudioSource miningAudio;
    [SerializeField] private Transform miningTool;
    InputManager inputManager;

    float nextLeftEditTime;
    float nextRightEditTime;
    bool isLeftMouseHolding;
    bool isRightMouseHolding;
    CameraController cameraController;
    bool isInputSubscribed;
    bool isUIInputBlocked;

    private Vector3 mouseHit = Vector3.zero;
    private Vector3 mouseHitNormal;
    private Vector3 viewDirection;
    private Vector3 erosionStartDirection;
    private float erosionElapsedTime;
    private bool hasErosionStart;

    private void Awake()
    {
        cameraController = GetComponent<CameraController>();
    }

    private void OnChangeEditMode(InputAction.CallbackContext context)
    { 
        if (!isUIInputBlocked && context.performed)
        { 
            isMiningMode = !isMiningMode;
            SetMiningMode(isMiningMode);
        }
    }
    private void OnChangeShadingMode(InputAction.CallbackContext context)
    {
        if (!isUIInputBlocked && context.performed)
        {
            terrainManager.Settings.Shading.IsSmoothShading = !terrainManager.Settings.Shading.IsSmoothShading;
            terrainManager.RegenerateAllChunks();
        }
    }

    private void OnLeftMouse(InputAction.CallbackContext context)
    {
        if (isUIInputBlocked) return;

        if (context.started)
        {
            isLeftMouseHolding = true;
            nextLeftEditTime = 0f;
            hasErosionStart = false;
            erosionElapsedTime = 0f;
            if (!isMiningMode || terrainManager == null || cameraController == null) return;

            UpdateMiningTarget();
            if (!anchor.activeSelf) return;

            UpdateTerrainEditing();
        }
        else if (context.canceled)
        {
            isLeftMouseHolding = false;
            hasErosionStart = false;
            erosionElapsedTime = 0f;
        }
    }

    private void OnRightMouse(InputAction.CallbackContext context)
    {
        if (isUIInputBlocked) return;

        if (context.started)
        {
            isRightMouseHolding = true;
            nextRightEditTime = 0f;
            if (!isMiningMode || terrainManager == null || cameraController == null) return;

            UpdateMiningTarget();
            if (!anchor.activeSelf) return;

            terrainManager.AddDensitySphere(mouseHit, settings, isAdding: true, worldErosionDirection: Vector3.zero);
            nextRightEditTime = Time.time + Mathf.Max(0.01f, settings.EditInterval);
        }
        else if (context.canceled)
        {
            isRightMouseHolding = false;
        }
    }

    private void SetMiningMode(bool enabled)
    {
        cameraController.SetMiningMode(enabled);
        miningTool.gameObject.SetActive(enabled);
        RefreshCursor();

        if (enabled && !isUIInputBlocked)
        { 
            UpdateMiningTarget();
        }
        else
        {
            isLeftMouseHolding = false;
            isRightMouseHolding = false;
            hasErosionStart = false;
            erosionElapsedTime = 0f;
            anchor.SetActive(false);
        }
    }

    public void SetViewCursorMode(CanvasController.ViewCursorMode mode)
    {
        isUIInputBlocked = mode == CanvasController.ViewCursorMode.Free;
        cameraController.SetUIInputBlocked(isUIInputBlocked);
        if (isUIInputBlocked)
        {
            isLeftMouseHolding = false;
            isRightMouseHolding = false;
            hasErosionStart = false;
            erosionElapsedTime = 0f;
            anchor.SetActive(false);
        }
        RefreshCursor();
    }

    private void RefreshCursor()
    {
        bool locked = isMiningMode && !isUIInputBlocked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void UpdateMiningTarget()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            anchor.SetActive(false);
            hasErosionStart = false;
            erosionElapsedTime = 0f;
            return;
        }

        Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
        Ray ray = mainCamera.ScreenPointToRay(screenCenter);
        // 중앙 레이는 near plane에서 시작하므로 카메라 기준 가시 거리에서 이를 뺀다.
        float maxDistance = Mathf.Min(100f, mainCamera.farClipPlane - mainCamera.nearClipPlane);
        if (RenderSettings.fog && RenderSettings.fogMode == FogMode.Linear)
        {
            maxDistance = Mathf.Min(maxDistance, RenderSettings.fogEndDistance - mainCamera.nearClipPlane);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance,
            LayerMask.GetMask("Plane", "Stone"), QueryTriggerInteraction.Collide))
        { 

            anchor.SetActive(true);
            anchor.transform.position = hit.point;
            float anchorRadius = isRightMouseHolding ? settings.AnchorRadius : appliedMiningSettings.AnchorRadius;
            anchor.transform.localScale = Vector3.one * (anchorRadius * 2f);
            mouseHit = hit.point;
            mouseHitNormal = hit.normal;
            viewDirection = ray.direction;
            return;
        }

        anchor.SetActive(false);
        hasErosionStart = false;
        erosionElapsedTime = 0f;
    }


    private void Start()
    {
        inputManager = GameManager.Instance.InputManager;

        SubscribeInputEvents();
        SetMiningMode(isMiningMode);
    }

    private void OnEnable()
    {
        SubscribeInputEvents();
    }

    private void OnDisable()
    {
        UnsubscribeInputEvents();
        miningTool.DOKill(complete: true);
        isLeftMouseHolding = false;
        isRightMouseHolding = false;
        hasErosionStart = false;
        erosionElapsedTime = 0f;
    }

    private void SubscribeInputEvents()
    {
        if (isInputSubscribed || GameManager.Instance == null)
        {
            return;
        }

        if (inputManager == null)
        {
            inputManager = GameManager.Instance.InputManager;
        }

        if (inputManager == null)
        {
            return;
        }

        inputManager.Subscribe(InputEvent.ChangeEditMode, OnChangeEditMode);
        inputManager.Subscribe(InputEvent.ChangeShadingMode, OnChangeShadingMode);
        inputManager.Subscribe(InputEvent.LeftMouseClick, OnLeftMouse);
        inputManager.Subscribe(InputEvent.RightMouseClick, OnRightMouse);
        upgradeManager = GameManager.Instance.Upgrade;
        upgradeManager.SubscribeUpgradeChanged(RefreshMiningStats);
        RefreshMiningStats();
        isInputSubscribed = true;
    }

    private void UnsubscribeInputEvents()
    {
        if (!isInputSubscribed || inputManager == null)
        {
            return;
        }

        inputManager.Unsubscribe(InputEvent.ChangeEditMode, OnChangeEditMode);
        inputManager.Unsubscribe(InputEvent.ChangeShadingMode, OnChangeShadingMode);
        inputManager.Unsubscribe(InputEvent.LeftMouseClick, OnLeftMouse);
        inputManager.Unsubscribe(InputEvent.RightMouseClick, OnRightMouse);
        upgradeManager.UnsubscribeUpgradeChanged(RefreshMiningStats);
        inputManager = null;
        isInputSubscribed = false;
    }

    private void RefreshMiningStats()
    {
        bool isUpgraded = upgradeManager.RuntimeLevel.GetEquipment(equipmentId) > 0;
        // 기본 설정은 유지하고, 매번 기본값으로부터 계산하여 중복 갱신에도 배율이 누적되지 않는다.
        appliedMiningSettings = new MiningSetting
        {
            AnchorRadius = settings.AnchorRadius * (isUpgraded ? 2f : 1f),
            HitPower = settings.HitPower * (isUpgraded ? 3f : 1f),
            EditPower = settings.EditPower,
            EditInterval = settings.EditInterval,
            HitInterval = settings.HitInterval,
            HitPassCount = settings.HitPassCount,
            HitEdgeWidth = settings.HitEdgeWidth,
            ErosionDirectionBlendTime = settings.ErosionDirectionBlendTime,
            ErosionSideStrength = settings.ErosionSideStrength,
            UseErosionDistanceFalloff = settings.UseErosionDistanceFalloff
        };
    }
    private void Update()
    {
        if (isUIInputBlocked) return;

        if (isMiningMode)
        {
            UpdateMiningTarget();
        }

        UpdateTerrainEditing();
    }

    private void UpdateTerrainEditing()
    {
        if (isUIInputBlocked || !isMiningMode || terrainManager == null || cameraController == null || !anchor.activeSelf)
        {
            hasErosionStart = false;
            erosionElapsedTime = 0f;
            return;
        }

        float interval = Mathf.Max(0.01f, settings.EditInterval);

        if (isLeftMouseHolding)
        {
            if (Time.time >= nextLeftEditTime)
            {
                if (!hasErosionStart)
                {
                    erosionStartDirection = -mouseHitNormal;
                    hasErosionStart = true;
                }

                float blend = Mathf.SmoothStep(0f, 1f, erosionElapsedTime / settings.ErosionDirectionBlendTime);
                Vector3 erosionDirection = Vector3.Slerp(erosionStartDirection, viewDirection, blend);
                if (terrainManager.AddDensitySphere(
                    mouseHit, appliedMiningSettings, isAdding: false, worldErosionDirection: erosionDirection))
                {
                    // 내부 계산 횟수 대신 실제 타격 간격으로 방향 전환을 진행한다.
                    erosionElapsedTime = Mathf.Min(erosionElapsedTime + settings.HitInterval, settings.ErosionDirectionBlendTime);
                    PlayMiningFeedback();
                }
                nextLeftEditTime = Time.time + settings.HitInterval;
            }
        }
        else
        {
            nextLeftEditTime = 0f;
        }

        if (isRightMouseHolding)
        {
            if (Time.time >= nextRightEditTime)
            {
                terrainManager.AddDensitySphere(mouseHit, settings, isAdding: true, worldErosionDirection: Vector3.zero);
                nextRightEditTime = Time.time + interval;
            }
        }
        else
        {
            nextRightEditTime = 0f;
        }
    }

    private void PlayMiningFeedback()
    {
        miningChips.transform.SetPositionAndRotation(
            mouseHit + mouseHitNormal * 0.05f, Quaternion.LookRotation(mouseHitNormal));
        miningChips.Emit(Random.Range(4, 8));

        miningAudio.pitch = Random.Range(0.94f, 1.06f);
        miningAudio.PlayOneShot(miningAudio.clip);

        miningTool.DOKill(complete: true);
        miningTool.DOPunchRotation(new Vector3(-12f, 0f, 5f), 0.13f, 1, 0.25f);
    }

}
