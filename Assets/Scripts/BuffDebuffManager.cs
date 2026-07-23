using UnityEngine;

public class BuffDebuffManager : MonoBehaviour
{
    private static BuffDebuffManager instance;

    [Header("1. 처녀귀신")]
    [SerializeField] private bool virginGhostBuffActive;
    [SerializeField] private bool virginGhostDebuffActive;
    [SerializeField, Range(0f, 1f)] private float commandInputReductionRatio = 0.8f;
    [SerializeField, Min(0f)] private float shakeAmplitude = 6f;
    [SerializeField, Min(0f)] private float shakeSpeed = 26f;

    [Header("2. 저승사자")]
    [SerializeField] private bool grimReaperBuffActive;
    [SerializeField] private bool grimReaperDebuffActive;
    [SerializeField, Min(0f)] private float receiptPatienceBonusSeconds = 10f;
    [SerializeField] private Color upgradedReceiptColor = new Color(1f, 0.86f, 0.08f, 1f);
    [SerializeField, Min(0.01f)] private float cookingTimerDecreaseMultiplier = 1.2f;

    [Header("3. 도깨비")]
    [SerializeField] private bool dokkaebiBuffActive;
    [SerializeField] private bool dokkaebiDebuffActive;
    [SerializeField] private Color dokkaebiShieldTextColor = new Color(1f, 0.86f, 0.08f, 1f);

    [Header("4. 작은 유령")]
    [SerializeField] private bool littleGhostBuffActive;
    [SerializeField] private bool littleGhostDebuffActive;
    [SerializeField, Range(0f, 1f)] private float unlockCostDiscountRatio = 0.2f;

    public static BuffDebuffManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<BuffDebuffManager>();
            }

            return instance;
        }
    }

    public static bool VirginGhostBuffActive => Instance != null && Instance.virginGhostBuffActive;
    public static bool VirginGhostDebuffActive => Instance != null && Instance.virginGhostDebuffActive;
    public static bool GrimReaperBuffActive => Instance != null && Instance.grimReaperBuffActive;
    public static bool GrimReaperDebuffActive => Instance != null && Instance.grimReaperDebuffActive;
    public static bool DokkaebiBuffActive => Instance != null && Instance.dokkaebiBuffActive;
    public static bool DokkaebiDebuffActive => Instance != null && Instance.dokkaebiDebuffActive;
    public static bool LittleGhostBuffActive => Instance != null && Instance.littleGhostBuffActive;
    public static bool LittleGhostDebuffActive => Instance != null && Instance.littleGhostDebuffActive;

    public static float ShakeAmplitude => Instance != null ? Instance.shakeAmplitude : 0f;
    public static float ShakeSpeed => Instance != null ? Instance.shakeSpeed : 0f;
    public static float ReceiptPatienceBonusSeconds => Instance != null ? Instance.receiptPatienceBonusSeconds : 0f;
    public static Color UpgradedReceiptColor => Instance != null ? Instance.upgradedReceiptColor : Color.yellow;
    public static float CookingTimerDecreaseMultiplier => Instance != null && Instance.grimReaperDebuffActive ? Instance.cookingTimerDecreaseMultiplier : 1f;
    public static Color DokkaebiShieldTextColor => Instance != null ? Instance.dokkaebiShieldTextColor : Color.yellow;
    public static bool ReceiptBuffActive => DokkaebiBuffActive;
    public static bool CommandMistakeShieldBuffActive => GrimReaperBuffActive;
    public static Color CommandMistakeShieldTextColor => DokkaebiShieldTextColor;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"{nameof(BuffDebuffManager)} duplicate found. Using the first active instance.");
            return;
        }

        instance = this;
    }

    public static string ApplyVirginGhostCommandReduction(string command)
    {
        if (!VirginGhostBuffActive || string.IsNullOrEmpty(command))
        {
            return command;
        }

        float keepRatio = Mathf.Clamp01(Instance.commandInputReductionRatio);
        int targetCount = Mathf.Max(1, Mathf.RoundToInt(command.Length * keepRatio));
        return command.Substring(0, Mathf.Min(targetCount, command.Length));
    }

    public static int ApplyLittleGhostUnlockDiscount(int cost)
    {
        if (Instance == null || !Instance.littleGhostBuffActive)
        {
            return cost;
        }

        float remainRatio = 1f - Mathf.Clamp01(Instance.unlockCostDiscountRatio);
        return Mathf.Max(0, Mathf.RoundToInt(cost * remainRatio));
    }
    public void ToggleGhostDebuffs(GhostType type, bool isActive)
    {
        switch (type)
        {
            case GhostType.Woman:
                virginGhostDebuffActive = isActive;
                break;
            case GhostType.DeadLion:
                grimReaperDebuffActive = isActive;
                break;
            case GhostType.Dokaebi:
                dokkaebiDebuffActive = isActive;
                break;
            case GhostType.Little:
                littleGhostDebuffActive = isActive;
                break;
        }

        Debug.Log($"BuffDebuffManager: {type} debuff set to {isActive}");
    }

    public void ToggleGhostBuffs(GhostType type, bool isActive)
    {
        switch (type)
        {
            case GhostType.Woman:
                virginGhostBuffActive = isActive;
                Debug.Log($"Virgin ghost buff feedback: command length ratio {(Mathf.Clamp01(commandInputReductionRatio) * 100f):0.#}%.");
                break;
            case GhostType.DeadLion:
                grimReaperBuffActive = isActive;
                break;
            case GhostType.Dokaebi:
                dokkaebiBuffActive = isActive;
                break;
            case GhostType.Little:
                littleGhostBuffActive = isActive;
                break;
        }

        Debug.Log($"BuffDebuffManager: {type} buff set to {isActive}");
    }
}

