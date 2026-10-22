using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// 월드 생성 조건, 변경 청크와 Region 파일을 소유한다. Unity 데이터 접근은 메인 스레드에서만 한다.
public sealed class WorldManager : MonoBehaviour
{
    public const int CurrentVersion = 7;
    private const int RegionSize = 4;
    private const uint RegionMagic = 0x31574644;
    private const int HeaderSize = 40;
    private const int EntrySize = 16;

    public WorldSaveData Metadata { get; private set; }
    public bool IsLoaded { get; private set; }

    private string worldPath;
    private TerrainData terrain;
    // Densities는 로드 적용 후 해제한다. 이후 저장은 현재 TerrainData에서 복사한다.
    private readonly Dictionary<Vector3Int, ChunkSaveData> changedChunks = new Dictionary<Vector3Int, ChunkSaveData>();
    private readonly Dictionary<Vector3Int, int> dirtyRegions = new Dictionary<Vector3Int, int>();
    private readonly Dictionary<Vector3Int, RegionSaveData> pendingRegions = new Dictionary<Vector3Int, RegionSaveData>();
    private int revision;
    private Task<bool> saveTask = Task.FromResult(true);
    private readonly CancellationTokenSource lifetime = new CancellationTokenSource();

    public async Task LoadOrCreateWorldAsync(TerrainSettings settings)
    {
        await saveTask;
        IsLoaded = false;
        terrain = null;
        changedChunks.Clear();
        dirtyRegions.Clear();
        pendingRegions.Clear();
        worldPath = Path.Combine(Application.persistentDataPath, "World");
        string metaPath = Path.Combine(worldPath, "world.meta");
        WorldSaveData meta = null;
        foreach (string path in new[] { metaPath, metaPath + ".bak" })
        {
            if (!File.Exists(path)) continue;
            try
            {
                meta = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(path));
                if (meta == null || !Guid.TryParse(meta.WorldId, out _) || meta.Version <= 0)
                    throw new InvalidDataException("월드 메타데이터 형식이 올바르지 않습니다.");
                // 호환되지 않는 현재 파일은 이전 버전의 백업으로 되돌리지 않는다.
                if (meta.Version != CurrentVersion) break;
                ValidateMetadata(meta);
                if (path != metaPath) WriteFile(metaPath, File.ReadAllBytes(path), true, lifetime.Token);
                break;
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidDataException)
            {
                meta = null;
                Debug.LogWarning($"월드 메타데이터를 읽지 못했습니다: {path}\n{error.Message}");
            }
        }

        if (meta == null || meta.Version != CurrentVersion)
        {
            // 이전 생성 규칙의 청크가 새 동굴을 덮지 않도록 원본 월드를 보존한다.
            string fullWorldPath = Path.GetFullPath(worldPath);
            if (!string.Equals(Path.GetDirectoryName(fullWorldPath), Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("월드 백업 경로가 저장 루트 밖입니다.");
            if (Directory.Exists(fullWorldPath))
            {
                string backupPath = fullWorldPath + "_Backup_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fffffff");
                Directory.Move(fullWorldPath, backupPath);
            }
            meta = new WorldSaveData
            {
                Version = CurrentVersion,
                WorldId = Guid.NewGuid().ToString("N"),
                RegionSize = RegionSize,
                Grid = settings.Grid,
                Surface = settings.Surface,
                Density = settings.Density,
                Cave = settings.Cave,
                MiningBounds = settings.MiningBounds,
                StoneSeed = settings.Stones.Seed,
                StonesPerChunk = settings.Stones.CountPerChunk
            };
            // 설정 객체와 분리하여 이후 인스펙터 변경이 저장 기준을 바꾸지 않게 한다.
            meta = JsonUtility.FromJson<WorldSaveData>(JsonUtility.ToJson(meta));
            ValidateMetadata(meta);
            Directory.CreateDirectory(Path.Combine(worldPath, "Regions"));
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(meta, true));
            WriteFile(metaPath, bytes, false, lifetime.Token);
            File.Copy(metaPath, metaPath + ".bak", true);
        }
        Metadata = meta;
        // 기존 Grid 객체를 참조하는 TerrainGridGeometry도 같은 값으로 동작하도록 값만 적용한다.
        settings.Grid.Width = meta.Grid.Width;
        settings.Grid.DensityFieldHeight = meta.Grid.DensityFieldHeight;
        settings.Grid.ChunkSize = meta.Grid.ChunkSize;
        settings.Grid.Resolution = meta.Grid.Resolution;
        settings.Surface = JsonUtility.FromJson<TerrainSurfaceSettings>(JsonUtility.ToJson(meta.Surface));
        settings.Density = JsonUtility.FromJson<TerrainDensitySettings>(JsonUtility.ToJson(meta.Density));
        settings.Cave = JsonUtility.FromJson<CaveSettings>(JsonUtility.ToJson(meta.Cave));
        settings.MiningBounds = meta.MiningBounds;
        settings.Stones.Seed = meta.StoneSeed;
        settings.Stones.CountPerChunk = meta.StonesPerChunk;

        var warnings = new List<string>();
        string root = worldPath;
        CancellationToken token = lifetime.Token;
        List<RegionSaveData> regions = await Task.Run(() => ReadRegions(root, meta, warnings, token), token);
        foreach (string warning in warnings) Debug.LogWarning(warning);
        foreach (RegionSaveData region in regions)
            foreach (ChunkSaveData chunk in region.Chunks) changedChunks.Add(chunk.Coordinate, chunk);
        IsLoaded = true;
    }

    private static void ValidateMetadata(WorldSaveData meta)
    {
        if (meta.RegionSize != RegionSize || meta.Grid == null || meta.Surface == null || meta.Density == null || meta.Cave == null ||
            meta.Grid.Width < 1 || meta.Grid.DensityFieldHeight < 1 || meta.Grid.ChunkSize < 1 ||
            !(meta.Grid.Resolution > 0f) || float.IsInfinity(meta.Grid.Resolution) || meta.StonesPerChunk < 0 || meta.StonesPerChunk > 8)
            throw new InvalidDataException("월드 생성 설정 또는 청크당 돌 개수가 올바르지 않습니다.");
        checked { _ = (meta.Grid.ChunkSize + 1) * (meta.Grid.ChunkSize + 1) * (meta.Grid.ChunkSize + 1); }
    }

    public void ApplySavedChunks(TerrainData data)
    {
        terrain = data;
        foreach (ChunkSaveData chunk in changedChunks.Values)
        {
            data.ApplySavedChunk(chunk);
            chunk.Densities = null;
        }
    }

    public void ApplyStoneStates(Vector3Int coordinate, StoneSpawnData[] spawns)
    {
        if (spawns.Length > 8) throw new InvalidOperationException("청크당 돌은 최대 8개입니다.");
        byte mask = changedChunks.TryGetValue(coordinate, out ChunkSaveData chunk)
            ? chunk.StoneMask : (byte)((1 << spawns.Length) - 1);
        for (int i = 0; i < spawns.Length; i++)
        {
            spawns[i].ChunkCoordinate = coordinate;
            spawns[i].SpawnIndex = i;
            spawns[i].IsCollected = (mask & (1 << i)) == 0;
        }
    }

    public void MarkStoneCollected(Vector3Int coordinate, int index)
    {
        ChunkSaveData chunk = GetChangedChunk(coordinate);
        chunk.StoneMask = (byte)(chunk.StoneMask & ~(1 << index));
        dirtyRegions[coordinate / RegionSize] = ++revision;
    }

    public void MarkDensityChanged(Vector3Int min, Vector3Int max)
    {
        Vector3Int counts = terrain.ChunkCounts;
        Vector3Int first = Vector3Int.Min(min / terrain.ChunkSize, counts - Vector3Int.one);
        Vector3Int last = Vector3Int.Min(max / terrain.ChunkSize, counts - Vector3Int.one);
        for (int x = first.x; x <= last.x; x++)
            for (int y = first.y; y <= last.y; y++)
                for (int z = first.z; z <= last.z; z++)
                {
                    var coordinate = new Vector3Int(x, y, z);
                    GetChangedChunk(coordinate).Encoding = WorldDensityEncoding.Raw;
                    dirtyRegions[coordinate / RegionSize] = ++revision;
                }
    }

    private ChunkSaveData GetChangedChunk(Vector3Int coordinate)
    {
        if (!changedChunks.TryGetValue(coordinate, out ChunkSaveData chunk))
        {
            int count = terrain.GetChunkData(coordinate).CreateStoneSpawns(
                Metadata.StoneSeed, Metadata.StonesPerChunk, terrain, Metadata.MiningBounds).Length;
            chunk = new ChunkSaveData { Coordinate = coordinate, StoneMask = (byte)((1 << count) - 1), Encoding = WorldDensityEncoding.Original };
            changedChunks.Add(coordinate, chunk);
        }
        return chunk;
    }

    // 반드시 게임 저장에 성공한 복귀 완료 지점에서 호출한다. 복사 중에는 await하지 않는다.
    public Task<bool> SaveWorldAsync()
    {
        if (!IsLoaded) throw new InvalidOperationException("월드 로드 전에는 저장할 수 없습니다.");
        foreach (var dirty in dirtyRegions)
            pendingRegions[dirty.Key] = new RegionSaveData { Coordinate = dirty.Key, Revision = dirty.Value };
        foreach (ChunkSaveData state in changedChunks.Values)
        {
            Vector3Int region = state.Coordinate / RegionSize;
            if (!dirtyRegions.ContainsKey(region)) continue;
            var copy = new ChunkSaveData { Coordinate = state.Coordinate, StoneMask = state.StoneMask, Encoding = state.Encoding };
            if (state.Encoding != WorldDensityEncoding.Original)
            {
                ChunkDensityData chunk = terrain.GetChunkData(state.Coordinate);
                copy.Encoding = chunk.State == ChunkDensityState.Blank ? WorldDensityEncoding.Blank :
                    chunk.State == ChunkDensityState.Fill ? WorldDensityEncoding.Fill : WorldDensityEncoding.Raw;
                if (copy.Encoding == WorldDensityEncoding.Raw) copy.Densities = chunk.Densities.ToArray();
            }
            pendingRegions[region].Chunks.Add(copy);
        }
        if (saveTask.IsCompleted) saveTask = WritePendingRegionsAsync();
        return saveTask;
    }

    public async Task<bool> ResetWorldAsync()
    {
        bool wasLoaded = IsLoaded;
        IsLoaded = false;
        await saveTask;
        lifetime.Token.ThrowIfCancellationRequested();

        string root = Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar);
        string resetPath = Path.GetFullPath(Path.Combine(root, "World"));
        if (!string.Equals(Path.GetDirectoryName(resetPath), root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("월드 초기화 경로가 저장 루트 밖입니다.");

        try
        {
            if (Directory.Exists(resetPath)) Directory.Delete(resetPath, true);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            IsLoaded = wasLoaded;
            Debug.LogError($"월드 초기화 실패: {resetPath}\n{error.Message}");
            return false;
        }

        terrain = null;
        Metadata = null;
        changedChunks.Clear();
        dirtyRegions.Clear();
        pendingRegions.Clear();
        revision = 0;
        return true;
    }

    private async Task<bool> WritePendingRegionsAsync()
    {
        bool succeeded = true;
        string root = worldPath;
        WorldSaveData meta = Metadata;
        CancellationToken token = lifetime.Token;
        while (pendingRegions.Count > 0)
        {
            RegionSaveData snapshot = null;
            foreach (RegionSaveData entry in pendingRegions.Values) { snapshot = entry; break; }
            pendingRegions.Remove(snapshot.Coordinate);
            try
            {
                await Task.Run(() =>
                {
                    byte[] bytes = EncodeRegion(snapshot, meta);
                    WriteFile(Path.Combine(root, "Regions", $"region_{snapshot.Coordinate.x}_{snapshot.Coordinate.y}_{snapshot.Coordinate.z}.dat"), bytes, false, token);
                }, token);
                if (dirtyRegions.TryGetValue(snapshot.Coordinate, out int current) && current == snapshot.Revision)
                    dirtyRegions.Remove(snapshot.Coordinate);
            }
            catch (OperationCanceledException) { return false; }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                succeeded = false;
                Debug.LogError($"월드 Region 저장 실패: {snapshot.Coordinate}\n{error.Message}");
                // 변경 표시는 유지한다. 다음 복귀에서 최신 상태를 다시 저장한다.
            }
        }
        return succeeded;
    }

    private static byte[] EncodeRegion(RegionSaveData region, WorldSaveData meta)
    {
        region.Chunks.Sort((a, b) => ChunkNumber(a.Coordinate).CompareTo(ChunkNumber(b.Coordinate)));
        var bodies = new List<byte[]>();
        foreach (ChunkSaveData chunk in region.Chunks) bodies.Add(EncodeChunk(chunk));
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(RegionMagic);
        writer.Write(meta.Version);
        writer.Write(new Guid(meta.WorldId).ToByteArray());
        writer.Write(region.Coordinate.x); writer.Write(region.Coordinate.y); writer.Write(region.Coordinate.z);
        writer.Write(region.Chunks.Count);
        int offset = HeaderSize + region.Chunks.Count * EntrySize;
        for (int i = 0; i < bodies.Count; i++)
        {
            writer.Write(ChunkNumber(region.Chunks[i].Coordinate));
            writer.Write(offset); writer.Write(bodies[i].Length);
            writer.Write(Checksum(bodies[i], 0, bodies[i].Length));
            offset += bodies[i].Length;
        }
        foreach (byte[] body in bodies) writer.Write(body);
        writer.Flush();
        byte[] contents = stream.ToArray();
        writer.Write(Checksum(contents, 0, contents.Length));
        return stream.ToArray();
    }

    private static int ChunkNumber(Vector3Int c) => ((c.x % RegionSize) * RegionSize + c.y % RegionSize) * RegionSize + c.z % RegionSize;

    private static byte[] EncodeChunk(ChunkSaveData chunk)
    {
        WorldDensityEncoding encoding = chunk.Encoding;
        byte[] payload = Array.Empty<byte>();
        if (encoding == WorldDensityEncoding.Raw)
        {
            byte[] values = chunk.Densities;
            bool uniform = true;
            for (int i = 1; i < values.Length; i++) if (values[i] != values[0]) { uniform = false; break; }
            if (uniform && (values[0] == 0 || values[0] == 255))
                encoding = values[0] == 0 ? WorldDensityEncoding.Blank : WorldDensityEncoding.Fill;
            else
            {
                using var runs = new MemoryStream();
                using var writer = new BinaryWriter(runs);
                for (int i = 0; i < values.Length;)
                {
                    byte value = values[i];
                    int length = 1;
                    while (i + length < values.Length && values[i + length] == value && length < ushort.MaxValue) length++;
                    writer.Write(value); writer.Write((ushort)length);
                    i += length;
                    if (runs.Length >= values.Length) break;
                }
                if (runs.Length < values.Length) { encoding = WorldDensityEncoding.Rle; payload = runs.ToArray(); }
                else payload = values;
            }
        }
        var body = new byte[payload.Length + 2];
        body[0] = chunk.StoneMask;
        body[1] = (byte)encoding;
        Buffer.BlockCopy(payload, 0, body, 2, payload.Length);
        return body;
    }

    private static List<RegionSaveData> ReadRegions(string root, WorldSaveData meta, List<string> warnings, CancellationToken token)
    {
        var result = new List<RegionSaveData>();
        int nx = (meta.Grid.Width - 1) / meta.Grid.ChunkSize + 1;
        int ny = (meta.Grid.DensityFieldHeight - 1) / meta.Grid.ChunkSize + 1;
        for (int x = 0; x < (nx + RegionSize - 1) / RegionSize; x++)
            for (int y = 0; y < (ny + RegionSize - 1) / RegionSize; y++)
                for (int z = 0; z < (nx + RegionSize - 1) / RegionSize; z++)
                {
                    token.ThrowIfCancellationRequested();
                    string path = Path.Combine(root, "Regions", $"region_{x}_{y}_{z}.dat");
                    foreach (string candidate in new[] { path, path + ".bak" })
                    {
                        if (!File.Exists(candidate)) continue;
                        try
                        {
                            long maximumLength = HeaderSize + 64L * (EntrySize + 2 + (long)(meta.Grid.ChunkSize + 1) * (meta.Grid.ChunkSize + 1) * (meta.Grid.ChunkSize + 1)) + 4;
                            if (new FileInfo(candidate).Length > maximumLength) throw new InvalidDataException("Region 파일 크기 초과");
                            byte[] bytes = File.ReadAllBytes(candidate);
                            result.Add(DecodeRegion(bytes, new Vector3Int(x, y, z), meta));
                            if (candidate != path) WriteFile(path, bytes, true, token);
                            break;
                        }
                        catch (Exception error) when (error is IOException || error is InvalidDataException || error is ArgumentException || error is OverflowException)
                        {
                            warnings.Add($"월드 Region 복구: {candidate}\n{error.Message}");
                        }
                    }
                }
        return result;
    }

    private static RegionSaveData DecodeRegion(byte[] bytes, Vector3Int coordinate, WorldSaveData meta)
    {
        if (bytes.Length < HeaderSize + 4) throw new InvalidDataException("잘린 Region 헤더");
        using var reader = new BinaryReader(new MemoryStream(bytes));
        reader.BaseStream.Position = bytes.Length - 4;
        if (reader.ReadUInt32() != Checksum(bytes, 0, bytes.Length - 4)) throw new InvalidDataException("Region 검사값 불일치");
        reader.BaseStream.Position = 0;
        if (reader.ReadUInt32() != RegionMagic || reader.ReadInt32() != meta.Version || new Guid(reader.ReadBytes(16)) != new Guid(meta.WorldId))
            throw new InvalidDataException("Region 버전 또는 월드 불일치");
        if (reader.ReadInt32() != coordinate.x || reader.ReadInt32() != coordinate.y || reader.ReadInt32() != coordinate.z)
            throw new InvalidDataException("Region 좌표 불일치");
        int count = reader.ReadInt32();
        if (count < 1 || count > 64 || HeaderSize + count * EntrySize > bytes.Length - 4) throw new InvalidDataException("Region 청크 수 오류");
        var result = new RegionSaveData { Coordinate = coordinate };
        int nextOffset = HeaderSize + count * EntrySize;
        int previousNumber = -1;
        for (int i = 0; i < count; i++)
        {
            reader.BaseStream.Position = HeaderSize + i * EntrySize;
            int number = reader.ReadInt32();
            int offset = reader.ReadInt32();
            int length = reader.ReadInt32();
            uint checksum = reader.ReadUInt32();
            if (number <= previousNumber || number >= 64 || offset != nextOffset || length < 2 || length > bytes.Length - 4 - offset)
                throw new InvalidDataException("청크 인덱스 또는 길이 오류");
            previousNumber = number;
            nextOffset += length;
            if (Checksum(bytes, offset, length) != checksum) throw new InvalidDataException("청크 검사값 불일치");
            Vector3Int chunkCoordinate = coordinate * RegionSize + new Vector3Int(number / 16, number / 4 % 4, number % 4);
            int size = meta.Grid.ChunkSize;
            int nx = (meta.Grid.Width - 1) / size + 1;
            int ny = (meta.Grid.DensityFieldHeight - 1) / size + 1;
            if (chunkCoordinate.x >= nx || chunkCoordinate.y >= ny || chunkCoordinate.z >= nx) throw new InvalidDataException("월드 밖 청크");
            int sx = Math.Min(size, meta.Grid.Width - chunkCoordinate.x * size) + (chunkCoordinate.x == nx - 1 ? 1 : 0);
            int sy = Math.Min(size, meta.Grid.DensityFieldHeight - chunkCoordinate.y * size) + (chunkCoordinate.y == ny - 1 ? 1 : 0);
            int sz = Math.Min(size, meta.Grid.Width - chunkCoordinate.z * size) + (chunkCoordinate.z == nx - 1 ? 1 : 0);
            int samples = checked(sx * sy * sz);
            byte mask = bytes[offset];
            if ((mask >> meta.StonesPerChunk) != 0) throw new InvalidDataException("돌 비트 범위 오류");
            var encoding = (WorldDensityEncoding)bytes[offset + 1];
            var chunk = new ChunkSaveData { Coordinate = chunkCoordinate, StoneMask = mask, Encoding = encoding };
            if (encoding == WorldDensityEncoding.Raw)
            {
                if (length != samples + 2) throw new InvalidDataException("밀도 길이 오류");
                chunk.Densities = new byte[samples];
                Buffer.BlockCopy(bytes, offset + 2, chunk.Densities, 0, samples);
            }
            else if (encoding == WorldDensityEncoding.Rle)
            {
                if ((length - 2) % 3 != 0 || length - 2 >= samples) throw new InvalidDataException("RLE 길이 오류");
                chunk.Densities = new byte[samples];
                reader.BaseStream.Position = offset + 2;
                int written = 0;
                while (reader.BaseStream.Position < offset + length)
                {
                    byte value = reader.ReadByte();
                    int run = reader.ReadUInt16();
                    if (run == 0 || run > samples - written) throw new InvalidDataException("RLE 반복 길이 오류");
                    for (int j = 0; j < run; j++) chunk.Densities[written++] = value;
                }
                if (written != samples) throw new InvalidDataException("RLE 샘플 수 오류");
                chunk.Encoding = WorldDensityEncoding.Raw;
            }
            else if ((encoding != WorldDensityEncoding.Original && encoding != WorldDensityEncoding.Blank && encoding != WorldDensityEncoding.Fill) || length != 2)
                throw new InvalidDataException("밀도 저장 방식 오류");
            result.Chunks.Add(chunk);
        }
        if (nextOffset != bytes.Length - 4) throw new InvalidDataException("Region 후행 데이터 오류");
        return result;
    }

    // FNV-1a: 파일과 청크의 우발적인 손상을 검출한다.
    private static uint Checksum(byte[] bytes, int offset, int length)
    {
        uint hash = 2166136261;
        unchecked { for (int i = offset; i < offset + length; i++) hash = (hash ^ bytes[i]) * 16777619; }
        return hash;
    }

    private static void WriteFile(string path, byte[] bytes, bool preserveBackup, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary = path + ".tmp";
        using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            file.Write(bytes, 0, bytes.Length);
            file.Flush(true);
        }
        token.ThrowIfCancellationRequested();
        if (File.Exists(path)) File.Replace(temporary, path, preserveBackup ? null : path + ".bak");
        else File.Move(temporary, path);
    }

    private void OnDestroy()
    {
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
