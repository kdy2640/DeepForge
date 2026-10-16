/// <summary>
/// 플레이어가 보유한 업그레이드와 현재 레벨을 나타냅니다.
/// </summary>
[System.Serializable]
public class UpgradeState
{
    public UpgradeDataSO data;
    public int level;

    // 설계도 구매로 해금한 최대 강화 레벨.
    public int unlockedLevel;
}
