using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;
using TMPro;

public class CustomerManager : MonoBehaviour
{
    [Header("Prefabs & Containers")]
    [SerializeField] private GameObject normalCustomerPrefab;
    [SerializeField] private GameObject[] ghostCustomerPrefabs;
    [SerializeField] private Transform customerContainer;

    [Header("Waypoints")]
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private RectTransform counterPoint;
    [SerializeField] private RectTransform pickupPoint;
    [SerializeField] private RectTransform exitPoint;

    [Header("Spawn Settings")]
    [SerializeField, Min(1f)] private float minSpawnTime = 5f;
    [SerializeField, Min(1f)] private float maxSpawnTime = 12f;
    [SerializeField, Range(0f, 1f)] private float ghostChance = 0.2f;
    [SerializeField, Min(0)] private int customersBeforeGhostVisit = 5;

    [Header("Order Settings")]
    [SerializeField, Min(1)] private int maxItemsInOrder = 3;
    [SerializeField] private TextAsset menuInfoCsv;
    [SerializeField] private string unlockSaveFileName = "menu_progress.json";
    [SerializeField, Min(0.1f)] private float unlockSavePollInterval = 0.5f;

    [Header("Dependencies")]
    [SerializeField] private UIStationScoller stationScroller;
    [SerializeField] private GhostEffectDirector ghostEffectDirector; 

    private float spawnTimer;
    private bool isCounterOccupied;
    private CustomerController currentCustomerAtCounter;
    private CustomerController activeGhost;
    private GameObject pendingGhostPrefab;
    private GhostType pendingGhostType = GhostType.None;
    private bool ghostTrialActive;
    private int ghostTrialCustomersHandled;
    private readonly Dictionary<string, string> menuNameById = new Dictionary<string, string>();
    private readonly List<string> availableMenus = new List<string>();
    private string unlockSavePath;
    private DateTime lastUnlockSaveWriteTimeUtc = DateTime.MinValue;
    private float nextUnlockSavePollTime;

    private Dictionary<int, CustomerController> waitingCustomers = new Dictionary<int, CustomerController>();

    private void Awake()
    {
        unlockSavePath = GetUnlockSavePath();
        LoadAvailableMenus();
        ResetSpawnTimer();
    }

    private void OnEnable()
    {
        CounterOrderController.OnReceiptPrinted += HandleReceiptPrinted;
        Receipt.ReceiptSlotEmptied += HandleReceiptEmptied;
    }

    private void OnDisable()
    {
        CounterOrderController.OnReceiptPrinted -= HandleReceiptPrinted;
        Receipt.ReceiptSlotEmptied -= HandleReceiptEmptied;
    }

    private void Update()
    {
        RefreshAvailableMenusIfSaveChanged();
        ManageSpawning();
        MonitorCounterState();
    }

    // --- Core Logic ---

    private void HandleReceiptPrinted(int slotIndex)
    {
        if (currentCustomerAtCounter != null && currentCustomerAtCounter.CurrentState == CustomerState.Ordering)
        {
            waitingCustomers[slotIndex] = currentCustomerAtCounter;
            currentCustomerAtCounter.AcceptOrder();

            currentCustomerAtCounter = null;
            isCounterOccupied = false;
            ResetSpawnTimer();
        }
    }

    private void HandleReceiptEmptied(int slotIndex, bool isSuccess, string receiptText)
    {
        if (waitingCustomers.TryGetValue(slotIndex, out CustomerController customer))
        {
            if (customer != null)
            {
                if (isSuccess)
                {
                    customer.OrderFulfilled();

                    GameStatsManager.Instance?.RegisterCustomerSuccess(receiptText, customer.TotalDrinksOrdered);

                    if (stationScroller != null)
                    {
                        stationScroller.MoveToPickup();
                    }
                }
                else
                {
                    customer.OrderFailed();
                }
            }

            waitingCustomers.Remove(slotIndex);
        }
    }

    private void HandleCustomerLeft(CustomerController customer)
    {
        customer.OnCustomerLeft -= HandleCustomerLeft;

        if (customer == activeGhost)
        {
            if (ghostEffectDirector != null)
            {
                ghostEffectDirector.TriggerGhostDeparture(activeGhost.CustomerGhostType, activeGhost.IsHappy);
            }

            activeGhost = null;
            pendingGhostPrefab = null;
            pendingGhostType = GhostType.None;
            ghostTrialActive = false;
            ghostTrialCustomersHandled = 0;
            Debug.Log("The ghost has left the building. A new ghost can now spawn.");
        }
        else if (ghostTrialActive && pendingGhostPrefab != null && customer.CustomerGhostType == GhostType.None)
        {
            ghostTrialCustomersHandled++;
            Debug.Log($"Ghost trial progress: {ghostTrialCustomersHandled}/{customersBeforeGhostVisit} customers handled before {pendingGhostType} visits.");
        }
    }

    // --- Spawning Logic ---

    private void ManageSpawning()
    {
        if (isCounterOccupied || availableMenus.Count == 0) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnCustomer();
        }
    }

    private void MonitorCounterState()
    {
        if (!isCounterOccupied || currentCustomerAtCounter == null) return;

        CustomerState state = currentCustomerAtCounter.CurrentState;
        if (state != CustomerState.Arriving && state != CustomerState.Ordering)
        {
            isCounterOccupied = false;
            currentCustomerAtCounter = null;
            ResetSpawnTimer();
        }
    }

    private void SpawnCustomer()
    {
        isCounterOccupied = true;
        bool spawningGhost = false;

        GameObject prefabToSpawn = normalCustomerPrefab;

        if (ShouldSpawnPendingGhost())
        {
            prefabToSpawn = pendingGhostPrefab;
            spawningGhost = true;
        }
        else if (CanStartGhostTrial())
        {
            TryStartGhostTrial();
        }

        GameObject customerObj = Instantiate(prefabToSpawn, customerContainer);
        currentCustomerAtCounter = customerObj.GetComponent<CustomerController>();

        if (currentCustomerAtCounter != null)
        {
            currentCustomerAtCounter.OnCustomerLeft += HandleCustomerLeft;
            GameStatsManager.Instance?.RegisterCustomerArrival();

            if (spawningGhost)
            {
                activeGhost = currentCustomerAtCounter;
                pendingGhostPrefab = null;
                pendingGhostType = GhostType.None;

                Debug.Log($"{activeGhost.CustomerGhostType} is now visiting as a customer after the ghost trial.");
            }

            currentCustomerAtCounter.InitializeWaypoints(spawnPoint, counterPoint, pickupPoint, exitPoint);
            string generatedOrderText = GenerateRandomOrder(out int totalDrinks);
            currentCustomerAtCounter.SetOrderText(generatedOrderText, totalDrinks);
            currentCustomerAtCounter.Spawn();
        }
        else
        {
            isCounterOccupied = false;
        }
    }

    private bool ShouldSpawnPendingGhost()
    {
        return ghostTrialActive
            && activeGhost == null
            && pendingGhostPrefab != null
            && ghostTrialCustomersHandled >= customersBeforeGhostVisit;
    }

    private bool CanStartGhostTrial()
    {
        return !ghostTrialActive
            && activeGhost == null
            && pendingGhostPrefab == null
            && ghostCustomerPrefabs != null
            && ghostCustomerPrefabs.Length > 0
            && Random.value <= ghostChance;
    }

    private void TryStartGhostTrial()
    {
        pendingGhostPrefab = GetRandomSupportedGhostPrefab(out pendingGhostType);

        if (pendingGhostPrefab == null || pendingGhostType == GhostType.None)
        {
            pendingGhostPrefab = null;
            pendingGhostType = GhostType.None;
            return;
        }

        ghostTrialActive = true;
        ghostTrialCustomersHandled = 0;

        if (ghostEffectDirector != null)
        {
            ghostEffectDirector.TriggerGhostArrival(pendingGhostType);
        }

        Debug.Log($"{pendingGhostType} trial started. The ghost will visit after {customersBeforeGhostVisit} customers.");
    }

    private GameObject GetRandomSupportedGhostPrefab(out GhostType ghostType)
    {
        ghostType = GhostType.None;
        if (ghostCustomerPrefabs == null || ghostCustomerPrefabs.Length == 0)
        {
            return null;
        }

        List<GameObject> supportedPrefabs = new List<GameObject>();
        List<GhostType> supportedTypes = new List<GhostType>();
        for (int i = 0; i < ghostCustomerPrefabs.Length; i++)
        {
            GameObject ghostPrefab = ghostCustomerPrefabs[i];
            CustomerController ghostController = ghostPrefab != null ? ghostPrefab.GetComponent<CustomerController>() : null;
            GhostType candidateType = ghostController != null ? ghostController.CustomerGhostType : GhostType.None;
            if (!HasImplementedGhostEffect(candidateType) || IsGhostBuffActive(candidateType))
            {
                continue;
            }

            supportedPrefabs.Add(ghostPrefab);
            supportedTypes.Add(candidateType);
        }

        if (supportedPrefabs.Count == 0)
        {
            return null;
        }

        int selectedIndex = Random.Range(0, supportedPrefabs.Count);
        ghostType = supportedTypes[selectedIndex];
        return supportedPrefabs[selectedIndex];
    }

    private bool HasImplementedGhostEffect(GhostType ghostType)
    {
        return ghostType == GhostType.Woman
            || ghostType == GhostType.DeadLion
            || ghostType == GhostType.Dokaebi
            || ghostType == GhostType.Little;
    }

    private bool IsGhostBuffActive(GhostType ghostType)
    {
        switch (ghostType)
        {
            case GhostType.Woman:
                return BuffDebuffManager.VirginGhostBuffActive;
            case GhostType.DeadLion:
                return BuffDebuffManager.GrimReaperBuffActive;
            case GhostType.Dokaebi:
                return BuffDebuffManager.DokkaebiBuffActive;
            case GhostType.Little:
                return BuffDebuffManager.LittleGhostBuffActive;
            default:
                return false;
        }
    }


    private string GenerateRandomOrder(out int totalDrinkCount)
    {
        int itemCount = Random.Range(1, maxItemsInOrder + 1);
        Dictionary<string, int> orderCounts = new Dictionary<string, int>();
        totalDrinkCount = 0; //track total!!! why bake straight into string bruh :( 

        for (int i = 0; i < itemCount; i++)
        {
            string randomMenu = availableMenus[Random.Range(0, availableMenus.Count)];
            if (orderCounts.ContainsKey(randomMenu))
            {
                orderCounts[randomMenu]++;
            }
            else
            {
                orderCounts[randomMenu] = 1;
            }
            totalDrinkCount++; 
        }

        System.Text.StringBuilder orderBuilder = new System.Text.StringBuilder();
        foreach (var kvp in orderCounts)
        {
            orderBuilder.AppendLine($"{kvp.Key} {kvp.Value}ÀÜ");
        }

        return orderBuilder.ToString().Trim();
    }

    private void LoadAvailableMenus()
    {
        menuNameById.Clear();
        availableMenus.Clear();

        if (menuInfoCsv == null) return;

        string[] lines = menuInfoCsv.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return;

        for (int i = 1; i < lines.Length; i++)
        {
            string[] cells = lines[i].Split(',');
            if (cells.Length > 1 && !string.IsNullOrWhiteSpace(cells[0]) && !string.IsNullOrWhiteSpace(cells[1]))
            {
                menuNameById[cells[0].Trim()] = cells[1].Trim();
            }
        }

        RefreshAvailableMenusFromUnlockSave();
    }

    private void ResetSpawnTimer()
    {
        spawnTimer = Random.Range(minSpawnTime, maxSpawnTime);
    }

    private void RefreshAvailableMenusIfSaveChanged()
    {
        if (Time.unscaledTime < nextUnlockSavePollTime)
        {
            return;
        }

        nextUnlockSavePollTime = Time.unscaledTime + unlockSavePollInterval;
        DateTime writeTimeUtc = File.Exists(unlockSavePath)
            ? File.GetLastWriteTimeUtc(unlockSavePath)
            : DateTime.MinValue;

        if (writeTimeUtc != lastUnlockSaveWriteTimeUtc)
        {
            RefreshAvailableMenusFromUnlockSave();
        }
    }

    private void RefreshAvailableMenusFromUnlockSave()
    {
        availableMenus.Clear();

        MenuUnlockSaveData unlockSaveData = LoadUnlockData();
        EnsureDefaultIceWaterUnlocked(unlockSaveData);

        foreach (KeyValuePair<string, string> menuEntry in menuNameById)
        {
            if (unlockSaveData.IsUnlocked(menuEntry.Key))
            {
                availableMenus.Add(menuEntry.Value);
            }
        }

        if (availableMenus.Count == 0 && menuNameById.TryGetValue("0", out string defaultMenuName))
        {
            availableMenus.Add(defaultMenuName);
        }

        lastUnlockSaveWriteTimeUtc = File.Exists(unlockSavePath)
            ? File.GetLastWriteTimeUtc(unlockSavePath)
            : DateTime.MinValue;
    }

    private void EnsureDefaultIceWaterUnlocked(MenuUnlockSaveData unlockSaveData)
    {
        MenuUnlockState defaultMenuState = unlockSaveData.GetOrCreate("0");
        if (defaultMenuState.isUnlocked && defaultMenuState.level > 0)
        {
            return;
        }

        defaultMenuState.isUnlocked = true;
        defaultMenuState.level = 1;
        SaveUnlockData(unlockSaveData);
    }

    private MenuUnlockSaveData LoadUnlockData()
    {
        if (!File.Exists(unlockSavePath))
        {
            return new MenuUnlockSaveData();
        }

        try
        {
            MenuUnlockSaveData saveData = JsonUtility.FromJson<MenuUnlockSaveData>(File.ReadAllText(unlockSavePath));
            if (saveData == null)
            {
                saveData = new MenuUnlockSaveData();
            }

            saveData.EnsureInitialized();
            return saveData;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"{nameof(CustomerManager)}: Failed to load {unlockSavePath}. {exception.Message}");
            return new MenuUnlockSaveData();
        }
    }

    private void SaveUnlockData(MenuUnlockSaveData saveData)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(unlockSavePath));
        File.WriteAllText(unlockSavePath, JsonUtility.ToJson(saveData, true));
    }

    private string GetUnlockSavePath()
    {
        return Path.Combine(Application.persistentDataPath, unlockSaveFileName);
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