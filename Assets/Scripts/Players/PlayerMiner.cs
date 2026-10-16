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

    [SerializeField] private TerrainManager terrainManager;
    [Header("채굴 타격 효과")]
    [SerializeField] private ParticleSystem miningChips;
    [SerializeField] private AudioSource miningAudio;
    [SerializeField, FormerlySerializedAs("miningTool")] private Transform miningToolAnchor;
    [Header("곡괭이 외형 · 장비 아이콘과 동일한 9단계")]
    [SerializeField] private Renderer pickaxeRenderer;
    [SerializeField] private Material[] pickaxeMaterials;
    InputManager inputManager;

    float nextLeftEditTime;
    bool isLeftMouseHolding;
    CameraController cameraController;
    bool isInputSubscribed;
    bool isUIInputBlocked;

    private Vector3 mouseHit = Vector3.zero;
    private Collider miningTargetCollider;
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
            GameManager.Instance.Utility.Audio.PlaySFX(SFXType.Mining_ModeChange);
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

    private void SetMiningMode(bool enabled)
    {
        cameraController.SetMiningMode(enabled);
        miningToolAnchor.DOKill(complete: true);
        miningToolAnchor.gameObject.SetActive(enabled);
        RefreshCursor();

        if (enabled && !isUIInputBlocked)
        { 
            UpdateMiningTarget();
        }
        else
        {
            isLeftMouseHolding = false;
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
            miningToolAnchor.DOKill(complete: true);
            isLeftMouseHolding = false;
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
        miningTargetCollider = null;
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
            LayerMask.GetMask("Plane", "Stone", "MineBoundary"), QueryTriggerInteraction.Collide) &&
            hit.collider.gameObject.layer != LayerMask.NameToLayer("MineBoundary"))
        { 

            anchor.SetActive(true);
            anchor.transform.position = hit.point;
            float anchorRadius = appliedMiningSettings.AnchorRadius;
            anchor.transform.localScale = Vector3.one * (anchorRadius * 2f);
            mouseHit = hit.point;
            miningTargetCollider = hit.collider;
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
        miningToolAnchor.DOKill(complete: true);
        isLeftMouseHolding = false;
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
        upgradeManager.UnsubscribeUpgradeChanged(RefreshMiningStats);
        inputManager = null;
        isInputSubscribed = false;
    }

    private void RefreshMiningStats()
    {
        pickaxeRenderer.sharedMaterial = pickaxeMaterials[upgradeManager.RuntimeLevel.GetEquipment((int)EquipmentType.Pickaxe)];
        EquipmentStat equipment = upgradeManager.RuntimeStat.Equipment;
        // 강화 대상은 계산된 최종값을 사용하고 나머지 채굴 설정은 유지한다.
        appliedMiningSettings = new MiningSetting
        {
            AnchorRadius = equipment.Get(EquipmentStatType.MiningRadius),
            HitPower = equipment.Get(EquipmentStatType.MiningPower),
            EditPower = settings.EditPower,
            EditInterval = settings.EditInterval,
            HitInterval = settings.HitInterval,
            HitPassCount = settings.HitPassCount,
            HitEdgeWidth = settings.HitEdgeWidth,
            HitDepthLayers = settings.HitDepthLayers,
            HitCorrectionWidth = settings.HitCorrectionWidth,
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


        if (isLeftMouseHolding)
        {
            if (Time.time >= nextLeftEditTime)
            {
                if (miningTargetCollider.gameObject.layer == LayerMask.NameToLayer("Stone"))
                {
                    miningTargetCollider.GetComponentInParent<StoneActor>().Collect();
                    PlayMiningFeedback();
                    hasErosionStart = false;
                    erosionElapsedTime = 0f;
                    nextLeftEditTime = Time.time + settings.HitInterval;
                    return;
                }

                if (!hasErosionStart)
                {
                    erosionStartDirection = -mouseHitNormal;
                    hasErosionStart = true;
                }

                float blend = Mathf.SmoothStep(0f, 1f, erosionElapsedTime / settings.ErosionDirectionBlendTime);
                Vector3 erosionDirection = Vector3.Slerp(erosionStartDirection, viewDirection, blend);
                if (terrainManager.MineCylinder(
                    mouseHit, appliedMiningSettings, erosionDirection))
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

    }

    private void PlayMiningFeedback()
    {
        miningChips.transform.SetPositionAndRotation(
            mouseHit + mouseHitNormal * 0.05f, Quaternion.LookRotation(mouseHitNormal));
        miningChips.Emit(Random.Range(4, 8));

        GameManager.Instance.Utility.Audio.PlaySFXRandomPitch(SFXType.Mining_Hit, 0.06f);
        GameManager.Instance.Utility.Audio.PlaySFX(SFXType.Mining_Debris);

        miningToolAnchor.DOKill(complete: true);
        miningToolAnchor.DOPunchPosition(new Vector3(-0.035f, -0.04f, 0.055f), 0.13f, 1, 0.25f);
        miningToolAnchor.DOPunchRotation(new Vector3(-12f, 0f, 5f), 0.13f, 1, 0.25f);
    }

}
