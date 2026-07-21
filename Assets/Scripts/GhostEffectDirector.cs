using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GhostEffectDirector : MonoBehaviour
{
    [Header("Atmosphere Visuals")]
    [SerializeField] private Image vignetteImage;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float targetDarkness = 0.8f;

    private BuffDebuffManager debuffManager;

    private void Awake()
    {
        debuffManager = BuffDebuffManager.Instance;

        if (vignetteImage != null)
        {
            Color c = vignetteImage.color;
            c.a = 0f;
            vignetteImage.color = c;
            vignetteImage.gameObject.SetActive(false);
        }
    }

    public void TriggerGhostArrival(GhostType ghostType)
    {
        if (ghostType == GhostType.None) return;

        Debug.Log($"GhostEffectDirector: {ghostType} has arrived! Activating debuffs and atmosphere...");

        if (debuffManager != null)
        {
            debuffManager.ToggleGhostDebuffs(ghostType, true);
        }

        if (vignetteImage != null)
        {
            vignetteImage.gameObject.SetActive(true);
            vignetteImage.DOFade(targetDarkness, fadeDuration);
        }

        //muffle BGM
        if (SoundManager.Instance != null && SoundManager.Instance.BGM != null)
        {
            SoundManager.Instance.BGM.SetMuffled(true);
        }
    }

    public void TriggerGhostDeparture(GhostType ghostType, bool isHappy)
    {
        if (ghostType == GhostType.None) return;

        Debug.Log($"GhostEffectDirector: {ghostType} left (Happy: {isHappy}). Restoring peace...");

        if (debuffManager != null)
        {
            debuffManager.ToggleGhostDebuffs(ghostType, false);
        }

        if (vignetteImage != null)
        {
            vignetteImage.DOFade(0f, fadeDuration).OnComplete(() => vignetteImage.gameObject.SetActive(false));
        }

        //restore BGM
        if (SoundManager.Instance != null && SoundManager.Instance.BGM != null)
        {
            SoundManager.Instance.BGM.SetMuffled(false);
        }
    }
}