using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// save data becomes JSON
[System.Serializable]
public class PlayerSaveData
{
    public int money = 0;
    // Future proofing: You can easily add upgrade trackers here later!
    // public bool[] unlockedUpgrades = new bool[5]; 
}

public class GameStatsManager : MonoBehaviour
{
    public static GameStatsManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text moneyTextLabel;
    [SerializeField] private TMP_Text customerStatsTextLabel;
    [SerializeField] private Image timerImage; 

    [Header("Economy Settings")]
    [SerializeField] private int baseMoneyPerDrink = 200;

    [Header("Round Settings")]
    [SerializeField] private float roundDurationSeconds = 300f; //preset is 5 minutes i guess 

    //persistent data
    private PlayerSaveData saveData = new PlayerSaveData();
    private string saveFilePath;

    //transient? data
    private int totalCustomersVisited;
    private int totalCustomersSatisfied;
    private float roundTimeRemaining;
    private bool isRoundActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        //setup json save file path
        saveFilePath = Path.Combine(Application.persistentDataPath, "player_wallet.json");

        LoadData();

        //reset transient data each round 
        totalCustomersVisited = 0;
        totalCustomersSatisfied = 0;
        roundTimeRemaining = roundDurationSeconds;
        isRoundActive = true;

        UpdateUI();
    }

    private void Update()
    {
        if (isRoundActive)
        {
            roundTimeRemaining -= Time.deltaTime;

            if (timerImage != null)
            {
                //scale time from 0 to 1 for fill
                timerImage.fillAmount = Mathf.Clamp01(roundTimeRemaining / roundDurationSeconds);
            }

            if (roundTimeRemaining <= 0f)
            {
                EndRound();
            }
        }
    }

    public void RegisterCustomerArrival()
    {
        totalCustomersVisited++;
        UpdateUI();
    }

    public void RegisterCustomerSuccess(int drinksInOrder = 1)
    {
        totalCustomersSatisfied++;

        //add money to save data
        saveData.money += (baseMoneyPerDrink * drinksInOrder);

        SaveData();
        UpdateUI();
    }

    private void EndRound()
    {
        isRoundActive = false;
        Debug.Log("Round is over! Time is up.");

        //TODO: UI stuff further? liek shop menu or end screen
    }

    private void UpdateUI()
    {
        if (moneyTextLabel != null)
        {
            moneyTextLabel.text = saveData.money.ToString("D5");
        }

        if (customerStatsTextLabel != null)
        {
            customerStatsTextLabel.text = $"{totalCustomersSatisfied} / {totalCustomersVisited}";
        }
    }

    // JSON LOGIC

    private void SaveData()
    {
        string json = JsonUtility.ToJson(saveData, true); //prettyprinting
        File.WriteAllText(saveFilePath, json);
    }

    private void LoadData()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            saveData = JsonUtility.FromJson<PlayerSaveData>(json);
        }
        else
        {
            //create new save data if no file
            saveData = new PlayerSaveData();
        }
    }
}