using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SyrupDispenserController : MonoBehaviour
{
    [SerializeField] private Button syrupButton;
    [SerializeField] private RectTransform syrupPosArea;

    private UnityAction clickListener;

    private void Awake()
    {
        if (syrupButton == null)
        {
            syrupButton = GetComponent<Button>();
        }

        clickListener = ApplySyrupToCupInSlot;
    }

    private void OnEnable()
    {
        if (syrupButton == null)
        {
            Debug.LogWarning($"{nameof(SyrupDispenserController)}: Syrup button is not assigned.");
            return;
        }

        syrupButton.onClick.AddListener(clickListener);
    }

    private void OnDisable()
    {
        if (syrupButton != null)
        {
            syrupButton.onClick.RemoveListener(clickListener);
        }
    }

    private void ApplySyrupToCupInSlot()
    {
        CupDragController cup = FindCupInSyrupPos();
        if (cup == null)
        {
            Debug.Log("SyrupPos has no CupSprite.");
            return;
        }

        cup.ApplyIngredient(CupContentState.SyrupEd);
    }

    private CupDragController FindCupInSyrupPos()
    {
        for (int i = CupDragController.SpawnedCups.Count - 1; i >= 0; i--)
        {
            CupDragController cup = CupDragController.SpawnedCups[i];
            if (cup != null && cup.IsInsideArea(syrupPosArea))
            {
                return cup;
            }
        }

        return null;
    }
}
