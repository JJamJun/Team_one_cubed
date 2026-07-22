using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CookingIngredientManager : MonoBehaviour
{
    [SerializeField] private string unlockSaveFileName = "menu_progress.json";
    [SerializeField] private Transform toolRoot;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color32(0x7B, 0x7B, 0x7B, 0xFF);
    [SerializeField, Min(0.1f)] private float savePollInterval = 0.5f;

    private const string IceWaterMenuId = "0";
    private const string PeachIceTeaMenuId = "1";
    private const string AmericanoMenuId = "2";
    private const string IcedAmericanoMenuId = "3";
    private const string AshatchuMenuId = "4";

    private float nextSavePollTime;
    private DateTime lastSaveWriteTimeUtc = DateTime.MinValue;
    private string cachedSavePath;

    private void Awake()
    {
        if (toolRoot == null)
        {
            toolRoot = FindToolRoot();
        }

        cachedSavePath = GetUnlockSavePath();
    }

    private void OnEnable()
    {
        RefreshToolLocks();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextSavePollTime)
        {
            return;
        }

        nextSavePollTime = Time.unscaledTime + savePollInterval;
        DateTime writeTimeUtc = File.Exists(cachedSavePath)
            ? File.GetLastWriteTimeUtc(cachedSavePath)
            : DateTime.MinValue;

        if (writeTimeUtc != lastSaveWriteTimeUtc)
        {
            RefreshToolLocks();
        }
    }

    public void RefreshToolLocks()
    {
        if (string.IsNullOrEmpty(cachedSavePath))
        {
            cachedSavePath = GetUnlockSavePath();
        }

        MenuUnlockSaveData saveData = LoadUnlockData();
        bool hasIceWater = EnsureDefaultIceWaterUnlocked(saveData);
        bool hasPeachIceTea = saveData.IsUnlocked(PeachIceTeaMenuId);
        bool hasCoffee = saveData.IsUnlocked(AmericanoMenuId)
            || saveData.IsUnlocked(IcedAmericanoMenuId)
            || saveData.IsUnlocked(AshatchuMenuId);

        SetToolState("TrashCan", true);
        SetToolState("Cups", true);
        SetToolState("WaterPot", hasIceWater);
        SetToolState("IceMachine", hasIceWater || saveData.IsUnlocked(IcedAmericanoMenuId) || saveData.IsUnlocked(AshatchuMenuId));
        SetToolState("IceTea", hasPeachIceTea || saveData.IsUnlocked(AshatchuMenuId));
        SetToolState("CoffeeMachine", hasCoffee);
        SetToolState("Syrup", false);

        lastSaveWriteTimeUtc = File.Exists(cachedSavePath)
            ? File.GetLastWriteTimeUtc(cachedSavePath)
            : DateTime.MinValue;
    }

    private bool EnsureDefaultIceWaterUnlocked(MenuUnlockSaveData saveData)
    {
        MenuUnlockState iceWater = saveData.GetOrCreate(IceWaterMenuId);
        if (iceWater.isUnlocked && iceWater.level > 0)
        {
            return true;
        }

        iceWater.isUnlocked = true;
        iceWater.level = 1;
        SaveUnlockData(saveData);
        return true;
    }

    private void SetToolState(string toolName, bool isActive)
    {
        Transform tool = FindTool(toolName);
        if (tool == null)
        {
            Debug.LogWarning($"{nameof(CookingIngredientManager)}: Tool '{toolName}' was not found.");
            return;
        }

        CanvasGroup canvasGroup = tool.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = tool.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = isActive;
        canvasGroup.blocksRaycasts = isActive;

        foreach (Image image in tool.GetComponentsInChildren<Image>(true))
        {
            if (ShouldKeepImageTransparent(image))
            {
                continue;
            }

            image.color = isActive ? activeColor : inactiveColor;
            image.raycastTarget = isActive;
        }

        foreach (Selectable selectable in tool.GetComponentsInChildren<Selectable>(true))
        {
            selectable.interactable = isActive;
        }

        foreach (Collider2D collider in tool.GetComponentsInChildren<Collider2D>(true))
        {
            collider.enabled = isActive;
        }

        foreach (CupDragSource dragSource in tool.GetComponentsInChildren<CupDragSource>(true))
        {
            dragSource.enabled = isActive;
        }

        foreach (ReturnableIngredientDrag ingredientDrag in tool.GetComponentsInChildren<ReturnableIngredientDrag>(true))
        {
            ingredientDrag.enabled = isActive;
        }

        foreach (SyrupDispenserController syrupDispenser in tool.GetComponentsInChildren<SyrupDispenserController>(true))
        {
            syrupDispenser.enabled = isActive;
        }
    }

    private Transform FindTool(string toolName)
    {
        if (toolRoot != null)
        {
            Transform foundInRoot = FindChildRecursive(toolRoot, toolName);
            if (foundInRoot != null)
            {
                return foundInRoot;
            }
        }

        GameObject foundObject = GameObject.Find(toolName);
        return foundObject != null ? foundObject.transform : null;
    }

    private static Transform FindToolRoot()
    {
        GameObject cookingPanel = GameObject.Find("CookingPanel");
        return cookingPanel != null ? cookingPanel.transform : null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private MenuUnlockSaveData LoadUnlockData()
    {
        if (!File.Exists(cachedSavePath))
        {
            return new MenuUnlockSaveData();
        }

        try
        {
            MenuUnlockSaveData saveData = JsonUtility.FromJson<MenuUnlockSaveData>(File.ReadAllText(cachedSavePath));
            if (saveData == null)
            {
                saveData = new MenuUnlockSaveData();
            }

            saveData.EnsureInitialized();
            return saveData;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"{nameof(CookingIngredientManager)}: Failed to load {cachedSavePath}. {exception.Message}");
            return new MenuUnlockSaveData();
        }
    }

    private void SaveUnlockData(MenuUnlockSaveData saveData)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(cachedSavePath));
        File.WriteAllText(cachedSavePath, JsonUtility.ToJson(saveData, true));
    }

    private string GetUnlockSavePath()
    {
        return Path.Combine(Application.persistentDataPath, unlockSaveFileName);
    }

    private static bool ShouldKeepImageTransparent(Image image)
    {
        return image == null
            || image.color.a <= 0.01f
            || image.name.EndsWith("Pos", StringComparison.OrdinalIgnoreCase);
    }

    [Serializable]
    private class MenuUnlockSaveData
    {
        public List<MenuUnlockState> menus = new List<MenuUnlockState>();

        public void EnsureInitialized()
        {
            if (menus == null)
            {
                menus = new List<MenuUnlockState>();
            }
        }

        public MenuUnlockState GetOrCreate(string menuId)
        {
            EnsureInitialized();
            MenuUnlockState state = menus.Find(unlock => unlock.menuID == menuId);
            if (state == null)
            {
                state = new MenuUnlockState { menuID = menuId, isUnlocked = false, level = 0 };
                menus.Add(state);
            }

            return state;
        }

        public bool IsUnlocked(string menuId)
        {
            MenuUnlockState state = GetOrCreate(menuId);
            return state.isUnlocked && state.level > 0;
        }
    }

    [Serializable]
    private class MenuUnlockState
    {
        public string menuID;
        public bool isUnlocked;
        public int level;
    }
}
