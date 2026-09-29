/// <summary>
/// 플레이어가 보유한 업그레이드와 현재 레벨을 나타냅니다.
/// </summary>
[System.Serializable]
public class UpgradeState
{
    public UpgradeDataSO data;
    public int level;

    // 시연용 1장 보유 여부. UpgradeSaveData에는 저장하지 않는다.
    public bool hasBlueprint;
}
