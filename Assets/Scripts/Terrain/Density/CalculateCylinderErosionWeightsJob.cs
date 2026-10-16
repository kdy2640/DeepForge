using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// 제거 후 밀도 복사본에서 원기둥 주변 표면의 수축 강도를 계산한다.
[BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
internal struct CalculateCylinderErosionWeightsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<byte> Densities;
    public Vector3Int Origin;
    public Vector3Int SampleCount;
    public float Resolution;
    public float DensityThreshold;
    public Vector3 Start;
    public Vector3 Direction;
    public float Length;
    public float Radius;
    public float CorrectionWidth;
    [WriteOnly] public NativeArray<float> Weights;

    public void Execute(int flatIndex)
    {
        Vector3Int local = new Vector3Int(
            flatIndex / (SampleCount.y * SampleCount.z),
            flatIndex / SampleCount.z % SampleCount.y,
            flatIndex % SampleCount.z);
        Vector3Int index = Origin + local;
        float density = Densities[flatIndex] * (1f / 255f);
        float maxWeight = 0f;
        for (int axis = 0; axis < 3; axis++)
        {
            int stride = axis == 0 ? SampleCount.y * SampleCount.z :
                (axis == 1 ? SampleCount.z : 1);
            for (int direction = -1; direction <= 1; direction += 2)
            {
                int neighborAxis = local[axis] + direction;
                if (neighborAxis < 0 || neighborAxis >= SampleCount[axis]) continue;
                float neighborDensity = Densities[flatIndex + direction * stride] * (1f / 255f);
                if ((density > DensityThreshold) == (neighborDensity > DensityThreshold)) continue;

                // 양쪽 endpoint에서 같은 순서로 보간한다.
                Vector3Int neighbor = index;
                neighbor[axis] += direction;
                Vector3Int edgeStart = direction > 0 ? index : neighbor;
                Vector3Int edgeEnd = direction > 0 ? neighbor : index;
                float startDensity = direction > 0 ? density : neighborDensity;
                float endDensity = direction > 0 ? neighborDensity : density;
                float t = (DensityThreshold - startDensity) / (endDensity - startDensity);
                Vector3 surface = Vector3.Lerp((Vector3)edgeStart * Resolution,
                    (Vector3)edgeEnd * Resolution, t);
                Vector3 offset = surface - Start;
                float along = Vector3.Dot(offset, Direction);
                float radialOutside = Mathf.Max(0f, (offset - Direction * along).magnitude - Radius);
                float axialOutside = Mathf.Max(0f, Mathf.Max(-along, along - Length));
                float distance = Mathf.Sqrt(radialOutside * radialOutside + axialOutside * axialOutside);
                float falloff = Mathf.Clamp01(1f - distance / CorrectionWidth);
                maxWeight = Mathf.Max(maxWeight, falloff * falloff * (3f - 2f * falloff));
            }
        }
        Weights[flatIndex] = maxWeight;
    }
}
