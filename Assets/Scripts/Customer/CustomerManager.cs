using System.Collections.Generic;
using UnityEngine;
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

    [Header("Order Settings")]
    [SerializeField, Min(1)] private int maxItemsInOrder = 3;
    [SerializeField] private TextAsset menuInfoCsv;

    [Header("Dependencies")]
    [SerializeField] private UIStationScoller stationScroller;

    private float spawnTimer;
    private bool isCounterOccupied;
    private CustomerController currentCustomerAtCounter;
    private CustomerController activeGhost; 
    private readonly List<string> availableMenus = new List<string>();

    private Dictionary<int, CustomerController> waitingCustomers = new Dictionary<int, CustomerController>();

    private void Awake()
    {
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

    private void HandleReceiptEmptied(int slotIndex, bool isSuccess)
    {
        if (waitingCustomers.TryGetValue(slotIndex, out CustomerController customer))
        {
            if (customer != null)
            {
                if (isSuccess)
                {
                    customer.OrderFulfilled();

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
        customer.OnCustomerLeft -= HandleCustomerLeft; //unsubscribe to prevent memory leaks

        if (customer == activeGhost)
        {
            activeGhost = null; 
            Debug.Log("Ghost left, new ghost can spawn");
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

        // NEW: Only roll for a ghost if there isn't one already in the store
        if (activeGhost == null && ghostCustomerPrefabs != null && ghostCustomerPrefabs.Length > 0 && Random.value <= ghostChance)
        {
            prefabToSpawn = ghostCustomerPrefabs[Random.Range(0, ghostCustomerPrefabs.Length)];
            spawningGhost = true;
        }

        GameObject customerObj = Instantiate(prefabToSpawn, customerContainer);
        currentCustomerAtCounter = customerObj.GetComponent<CustomerController>();

        if (currentCustomerAtCounter != null)
        {
            // NEW: Subscribe to the leave event
            currentCustomerAtCounter.OnCustomerLeft += HandleCustomerLeft;

            if (spawningGhost)
            {
                activeGhost = currentCustomerAtCounter;
            }

            currentCustomerAtCounter.InitializeWaypoints(spawnPoint, counterPoint, pickupPoint, exitPoint);
            currentCustomerAtCounter.SetOrderText(GenerateRandomOrder());
            currentCustomerAtCounter.Spawn();
        }
        else
        {
            isCounterOccupied = false;
        }
    }

    private string GenerateRandomOrder()
    {
        int itemCount = Random.Range(1, maxItemsInOrder + 1);
        Dictionary<string, int> orderCounts = new Dictionary<string, int>();

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
        if (menuInfoCsv == null) return;

        string[] lines = menuInfoCsv.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return;

        for (int i = 1; i < lines.Length; i++)
        {
            string[] cells = lines[i].Split(',');
            if (cells.Length > 1 && !string.IsNullOrWhiteSpace(cells[1]))
            {
                availableMenus.Add(cells[1].Trim());
            }
        }
    }

    private void ResetSpawnTimer()
    {
        spawnTimer = Random.Range(minSpawnTime, maxSpawnTime);
    }
}