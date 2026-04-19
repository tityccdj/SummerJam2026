using UnityEngine;

public class CharacterCustomization : MonoBehaviour
{
    [SerializeField]
    private Character character;
    [SerializeField]
    private UICharacter uiCharacter;

    private void Start()
    {
        character.LoadCharacterData();
        character.PlayAnimation("idle");
        uiCharacter.Setup(new UICharacter.Param
        {
            onPlay = OnPlay,
            onBack = OnBack,
            onNextHair = OnNextHair,
            onPrevHair = OnPrevHair,
            onNextCloth = OnNextCloth,
            onPrevCloth = OnPrevCloth,
            onHairColorChange = OnHairColorChange,
            initialHairColor = character.HairColor
        });
    }

    private void OnPlay()
    {
        character.SaveCharacterData();
        character.PlayAnimation("run");
        Utility.WaitForSeconds(0.5f, () =>
        {
            SceneLoader.Instance.LoadScene("Game");
        });
    }

    private void OnBack()
    {
        SceneLoader.Instance.LoadScene("Title");
    }

    private void OnPrevHair()
    {
        int newIndex = (character.CurrentHairIndex - 1 + character.HairCount) % character.HairCount;
        character.SetHair(newIndex);
    }

    private void OnNextHair()
    {
        int newIndex = (character.CurrentHairIndex + 1) % character.HairCount;
        character.SetHair(newIndex);
    }

    private void OnPrevCloth()
    {
        int newIndex = (character.CurrentClothIndex - 1 + character.ClothCount) % character.ClothCount;
        character.SetCloth(newIndex);
    }

    private void OnNextCloth()
    {
        int newIndex = (character.CurrentClothIndex + 1) % character.ClothCount;
        character.SetCloth(newIndex);
    }

    private void OnHairColorChange(Color color)
    {
        character.SetHairColor(color);
    }
}
