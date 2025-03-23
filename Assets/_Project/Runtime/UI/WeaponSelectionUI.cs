using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class WeaponSelectionUI : MonoBehaviour
{
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private GameObject selectionUIPanel;
    [SerializeField] private GameObject weaponSlotPrefab;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private float hideDelay = 3f;
    
    private List<WeaponSlotUI> weaponSlots = new List<WeaponSlotUI>();
    private Coroutine hideCoroutine;
    
    private void Start()
    {
        if (weaponManager == null)
        {
            weaponManager = FindFirstObjectByType<WeaponManager>();
            if (weaponManager == null)
            {
                Debug.LogError("WeaponSelectionUI: No WeaponManager found!");
                return;
            }
        }
        
        // Initialize UI with all available weapons
        InitializeWeaponSlots();
        
        // Hide the selection UI initially
        selectionUIPanel.SetActive(false);
        
        // Listen for weapon change events
        weaponManager.onWeaponChanged.AddListener(OnWeaponChanged);
    }
    
    private void OnEnable()
    {
        // Add input callbacks
        Player playerRef = FindFirstObjectByType<Player>();
        PlayerInputActions inputActions = playerRef?.GetComponent<Player>()._inputActions;
        
        if (inputActions != null)
        {
            inputActions.Gameplay.Weapon1.performed += _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon2.performed += _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon3.performed += _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon4.performed += _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon5.performed += _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon6.performed += _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon7.performed += _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon8.performed += _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon9.performed += _ => ShowSelectionUI();
            inputActions.Gameplay.WeaponSwitch.performed += _ => ShowSelectionUI();
            inputActions.Gameplay.LastWeapon.performed += _ => ShowSelectionUI();
        }
    }
    
    private void OnDisable()
    {
        // Remove input callbacks
        Player playerRef = FindFirstObjectByType<Player>();
        PlayerInputActions inputActions = playerRef?.GetComponent<Player>()._inputActions;
        
        if (inputActions != null)
        {
            inputActions.Gameplay.Weapon1.performed -= _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon2.performed -= _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon3.performed -= _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon4.performed -= _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon5.performed -= _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon6.performed -= _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon7.performed -= _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon8.performed -= _ => ShowSelectionUI();
            inputActions.Gameplay.Weapon9.performed -= _ => ShowSelectionUI();
            inputActions.Gameplay.WeaponSwitch.performed -= _ => ShowSelectionUI();
            inputActions.Gameplay.LastWeapon.performed -= _ => ShowSelectionUI();
        }
    }
    
    private void InitializeWeaponSlots()
    {
        // Clear any existing slots
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        weaponSlots.Clear();
        
        // Get the weapon data from serialized array
        Object[] weaponDataArray = Resources.FindObjectsOfTypeAll(typeof(WeaponData));
        
        // Create slots for each weapon
        for (int i = 0; i < weaponDataArray.Length; i++)
        {
            WeaponData weaponData = weaponDataArray[i] as WeaponData;
            if (weaponData != null)
            {
                GameObject slotObj = Instantiate(weaponSlotPrefab, slotsContainer);
                WeaponSlotUI slotUI = slotObj.GetComponent<WeaponSlotUI>();
                
                if (slotUI != null)
                {
                    // Use weaponSlot from WeaponData if available, otherwise fallback to index
                    int slotNumber = weaponData.weaponSlot > 0 ? weaponData.weaponSlot : (i + 1);
                    slotUI.Initialize(slotNumber, weaponData);
                    weaponSlots.Add(slotUI);
                }
            }
        }
        
        // Sort weapon slots by slot number
        weaponSlots.Sort((a, b) => a.GetSlotNumber().CompareTo(b.GetSlotNumber()));
        
        // Re-arrange them in the UI
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            weaponSlots[i].transform.SetSiblingIndex(i);
        }
    }
    
    private void OnWeaponChanged(WeaponData weaponData, int ammo)
    {
        // Find the selected weapon and update UI
        int selectedIndex = -1;
        
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            if (weaponSlots[i].GetWeaponData() == weaponData)
            {
                selectedIndex = i;
                break;
            }
        }
        
        // Update selection UI
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            weaponSlots[i].SetSelected(i == selectedIndex);
            
            // Update ammo if this is the selected weapon
            if (i == selectedIndex)
            {
                weaponSlots[i].UpdateAmmo(ammo, weaponData.maxAmmo);
            }
        }
        
        // Show UI when weapon changes
        ShowSelectionUI();
    }
    
    private void ShowSelectionUI()
    {
        // Show the selection UI
        selectionUIPanel.SetActive(true);
        
        // Cancel previous hide coroutine if running
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        
        // Start new hide coroutine
        hideCoroutine = StartCoroutine(HideSelectionUIAfterDelay());
    }
    
    private IEnumerator HideSelectionUIAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);
        selectionUIPanel.SetActive(false);
        hideCoroutine = null;
    }
}

// Class to handle individual weapon slot UI
public class WeaponSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Image selectionBg;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color unselectedColor = Color.gray;
    
    private WeaponData weaponData;
    private int slotNumber;
    
    public void Initialize(int number, WeaponData data)
    {
        slotNumber = number;
        weaponData = data;
        
        // Set slot number
        if (numberText != null)
        {
            numberText.text = number.ToString();
        }
        
        // Set weapon name
        if (nameText != null)
        {
            nameText.text = data.weaponName;
        }
        
        // Set weapon icon if available
        if (weaponIcon != null && data.weaponIcon != null)
        {
            weaponIcon.sprite = data.weaponIcon;
            weaponIcon.enabled = true;
        }
        else if (weaponIcon != null)
        {
            weaponIcon.enabled = false;
        }
        
        // Set initial ammo to max
        if (ammoText != null)
        {
            ammoText.text = $"{data.maxAmmo}/{data.maxAmmo}";
        }
        
        // Initialize as unselected
        SetSelected(false);
    }
    
    public void UpdateAmmo(int currentAmmo, int maxAmmo)
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo}/{maxAmmo}";
        }
    }
    
    public void SetSelected(bool selected)
    {
        if (selectionBg != null)
        {
            selectionBg.color = selected ? selectedColor : unselectedColor;
        }
        
        // Make selected item more visible
        if (nameText != null)
        {
            nameText.color = selected ? Color.white : new Color(0.8f, 0.8f, 0.8f, 0.8f);
            nameText.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }
        
        if (numberText != null)
        {
            numberText.color = selected ? Color.white : new Color(0.8f, 0.8f, 0.8f, 0.8f);
            numberText.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }
        
        if (ammoText != null)
        {
            ammoText.color = selected ? Color.white : new Color(0.8f, 0.8f, 0.8f, 0.8f);
        }
    }
    
    public WeaponData GetWeaponData()
    {
        return weaponData;
    }
    
    public int GetSlotNumber()
    {
        return slotNumber;
    }
}