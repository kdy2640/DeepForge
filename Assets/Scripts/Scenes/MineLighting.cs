using UnityEngine;
using UnityEngine.Rendering;

// 입구 아래로 내려간 깊이에 따라 외부 조명과 플레이어 헤드라이트를 전환한다.
[DisallowMultipleComponent]
public sealed class MineLighting : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private BaseCamp baseCamp;
    [SerializeField] private Light sunlight;
    [SerializeField] private Light headlight;

    [Header("입구 아래 전환 깊이")]
    [SerializeField, Min(0f)] private float startDepth = 2f;
    [SerializeField, Min(0f)] private float fullDepth = 7f;
    [Header("광산 내부 조명")]
    [SerializeField, Range(0f, 1f)] private float undergroundAmbientScale = 0.06f;
    [SerializeField, Range(0f, 1f)] private float undergroundReflectionScale = 0.05f;
    [SerializeField] private Color undergroundFogColor = new Color(0.04f, 0.05f, 0.065f);

    private float outsideSunIntensity;
    private SphericalHarmonicsL2 outsideAmbientProbe;
    private float outsideReflectionIntensity;
    private Color outsideFogColor;
    private Camera playerCamera;
    private Color outsideBackgroundColor;
    private float headlightIntensity;

    private void Start()
    {
        outsideSunIntensity = sunlight.intensity;
        outsideAmbientProbe = RenderSettings.ambientProbe;
        outsideReflectionIntensity = RenderSettings.reflectionIntensity;
        outsideFogColor = RenderSettings.fogColor;
        playerCamera = headlight.GetComponentInParent<Camera>();
        outsideBackgroundColor = playerCamera.backgroundColor;
        headlightIntensity = headlight.intensity;
        ApplyLighting(0f);
    }

    private void LateUpdate()
    {
        float depth = baseCamp.EntrancePoint.position.y - player.position.y;
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(startDepth, fullDepth, depth));
        ApplyLighting(blend);
    }

    private void ApplyLighting(float blend)
    {
        sunlight.intensity = Mathf.Lerp(outsideSunIntensity, 0f, blend);
        headlight.intensity = headlightIntensity * blend;

        // 현재 값에 누적해서 곱하지 않고 지상에서 보관한 환경광을 기준으로 계산한다.
        float ambientScale = Mathf.Lerp(1f, undergroundAmbientScale, blend);
        SphericalHarmonicsL2 ambientProbe = outsideAmbientProbe;
        for (int channel = 0; channel < 3; channel++)
            for (int coefficient = 0; coefficient < 9; coefficient++)
                ambientProbe[channel, coefficient] *= ambientScale;
        RenderSettings.ambientProbe = ambientProbe;
        RenderSettings.reflectionIntensity = outsideReflectionIntensity * Mathf.Lerp(1f, undergroundReflectionScale, blend);
        RenderSettings.fogColor = Color.Lerp(outsideFogColor, undergroundFogColor, blend);
        // 원거리 지형이 그려지지 않는 곳도 안개와 같은 색으로 이어지게 한다.
        playerCamera.backgroundColor = Color.Lerp(outsideBackgroundColor, undergroundFogColor, blend);
    }
}
