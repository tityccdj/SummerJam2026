using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneHelper : MonoBehaviour
{
    [Header("Route Preview")]
    [SerializeField] private bool createRouteRendererOnStart = true;
    [SerializeField] private bool randomizeRouteOnStart = true;
    [SerializeField] private int fallbackRouteId = 1;
    [SerializeField] private string rendererObjectName = "RoutesRenderer";

    [Header("Gameplay Bootstrap")]
    [SerializeField] private bool bootstrapPlayerSystems = true;
    [SerializeField] private bool bootstrapRouteProps = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string overHeatSliderName = "Slider (OverHeat)";
    [SerializeField] private int totalLapCount = 3;
    [SerializeField] private int cpuRunnerCount = 4;
    [SerializeField] private Canvas canvas;
 

    private void Start()
    {
        AudioManager.Instance?.StopMusic(true);
        StartCoroutine(StartBgm());

        RoutesRenderer routesRenderer = null;

        if (createRouteRendererOnStart)
        {
            routesRenderer = SetupRoutesRenderer();
        }

        if (!bootstrapPlayerSystems)
        {
            if (bootstrapRouteProps)
            {
                SetupRouteProps(routesRenderer != null ? routesRenderer : FindFirstObjectByType<RoutesRenderer>(), null);
            }

            return;
        }

        SetupPlayerSystems(routesRenderer != null ? routesRenderer : FindFirstObjectByType<RoutesRenderer>());
    }

    private RoutesRenderer SetupRoutesRenderer()
    {
        RouteDatabase.Instance.LoadAllRoutes();

        RoutesRenderer routesRenderer = FindFirstObjectByType<RoutesRenderer>();
        if (routesRenderer == null)
        {
            GameObject rendererObject = new GameObject(rendererObjectName);
            routesRenderer = rendererObject.AddComponent<RoutesRenderer>();
        }

        routesRenderer.RouteId = GetRouteIdForSceneLoad();
        routesRenderer.Refresh();
        return routesRenderer;
    }

    private void SetupPlayerSystems(RoutesRenderer routesRenderer)
    {
        if (routesRenderer == null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        PlayerSplineRunner playerRunner = null;
        if (playerObject != null)
        {
            playerRunner = playerObject.GetComponent<PlayerSplineRunner>();
            if (playerRunner == null)
            {
                playerRunner = playerObject.AddComponent<PlayerSplineRunner>();
            }
        }

        Joystick joystick = FindFirstObjectByType<Joystick>();
        if (joystick != null)
        {
            joystick.AxisOptions = AxisOptions.Both;
            joystick.SnapX = false;
            joystick.SnapY = false;
        }

        Slider slider = FindOrCreateOverHeatSlider();

        Image itemUI = FindExistingItemUI();
        if (playerRunner != null)
        {
            playerRunner.ConfigureRunner("Player", true, routesRenderer, joystick, slider, totalLapCount);
            playerRunner.ResetRunnerState(0f, 0f);
            playerRunner.SetItemUI(itemUI);
        }
        

        RaceManager2D raceManager = SetupRaceManager(playerRunner, routesRenderer);
        PlayerSplineRunner activeHumanRunner = raceManager != null && raceManager.HumanRunner != null
            ? raceManager.HumanRunner
            : playerRunner;

        if (activeHumanRunner != null && activeHumanRunner != playerRunner)
        {
            activeHumanRunner.ConfigureRunner("Player", true, routesRenderer, joystick, slider, totalLapCount);
            activeHumanRunner.ResetRunnerState(0f, 0f);
            activeHumanRunner.SetItemUI(itemUI);
        }

        BindButtons(activeHumanRunner);

        Transform cameraTarget = raceManager != null && raceManager.HumanRunner != null
            ? raceManager.HumanRunner.transform
            : playerObject != null ? playerObject.transform : null;

        if (cameraTarget != null)
        {
            BindCamera(cameraTarget);
        }

        if (bootstrapRouteProps)
        {
            SetupRouteProps(routesRenderer, activeHumanRunner != null ? activeHumanRunner.transform : cameraTarget);
        }
    }

    private void BindButtons(PlayerSplineRunner playerRunner)
    {
        if (playerRunner == null)
        {
            return;
        }

        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            string buttonName = button.gameObject.name.ToLowerInvariant();

            if (buttonName.Contains("pump") || buttonName.Contains("run"))
            {
                button.onClick.AddListener(playerRunner.PlayerRun);
            }
            else if (buttonName.Contains("cool"))
            {
                button.onClick.AddListener(playerRunner.CoolDown);
            }
            else if (buttonName.Contains("item"))
            {
                button.onClick.AddListener(playerRunner.UseItem);
            }
        }
    }

    private RaceManager2D SetupRaceManager(PlayerSplineRunner playerRunner, RoutesRenderer routesRenderer)
    {
        if (routesRenderer == null)
        {
            return null;
        }

        RaceManager2D raceManager = FindFirstObjectByType<RaceManager2D>();
        if (raceManager == null)
        {
            GameObject raceManagerObject = new GameObject("RaceManager2D");
            raceManager = raceManagerObject.AddComponent<RaceManager2D>();
        }

        raceManager.ConfigureRace(cpuRunnerCount, totalLapCount);
        raceManager.SetupRace(playerRunner, routesRenderer, canvas);
        return raceManager;
    }

    private void BindCamera(Transform playerTransform)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        CameraFollow2D cameraFollow = mainCamera.GetComponent<CameraFollow2D>();
        if (cameraFollow == null)
        {
            cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow2D>();
        }

        cameraFollow.SetTarget(playerTransform);
    }

    private void SetupRouteProps(RoutesRenderer routesRenderer, Transform playerTarget)
    {
        if (routesRenderer == null)
        {
            return;
        }

        RoutePropManager2D propManager = FindFirstObjectByType<RoutePropManager2D>();
        if (propManager == null)
        {
            GameObject propObject = new GameObject("RoutePropManager2D");
            propManager = propObject.AddComponent<RoutePropManager2D>();
        }

        propManager.Initialize(routesRenderer, playerTarget, Camera.main);
    }

    private Slider FindOrCreateOverHeatSlider()
    {
        Slider slider = FindFirstObjectByType<Slider>();
        if (slider != null)
        {
            return slider;
        }

        if (canvas == null)
        {
            return null;
        }

        GameObject sliderObject = new GameObject(overHeatSliderName, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(canvas.transform, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 1f);
        sliderRect.anchorMax = new Vector2(0.5f, 1f);
        sliderRect.pivot = new Vector2(0.5f, 1f);
        sliderRect.anchoredPosition = new Vector2(0f, -28f);
        sliderRect.sizeDelta = new Vector2(320f, 28f);

        GameObject background = CreateSliderGraphic("Background", sliderObject.transform, new Color(0.15f, 0.15f, 0.18f, 0.9f));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        GameObject fill = CreateSliderGraphic("Fill", fillArea.transform, new Color(1f, 0.35f, 0.2f, 0.95f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Slider createdSlider = sliderObject.GetComponent<Slider>();
        createdSlider.direction = Slider.Direction.LeftToRight;
        createdSlider.fillRect = fillRect;
        createdSlider.targetGraphic = fill.GetComponent<Image>();
        createdSlider.minValue = 0f;
        createdSlider.maxValue = 100f;
        createdSlider.value = 0f;

        return createdSlider;
    }

    private static GameObject CreateSliderGraphic(string objectName, Transform parent, Color color)
    {
        GameObject graphicObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        graphicObject.transform.SetParent(parent, false);

        Image image = graphicObject.GetComponent<Image>();
        image.color = color;

        RectTransform rectTransform = graphicObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        return graphicObject;
    }

    private int GetRouteIdForSceneLoad()
    {
        IReadOnlyCollection<int> availableRouteIds = RouteDatabase.Instance.GetAvailableRouteIds();
        if (availableRouteIds == null || availableRouteIds.Count == 0)
        {
            return fallbackRouteId;
        }

        if (!randomizeRouteOnStart)
        {
            return RouteDatabase.Instance.HasRoute(fallbackRouteId) ? fallbackRouteId : GetFirstRouteId(availableRouteIds);
        }

        int selectedIndex = Random.Range(0, availableRouteIds.Count);
        int currentIndex = 0;

        foreach (int routeId in availableRouteIds)
        {
            if (currentIndex == selectedIndex)
            {
                return routeId;
            }

            currentIndex++;
        }

        return GetFirstRouteId(availableRouteIds);
    }

    private static int GetFirstRouteId(IReadOnlyCollection<int> availableRouteIds)
    {
        foreach (int routeId in availableRouteIds)
        {
            return routeId;
        }

        return 1;
    }
    private Image FindExistingItemUI()
    {
        // 1. ���һ���������㹨�
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            // 2. �һ�������դ���� "item" (��觨еç�Ѻ Button (Item) �ͧ�س)
            if (button.gameObject.name.ToLowerInvariant().Contains("item"))
            {
                // 3. ������ Object �١������ "Image" ����ç���ҧ� Hierarchy �ͧ�س
                Transform targetImageObj = button.transform.Find("imgitem");

                if (targetImageObj != null)
                {
                    Image iconImage = targetImageObj.GetComponent<Image>();
                    iconImage.enabled = false; // ��͹�ٻ����͹�͹�������
                    return iconImage;
                }
            }
        }

        Debug.LogWarning("�Ҫ�ͧ����ٻ���������! ��س�����һ������� Button (Item) ���١���� Image �������");
        return null;
    }

    IEnumerator StartBgm()
    {
        yield return new WaitForSeconds(3f);
        AudioManager.Instance?.PlayMusic("bgm", 0.25f, true);
    }

}
