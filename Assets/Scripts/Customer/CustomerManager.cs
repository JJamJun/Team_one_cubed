using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;
using TMPro;

public class CustomerManager : MonoBehaviour
{
    public static event Action WrongOrderSubmitted;
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
    [Header("Unlock Progression")]
    [SerializeField] private string defaultMenuId = "0";
    [SerializeField, Min(0)] private int ratingUnlockCount = 2;
    [SerializeField, Min(0)] private int mixedOrderUnlockCount = 3;
    [SerializeField, Min(1)] private int onlyDefaultMaxItems = 1;
    [SerializeField, Min(1)] private int oneUnlockMaxItems = 2;
    [SerializeField, Min(1f)] private float onlyDefaultSpawnTime = 10f;

    [Header("Rating Based Ghost Chance")]
    [SerializeField, Range(0f, 1f)] private float ghostChanceScore0 = 0f;
    [SerializeField, Range(0f, 1f)] private float ghostChanceRating1 = 0.01f;
    [SerializeField, Range(0f, 1f)] private float ghostChanceRating2 = 0.05f;
    [SerializeField, Range(0f, 1f)] private float ghostChanceRating3 = 0.1f;
    [SerializeField, Range(0f, 1f)] private float ghostChanceRating4 = 0.2f;
    [SerializeField, Range(0f, 1f)] private float ghostChanceRating5 = 0.25f;

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
    private int unlockedMenuCountExcludingDefault;
    private int currentMaxItemsInOrder;
    private bool mixedOrdersAllowed;

    private Dictionary<int, CustomerController> waitingCustomers = new Dictionary<int, CustomerController>();

    private void Awake()
    {
        unlockSavePath = GetUnlockSavePath();
        LoadAvailableMenus();
        ResetSpawnTimer();
    }

    private void OnEnable()
    {
        CounterOrderController.GetSubmittedOrderError = GetSubmittedCounterOrderError;
        CounterOrderController.OnReceiptPrinted += HandleReceiptPrinted;
        Receipt.ReceiptSlotEmptied += HandleReceiptEmptied;
    }

    private void OnDisable()
    {
        CounterOrderController.GetSubmittedOrderError -= GetSubmittedCounterOrderError;
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

    private string GetSubmittedCounterOrderError(string submittedOrderText)
    {
        if (currentCustomerAtCounter == null || currentCustomerAtCounter.CurrentState != CustomerState.Ordering)
        {
            return " \uD604\uC7AC \uC190\uB2D8\uC774 \uC5C6\uC2B5\uB2C8\uB2E4! ";
        }

        if (DoOrdersMatch(currentCustomerAtCounter.OrderText, submittedOrderText))
        {
            return null;
        }

        Debug.Log($"{nameof(CustomerManager)}: submitted order does not match customer order.\nCustomer:\n{currentCustomerAtCounter.OrderText}\nSubmitted:\n{submittedOrderText}");
        WrongOrderSubmitted?.Invoke();
        currentCustomerAtCounter.OrderFailed(CustomerAngryReason.WrongOrder, true);
        return string.Empty;
    }

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

    private void HandleReceiptEmptied(int slotIndex, bool isSuccess, string receiptText, bool completedWithinHalfTime, ReceiptFailureReason failureReason)
    {
        if (waitingCustomers.TryGetValue(slotIndex, out CustomerController customer))
        {
            if (customer != null)
            {
                if (isSuccess)
                {
                    customer.OrderFulfilled();

                    GameStatsManager.Instance?.RegisterCustomerSuccess(receiptText, customer.TotalDrinksOrdered);
                    ReputationRatingManager.Instance?.RegisterReceiptSuccess(customer.TotalDrinksOrdered, completedWithinHalfTime, customer.CustomerGhostType != GhostType.None);

                    if (stationScroller != null)
                    {
                        stationScroller.MoveToPickup();
                    }
                }
                else
                {
                    bool shouldTriggerAngryEvent = failureReason == ReceiptFailureReason.Timeout;
                    CustomerAngryReason angryReason = shouldTriggerAngryEvent
                        ? CustomerAngryReason.ReceiptTimeout
                        : CustomerAngryReason.None;
                    customer.OrderFailed(angryReason, shouldTriggerAngryEvent);
                }
            }

            waitingCustomers.Remove(slotIndex);
        }
    }

    private bool DoOrdersMatch(string expectedOrderText, string submittedOrderText)
    {
        Dictionary<string, int> expectedOrder = ParseOrderText(expectedOrderText);
        Dictionary<string, int> submittedOrder = ParseOrderText(submittedOrderText);
        if (expectedOrder.Count != submittedOrder.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, int> expectedLine in expectedOrder)
        {
            if (!submittedOrder.TryGetValue(expectedLine.Key, out int submittedCount)
                || submittedCount != expectedLine.Value)
            {
                return false;
            }
        }

        return true;
    }

    private Dictionary<string, int> ParseOrderText(string orderText)
    {
        Dictionary<string, int> order = new Dictionary<string, int>();
        if (string.IsNullOrWhiteSpace(orderText))
        {
            return order;
        }

        string[] lines = orderText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            int countSuffixIndex = line.LastIndexOf("\uC794", StringComparison.Ordinal);
            if (countSuffixIndex <= 0)
            {
                continue;
            }

            string beforeSuffix = line.Substring(0, countSuffixIndex).Trim();
            int countStartIndex = beforeSuffix.LastIndexOf(' ');
            if (countStartIndex <= 0)
            {
                continue;
            }

            string menuName = beforeSuffix.Substring(0, countStartIndex).Trim();
            string countText = beforeSuffix.Substring(countStartIndex + 1).Trim();
            if (string.IsNullOrEmpty(menuName) || !int.TryParse(countText, out int count))
            {
                continue;
            }

            if (order.ContainsKey(menuName))
            {
                order[menuName] += count;
            }
            else
            {
                order[menuName] = count;
            }
        }

        return order;
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
        if (IsAngryEventBlockingSpawn()) return;

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

    private bool IsAngryEventBlockingSpawn()
    {
        return AngryManager.Instance != null
            && AngryManager.Instance.HasPendingAngryEvent;
    }

    private void SpawnCustomer()
    {
        isCounterOccupied = true;
        bool spawningPendingGhost = ShouldSpawnPendingGhost();
        GameObject prefabToSpawn = spawningPendingGhost ? pendingGhostPrefab : normalCustomerPrefab;

        if (!spawningPendingGhost && CanStartGhostTrial())
        {
            TryStartGhostTrial();
        }

        GameObject customerObj = Instantiate(prefabToSpawn, customerContainer);
        currentCustomerAtCounter = customerObj.GetComponent<CustomerController>();

        if (currentCustomerAtCounter != null)
        {
            currentCustomerAtCounter.OnCustomerLeft += HandleCustomerLeft;
            GameStatsManager.Instance?.RegisterCustomerArrival();

            if (spawningPendingGhost)
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
            && Random.value <= GetCurrentGhostChance();
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
        int itemCount = Random.Range(1, GetCurrentMaxItemsInOrder() + 1);
        Dictionary<string, int> orderCounts = new Dictionary<string, int>();
        totalDrinkCount = 0; //track total!!! why bake straight into string bruh :( 

        if (!mixedOrdersAllowed)
        {
            string randomMenu = availableMenus[Random.Range(0, availableMenus.Count)];
            orderCounts[randomMenu] = itemCount;
            totalDrinkCount = itemCount;
        }
        else
        {
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
        }

        System.Text.StringBuilder orderBuilder = new System.Text.StringBuilder();
        foreach (var kvp in orderCounts)
        {
            orderBuilder.AppendLine($"{kvp.Key} {kvp.Value}\uC794");
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
        if (unlockedMenuCountExcludingDefault <= 0)
        {
            spawnTimer = onlyDefaultSpawnTime;
            return;
        }

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

        ApplyUnlockProgression(unlockSaveData);
    }

    private void ApplyUnlockProgression(MenuUnlockSaveData unlockSaveData)
    {
        unlockedMenuCountExcludingDefault = CountUnlockedMenusExcludingDefault(unlockSaveData);
        mixedOrdersAllowed = unlockedMenuCountExcludingDefault >= mixedOrderUnlockCount;

        if (unlockedMenuCountExcludingDefault <= 0)
        {
            currentMaxItemsInOrder = onlyDefaultMaxItems;
        }
        else if (unlockedMenuCountExcludingDefault == 1)
        {
            currentMaxItemsInOrder = oneUnlockMaxItems;
        }
        else
        {
            currentMaxItemsInOrder = maxItemsInOrder;
        }
    }

    private int CountUnlockedMenusExcludingDefault(MenuUnlockSaveData unlockSaveData)
    {
        int count = 0;
        unlockSaveData.EnsureInitialized();
        for (int i = 0; i < unlockSaveData.menus.Count; i++)
        {
            MenuUnlockState state = unlockSaveData.menus[i];
            if (state == null
                || state.menuID == defaultMenuId
                || !state.isUnlocked
                || state.level <= 0)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private int GetCurrentMaxItemsInOrder()
    {
        return Mathf.Max(1, currentMaxItemsInOrder > 0 ? currentMaxItemsInOrder : maxItemsInOrder);
    }

    private float GetCurrentGhostChance()
    {
        if (unlockedMenuCountExcludingDefault < ratingUnlockCount)
        {
            return 0f;
        }

        if (ReputationRatingManager.Instance == null || !ReputationRatingManager.Instance.IsRatingUnlocked)
        {
            return ghostChance;
        }

        switch (ReputationRatingManager.Instance.CurrentAngryEventBand)
        {
            case 0: return ghostChanceScore0;
            case 1: return ghostChanceRating1;
            case 2: return ghostChanceRating2;
            case 3: return ghostChanceRating3;
            case 4: return ghostChanceRating4;
            case 5: return ghostChanceRating5;
            default: return ghostChance;
        }
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
