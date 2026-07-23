using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CookingPanelEventNoticeAnimator : MonoBehaviour
{
    [SerializeField] private GameObject cookingPanel;
    [SerializeField] private RectTransform bellLeft;
    [SerializeField] private RectTransform angryLeft;
    [SerializeField] private RectTransform angryRight;
    [SerializeField, Min(0.01f)] private float showDuration = 0.35f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 1.2f;
    [SerializeField, Min(0f)] private float visibleCenterTolerance = 200f;

    private Vector3 bellLeftScale = Vector3.one;
    private Vector3 angryLeftScale = Vector3.one;
    private Vector3 angryRightScale = Vector3.one;
    private Sequence bellLeftSequence;
    private Sequence angryLeftSequence;
    private Sequence angryRightSequence;

    private void Awake()
    {
        AutoBindReferences();
        CaptureScale(bellLeft, ref bellLeftScale);
        CaptureScale(angryLeft, ref angryLeftScale);
        CaptureScale(angryRight, ref angryRightScale);

        SetHidden(bellLeft, bellLeftScale);
        SetHidden(angryLeft, angryLeftScale);
        SetHidden(angryRight, angryRightScale);
    }

    private void OnEnable()
    {
        CustomerController.CustomerBellDinged += ShowBellLeft;
        CustomerController.CustomerPatienceExpired += ShowAngryLeft;
        Receipt.ReceiptTimedOut += ShowAngryRight;
    }

    private void OnDisable()
    {
        CustomerController.CustomerBellDinged -= ShowBellLeft;
        CustomerController.CustomerPatienceExpired -= ShowAngryLeft;
        Receipt.ReceiptTimedOut -= ShowAngryRight;

        KillAllTweens();
    }

    private void ShowBellLeft()
    {
        if (!IsCookingPanelVisible())
        {
            return;
        }

        PlayNotice(bellLeft, bellLeftScale, ref bellLeftSequence);
    }

    private void ShowAngryLeft()
    {
        if (!IsCookingPanelVisible())
        {
            return;
        }

        SoundManager.Instance?.SFX?.PlayFail();
        PlayNotice(angryLeft, angryLeftScale, ref angryLeftSequence);
    }

    private void ShowAngryRight()
    {
        if (!IsCookingPanelVisible())
        {
            return;
        }

        PlayNotice(angryRight, angryRightScale, ref angryRightSequence);
    }

    private void PlayNotice(RectTransform target, Vector3 originalScale, ref Sequence activeSequence)
    {
        if (target == null)
        {
            return;
        }

        CanvasGroup canvasGroup = GetOrAddCanvasGroup(target.gameObject);
        activeSequence?.Kill();
        target.DOKill();
        canvasGroup.DOKill();

        target.gameObject.SetActive(true);
        target.localScale = Vector3.zero;
        canvasGroup.alpha = 1f;

        activeSequence = DOTween.Sequence().SetTarget(target);
        activeSequence.Append(target.DOScale(originalScale, showDuration).SetEase(Ease.OutBounce));
        activeSequence.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.Linear));
        activeSequence.OnComplete(() => SetHidden(target, originalScale));
    }

    private void SetHidden(RectTransform target, Vector3 originalScale)
    {
        if (target == null)
        {
            return;
        }

        target.localScale = originalScale;

        CanvasGroup canvasGroup = GetOrAddCanvasGroup(target.gameObject);
        canvasGroup.alpha = 0f;
        target.gameObject.SetActive(false);
    }

    private void CaptureScale(RectTransform target, ref Vector3 scale)
    {
        if (target != null)
        {
            scale = target.localScale;
        }
    }

    private bool IsCookingPanelVisible()
    {
        GameObject panel = cookingPanel != null ? cookingPanel : gameObject;
        if (panel == null || !panel.activeInHierarchy)
        {
            return false;
        }

        RectTransform panelRect = panel.transform as RectTransform;
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (panelRect == null || canvasRect == null)
        {
            return true;
        }

        Vector3 panelCenter = canvasRect.InverseTransformPoint(panelRect.position);
        return Mathf.Abs(panelCenter.x) <= visibleCenterTolerance;
    }

    private void KillAllTweens()
    {
        bellLeftSequence?.Kill();
        angryLeftSequence?.Kill();
        angryRightSequence?.Kill();

        if (bellLeft != null) bellLeft.DOKill();
        if (angryLeft != null) angryLeft.DOKill();
        if (angryRight != null) angryRight.DOKill();
    }

    private void AutoBindReferences()
    {
        if (cookingPanel == null)
        {
            cookingPanel = gameObject.name == "CookingPanel" ? gameObject : FindSceneObject("CookingPanel");
        }

        if (bellLeft == null)
        {
            bellLeft = FindSceneRectTransform("BellLeft");
        }

        if (angryLeft == null)
        {
            angryLeft = FindSceneRectTransform("AngryLeft");
        }

        if (angryRight == null)
        {
            angryRight = FindSceneRectTransform("AngryRight");
        }
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private static RectTransform FindSceneRectTransform(string targetName)
    {
        GameObject target = FindSceneObject(targetName);
        return target != null ? target.transform as RectTransform : null;
    }

    private static GameObject FindSceneObject(string targetName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target.gameObject.scene.IsValid() && target.name == targetName)
            {
                return target.gameObject;
            }
        }

        return null;
    }
}
