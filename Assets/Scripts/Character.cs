using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer hairRenderer;
    [SerializeField]
    private Sprite[] hairSprites;
    [SerializeField]
    private SpriteRenderer faceRenderer;
    [SerializeField]
    private Sprite[] faceSprites;
    [SerializeField]
    private Renderer renderer;
    [SerializeField]
    private Color[] clothColors;
    [SerializeField]
    private Animator animator;

    private int currentHairIndex = 0;
    public int CurrentHairIndex => currentHairIndex;
    private int currentFaceIndex = 0;
    public int CurrentFaceIndex => currentFaceIndex;
    private int currentClothIndex = 0;
    public int CurrentClothIndex => currentClothIndex;
    public int HairCount => hairSprites.Length;
    public int FaceCount => faceSprites.Length;
    public int ClothCount => clothColors.Length;
    private Color hairColor = Color.white;
    public Color HairColor => hairColor;
    private const string HairIndexKey = "HairIndex";
    private const string HairColorKey = "HairColor";
    private const string FaceIndexKey = "FaceIndex"; 
    private const string ClothIndexKey = "ClothIndex";

    public void SetHair(int index)
    {
        if (index >= 0 && index < hairSprites.Length)
        {
            currentHairIndex = index;
            hairRenderer.sprite = hairSprites[currentHairIndex];
        }
    }

    public void SetFace(int index)
    {
        if (index >= 0 && index < faceSprites.Length)
        {
            currentFaceIndex = index;
            faceRenderer.sprite = faceSprites[currentFaceIndex];
        }
    }

    public void SetCloth(int index)
    {
        if (index >= 0 && index < clothColors.Length)
        {
            currentClothIndex = index;
            renderer.material.SetColor("_TargetColor", clothColors[currentClothIndex]);
        }
    }

    public void SetHairColor(Color color)
    {
        hairColor = color;
        hairRenderer.color = hairColor;
    }

    public void SaveCharacterData()
    {
        PlayerPrefs.SetInt(HairIndexKey, currentHairIndex);
        PlayerPrefs.SetInt(FaceIndexKey, currentFaceIndex);
        PlayerPrefs.SetInt(ClothIndexKey, currentClothIndex);
        PlayerPrefs.SetString(HairColorKey, $"#{ColorUtility.ToHtmlStringRGBA(hairColor)}");
        PlayerPrefs.Save();
    }

    public void LoadCharacterData()
    {
        if (PlayerPrefs.HasKey(HairIndexKey) && hairSprites.Length > 0)
        {
            currentHairIndex = PlayerPrefs.GetInt(HairIndexKey);
            hairRenderer.sprite = hairSprites[currentHairIndex];
        }

        if (PlayerPrefs.HasKey(FaceIndexKey) && faceSprites.Length > 0)
        {
            currentFaceIndex = PlayerPrefs.GetInt(FaceIndexKey);
            faceRenderer.sprite = faceSprites[currentFaceIndex];
        }

        if (PlayerPrefs.HasKey(ClothIndexKey) && clothColors.Length > 0)
        {
            currentClothIndex = PlayerPrefs.GetInt(ClothIndexKey);
            renderer.material.SetColor("_TargetColor", clothColors[currentClothIndex]);
        }

        if (PlayerPrefs.HasKey(HairColorKey))
        {
            string colorString = PlayerPrefs.GetString(HairColorKey);
            if (ColorUtility.TryParseHtmlString(colorString, out Color savedColor))
            {
                SetHairColor(savedColor);
            }
        }
    }

    public void RandomizeAppearance()
    {
        int randomHairIndex = Random.Range(0, HairCount);
        int randomFaceIndex = Random.Range(0, FaceCount);
        int randomClothIndex = Random.Range(0, ClothCount);
        Color randomHairColor = Random.ColorHSV();
        SetHair(randomHairIndex);
        SetFace(randomFaceIndex);
        SetCloth(randomClothIndex);
        SetHairColor(randomHairColor);
    }

    public void PlayAnimation(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        animator.Play(stateName, 0, 0f);
    }

    public void SetFlipX(bool flip)
    {
        // should flip by scale
        Vector3 localScale = transform.localScale;
        localScale.x = Mathf.Abs(localScale.x) * (flip ? -1 : 1);
        transform.localScale = localScale;
    }
}
