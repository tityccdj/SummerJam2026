using System;
using UnityEngine;
using UnityEngine.UI;

public class UIMainMenu : MonoBehaviour
{
    public struct Param
    {
        public Action onPlay;
        public Action onSetting;
        public Action onTutorial;
        public Action onExit;
    }

    [SerializeField]
    private Button playButton;
    [SerializeField]
    private Button settingButton;
    [SerializeField]
    private Button tutorialButton;

    public void Setup(Param param)
    {
        playButton.onClick.AddListener(() => param.onPlay?.Invoke());
        settingButton.onClick.AddListener(() => param.onSetting?.Invoke());
        tutorialButton.onClick.AddListener(() => param.onTutorial?.Invoke());
    }
}
