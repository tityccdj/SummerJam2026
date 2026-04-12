using UnityEngine;

[DisallowMultipleComponent]
public class RoutesRenderer : MonoBehaviour
{
    [Header("Route Source")]
    [SerializeField] private int routeId = 1;
    [SerializeField] private bool refreshOnAwake = true;

    [Header("Track Style")]
    [SerializeField] private float widthMultiplier = 1f;
    [SerializeField] private Color trackColor = new Color(0.16f, 0.16f, 0.18f, 1f);
    [SerializeField] private Color centerLineColor = new Color(0.95f, 0.84f, 0.26f, 1f);
    [SerializeField] private float centerLineWidth = 0.18f;
    [SerializeField] private int sortingOrder = 0;
    [SerializeField] private int centerLineSortingOrder = 1;

    private LineRenderer trackRenderer;
    private LineRenderer centerLineRenderer;
    private Material sharedLineMaterial;

    public int RouteId
    {
        get => routeId;
        set => routeId = Mathf.Max(1, value);
    }

    public RouteData GetRouteData()
    {
        return RouteDatabase.Instance.GetRoute(routeId);
    }

    private void Awake()
    {
        if (refreshOnAwake)
        {
            Refresh();
        }
    }

    [ContextMenu("Refresh Route")]
    public void Refresh()
    {
        RouteData route = RouteDatabase.Instance.GetRoute(routeId);
        if (route == null)
        {
            return;
        }

        EnsureRenderers();

        Vector3[] splinePoints = route.GetSplinePoints();
        if (splinePoints.Length == 0)
        {
            return;
        }

        ApplyToRenderer(trackRenderer, splinePoints, route.trackWidth * widthMultiplier, trackColor, sortingOrder, route.closed);
        ApplyToRenderer(centerLineRenderer, splinePoints, centerLineWidth, centerLineColor, centerLineSortingOrder, route.closed);
    }

    private void EnsureRenderers()
    {
        if (sharedLineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            sharedLineMaterial = new Material(shader)
            {
                name = "RouteLineMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        if (trackRenderer == null)
        {
            trackRenderer = GetOrCreateRenderer("Track");
        }

        if (centerLineRenderer == null)
        {
            centerLineRenderer = GetOrCreateRenderer("CenterLine");
        }
    }

    private LineRenderer GetOrCreateRenderer(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(transform, false);
            child = childObject.transform;
        }

        LineRenderer renderer = child.GetComponent<LineRenderer>();
        if (renderer == null)
        {
            renderer = child.gameObject.AddComponent<LineRenderer>();
        }

        renderer.material = sharedLineMaterial;
        renderer.textureMode = LineTextureMode.Stretch;
        renderer.alignment = LineAlignment.TransformZ;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.generateLightingData = false;
        renderer.numCapVertices = 6;
        renderer.numCornerVertices = 4;
        renderer.useWorldSpace = false;

        return renderer;
    }

    private void ApplyToRenderer(LineRenderer renderer, Vector3[] points, float width, Color color, int rendererSortingOrder, bool loop)
    {
        renderer.loop = loop;
        renderer.widthMultiplier = width;
        renderer.positionCount = points.Length;
        renderer.startColor = color;
        renderer.endColor = color;
        renderer.sortingOrder = rendererSortingOrder;
        renderer.SetPositions(points);
    }

    private void OnDestroy()
    {
        if (sharedLineMaterial != null)
        {
            Destroy(sharedLineMaterial);
        }
    }
}
