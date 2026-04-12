using System.Collections.Generic;
using UnityEngine;

public class RouteDatabase : Singleton<RouteDatabase>
{
    private const string ResourcesFolder = "routes";

    private readonly Dictionary<int, RouteData> routesById = new Dictionary<int, RouteData>();
    private bool isInitialized;

    protected override void Awake()
    {
        base.Awake();

        if (!isInitialized)
        {
            LoadAllRoutes();
        }
    }

    public void LoadAllRoutes()
    {
        routesById.Clear();

        TextAsset[] routeAssets = Resources.LoadAll<TextAsset>(ResourcesFolder);
        foreach (TextAsset routeAsset in routeAssets)
        {
            RouteData route = JsonUtility.FromJson<RouteData>(routeAsset.text);
            if (route == null || !route.IsValid())
            {
                Debug.LogWarning($"[RouteDatabase] Route asset '{routeAsset.name}' is invalid.");
                continue;
            }

            routesById[route.routeId] = route;
        }

        isInitialized = true;
    }

    public RouteData GetRoute(int routeId)
    {
        if (!isInitialized)
        {
            LoadAllRoutes();
        }

        if (routesById.TryGetValue(routeId, out RouteData route))
        {
            return route;
        }

        Debug.LogWarning($"[RouteDatabase] Route id '{routeId}' was not found.");
        return null;
    }

    public bool HasRoute(int routeId)
    {
        if (!isInitialized)
        {
            LoadAllRoutes();
        }

        return routesById.ContainsKey(routeId);
    }

    public IReadOnlyCollection<int> GetAvailableRouteIds()
    {
        if (!isInitialized)
        {
            LoadAllRoutes();
        }

        return routesById.Keys;
    }
}
