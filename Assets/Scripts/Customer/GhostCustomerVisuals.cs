using UnityEngine;
using UnityEngine.UI;

public class GhostCustomerVisuals : MonoBehaviour, ICustomerVisuals
{
    [Header("Ghost Image Component")]
    [SerializeField] private Image ghostImage;

    [Header("Mood Sprites")]
    [SerializeField] private Sprite neutralSprite;
    [SerializeField] private Sprite happySprite;
    [SerializeField] private Sprite angrySprite;

    public void SetNeutral()
    {
        if (neutralSprite != null && ghostImage != null) ghostImage.sprite = neutralSprite;
    }

    public void SetHappy()
    {
        if (happySprite != null && ghostImage != null) ghostImage.sprite = happySprite;
    }

    public void SetAngry()
    {
        if (angrySprite != null && ghostImage != null) ghostImage.sprite = angrySprite;
    }
    public void SetVisible(bool isVisible)
    {
        if (ghostImage != null) ghostImage.gameObject.SetActive(isVisible);
    }
}