using UnityEngine;
using UnityEngine.UI;

public class NormalCustomerVisuals : MonoBehaviour, ICustomerVisuals
{
    [Header("Image Layers")]
    [SerializeField] private Image bodyImage;
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
    [SerializeField] private Sprite scaryEyes;
    [SerializeField] private Sprite scaryMouth;

    private Sprite defaultEyes;
    private Sprite defaultMouth;

    private void Start()
    {
        RandomizeAppearance();
        CaptureDefaultMoodSprites();
    }

    private void CaptureDefaultMoodSprites()
    {
        if (eyesImage != null)
        {
            defaultEyes = eyesImage.sprite;
        }

        if (mouthImage != null)
        {
            defaultMouth = mouthImage.sprite;
        }
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
        RestoreDefaultMoodSprites();
    }

    public void SetHappy()
    {
        RestoreDefaultMoodSprites();
    }

    public void SetAngry()
    {
        if (scaryEyes != null && eyesImage != null)
        {
            eyesImage.sprite = scaryEyes;
        }
        else if (angryEyes != null && eyesImage != null)
        {
            eyesImage.sprite = angryEyes;
        }

        if (scaryMouth != null && mouthImage != null)
        {
            mouthImage.sprite = scaryMouth;
        }
        else if (angryMouth != null && mouthImage != null)
        {
            mouthImage.sprite = angryMouth;
        }
    }

    private void RestoreDefaultMoodSprites()
    {
        if (eyesImage != null && defaultEyes != null)
        {
            eyesImage.sprite = defaultEyes;
        }

        if (mouthImage != null && defaultMouth != null)
        {
            mouthImage.sprite = defaultMouth;
        }
    }

    public void SetVisible(bool isVisible)
    {
        if (bodyImage != null) bodyImage.gameObject.SetActive(isVisible);
        if (clothesImage != null) clothesImage.gameObject.SetActive(isVisible);
        if (hairImage != null) hairImage.gameObject.SetActive(isVisible);
        if (eyesImage != null) eyesImage.gameObject.SetActive(isVisible);
        if (mouthImage != null) mouthImage.gameObject.SetActive(isVisible);
    }
}
