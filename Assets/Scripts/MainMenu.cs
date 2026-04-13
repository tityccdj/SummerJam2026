using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private UIMainMenu uiMainMenu;
    [SerializeField]
    private UISetting uiSetting;

    void Start()
    {
        AudioManager.Instance.PlayMusic("title", 0.8f, true);

        uiMainMenu.Setup(new UIMainMenu.Param
        {
            onPlay = () => SceneLoader.Instance.LoadScene("Game"),
            onSetting = () =>
            {
                uiMainMenu.gameObject.SetActive(false);
                uiSetting.gameObject.SetActive(true);
            },
            onExit = () => Application.Quit(),
        });
        uiSetting.Setup(new UISetting.Param
        {
            mainVolume = AudioManager.Instance.GetMasterVolume(),
            bgmVolume = AudioManager.Instance.GetMusicVolume(),
            sfxVolume = AudioManager.Instance.GetSFXVolume(),
            onMainVolumeChanged = value => AudioManager.Instance.SetMasterVolume(value),
            onBgmVolumeChanged = value => AudioManager.Instance.SetMusicVolume(value),
            onSfxVolumeChanged = value => AudioManager.Instance.SetSFXVolume(value),
            onBack = () =>
            {
                uiMainMenu.gameObject.SetActive(true);
                uiSetting.gameObject.SetActive(false);
            },
        });
        uiMainMenu.gameObject.SetActive(true);
        uiSetting.gameObject.SetActive(false);
    }
}
