using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private InputManager inputManager;
    [SerializeField] private SceneController sceneController;

    public StockManager StockManager { get; private set; }
    public UpgradeManager Upgrade { get; private set; }
    public UtilityManager Utility { get; private set; }
    public BaseCampManager BaseCamp { get; private set; }

    public InputManager InputManager => inputManager;
    public SceneController SceneController => sceneController;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        StockManager = GetComponent<StockManager>();
        Upgrade = GetComponent<UpgradeManager>();
        Utility = GetComponentInChildren<UtilityManager>(true);
        BaseCamp = GetComponentInChildren<BaseCampManager>(true);

        if (inputManager == null)
        {
            inputManager = GetComponentInChildren<InputManager>(true);
        }

        if (sceneController == null)
        {
            sceneController = GetComponentInChildren<SceneController>(true);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
