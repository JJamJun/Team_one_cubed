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
    [SerializeField] private Sprite[] scaryBodySprites; // Array for the random scary bodies

    private Sprite defaultEyes;
    private Sprite defaultMouth;
    private Sprite defaultBody; // To remember the original body

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

        if (bodyImage != null)
        {
            defaultBody = bodyImage.sprite;
        }
    }

    private void RandomizeAppearance()
    {
        if (clothesSprites.Length > 0 && clothesImage != null)
        {
            clothesImage.sprite = clothesSprites[Random.Range(0, clothesSprites.Length)];
            clothesImage.SetNativeSize();
        }

        if (hairSprites.Length > 0 && hairImage != null)
        {
            hairImage.sprite = hairSprites[Random.Range(0, hairSprites.Length)];
            hairImage.SetNativeSize();
        }

        if (eyeSprites.Length > 0 && eyesImage != null)
        {
            eyesImage.sprite = eyeSprites[Random.Range(0, eyeSprites.Length)];
            eyesImage.SetNativeSize();
        }

        if (mouthSprites.Length > 0 && mouthImage != null)
        {
            mouthImage.sprite = mouthSprites[Random.Range(0, mouthSprites.Length)];
            mouthImage.SetNativeSize();
        }
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
        if (angryEyes != null && eyesImage != null)
        {
            eyesImage.sprite = angryEyes;
        }
        else if (scaryEyes != null && eyesImage != null)
        {
            eyesImage.sprite = scaryEyes;
        }

        if (angryMouth != null && mouthImage != null)
        {
            mouthImage.sprite = angryMouth;
        }
        else if (scaryMouth != null && mouthImage != null)
        {
            mouthImage.sprite = scaryMouth;
        }
    }

    public void SetScary()
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

        if (scaryBodySprites != null && scaryBodySprites.Length > 0 && bodyImage != null)
        {
            bodyImage.sprite = scaryBodySprites[Random.Range(0, scaryBodySprites.Length)];
        }
    }

    private void RestoreDefaultMoodSprites()
    {
        if (eyesImage != null && defaultEyes != null)
        {
            eyesImage.sprite = defaultEyes;
            eyesImage.SetNativeSize();
        }

        if (mouthImage != null && defaultMouth != null)
        {
            mouthImage.sprite = defaultMouth;
            mouthImage.SetNativeSize();
        }

        if (bodyImage != null && defaultBody != null)
        {
            bodyImage.sprite = defaultBody;
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
