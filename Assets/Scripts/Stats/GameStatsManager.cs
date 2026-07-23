using System.IO;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// save data becomes JSON
[System.Serializable]
public class PlayerSaveData
{
    public int coin = 0;
    public int soul = 0;
    public int money = 0;
    // Future proofing: You can easily add upgrade trackers here later!
    // public bool[] unlockedUpgrades = new bool[5]; 
}

public class GameStatsManager : MonoBehaviour
{
    public static GameStatsManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text moneyTextLabel;
    [SerializeField] private TMP_Text moneyUnitTextLabel;
    [SerializeField] private TMP_Text customerStatsTextLabel;
    [SerializeField] private Image timerImage; 

    [Header("Money Display Settings")]
    [SerializeField] private string moneyUnitText = "원";
    [SerializeField] private float moneyValueUnitGap = 4f;

    [Header("Economy Settings")]
    [SerializeField] private int baseMoneyPerDrink = 200;
    [SerializeField] private string menuInfoResourcePath = "menu_info";

    [Header("Round Settings")]
    [SerializeField] private float roundDurationSeconds = 300f; //preset is 5 minutes i guess 

    [Header("Save Reset Settings")]
    [SerializeField] private bool resetProgressOnGameStart = true;
    [SerializeField] private string menuProgressSaveFileName = "menu_progress.json";

    //persistent data
    private PlayerSaveData saveData = new PlayerSaveData();
    private string saveFilePath;
    private readonly Dictionary<string, MenuPriceInfo> menuPriceByName = new Dictionary<string, MenuPriceInfo>();

    //transient? data
    private int totalCustomersVisited;
    private int totalCustomersSatisfied;
    private float roundTimeRemaining;
    private bool isRoundActive;

    public int CurrentCoin => saveData != null ? saveData.coin : 0;

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

        ResetProgressOnGameStart();
        AutoBindMoneyUnitText();
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
        RegisterCustomerSuccess(null, drinksInOrder);
    }

    public void RegisterCustomerSuccess(string receiptText, int drinksInOrder = 1)
    {
        totalCustomersSatisfied++;

        saveData.coin += CalculateReceiptReward(receiptText, drinksInOrder);
        SyncLegacyMoneyField();

        SaveData();
        UpdateUI();
    }

    public bool TrySpendCoins(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (saveData == null)
        {
            LoadData();
        }

        if (saveData.coin < amount)
        {
            return false;
        }

        saveData.coin -= amount;
        SyncLegacyMoneyField();
        SaveData();
        UpdateUI();
        return true;
    }

    public void RefreshWalletFromDisk()
    {
        LoadData();
        UpdateUI();
    }

    private int CalculateReceiptReward(string receiptText, int fallbackDrinkCount)
    {
        LoadMenuPricesIfNeeded();

        int receiptReward = 0;
        int parsedDrinkCount = 0;
        foreach (ReceiptRewardLine line in ParseReceiptRewardLines(receiptText))
        {
            parsedDrinkCount += line.Count;
            if (menuPriceByName.TryGetValue(line.MenuName, out MenuPriceInfo priceInfo))
            {
                receiptReward += priceInfo.GetPriceForLevel(GetUnlockedMenuLevel(priceInfo.MenuId)) * line.Count;
            }
            else
            {
                receiptReward += baseMoneyPerDrink * line.Count;
                Debug.LogWarning($"{nameof(GameStatsManager)}: Menu '{line.MenuName}' was not found in Resources/{menuInfoResourcePath}. Using fallback price.");
            }
        }

        if (parsedDrinkCount > 0)
        {
            return receiptReward;
        }

        return baseMoneyPerDrink * Mathf.Max(1, fallbackDrinkCount);
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
            AutoBindMoneyUnitText();
            string formattedMoney = saveData.coin.ToString("N0", CultureInfo.InvariantCulture);
            moneyTextLabel.text = moneyUnitTextLabel != null ? formattedMoney : $"{formattedMoney}{moneyUnitText}";
            UpdateMoneyUnitPosition();
        }

        if (customerStatsTextLabel != null)
        {
            customerStatsTextLabel.text = $"{totalCustomersSatisfied} / {totalCustomersVisited}";
        }
    }

    private void UpdateMoneyUnitPosition()
    {
        if (moneyTextLabel == null || moneyUnitTextLabel == null)
        {
            return;
        }

        moneyUnitTextLabel.text = moneyUnitText;
        moneyTextLabel.ForceMeshUpdate();
        moneyUnitTextLabel.ForceMeshUpdate();

        RectTransform valueRect = moneyTextLabel.rectTransform;
        RectTransform unitRect = moneyUnitTextLabel.rectTransform;
        if (valueRect == null || unitRect == null || valueRect.parent != unitRect.parent)
        {
            return;
        }

        Vector2 valuePosition = valueRect.anchoredPosition;
        Vector2 unitPosition = unitRect.anchoredPosition;

        Bounds valueBounds = moneyTextLabel.textBounds;
        Bounds unitBounds = moneyUnitTextLabel.textBounds;
        float valueRightEdge = valuePosition.x + valueBounds.center.x + valueBounds.extents.x;
        float unitLeftEdge = unitBounds.center.x - unitBounds.extents.x;

        unitPosition.x = valueRightEdge + moneyValueUnitGap - unitLeftEdge;
        unitPosition.y = valuePosition.y;
        unitRect.anchoredPosition = unitPosition;
    }

    private void AutoBindMoneyUnitText()
    {
        if (moneyUnitTextLabel != null)
        {
            return;
        }

        TMP_Text[] labels = Resources.FindObjectsOfTypeAll<TMP_Text>();
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null || label == moneyTextLabel || !label.gameObject.scene.IsValid())
            {
                continue;
            }

            if (label.name == "MoneyText")
            {
                moneyUnitTextLabel = label;
                return;
            }
        }
    }

    private void LoadMenuPricesIfNeeded()
    {
        if (menuPriceByName.Count > 0)
        {
            return;
        }

        TextAsset menuInfoAsset = Resources.Load<TextAsset>(menuInfoResourcePath);
        if (menuInfoAsset == null)
        {
            Debug.LogWarning($"{nameof(GameStatsManager)}: Resources/{menuInfoResourcePath} was not found. Using fallback drink price.");
            return;
        }

        string[] lines = menuInfoAsset.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            return;
        }

        List<string> headers = ParseCsvLine(lines[0]);
        int idIndex = headers.IndexOf("menuID");
        int nameIndex = headers.IndexOf("MenuName");
        int[] priceIndexes =
        {
            headers.IndexOf("Lv1Price"),
            headers.IndexOf("Lv2Price"),
            headers.IndexOf("Lv3Price"),
            headers.IndexOf("Lv4Price"),
            headers.IndexOf("Lv5Price")
        };

        for (int i = 1; i < lines.Length; i++)
        {
            List<string> cells = ParseCsvLine(lines[i]);
            if (!HasCell(cells, idIndex) || !HasCell(cells, nameIndex))
            {
                continue;
            }

            int[] prices = new int[priceIndexes.Length];
            for (int level = 0; level < priceIndexes.Length; level++)
            {
                prices[level] = HasCell(cells, priceIndexes[level]) && int.TryParse(cells[priceIndexes[level]], out int price)
                    ? price
                    : baseMoneyPerDrink;
            }

            string menuName = cells[nameIndex].Trim();
            if (!string.IsNullOrWhiteSpace(menuName))
            {
                menuPriceByName[menuName] = new MenuPriceInfo(cells[idIndex].Trim(), prices);
            }
        }
    }

    private List<ReceiptRewardLine> ParseReceiptRewardLines(string receiptText)
    {
        List<ReceiptRewardLine> lines = new List<ReceiptRewardLine>();
        if (string.IsNullOrWhiteSpace(receiptText))
        {
            return lines;
        }

        string[] receiptLines = receiptText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < receiptLines.Length; i++)
        {
            string line = receiptLines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            int count = 1;
            string menuName = line;
            int lastSpaceIndex = line.LastIndexOf(' ');
            if (lastSpaceIndex >= 0 && lastSpaceIndex + 1 < line.Length)
            {
                menuName = line.Substring(0, lastSpaceIndex).Trim();
                string countText = line.Substring(lastSpaceIndex + 1).Trim().Replace("잔", string.Empty);
                if (!int.TryParse(countText, out count))
                {
                    count = 1;
                }
            }

            if (!string.IsNullOrWhiteSpace(menuName))
            {
                lines.Add(new ReceiptRewardLine(menuName, Mathf.Max(1, count)));
            }
        }

        return lines;
    }

    private int GetUnlockedMenuLevel(string menuId)
    {
        string path = Path.Combine(Application.persistentDataPath, menuProgressSaveFileName);
        if (!File.Exists(path))
        {
            return menuId == "0" ? 1 : 0;
        }

        try
        {
            MenuUnlockSaveData unlockSaveData = JsonUtility.FromJson<MenuUnlockSaveData>(File.ReadAllText(path));
            if (unlockSaveData == null || unlockSaveData.menus == null)
            {
                return menuId == "0" ? 1 : 0;
            }

            MenuUnlockState state = unlockSaveData.menus.Find(menu => menu.menuID == menuId);
            if (state == null || !state.isUnlocked)
            {
                return menuId == "0" ? 1 : 0;
            }

            return Mathf.Clamp(state.level <= 0 ? 1 : state.level, 1, 5);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"{nameof(GameStatsManager)}: Failed to load menu progress. {exception.Message}");
            return menuId == "0" ? 1 : 0;
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        List<string> cells = new List<string>();
        if (line == null)
        {
            return cells;
        }

        System.Text.StringBuilder current = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                cells.Add(current.ToString().Trim());
                current.Length = 0;
            }
            else
            {
                current.Append(c);
            }
        }

        cells.Add(current.ToString().Trim());
        return cells;
    }

    private static bool HasCell(List<string> cells, int index)
    {
        return cells != null && index >= 0 && index < cells.Count;
    }

    // JSON LOGIC

    private void SaveData()
    {
        SyncLegacyMoneyField();
        string json = JsonUtility.ToJson(saveData, true); //prettyprinting
        File.WriteAllText(saveFilePath, json);
    }

    private void LoadData()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            saveData = JsonUtility.FromJson<PlayerSaveData>(json);
            MigrateLegacyMoneyField();
        }
        else
        {
            //create new save data if no file
            saveData = new PlayerSaveData();
            SyncLegacyMoneyField();
        }
    }

    private void ResetProgressOnGameStart()
    {
        if (!resetProgressOnGameStart)
        {
            return;
        }

        DeleteSaveFile(saveFilePath);
        DeleteSaveFile(Path.Combine(Application.persistentDataPath, menuProgressSaveFileName));
    }

    private void DeleteSaveFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        File.Delete(path);
    }

    private void MigrateLegacyMoneyField()
    {
        if (saveData == null)
        {
            saveData = new PlayerSaveData();
        }

        if (saveData.coin <= 0 && saveData.money > 0)
        {
            saveData.coin = saveData.money;
            SaveData();
            return;
        }

        SyncLegacyMoneyField();
    }

    private void SyncLegacyMoneyField()
    {
        if (saveData != null)
        {
            saveData.money = saveData.coin;
        }
    }

    private class MenuPriceInfo
    {
        private readonly int[] pricesByLevel;

        public MenuPriceInfo(string menuId, int[] pricesByLevel)
        {
            MenuId = menuId;
            this.pricesByLevel = pricesByLevel;
        }

        public string MenuId { get; }

        public int GetPriceForLevel(int level)
        {
            if (pricesByLevel == null || pricesByLevel.Length == 0)
            {
                return 0;
            }

            int index = Mathf.Clamp(level <= 0 ? 1 : level, 1, pricesByLevel.Length) - 1;
            return pricesByLevel[index];
        }
    }

    private class ReceiptRewardLine
    {
        public ReceiptRewardLine(string menuName, int count)
        {
            MenuName = menuName;
            Count = count;
        }

        public string MenuName { get; }
        public int Count { get; }
    }

    [System.Serializable]
    private class MenuUnlockSaveData
    {
        public List<MenuUnlockState> menus = new List<MenuUnlockState>();
    }

    [System.Serializable]
    private class MenuUnlockState
    {
        public string menuID;
        public bool isUnlocked;
        public int level;
    }
}
