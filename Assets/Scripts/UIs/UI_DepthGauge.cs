using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 입구를 0m로 삼고, 지형과 동일한 심도 기준으로 지층을 표시한다.
public sealed class UI_DepthGauge : MonoBehaviour
{
    private Transform player;
    private BaseCamp baseCamp;
    private TerrainManager terrain;
    [SerializeField] private CanvasGroup visibility;
    [SerializeField] private TMP_Text depthText;
    [SerializeField] private TMP_Text layerText;
    [SerializeField] private Image positionMarker;
    [SerializeField] private RectTransform ruler;
    [SerializeField] private RectTransform[] ticks;
    [SerializeField] private TMP_Text[] tickLabels;
    [SerializeField] private Image[] layerBands;
    [SerializeField] private RectTransform[] layerBoundaries;
    [SerializeField] private string[] layerNames;
    [SerializeField, Min(1f)] private float visibleMeters = 40f;
    [SerializeField, Min(1f)] private float tickMeters = 5f;

    private TerrainLayer[] layers;
    private float[] layerBottoms;
    private float[] layerTops;
    private bool ready;
    private int currentLayer = -1;
    private float layerHighlight;

    public void Init()
    {
        player = Object.FindFirstObjectByType<PlayerController>().transform;
        baseCamp = Object.FindFirstObjectByType<BaseCamp>();
        terrain = Object.FindFirstObjectByType<TerrainManager>();
        visibility.alpha = 0f;
    }

    public IEnumerator Show()
    {
        if (ready)
            yield break;

        while (!terrain.IsInitialLoadComplete)
            yield return null;

        TerrainData data = terrain.Data;
        layers = data.Layers;
        layerBottoms = new float[layers.Length];
        layerTops = new float[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            float top = data.LayerOriginY - layers[i].StartDepth;
            float bottom = i + 1 < layers.Length
                ? data.LayerOriginY - layers[i + 1].StartDepth
                : terrain.Settings.MiningBounds.min.y;
            layerTops[i] = terrain.transform.TransformPoint(Vector3.up * top).y;
            layerBottoms[i] = terrain.transform.TransformPoint(Vector3.up * bottom).y;
            layerBands[i].color = layers[i].Color;
            layerBoundaries[i].GetComponent<Image>().color =
                Color.Lerp(layers[i].Color, Color.white, 0.45f);
        }

        ready = true;
        visibility.alpha = 1f;
    }

    private void LateUpdate()
    {
        if (!ready)
            return;

        float depth = Mathf.Max(0f, baseCamp.EntrancePoint.position.y - player.position.y);
        RefreshDepth(depth);
        RefreshLayers(depth);
    }

    private void RefreshDepth(float depth)
    {
        depthText.SetText("{0:0} <size=24>m</size>", depth);
        float pixelsPerMeter = ruler.rect.height / visibleMeters;
        float firstDepth = Mathf.Floor((depth - visibleMeters * 0.5f) / tickMeters) * tickMeters;
        for (int i = 0; i < ticks.Length; i++)
        {
            float tickDepth = firstDepth + i * tickMeters;
            float y = (depth - tickDepth) * pixelsPerMeter;
            bool visible = tickDepth >= 0f && Mathf.Abs(y) <= ruler.rect.height * 0.5f;
            ticks[i].gameObject.SetActive(visible);
            if (!visible)
                continue;

            bool major = Mathf.RoundToInt(tickDepth / tickMeters) % 2 == 0;
            ticks[i].anchoredPosition = new Vector2(ticks[i].anchoredPosition.x, y);
            ticks[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, major ? 12f : 6f);
            tickLabels[i].gameObject.SetActive(major);
            tickLabels[i].SetText("{0:0}", tickDepth);
        }
    }

    private void RefreshLayers(float depth)
    {
        float entranceY = baseCamp.EntrancePoint.position.y;
        float pixelsPerMeter = ruler.rect.height / visibleMeters;
        float halfHeight = ruler.rect.height * 0.5f;
        for (int i = 0; i < layers.Length; i++)
        {
            float topDepth = Mathf.Max(0f, entranceY - layerTops[i]);
            float bottomDepth = entranceY - layerBottoms[i];
            float top = Mathf.Min(halfHeight, (depth - topDepth) * pixelsPerMeter);
            float bottom = Mathf.Max(-halfHeight, (depth - bottomDepth) * pixelsPerMeter);
            bool visible = top > bottom;
            layerBands[i].gameObject.SetActive(visible);
            if (visible)
            {
                RectTransform band = layerBands[i].rectTransform;
                band.anchoredPosition = new Vector2(band.anchoredPosition.x, (top + bottom) * 0.5f);
                band.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, top - bottom);
            }

            // 지층이 바뀌는 위쪽 경계만 표시한다.
            float boundaryY = (depth - topDepth) * pixelsPerMeter;
            layerBoundaries[i].gameObject.SetActive(
                topDepth > 0f && bottomDepth > topDepth && Mathf.Abs(boundaryY) <= halfHeight);
            layerBoundaries[i].anchoredPosition = new Vector2(layerBoundaries[i].anchoredPosition.x, boundaryY);
        }

        TerrainData data = terrain.Data;
        float localY = terrain.transform.InverseTransformPoint(player.position).y;
        byte typeId = data.GetLayerAtHeight(localY).TypeId;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].TypeId != typeId || currentLayer == i)
                continue;

            currentLayer = i;
            layerText.text = layerNames[i];
            layerHighlight = 1f;
        }

        Color accent = Color.Lerp(layers[currentLayer].Color, Color.white, 0.55f);
        positionMarker.color = accent;
        layerHighlight = Mathf.MoveTowards(layerHighlight, 0f, Time.unscaledDeltaTime / 1.5f);
        layerText.color = new Color(accent.r, accent.g, accent.b, Mathf.Lerp(0.65f, 1f, layerHighlight));
    }
}
