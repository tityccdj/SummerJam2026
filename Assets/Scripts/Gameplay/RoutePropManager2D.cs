using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RoutePropManager2D : MonoBehaviour
{
    private sealed class PropInstance
    {
        public GameObject Root;
        public SpriteRenderer[] Renderers;
        public Color[] BaseColors;
        public Vector2 Position;
        public float CurrentAlpha = 1f;
        public bool Visible;

        public Bounds WorldBounds
        {
            get
            {
                if (Renderers == null || Renderers.Length == 0)
                {
                    Vector3 fallback = Root != null ? Root.transform.position : Vector3.zero;
                    return new Bounds(fallback, Vector3.one);
                }

                SpriteRenderer firstRenderer = null;
                for (int i = 0; i < Renderers.Length; i++)
                {
                    if (Renderers[i] != null)
                    {
                        firstRenderer = Renderers[i];
                        break;
                    }
                }

                if (firstRenderer == null)
                {
                    Vector3 fallback = Root != null ? Root.transform.position : Vector3.zero;
                    return new Bounds(fallback, Vector3.one);
                }

                Bounds bounds = firstRenderer.bounds;
                for (int i = 0; i < Renderers.Length; i++)
                {
                    if (Renderers[i] != null)
                    {
                        bounds.Encapsulate(Renderers[i].bounds);
                    }
                }

                return bounds;
            }
        }

        public void ApplyAlpha(float alpha)
        {
            CurrentAlpha = Mathf.Clamp01(alpha);

            if (Renderers == null)
            {
                return;
            }

            for (int i = 0; i < Renderers.Length; i++)
            {
                SpriteRenderer renderer = Renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Color color = BaseColors != null && i < BaseColors.Length ? BaseColors[i] : renderer.color;
                color.a = CurrentAlpha;
                renderer.color = color;
            }
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;

            if (Renderers == null)
            {
                return;
            }

            for (int i = 0; i < Renderers.Length; i++)
            {
                if (Renderers[i] != null)
                {
                    Renderers[i].enabled = visible;
                }
            }
        }
    }

    [Header("Prop Source")]
    [SerializeField] private string propResourcesPath = "prefabs/props";
    [SerializeField] private int minPropIndex = 1;
    [SerializeField] private int maxPropIndex = 6;

    [Header("Placement")]
    [SerializeField] private int targetPropCount = 24;
    [SerializeField] private float minSplineClearance = 0.95f;
    [SerializeField] private float maxSplineClearance = 2.55f;
    [SerializeField] private float minPropSpacing = 1.15f;
    [SerializeField] private float propWorldScale = 1f;

    [Header("Viewport")]
    [SerializeField] private float viewportPadding = 4f;
    [SerializeField] private float renderPrewarmPadding = 6f;

    [Header("Occlusion")]
    [SerializeField] private float blockedAlpha = 0.5f;
    [SerializeField] private float alphaBlendSpeed = 10f;
    [SerializeField] private int propSortingOrderOffset = 20;

    private readonly List<PropInstance> propInstances = new List<PropInstance>();
    private RoutesRenderer routesRenderer;
    private RouteData routeData;
    private Transform playerTarget;
    private Camera targetCamera;
    private bool isInitialized;

    public void Initialize(RoutesRenderer renderer, Transform playerTransform, Camera cameraTarget = null)
    {
        routesRenderer = renderer;
        routeData = routesRenderer != null ? routesRenderer.GetRouteData() : null;
        playerTarget = playerTransform;
        targetCamera = cameraTarget != null ? cameraTarget : Camera.main;

        BuildProps();
        isInitialized = true;
        RefreshPropStates(true);
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        RefreshPropStates(false);
    }

    private void BuildProps()
    {
        ClearProps();

        if (routeData == null || !routeData.IsValid())
        {
            return;
        }

        Vector3[] splinePoints = routeData.GetSplinePoints();
        if (splinePoints == null || splinePoints.Length < 2)
        {
            return;
        }

        float routeLength = EstimateRouteLength(splinePoints);
        int desiredCount = targetPropCount;
        if (routeLength > 0f)
        {
            desiredCount = Mathf.Clamp(Mathf.RoundToInt(routeLength / 6.5f), Mathf.Min(10, targetPropCount), Mathf.Max(targetPropCount, 28));
        }

        int seed = routeData.routeId * 10007 + routeData.points.Length * 97;
        System.Random random = new System.Random(seed);
        List<Vector2> occupiedPositions = new List<Vector2>();

        int maxAttempts = Mathf.Max(64, desiredCount * 28);
        float minClearanceSqr = minSplineClearance * minSplineClearance;
        int frontLoadedCount = Mathf.Max(4, desiredCount / 4);
        int placedCount = 0;

        for (int attempt = 0; attempt < maxAttempts && propInstances.Count < desiredCount; attempt++)
        {
            float progress;
            if (placedCount < frontLoadedCount)
            {
                float frontT = frontLoadedCount <= 1 ? 0f : placedCount / (float)(frontLoadedCount - 1);
                progress = Mathf.Lerp(0.02f, 0.16f, frontT);
            }
            else
            {
                float backT = Mathf.InverseLerp(frontLoadedCount, Mathf.Max(frontLoadedCount + 1, desiredCount - 1), placedCount);
                progress = Mathf.Lerp(0.16f, 0.98f, backT);
            }

            progress += Mathf.Lerp(-0.015f, 0.015f, (float)random.NextDouble());
            progress = Mathf.Repeat(progress, 1f);

            Vector2 center = routeData.EvaluatePosition(progress);
            Vector2 normal = routeData.EvaluateNormal(progress);
            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = Vector2.up;
            }

            float side = random.NextDouble() < 0.5 ? -1f : 1f;
            float clearance = Mathf.Lerp(minSplineClearance, maxSplineClearance, (float)random.NextDouble());
            Vector2 candidate = center + normal.normalized * (clearance * side);

            if (DistanceToSpline(candidate, splinePoints) < minClearanceSqr)
            {
                continue;
            }

            if (IsTooCloseToExisting(candidate, occupiedPositions))
            {
                continue;
            }

            GameObject propObject = CreatePropObject(random);
            if (propObject == null)
            {
                continue;
            }

            propObject.transform.SetParent(transform, true);
            propObject.transform.position = new Vector3(candidate.x, candidate.y, 0f);
            propObject.transform.localScale = Vector3.one * propWorldScale;

            SpriteRenderer[] renderers = propObject.GetComponentsInChildren<SpriteRenderer>(true);
            Color[] baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = renderers[i];
                if (spriteRenderer == null)
                {
                    continue;
                }

                baseColors[i] = spriteRenderer.color;
                spriteRenderer.sortingOrder += propSortingOrderOffset;
            }

            PropInstance instance = new PropInstance
            {
                Root = propObject,
                Renderers = renderers,
                BaseColors = baseColors,
                Position = candidate
            };

            instance.ApplyAlpha(1f);
            propInstances.Add(instance);
            occupiedPositions.Add(candidate);
            placedCount++;
        }
    }

    private GameObject CreatePropObject(System.Random random)
    {
        int minIndex = Mathf.Min(minPropIndex, maxPropIndex);
        int maxIndex = Mathf.Max(minPropIndex, maxPropIndex);
        int propIndex = random.Next(minIndex, maxIndex + 1);

        GameObject prefab = LoadPropPrefab(propIndex);
        if (prefab == null)
        {
            return null;
        }

        GameObject propObject = Instantiate(prefab);
        propObject.name = prefab.name;
        return propObject;
    }

    private GameObject LoadPropPrefab(int propIndex)
    {
        GameObject prefab = Resources.Load<GameObject>($"{propResourcesPath}/prop{propIndex}");
        if (prefab != null)
        {
            return prefab;
        }

        prefab = Resources.Load<GameObject>($"props/prop{propIndex}");
        if (prefab != null)
        {
            return prefab;
        }

        return Resources.Load<GameObject>($"prefabs/props/{propIndex}");
    }

    private void RefreshPropStates(bool forceImmediate)
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null || propInstances.Count == 0)
        {
            return;
        }

        Bounds viewportBounds = GetCameraBounds(targetCamera, Mathf.Max(viewportPadding, renderPrewarmPadding));
        Bounds playerBounds = GetPlayerBounds();
        bool playerHasBounds = playerBounds.size.sqrMagnitude > 0.001f;
        float alphaStep = forceImmediate ? 1f : Mathf.Clamp01(alphaBlendSpeed * Time.deltaTime);

        for (int i = 0; i < propInstances.Count; i++)
        {
            PropInstance prop = propInstances[i];
            if (prop == null || prop.Root == null)
            {
                continue;
            }

            Bounds propBounds = prop.WorldBounds;
            bool shouldRender = viewportBounds.size.sqrMagnitude <= 0.001f || viewportBounds.Intersects(propBounds);
            prop.SetVisible(true);

            float targetAlpha = 1f;
            if (playerHasBounds && shouldRender && playerBounds.Intersects(propBounds))
            {
                targetAlpha = blockedAlpha;
            }

            if (forceImmediate)
            {
                prop.ApplyAlpha(targetAlpha);
            }
            else
            {
                prop.ApplyAlpha(Mathf.MoveTowards(prop.CurrentAlpha, targetAlpha, alphaStep));
            }
        }
    }

    private Bounds GetPlayerBounds()
    {
        if (playerTarget == null)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        SpriteRenderer[] renderers = playerTarget.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return new Bounds(playerTarget.position, Vector3.one);
        }

        SpriteRenderer firstRenderer = null;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                firstRenderer = renderers[i];
                break;
            }
        }

        if (firstRenderer == null)
        {
            return new Bounds(playerTarget.position, Vector3.one);
        }

        Bounds bounds = firstRenderer.bounds;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return bounds;
    }

    private static Bounds GetCameraBounds(Camera cameraTarget, float padding)
    {
        if (cameraTarget == null)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Vector3 center = cameraTarget.transform.position;
        if (cameraTarget.orthographic)
        {
            float halfHeight = cameraTarget.orthographicSize + padding;
            float halfWidth = halfHeight * cameraTarget.aspect;
            return new Bounds(center, new Vector3(halfWidth * 2f, halfHeight * 2f, 10f));
        }

        Vector3 lowerLeft = cameraTarget.ViewportToWorldPoint(new Vector3(0f, 0f, cameraTarget.nearClipPlane));
        Vector3 upperRight = cameraTarget.ViewportToWorldPoint(new Vector3(1f, 1f, cameraTarget.nearClipPlane));
        Vector3 size = upperRight - lowerLeft;
        size.x += padding * 2f;
        size.y += padding * 2f;
        return new Bounds((lowerLeft + upperRight) * 0.5f, size);
    }

    private static float EstimateRouteLength(Vector3[] splinePoints)
    {
        if (splinePoints == null || splinePoints.Length < 2)
        {
            return 0f;
        }

        float length = 0f;
        for (int i = 1; i < splinePoints.Length; i++)
        {
            length += Vector3.Distance(splinePoints[i - 1], splinePoints[i]);
        }

        return length;
    }

    private static float DistanceToSpline(Vector2 point, Vector3[] splinePoints)
    {
        if (splinePoints == null || splinePoints.Length < 2)
        {
            return float.PositiveInfinity;
        }

        float bestDistanceSqr = float.PositiveInfinity;
        for (int i = 1; i < splinePoints.Length; i++)
        {
            Vector2 a = splinePoints[i - 1];
            Vector2 b = splinePoints[i];
            float distanceSqr = DistancePointToSegmentSqr(point, a, b);
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
            }
        }

        return bestDistanceSqr;
    }

    private static float DistancePointToSegmentSqr(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float abLengthSqr = ab.sqrMagnitude;
        if (abLengthSqr < 0.0001f)
        {
            return (point - a).sqrMagnitude;
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / abLengthSqr);
        Vector2 projection = a + ab * t;
        return (point - projection).sqrMagnitude;
    }

    private bool IsTooCloseToExisting(Vector2 candidate, List<Vector2> occupiedPositions)
    {
        float minSpacingSqr = minPropSpacing * minPropSpacing;
        for (int i = 0; i < occupiedPositions.Count; i++)
        {
            if ((occupiedPositions[i] - candidate).sqrMagnitude < minSpacingSqr)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearProps()
    {
        for (int i = 0; i < propInstances.Count; i++)
        {
            if (propInstances[i] != null && propInstances[i].Root != null)
            {
                Destroy(propInstances[i].Root);
            }
        }

        propInstances.Clear();
    }

    private void OnDestroy()
    {
        ClearProps();
    }
}
