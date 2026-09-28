using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

// 유한한 생성 영역의 Region 통로와 지표 출입구를 만든다.
public class CaveGenerator
{
    // Carve 전 기본 밀도를 읽어 지하 통로와 지표 출입구를 만든다. 밀도는 수정하지 않는다.
    // bounds는 반지름·벽면 보정·밀도 전이를 포함할 지하 허용 영역이며 출입구는 이 영역 밖으로 이어진다.
    public CaveCarveSegment[] GenerateWithEntrance(
        TerrainData data, CaveSettings settings, float densityThreshold)
    {
        float margin = settings.RadiusRange.y + settings.NoiseAmplitude + (1f - densityThreshold) * settings.TransitionWidth;
        Bounds centerBounds = new Bounds(settings.Bounds.center, settings.Bounds.size - Vector3.one * (2f * margin));
        CaveCarveSegment[] segments = Generate(centerBounds, settings);

        CaveCarveSegment entrance = CreateCaveEntrance(data, segments, settings, densityThreshold);
        int entranceStart = segments.Length;
        int entranceCount = Mathf.CeilToInt(Vector3.Distance(entrance.Start, entrance.End) / settings.SegmentLength);
        System.Array.Resize(ref segments, entranceStart + entranceCount);
        Vector3 previous = entrance.Start;
        for (int i = 1; i <= entranceCount; i++)
        {
            Vector3 point = i == entranceCount ? entrance.End :
                Vector3.Lerp(entrance.Start, entrance.End, (float)i / entranceCount);
            segments[entranceStart + i - 1] = new CaveCarveSegment
            {
                Start = previous,
                End = point,
                Radius = entrance.Radius
            };
            previous = point;
        }

        return segments;
    }

    // 지표의 빈 공간에서 시작해 제한 경사로 연결 가능한 가장 가까운 동굴 구간 끝점에 붙인다.
    private CaveCarveSegment CreateCaveEntrance(
        TerrainData data, CaveCarveSegment[] segments, CaveSettings settings, float densityThreshold)
    {
        Vector3Int column = data.PositionToIndex(new Vector3(settings.EntrancePosition.x, 0f, settings.EntrancePosition.y));
        float surfaceY = 0f;
        bool foundSurface = false;
        for (int y = data.DensityFieldHeight - 1; y >= 0; y--)
        {
            float lower = data.GetDensity(new Vector3Int(column.x, y, column.z));
            float upper = data.GetDensity(new Vector3Int(column.x, y + 1, column.z));
            if (lower > densityThreshold && upper <= densityThreshold)
            {
                float t = (densityThreshold - lower) / (upper - lower);
                surfaceY = (y + t) * data.Resolution;
                foundSurface = true;
                break;
            }
        }
        if (!foundSurface)
        {
            throw new System.InvalidOperationException("동굴 출입구 위치에 지표 교차점이 없습니다. 출입구 X/Z와 기본 지형 설정을 확인하세요.");
        }

        float margin = settings.RadiusRange.y + settings.NoiseAmplitude + (1f - densityThreshold) * settings.TransitionWidth;
        Vector3 entrance = new Vector3(column.x * data.Resolution, surfaceY + margin, column.z * data.Resolution);
        float maxSlope = Mathf.Tan(settings.EntranceMaxSlope * Mathf.Deg2Rad);
        var candidates = new HashSet<Vector3>();
        foreach (CaveCarveSegment segment in segments)
        {
            for (int endpoint = 0; endpoint < 2; endpoint++)
            {
                Vector3 point = endpoint == 0 ? segment.Start : segment.End;
                Vector3 difference = entrance - point;
                float horizontalDistance = new Vector2(difference.x, difference.z).magnitude;
                if (point.y >= surfaceY || difference.y > horizontalDistance * maxSlope) continue;
                candidates.Add(point);
            }
        }
        var ordered = new List<Vector3>(candidates);
        ordered.Sort((a, b) =>
        {
            int distanceOrder = (a - entrance).sqrMagnitude.CompareTo((b - entrance).sqrMagnitude);
            if (distanceOrder != 0) return distanceOrder;
            int xOrder = a.x.CompareTo(b.x);
            if (xOrder != 0) return xOrder;
            int yOrder = a.y.CompareTo(b.y);
            return yOrder != 0 ? yOrder : a.z.CompareTo(b.z);
        });

        // 도착부 이전에 다른 동굴을 관통하면 진입로 바닥이 꺼지므로 해당 후보를 제외한다.
        float joinDistance = 2f * (settings.RadiusRange.y + settings.NoiseAmplitude);
        foreach (Vector3 connection in ordered)
        {
            float length = Vector3.Distance(entrance, connection);
            if (length <= joinDistance)
            {
                return new CaveCarveSegment { Start = entrance, End = connection, Radius = settings.RadiusRange.y };
            }
            Vector3 approach = (connection - entrance) * ((length - joinDistance) / length);
            float a = approach.sqrMagnitude;
            bool clear = true;
            foreach (CaveCarveSegment segment in segments)
            {
                // 도착부를 뺀 진입 선분과 기존 선분의 최단 거리를 계산한다.
                Vector3 direction = segment.End - segment.Start;
                Vector3 offset = entrance - segment.Start;
                float e = direction.sqrMagnitude;
                float c = Vector3.Dot(approach, offset);
                float s;
                float t = 0f;
                if (e == 0f)
                {
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    float b = Vector3.Dot(approach, direction);
                    float f = Vector3.Dot(direction, offset);
                    float denominator = a * e - b * b;
                    s = denominator > 0f ? Mathf.Clamp01((b * f - c * e) / denominator) : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f)
                    {
                        t = 0f;
                        s = Mathf.Clamp01(-c / a);
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                        s = Mathf.Clamp01((b - c) / a);
                    }
                }
                float clearance = settings.RadiusRange.y + segment.Radius + 2f * settings.NoiseAmplitude;
                if ((offset + approach * s - direction * t).sqrMagnitude < clearance * clearance)
                {
                    clear = false;
                    break;
                }
            }
            if (clear)
            {
                return new CaveCarveSegment { Start = entrance, End = connection, Radius = settings.RadiusRange.y };
            }
        }

        throw new System.InvalidOperationException("경사와 통로 간격을 만족하는 동굴 출입구를 만들 수 없습니다. 출입구 위치·동굴 영역·경사 설정을 확인하세요.");
    }

    // centerBounds는 지형 로컬 좌표의 통로 중심 허용 영역이다.
    // 호출 조건: 양수인 영역 크기와 settings.RegionCounts, settings.NodesPerRegion >= 2,
    // settings.ExtraConnectionsPerRegion >= 0, 0 < settings.RadiusRange.x <= settings.RadiusRange.y, settings.BendDistance >= 0, settings.SegmentLength > 0.
    // 벽면 보정과 밀도 전이를 포함한 외곽 여유, 지표 깊이는 호출 측에서 확보한다.
    public CaveCarveSegment[] Generate(Bounds centerBounds, CaveSettings settings)
    {
        Vector3 origin = centerBounds.min;
        Vector3 regionSize = new Vector3(
            centerBounds.size.x / settings.RegionCounts.x,
            centerBounds.size.y / settings.RegionCounts.y,
            centerBounds.size.z / settings.RegionCounts.z);
        var segments = new List<CaveCarveSegment>();
        for (int x = 0; x < settings.RegionCounts.x; x++)
        {
            for (int y = 0; y < settings.RegionCounts.y; y++)
            {
                for (int z = 0; z < settings.RegionCounts.z; z++)
                {
                    Vector3Int coordinate = new Vector3Int(x, y, z);
                    uint regionSeed = math.hash(new int4(settings.Seed, x, y, z));
                    List<Vector3> nodes = PlaceNodes(
                        settings.Seed, coordinate, settings.RegionCounts, origin, regionSize, settings.NodesPerRegion);
                    List<Vector2Int> connections = BuildConnections(
                        nodes, settings.NodesPerRegion, settings.ExtraConnectionsPerRegion);
                    Vector3 regionMin = origin + Vector3.Scale((Vector3)coordinate, regionSize);
                    Vector3 regionMax = origin + Vector3.Scale((Vector3)(coordinate + Vector3Int.one), regionSize);
                    BuildPaths(nodes, connections, regionSeed, regionMin, regionMax,
                        settings.RadiusRange.x, settings.RadiusRange.y, settings.BendDistance, settings.SegmentLength, segments);
                }
            }
        }

        return segments.ToArray();
    }

    // 내부 지점을 성긴 칸에 분산하고, 각 이웃 Region과 공유할 면 접점을 뒤에 붙인다.
    private static List<Vector3> PlaceNodes(
        int seed, Vector3Int coordinate, Vector3Int regionCounts,
        Vector3 origin, Vector3 regionSize, int nodeCount)
    {
        uint regionSeed = math.hash(new int4(seed, coordinate.x, coordinate.y, coordinate.z));
        var random = new Random(regionSeed | 1u);
        Vector3 regionMin = origin + Vector3.Scale((Vector3)coordinate, regionSize);
        Vector3Int cellCounts = Vector3Int.one;
        // 가장 긴 칸의 축부터 나누어 얇은 Region에도 지점들을 분산한다.
        while (cellCounts.x * cellCounts.y * cellCounts.z < nodeCount)
        {
            Vector3 cellSize = new Vector3(
                regionSize.x / cellCounts.x, regionSize.y / cellCounts.y, regionSize.z / cellCounts.z);
            if (cellSize.x >= cellSize.y && cellSize.x >= cellSize.z) cellCounts.x++;
            else if (cellSize.y >= cellSize.z) cellCounts.y++;
            else cellCounts.z++;
        }

        int cellCount = cellCounts.x * cellCounts.y * cellCounts.z;
        int[] cells = new int[cellCount];
        for (int i = 0; i < cellCount; i++) cells[i] = i;
        var nodes = new List<Vector3>(nodeCount + 6);
        for (int i = 0; i < nodeCount; i++)
        {
            int selected = random.NextInt(i, cellCount);
            int cell = cells[selected];
            cells[selected] = cells[i];
            cells[i] = cell;
            float3 jitter = random.NextFloat3(0.25f, 0.75f);
            Vector3 fraction = new Vector3(
                (cell / (cellCounts.y * cellCounts.z) + jitter.x) / cellCounts.x,
                (cell / cellCounts.z % cellCounts.y + jitter.y) / cellCounts.y,
                (cell % cellCounts.z + jitter.z) / cellCounts.z);
            nodes.Add(regionMin + Vector3.Scale(fraction, regionSize));
        }

        for (int axis = 0; axis < 3; axis++)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                int neighbor = coordinate[axis] + side;
                if (neighbor < 0 || neighbor >= regionCounts[axis]) continue;

                // 면의 음수 방향 Region 좌표와 축이 공유 키다. 양쪽에서 같은 위치를 계산한다.
                Vector3Int lower = coordinate;
                if (side < 0) lower[axis]--;
                uint faceSeed = math.hash(new int4(seed, lower.x, lower.y, lower.z))
                    ^ (0x9E3779B9u * (uint)(axis + 1));
                var faceRandom = new Random(faceSeed | 1u);
                float3 fraction = faceRandom.NextFloat3(0.25f, 0.75f);
                Vector3 portal = origin + Vector3.Scale(
                    (Vector3)lower + new Vector3(fraction.x, fraction.y, fraction.z), regionSize);
                portal[axis] = origin[axis] + (lower[axis] + 1) * regionSize[axis];
                nodes.Add(portal);
            }
        }

        return nodes;
    }

    // 내부 MST의 끝점 하나를 남기고 순환 및 경계 접점 연결을 추가한다.
    private static List<Vector2Int> BuildConnections(
        List<Vector3> nodes, int nodeCount, int extraConnections)
    {
        var connections = new List<Vector2Int>();
        bool[] connected = new bool[nodeCount];
        float[] distances = new float[nodeCount];
        int[] parents = new int[nodeCount];
        int[] degrees = new int[nodeCount];
        for (int i = 0; i < nodeCount; i++) distances[i] = float.PositiveInfinity;
        distances[0] = 0f;
        for (int step = 0; step < nodeCount; step++)
        {
            int next = -1;
            float shortest = float.PositiveInfinity;
            for (int i = 0; i < nodeCount; i++)
            {
                if (!connected[i] && distances[i] < shortest)
                {
                    next = i;
                    shortest = distances[i];
                }
            }
            connected[next] = true;
            if (step > 0)
            {
                int parent = parents[next];
                connections.Add(new Vector2Int(Mathf.Min(parent, next), Mathf.Max(parent, next)));
                degrees[parent]++;
                degrees[next]++;
            }
            for (int i = 0; i < nodeCount; i++)
            {
                float distance = (nodes[i] - nodes[next]).sqrMagnitude;
                if (!connected[i] && distance < distances[i])
                {
                    distances[i] = distance;
                    parents[i] = next;
                }
            }
        }

        int deadEnd = 0;
        while (degrees[deadEnd] != 1) deadEnd++;
        var candidates = new List<Vector2Int>();
        for (int a = 0; a < nodeCount; a++)
        {
            for (int b = a + 1; b < nodeCount; b++)
            {
                Vector2Int edge = new Vector2Int(a, b);
                if (a != deadEnd && b != deadEnd && !connections.Contains(edge)) candidates.Add(edge);
            }
        }
        candidates.Sort((a, b) =>
        {
            int distanceOrder = (nodes[a.x] - nodes[a.y]).sqrMagnitude.CompareTo(
                (nodes[b.x] - nodes[b.y]).sqrMagnitude);
            if (distanceOrder != 0) return distanceOrder;
            int startOrder = a.x.CompareTo(b.x);
            return startOrder != 0 ? startOrder : a.y.CompareTo(b.y);
        });
        for (int i = 0; i < Mathf.Min(extraConnections, candidates.Count); i++)
        {
            connections.Add(candidates[i]);
        }

        // 경계 접점은 반드시 내부 연결에 붙인다. 보존한 막다른 길은 사용하지 않는다.
        for (int portal = nodeCount; portal < nodes.Count; portal++)
        {
            int nearest = -1;
            float shortest = float.PositiveInfinity;
            for (int i = 0; i < nodeCount; i++)
            {
                float distance = (nodes[i] - nodes[portal]).sqrMagnitude;
                if (i != deadEnd && distance < shortest)
                {
                    nearest = i;
                    shortest = distance;
                }
            }
            connections.Add(new Vector2Int(nearest, portal));
        }

        return connections;
    }

    // 연결별 난수와 고정 끝점으로 cubic Bezier를 만들고 연속된 Capsule 구간으로 나눈다.
    private static void BuildPaths(
        List<Vector3> nodes, List<Vector2Int> connections, uint regionSeed,
        Vector3 regionMin, Vector3 regionMax, float minRadius, float maxRadius,
        float bendDistance, float segmentLength, List<CaveCarveSegment> segments)
    {
        foreach (Vector2Int connection in connections)
        {
            uint pathSeed = math.hash(new int4(unchecked((int)regionSeed), connection.x, connection.y, 1));
            var random = new Random(pathSeed | 1u);
            float radius = random.NextFloat(minRadius, maxRadius);
            Vector3 start = nodes[connection.x];
            Vector3 end = nodes[connection.y];
            Vector3 direction = (end - start).normalized;
            float bend = Mathf.Min(bendDistance, Vector3.Distance(start, end) * 0.25f);
            float3 random1 = random.NextFloat3(-1f, 1f);
            float3 random2 = random.NextFloat3(-1f, 1f);
            Vector3 offset1 = new Vector3(random1.x, random1.y, random1.z);
            Vector3 offset2 = new Vector3(random2.x, random2.y, random2.z);
            offset1 = (offset1 - direction * Vector3.Dot(offset1, direction)).normalized * bend;
            offset2 = (offset2 - direction * Vector3.Dot(offset2, direction)).normalized * bend;
            Vector3 control1 = Vector3.Max(regionMin, Vector3.Min(regionMax, Vector3.Lerp(start, end, 1f / 3f) + offset1));
            Vector3 control2 = Vector3.Max(regionMin, Vector3.Min(regionMax, Vector3.Lerp(start, end, 2f / 3f) + offset2));

            // 곡선의 최대 속도 상한으로 나누어 각 선분 길이를 segmentLength 이하로 제한한다.
            float maxControlEdge = Mathf.Max(Vector3.Distance(start, control1),
                Mathf.Max(Vector3.Distance(control1, control2), Vector3.Distance(control2, end)));
            int divisions = Mathf.Max(1, Mathf.CeilToInt(3f * maxControlEdge / segmentLength));
            Vector3 previous = start;
            for (int i = 1; i <= divisions; i++)
            {
                float t = (float)i / divisions;
                float u = 1f - t;
                Vector3 point = i == divisions ? end :
                    u * u * u * start + 3f * u * u * t * control1 + 3f * u * t * t * control2 + t * t * t * end;
                segments.Add(new CaveCarveSegment { Start = previous, End = point, Radius = radius });
                previous = point;
            }
        }
    }
}
