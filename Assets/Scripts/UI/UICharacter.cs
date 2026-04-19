using System;
using UnityEngine;
using UnityEngine.UI;

public class UICharacter : MonoBehaviour
{
    public struct Param
    {
        public Action onPlay;
        public Action onBack;
        public Action onNextHair;
        public Action onPrevHair;
        public Action onNextCloth;
        public Action onPrevCloth;
        public Action<Color> onHairColorChange;
        public Color initialHairColor;
    }

    [SerializeField]
    private Button playBtn;
    [SerializeField]
    private Button backBtn;
    [SerializeField]
    private Button nextHairBtn;
    [SerializeField]
    private Button prevHairBtn;
    [SerializeField]
    private Button nextClothBtn;
    [SerializeField]
    private Button prevClothBtn;
    [SerializeField]
    private Button hairColorBtn;
    [SerializeField]
    private FlexibleColorPicker hairColor;

    public void Setup(Param param)
    {
        playBtn.onClick.AddListener(() => param.onPlay?.Invoke());
        backBtn.onClick.AddListener(() => param.onBack?.Invoke());
        nextHairBtn.onClick.AddListener(() => param.onNextHair?.Invoke());
        prevHairBtn.onClick.AddListener(() => param.onPrevHair?.Invoke());
        nextClothBtn.onClick.AddListener(() => param.onNextCloth?.Invoke());
        prevClothBtn.onClick.AddListener(() => param.onPrevCloth?.Invoke());
        hairColor.onColorChange.AddListener(color => param.onHairColorChange?.Invoke(color));
        hairColorBtn.onClick.AddListener(() => hairColor.gameObject.SetActive(!hairColor.gameObject.activeSelf));
        hairColor.SetColor(param.initialHairColor);
    }
}
