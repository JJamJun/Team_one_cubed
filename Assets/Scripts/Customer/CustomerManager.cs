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

    private float spawnTimer;
    private bool isCounterOccupied;
    private CustomerController currentCustomerAtCounter;
    private readonly List<string> availableMenus = new List<string>();

    private void Awake()
    {
        LoadAvailableMenus();
        ResetSpawnTimer();
    }

    private void Update()
    {
        ManageSpawning();
        MonitorCounterState();
    }

    private void ManageSpawning()
    {
        if (isCounterOccupied || availableMenus.Count == 0)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnCustomer();
        }
    }

    private void MonitorCounterState()
    {
        if (!isCounterOccupied || currentCustomerAtCounter == null)
        {
            return;
        }

        //if the customer is no longer arriving or ordering, they have left the counter
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

        //determine whether to spawn a ghost or normal customer
        GameObject prefabToSpawn = normalCustomerPrefab;
        if (ghostCustomerPrefabs != null && ghostCustomerPrefabs.Length > 0 && Random.value <= ghostChance)
        {
            prefabToSpawn = ghostCustomerPrefabs[Random.Range(0, ghostCustomerPrefabs.Length)];
        }

        //instantiate and initialize
        GameObject customerObj = Instantiate(prefabToSpawn, customerContainer);
        currentCustomerAtCounter = customerObj.GetComponent<CustomerController>();

        if (currentCustomerAtCounter != null)
        {
            currentCustomerAtCounter.InitializeWaypoints(spawnPoint, counterPoint, pickupPoint, exitPoint);

            //generate a random order text to display in the bubble
            string orderText = GenerateRandomOrder();
            currentCustomerAtCounter.SetOrderText(orderText);

            currentCustomerAtCounter.Spawn();
        }
        else
        {
            Debug.LogWarning("Spawned customer prefab is missing CustomerController");
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
        if (menuInfoCsv == null)
        {
            Debug.LogWarning("CustomerManager: menu_info CSV is missing");
            return;
        }

        string[] lines = menuInfoCsv.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return;

        //start at 1 to skip the header row
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