using UnityEngine;
using UnityEngine.VFX;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public BgmManager BGM { get; private set; }
    public SfxManager SFX { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // child managers
        BGM = GetComponentInChildren<BgmManager>();
        SFX = GetComponentInChildren<SfxManager>();
    }
}