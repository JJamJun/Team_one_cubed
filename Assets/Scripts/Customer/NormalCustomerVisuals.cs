using UnityEngine;
using UnityEngine.UI;

public class NormalCustomerVisuals : MonoBehaviour, ICustomerVisuals
{
    [Header("Image Layers")]
    [SerializeField] private Image clothesImage;
    [SerializeField] private Image hairImage;
    [SerializeField] private Image eyesImage;
    [SerializeField] private Image mouthImage;

    [Header("Randomization Arrays")]
    [SerializeField] private Sprite[] clothesSprites;
    [SerializeField] private Sprite[] hairSprites;
    [SerializeField] private Sprite[] eyeSprites;
    [SerializeField] private Sprite[] mouthSprites;

    [Header("Mood Overrides")]
    [SerializeField] private Sprite angryEyes;
    [SerializeField] private Sprite angryMouth;

    private void Start()
    {
        RandomizeAppearance();
    }

    private void RandomizeAppearance()
    {
        if (clothesSprites.Length > 0 && clothesImage != null)
            clothesImage.sprite = clothesSprites[Random.Range(0, clothesSprites.Length)];

        if (hairSprites.Length > 0 && hairImage != null)
            hairImage.sprite = hairSprites[Random.Range(0, hairSprites.Length)];

        if (eyeSprites.Length > 0 && eyesImage != null)
            eyesImage.sprite = eyeSprites[Random.Range(0, eyeSprites.Length)];

        if (mouthSprites.Length > 0 && mouthImage != null)
            mouthImage.sprite = mouthSprites[Random.Range(0, mouthSprites.Length)];
    }

    public void SetNeutral()
    {
        //blank for now, may need to implement later 
    }

    public void SetHappy()
    {
        //blank
    }

    public void SetAngry()
    {
        if (angryEyes != null && eyesImage != null) eyesImage.sprite = angryEyes;
        if (angryMouth != null && mouthImage != null) mouthImage.sprite = angryMouth;
    }
}