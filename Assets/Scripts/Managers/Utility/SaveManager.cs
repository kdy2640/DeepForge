using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [SerializeField] private string saveFileName = "save.json";

    private bool loadedSuccessfully;

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    private void Start()
    {
        LoadGame();
    }// 게임 시작 시 자동 로드 옵션이 켜져 있으면 저장 파일을 불러온다.
    // 저장 파일이 없으면 아무것도 적용하지 않는다.

    private void OnApplicationQuit()
    {
        SaveGame();
    }// 게임 종료 시 자동 저장 옵션이 켜져 있으면 현재 데이터를 저장한다.
    // 에디터 플레이 종료 시에도 호출될 수 있다.


    public bool SaveGame()
    {
        // 읽기에 실패한 기존 재고를 기본값으로 덮어쓰지 않는다.
        if (!loadedSuccessfully) return false;
        GameSaveData saveData = CreateSaveData();
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(saveData, true));
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            using (var file = new FileStream(SavePath + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                file.Write(bytes, 0, bytes.Length);
                file.Flush(true);
            }
            if (File.Exists(SavePath)) File.Replace(SavePath + ".tmp", SavePath, SavePath + ".bak");
            else File.Move(SavePath + ".tmp", SavePath);
            return true;
        }
        catch (System.Exception error) when (error is IOException || error is System.UnauthorizedAccessException)
        {
            Debug.LogError($"게임 저장 실패: {SavePath}\n{error.Message}");
            return false;
        }
    }

    public void LoadGame()
    {
        loadedSuccessfully = false;
        if (!HasSave())
        {
            ResetSave();
            return;
        }
        foreach (string path in new[] { SavePath, SavePath + ".bak" })
        {
            if (!File.Exists(path)) continue;
            GameSaveData saveData;
            try
            {
                string json = File.ReadAllText(path);
                saveData = JsonUtility.FromJson<GameSaveData>(json.Replace("\"type\":", "\"id\":"));
                if (saveData == null || saveData.stock == null) throw new InvalidDataException("게임 저장 데이터 형식 오류");
                if (path != SavePath)
                {
                    File.Copy(path, SavePath + ".tmp", true);
                    if (File.Exists(SavePath)) File.Replace(SavePath + ".tmp", SavePath, null);
                    else File.Move(SavePath + ".tmp", SavePath);
                }
            }
            catch (System.Exception error) when (error is IOException || error is InvalidDataException || error is System.UnauthorizedAccessException || error is System.ArgumentException)
            {
                Debug.LogWarning($"게임 저장 읽기 실패: {path}\n{error.Message}");
                continue;
            }
            ApplySaveData(saveData);
            loadedSuccessfully = true;
            return;
        }
        Debug.LogError("게임 저장과 백업을 읽지 못했습니다. 기존 파일을 보존하고 저장을 중단합니다.");
    }

    [ContextMenu("Delete Save")]
    public void DeleteSave()
    {
        foreach (string path in new[] { SavePath, SavePath + ".bak", SavePath + ".tmp" })
            if (File.Exists(path)) File.Delete(path);
    }

    [ContextMenu("Reset Save")]
    public void ResetSave()
    {
        GameManager.Instance.Upgrade.ResetUpgradeSaveData();
        GameManager.Instance.Utility.Tutorial.ResetTutorialSaveData();
        GameManager.Instance.Utility.Audio.ResetAudioSaveData();
        GameManager.Instance.StockManager.ResetStockSaveData();
        GameManager.Instance.GameLoop.BackPack.Clear();
        GameManager.Instance.BaseCamp.Order.ResetOrderSaveData();
        GameManager.Instance.BaseCamp.Smith.ResetSmithSaveData();
        DeleteSave();
        loadedSuccessfully = true;
    }

    public bool HasSave()
    {
        return File.Exists(SavePath) || File.Exists(SavePath + ".bak");
    }// 저장 파일이 존재하는지 확인한다.

    private GameSaveData CreateSaveData()
    {
        GameSaveData saveData = new();
        saveData.upgrades = GameManager.Instance.Upgrade.CreateUpgradeSaveData();
        saveData.tutorials = GameManager.Instance.Utility.Tutorial.CreateTutorialSaveData();
        saveData.audio = GameManager.Instance.Utility.Audio.CreateAudioSaveData();
        saveData.stock = GameManager.Instance.StockManager.CreateStockSaveData();
        saveData.backPack = GameManager.Instance.GameLoop.BackPack.CreateSaveData();
        saveData.orders = GameManager.Instance.BaseCamp.Order.CreateOrderSaveData();
        saveData.smith = GameManager.Instance.BaseCamp.Smith.CreateSmithSaveData();
        return saveData;
    }// 저장할 전체 데이터를 만든다.

    private void ApplySaveData(GameSaveData saveData)
    {
        GameManager.Instance.Upgrade.LoadUpgradeSaveData(saveData.upgrades);
        GameManager.Instance.Utility.Tutorial.LoadTutorialSaveData(saveData.tutorials);
        GameManager.Instance.Utility.Audio.LoadAudioSaveData(saveData.audio);
        GameManager.Instance.StockManager.LoadStockSaveData(saveData.stock);
        GameManager.Instance.GameLoop.BackPack.LoadSaveData(saveData.backPack);
        GameManager.Instance.BaseCamp.Order.LoadOrderSaveData(saveData.orders);
        GameManager.Instance.BaseCamp.Smith.LoadSmithSaveData(saveData.smith);
    }// 불러온 저장 데이터를 실제 게임 상태에 반영한다.
}
