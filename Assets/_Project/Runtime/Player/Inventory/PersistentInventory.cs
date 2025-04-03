using UnityEngine;

public class PersistentInventory : MonoBehaviour
{
    private static PersistentInventory _instance;
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}