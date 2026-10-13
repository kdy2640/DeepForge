using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class ForgedGearIconBaker : MonoBehaviour
{
    private const int IconSize = 256;
    [SerializeField] private Camera captureCamera;
    [SerializeField] private Light captureLight;
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Material sourceMaterial;

    private readonly Queue<ForgedGear> pending = new();
    private Material captureMaterial;
    private Cubemap captureReflection;
    private RenderTexture target;
    private RenderTexture previewTarget;
    private int previewId = -1;
    private ForgedGearData previewMaterials;
    private bool previewPending;
    private GameObject model;
    private int modelId = -1;

    private void Awake()
    {
        captureMaterial = new Material(sourceMaterial);
        captureReflection = new Cubemap(1, TextureFormat.RGBA32, false);
        for (int face = 0; face < 6; face++)
            captureReflection.SetPixel((CubemapFace)face, 0, 0, new Color(0.65f, 0.65f, 0.65f));
        captureReflection.Apply(false, true);
        target = new RenderTexture(IconSize, IconSize, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
        {
            name = "Forged gear capture",
            useMipMap = false,
            antiAliasing = 1
        };
        target.Create();
        captureCamera.enabled = false;
        captureLight.enabled = false;
    }

    public void Request(ForgedGear gear)
    {
        if (gear.IconReleased || gear.Icon != null || pending.Contains(gear)) return;
        pending.Enqueue(gear);
    }

    public RenderTexture RequestPreview(int gearId, ForgedGearData materials)
    {
        if (previewTarget == null)
        {
            previewTarget = new RenderTexture(1024, 256, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "Forge preview",
                useMipMap = false,
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            previewTarget.Create();
        }

        if (previewId != gearId || previewMaterials.metalOreId != materials.metalOreId
            || previewMaterials.handleOreId != materials.handleOreId)
        {
            previewId = gearId;
            previewMaterials = materials;
            previewPending = true;
        }
        return previewTarget;
    }

    public void ReleasePreview()
    {
        previewPending = false;
        previewId = -1;
        if (previewTarget == null) return;
        previewTarget.Release();
        Destroy(previewTarget);
        previewTarget = null;
    }

    private void LateUpdate()
    {
        if (previewPending)
        {
            RenderGear(previewId, previewMaterials, new Vector3(0f, 0f, -90f), previewTarget);
            previewPending = false;
            return;
        }

        while (pending.Count > 0)
        {
            ForgedGear gear = pending.Dequeue();
            if (gear.IconReleased || gear.Icon != null) continue;
            gear.SetIcon(Bake(gear));
            // At most one model render per frame, even when a whole inventory opens.
            break;
        }
    }

    private Sprite Bake(ForgedGear gear)
    {
        RenderGear(gear.id, gear.data, ForgedGearDB.GetData(gear.id).IconRotation, target);
        var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false)
        {
            name = "Forged gear " + gear.id,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        // Drop the CPU copy before copying the rendered pixels entirely on the GPU.
        texture.Apply(false, true);
        Graphics.CopyTexture(target, texture);
        return Sprite.Create(texture, new Rect(0, 0, IconSize, IconSize),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

    private void RenderGear(int gearId, ForgedGearData materials, Vector3 rotation, RenderTexture destination)
    {
        ForgedGearSO data = ForgedGearDB.GetData(gearId);
        if (modelId != gearId)
        {
            if (model != null) Destroy(model);
            model = Instantiate(data.IconModel, modelRoot);
            modelId = gearId;
            foreach (Transform part in model.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = modelRoot.gameObject.layer;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = captureMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(rotation);
        model.SetActive(true);
        captureMaterial.SetColor("_Metal", OreDataDB.GetData(materials.metalOreId).Color);
        Color handle = OreDataDB.GetData(materials.handleOreId).Color;
        captureMaterial.SetColor("_Wood", handle);
        captureMaterial.SetColor("_Leather", handle);

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        captureCamera.transform.SetPositionAndRotation(bounds.center - Vector3.forward * (bounds.extents.z + 5f), Quaternion.identity);
        captureCamera.aspect = (float)destination.width / destination.height;
        captureCamera.orthographicSize = Mathf.Max(bounds.extents.x / captureCamera.aspect, bounds.extents.y) * 1.12f;
        captureCamera.farClipPlane = bounds.size.z + 10f;

        // The icon uses fixed lighting regardless of the current depth, fog, or time of day.
        var sceneLights = new List<Light>();
        foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light != captureLight && light.type == LightType.Directional && light.enabled)
            {
                sceneLights.Add(light);
                light.enabled = false;
            }
        }
        bool fog = RenderSettings.fog;
        SphericalHarmonicsL2 ambient = RenderSettings.ambientProbe;
        float reflection = RenderSettings.reflectionIntensity;
        DefaultReflectionMode reflectionMode = RenderSettings.defaultReflectionMode;
        Texture reflectionTexture = RenderSettings.customReflectionTexture;
        Light sun = RenderSettings.sun;
        try
        {
            RenderSettings.fog = false;
            SphericalHarmonicsL2 iconAmbient = new();
            iconAmbient.AddAmbientLight(new Color(0.65f, 0.65f, 0.65f));
            RenderSettings.ambientProbe = iconAmbient;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = captureReflection;
            RenderSettings.reflectionIntensity = 1f;
            captureLight.enabled = true;
            RenderSettings.sun = captureLight;
            RenderPipeline.SubmitRenderRequest(captureCamera,
                new UniversalRenderPipeline.SingleCameraRequest { destination = destination });
        }
        finally
        {
            captureLight.enabled = false;
            model.SetActive(false);
            RenderSettings.sun = sun;
            RenderSettings.fog = fog;
            RenderSettings.ambientProbe = ambient;
            RenderSettings.reflectionIntensity = reflection;
            RenderSettings.customReflectionTexture = reflectionTexture;
            RenderSettings.defaultReflectionMode = reflectionMode;
            foreach (Light light in sceneLights) light.enabled = true;
        }

    }

    private void OnDestroy()
    {
        pending.Clear();
        ReleasePreview();
        Destroy(model);
        Destroy(captureMaterial);
        Destroy(captureReflection);
        target.Release();
        Destroy(target);
    }
}
