using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

// 유한한 생성 영역의 Region 통로와 지표 출입구를 만든다.
public class CaveGenerator
{
    // 경사로 끝을 고정 갈림길로 삼아 두 내부 노드에 연결한다.
    public CaveCarveSegment[] GenerateWithEntrance(
        CaveSettings settings, float densityThreshold, Vector3 entrancePosition, Vector3 entranceForward)
    {
        float margin = settings.RadiusRange.y + settings.NoiseAmplitude + (1f - densityThreshold) * settings.TransitionWidth;
        Bounds centerBounds = new Bounds(settings.Bounds.center, settings.Bounds.size - Vector3.one * (2f * margin));
        CaveCarveSegment[] segments = Generate(centerBounds, settings, out List<Vector3> nodes);

        Vector3 forward = new Vector3(entranceForward.x, 0f, entranceForward.z).normalized;
        Vector3 entranceStart = entrancePosition + Vector3.up * margin;
        Vector3 fork = entranceStart + forward * settings.EntranceLength -
            Vector3.up * (settings.EntranceLength * Mathf.Tan(settings.EntranceMaxSlope * Mathf.Deg2Rad));
        List<CaveCarveSegment> branches = CreateCaveEntrance(segments, nodes, settings, centerBounds, fork, forward);
        var result = new List<CaveCarveSegment>(segments);
        int divisions = Mathf.CeilToInt(Vector3.Distance(entranceStart, fork) / settings.SegmentLength);
        Vector3 previous = entranceStart;
        for (int i = 1; i <= divisions; i++)
        {
            Vector3 point = i == divisions ? fork : Vector3.Lerp(entranceStart, fork, (float)i / divisions);
            result.Add(new CaveCarveSegment { Start = previous, End = point, Radius = settings.RadiusRange.y });
            previous = point;
        }
        result.AddRange(branches);
        return result.ToArray();
    }

    // 실제 내부 노드를 거리순으로 검사하고, 갈림길에서 방향이 벌어지는 두 경로를 선택한다.
    private List<CaveCarveSegment> CreateCaveEntrance(
        CaveCarveSegment[] segments, List<Vector3> nodes, CaveSettings settings,
        Bounds centerBounds, Vector3 entrance, Vector3 forward)
    {
        float maxSlope = Mathf.Tan(settings.EntranceMaxSlope * Mathf.Deg2Rad);
        var candidates = new List<Vector3>();
        foreach (Vector3 point in nodes)
        {
            Vector3 difference = point - entrance;
            float horizontalDistance = new Vector2(difference.x, difference.z).magnitude;
            if (difference.y >= 0f || -difference.y > horizontalDistance * maxSlope ||
                Vector3.Dot(difference, forward) <= 0f) continue;
            candidates.Add(point);
        }
        candidates.Sort((a, b) =>
        {
            int distanceOrder = (a - entrance).sqrMagnitude.CompareTo((b - entrance).sqrMagnitude);
            if (distanceOrder != 0) return distanceOrder;
            int xOrder = a.x.CompareTo(b.x);
            if (xOrder != 0) return xOrder;
            int yOrder = a.y.CompareTo(b.y);
            return yOrder != 0 ? yOrder : a.z.CompareTo(b.z);
        });

        // 입구는 일반 동굴보다 높다. 입구 연결에만 이 범위를 사용한다.
        Vector3 pathMin = Vector3.Min(centerBounds.min, entrance);
        Vector3 pathMax = Vector3.Max(centerBounds.max, entrance);
        // 접속점의 중심선뿐 아니라 통로 반지름까지 합쳐지는 도착부를 제외한다.
        float joinDistance = 3f * (settings.RadiusRange.y + settings.NoiseAmplitude);
        float forkDistance = 4f * (settings.RadiusRange.y + settings.NoiseAmplitude);
        var paths = new List<List<CaveCarveSegment>>();
        for (int candidate = 0; candidate < candidates.Count; candidate++)
        {
            var path = new List<CaveCarveSegment>();
            BuildPaths(new List<Vector3> { entrance, candidates[candidate] }, new List<Vector2Int> { new Vector2Int(0, 1) },
                math.hash(new int4(settings.Seed, candidate, 0, 17)), pathMin, pathMax,
                settings.RadiusRange.y, settings.RadiusRange.y, settings.BendDistance, settings.SegmentLength, path);

            // 수평 곡선을 따라 일정한 경사로 내려간다. 제어점의 높이 난수로 급경사가 생기지 않게 한다.
            float horizontalLength = 0f;
            foreach (CaveCarveSegment leg in path)
            {
                Vector3 delta = leg.End - leg.Start;
                horizontalLength += new Vector2(delta.x, delta.z).magnitude;
            }
            var slopedPath = new List<CaveCarveSegment>(path.Count);
            float horizontalTravelled = 0f;
            Vector3 previous = entrance;
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 delta = path[i].End - path[i].Start;
                horizontalTravelled += new Vector2(delta.x, delta.z).magnitude;
                Vector3 end = path[i].End;
                end.y = Mathf.Lerp(entrance.y, candidates[candidate].y, horizontalTravelled / horizontalLength);
                if (i == path.Count - 1) end = candidates[candidate];
                int divisions = Mathf.CeilToInt(Vector3.Distance(previous, end) / settings.SegmentLength);
                Vector3 start = previous;
                for (int j = 1; j <= divisions; j++)
                {
                    Vector3 point = j == divisions ? end : Vector3.Lerp(start, end, (float)j / divisions);
                    slopedPath.Add(new CaveCarveSegment { Start = previous, End = point, Radius = path[i].Radius });
                    previous = point;
                }
            }
            path = slopedPath;

            bool walkable = true;
            float length = 0f;
            foreach (CaveCarveSegment leg in path)
            {
                Vector3 delta = leg.End - leg.Start;
                length += delta.magnitude;
                if (delta.y > 0.00001f || -delta.y > new Vector2(delta.x, delta.z).magnitude * maxSlope + 0.00001f)
                {
                    walkable = false;
                    break;
                }
            }
            if (!walkable) continue;

            bool networkClear = false;
            // 먼저 기존 동굴과 검사한 뒤, 앞서 통과한 경로들과 한 쌍씩 검사한다.
            for (int group = -1; group < paths.Count; group++)
            {
                if (group >= 0)
                {
                    Vector3 heading = path[0].End - entrance;
                    Vector3 otherHeading = paths[group][0].End - entrance;
                    heading.y = 0f;
                    otherHeading.y = 0f;
                    if (Vector3.Angle(heading, otherHeading) < 45f) continue;
                }
                IReadOnlyList<CaveCarveSegment> obstacles = group < 0 ? (IReadOnlyList<CaveCarveSegment>)segments : paths[group];
                bool clear = true;
                float travelled = 0f;
                foreach (CaveCarveSegment leg in path)
                {
                    float legLength = Vector3.Distance(leg.Start, leg.End);
                    float from = Mathf.Max(travelled, group < 0 ? 0f : forkDistance);
                    float to = Mathf.Min(travelled + legLength, group < 0 ? length - joinDistance : length);
                    travelled += legLength;
                    if (from >= to) continue;
                    Vector3 start = Vector3.Lerp(leg.Start, leg.End, (from - travelled + legLength) / legLength);
                    Vector3 end = Vector3.Lerp(leg.Start, leg.End, (to - travelled + legLength) / legLength);
                    Vector3 approach = end - start;
                    float a = approach.sqrMagnitude;
                    float obstacleTravelled = 0f;
                    foreach (CaveCarveSegment obstacle in obstacles)
                    {
                        Vector3 obstacleStart = obstacle.Start;
                        if (group >= 0)
                        {
                            float obstacleLength = Vector3.Distance(obstacle.Start, obstacle.End);
                            float skipped = Mathf.Max(0f, forkDistance - obstacleTravelled);
                            obstacleTravelled += obstacleLength;
                            if (skipped >= obstacleLength) continue;
                            obstacleStart = Vector3.Lerp(obstacle.Start, obstacle.End, skipped / obstacleLength);
                        }
                        float clearance = leg.Radius + obstacle.Radius + 2f * settings.NoiseAmplitude;
                        // 멀리 떨어진 구간은 선분 거리 계산을 생략한다.
                        Vector3 min = Vector3.Min(start, end) - Vector3.one * clearance;
                        Vector3 max = Vector3.Max(start, end) + Vector3.one * clearance;
                        Vector3 obstacleMin = Vector3.Min(obstacleStart, obstacle.End);
                        Vector3 obstacleMax = Vector3.Max(obstacleStart, obstacle.End);
                        if (obstacleMax.x < min.x || obstacleMin.x > max.x ||
                            obstacleMax.y < min.y || obstacleMin.y > max.y ||
                            obstacleMax.z < min.z || obstacleMin.z > max.z) continue;

                        Vector3 direction = obstacle.End - obstacleStart;
                        Vector3 offset = start - obstacleStart;
                        float e = direction.sqrMagnitude;
                        float c = Vector3.Dot(approach, offset);
                        float s;
                        float t = 0f;
                        if (e == 0f) s = Mathf.Clamp01(-c / a);
                        else
                        {
                            float b = Vector3.Dot(approach, direction);
                            float f = Vector3.Dot(direction, offset);
                            float denominator = a * e - b * b;
                            s = denominator > 0f ? Mathf.Clamp01((b * f - c * e) / denominator) : 0f;
                            t = (b * s + f) / e;
                            if (t < 0f) { t = 0f; s = Mathf.Clamp01(-c / a); }
                            else if (t > 1f) { t = 1f; s = Mathf.Clamp01((b - c) / a); }
                        }
                        if ((offset + approach * s - direction * t).sqrMagnitude < clearance * clearance)
                        {
                            clear = false;
                            break;
                        }
                    }
                    if (!clear) break;
                }
                if (group < 0)
                {
                    networkClear = clear;
                    if (!clear) break;
                }
                else if (clear)
                {
                    var branches = new List<CaveCarveSegment>(paths[group]);
                    branches.AddRange(path);
                    return branches;
                }
            }
            if (networkClear) paths.Add(path);
        }
        throw new System.InvalidOperationException("경사와 통로 간격을 만족하는 입구 갈림길을 만들 수 없습니다. 출입구 위치·동굴 영역·경사 설정을 확인하세요.");
    }

    // centerBounds는 지형 로컬 좌표의 통로 중심 허용 영역이다.
    // 호출 조건: 양수인 영역 크기와 settings.RegionCounts, settings.NodesPerRegion >= 2,
    // settings.ExtraConnectionsPerRegion >= 0, 0 < settings.RadiusRange.x <= settings.RadiusRange.y, settings.BendDistance >= 0, settings.SegmentLength > 0.
    // 벽면 보정과 밀도 전이를 포함한 외곽 여유, 지표 깊이는 호출 측에서 확보한다.
    public CaveCarveSegment[] Generate(Bounds centerBounds, CaveSettings settings, out List<Vector3> entranceNodes)
    {
        entranceNodes = new List<Vector3>();
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
                    for (int i = 0; i < settings.NodesPerRegion; i++) entranceNodes.Add(nodes[i]);
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
            float3 jitter = random.NextFloat3(new float3(0.25f), new float3(0.75f, 1f, 0.75f));
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
                float3 fraction = faceRandom.NextFloat3(new float3(0.25f), new float3(0.75f, 1f, 0.75f));
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
