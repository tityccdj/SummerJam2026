using System;
using UnityEngine;
using UnityEngine.UI;

public class UIMainMenu : MonoBehaviour
{
    public struct Param
    {
        public Action onPlay;
        public Action onSetting;
        public Action onExit;
    }

    [SerializeField]
    private Button playButton;
    [SerializeField]
    private Button settingButton;
    [SerializeField]
    private Button exitButton;

    void Start()
    {
        if (Utility.IsWebGL())
        {
            exitButton.gameObject.SetActive(false);
        }
    }

    public void Setup(Param param)
    {
        playButton.onClick.AddListener(() => param.onPlay?.Invoke());
        settingButton.onClick.AddListener(() => param.onSetting?.Invoke());
        exitButton.onClick.AddListener(() => param.onExit?.Invoke());
    }
}
