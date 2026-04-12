using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private UIMainMenu uiMainMenu;

    void Start()
    {
        uiMainMenu.Setup(new UIMainMenu.Param
        {
            onPlay = () => SceneLoader.Instance.LoadScene("Game"),
            onSetting = () => Debug.Log("Setting"),
            onExit = () => Application.Quit(),
        });
    }
}
