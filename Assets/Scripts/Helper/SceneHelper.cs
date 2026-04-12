using UnityEngine;
using System.Collections.Generic;

public class SceneHelper : MonoBehaviour
{
    [Header("Route Preview")]
    [SerializeField] private bool createRouteRendererOnStart = true;
    [SerializeField] private bool randomizeRouteOnStart = true;
    [SerializeField] private int fallbackRouteId = 1;
    [SerializeField] private string rendererObjectName = "RoutesRenderer";

    private void Start()
    {
        if (!createRouteRendererOnStart)
        {
            return;
        }

        RouteDatabase.Instance.LoadAllRoutes();

        RoutesRenderer routesRenderer = FindFirstObjectByType<RoutesRenderer>();
        if (routesRenderer == null)
        {
            GameObject rendererObject = new GameObject(rendererObjectName);
            routesRenderer = rendererObject.AddComponent<RoutesRenderer>();
        }

        routesRenderer.RouteId = GetRouteIdForSceneLoad();
        routesRenderer.Refresh();
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
}
