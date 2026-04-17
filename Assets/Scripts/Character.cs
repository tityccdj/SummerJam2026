using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer hairRenderer;
    [SerializeField]
    private Sprite[] hairSprites;
    [SerializeField]
    private Renderer renderer;
    [SerializeField]
    private Color[] clothColors;

    private int currentHairIndex = 0;
    public int CurrentHairIndex => currentHairIndex;
    private int currentClothIndex = 0;
    public int CurrentClothIndex => currentClothIndex;
    public int HairCount => hairSprites.Length;
    public int ClothCount => clothColors.Length;
    private const string HairIndexKey = "HairIndex";
    private const string ClothIndexKey = "ClothIndex";

    public void SetHair(int index)
    {
        if (index >= 0 && index < hairSprites.Length)
        {
            currentHairIndex = index;
            hairRenderer.sprite = hairSprites[currentHairIndex];
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

    public void SaveCharacterData()
    {
        PlayerPrefs.SetInt(HairIndexKey, currentHairIndex);
        PlayerPrefs.SetInt(ClothIndexKey, currentClothIndex);
        PlayerPrefs.Save();
    }

    public void LoadCharacterData()
    {
        if (PlayerPrefs.HasKey(HairIndexKey))
        {
            currentHairIndex = PlayerPrefs.GetInt(HairIndexKey);
            hairRenderer.sprite = hairSprites[currentHairIndex];
        }

        if (PlayerPrefs.HasKey(ClothIndexKey))
        {
            currentClothIndex = PlayerPrefs.GetInt(ClothIndexKey);
            renderer.material.SetColor("_TargetColor", clothColors[currentClothIndex]);
        }
    }

    public void RandomizeAppearance()
    {
        int randomHairIndex = Random.Range(0, HairCount);
        int randomClothIndex = Random.Range(0, ClothCount);
        SetHair(randomHairIndex);
        SetCloth(randomClothIndex);
    }
}
