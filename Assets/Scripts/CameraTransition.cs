using UnityEngine;
using DG.Tweening;

public class UIStationScoller : MonoBehaviour
{
    [SerializeField] private RectTransform mainContentPanel;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private Ease transitionEase = Ease.InOutQuad;

    private float slideDistance = 1920f;

    private void Awake()
    {
        if (mainContentPanel == null)
        {
            mainContentPanel = GetComponent<RectTransform>();
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            slideDistance = canvas.GetComponent<RectTransform>().rect.width;
        }
    }

    public void MoveToCounter()
    {
        mainContentPanel.DOKill();
        mainContentPanel.DOLocalMoveX(0f, transitionDuration).SetEase(transitionEase);
        Debug.Log("Sliding to Counter!");
    }

    public void MoveToKitchen()
    {
        mainContentPanel.DOKill();
        mainContentPanel.DOLocalMoveX(-slideDistance, transitionDuration).SetEase(transitionEase);
        Debug.Log("Sliding to Kitchen!");
    }
}