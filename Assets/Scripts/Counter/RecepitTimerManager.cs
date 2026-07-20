using UnityEngine;

public class RecepitTimerManager : MonoBehaviour
{
    private static RecepitTimerManager instance;

    [SerializeField] private float timeLimitSeconds = 30f;
    [SerializeField] private float timeIncreaseSeconds = 5f;

    public static RecepitTimerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<RecepitTimerManager>();
            }

            return instance;
        }
    }

    public float TimeLimitSeconds => Mathf.Max(0.01f, timeLimitSeconds);
    public float TimeIncreaseSeconds => Mathf.Max(0f, timeIncreaseSeconds);

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"{nameof(RecepitTimerManager)} duplicate found. Using the first active instance.");
            return;
        }

        instance = this;
    }
}
