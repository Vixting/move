using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private UIDocument mainMenuDocument;
    [SerializeField] private GameObject mainMenuObject;
   
    private static GameManager _instance;
    private MainMenuController _menuController;
    private Player _playerInstance;
   
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
               
                if (_instance == null)
                {
                    GameObject obj = new GameObject("GameManager");
                    _instance = obj.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }
   
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
       
        _instance = this;
        DontDestroyOnLoad(gameObject);
       
        if (mainMenuObject != null)
        {
            _menuController = mainMenuObject.GetComponent<MainMenuController>();
            if (_menuController == null)
            {
                _menuController = mainMenuObject.AddComponent<MainMenuController>();
            }
           
            _menuController.SetupMenu(mainMenuDocument, levelManager);
        }
        else if (mainMenuDocument != null)
        {
            // If no menu object is provided, try to find or create one
            GameObject menuObj = mainMenuDocument.gameObject;
            _menuController = menuObj.GetComponent<MainMenuController>();
            if (_menuController == null)
            {
                _menuController = menuObj.AddComponent<MainMenuController>();
            }
            
            _menuController.SetupMenu(mainMenuDocument, levelManager);
        }
    }
   
    private void Start()
    {
        // Initialize the menu after all components are set up
        if (_menuController != null && mainMenuDocument != null)
        {
            _menuController.SetupMenu(mainMenuDocument, levelManager);
        }
    }
   
    public void RegisterPlayer(Player player)
    {
        _playerInstance = player;
    }
   
    public void OnLevelLoaded(bool isGameplayLevel)
    {
        if (mainMenuDocument != null)
        {
            mainMenuDocument.gameObject.SetActive(!isGameplayLevel);
        }
       
        if (_playerInstance != null)
        {
            _playerInstance.EnableGameplayMode(isGameplayLevel);
        }
    }
   
    public void ReturnToMainMenu()
    {
        if (levelManager != null)
        {
            levelManager.LoadMainMenu();
        }
    }
}