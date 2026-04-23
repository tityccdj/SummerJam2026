using System.Collections.Generic;
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
    [SerializeField] private Color startLineColor = Color.white;
    [SerializeField] private float centerLineWidth = 0.18f;
    [SerializeField] private float startLineWidth = 1f; // 🌟 ปรับความหนาของรูปเส้นชัยได้ตรงนี้
    [SerializeField] private int sortingOrder = 0;
    [SerializeField] private int centerLineSortingOrder = 1;
    [SerializeField] private int startLineSortingOrder = 2;

    [Header("Props (Cheerleaders)")]
    [SerializeField] private string cheerPrefabPath = "prefabs/cheer";
    [SerializeField] private float distanceOutsideTrack = 2.0f;
    private List<GameObject> spawnedCheerleaders = new List<GameObject>();

    private LineRenderer trackRenderer;
    private LineRenderer centerLineRenderer;

    // 🌟 เปลี่ยนตัวแปร StartLine จาก LineRenderer เป็น SpriteRenderer
    private SpriteRenderer startLineRenderer;
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

        // 🌟 เรียกฟังก์ชันสร้างเส้นชัยที่เป็นรูปภาพ
        ApplyStartLine(route);

        SpawnCheerleaders(route);
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

        // 🌟 แยกการสร้าง StartLine ออกมา เพราะมันเป็น SpriteRenderer
        if (startLineRenderer == null)
        {
            Transform child = transform.Find("StartLine");
            if (child == null)
            {
                GameObject childObject = new GameObject("StartLine");
                childObject.transform.SetParent(transform, false);
                child = childObject.transform;
            }

            startLineRenderer = child.GetComponent<SpriteRenderer>();
            if (startLineRenderer == null)
            {
                startLineRenderer = child.gameObject.AddComponent<SpriteRenderer>();
            }
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

    // 🌟 เปลี่ยนโค้ดตรงนี้เพื่อรองรับรูปภาพ (Sprite)
    private void ApplyStartLine(RouteData route)
    {
        if (startLineRenderer == null || route == null || !route.IsValid())
        {
            return;
        }

        // 1. หาจุดกึ่งกลางและทิศทางขวางถนน (Normal)
        Vector2 center = route.EvaluatePosition(0f);
        Vector2 normal = route.EvaluateNormal(0f).normalized;
        float trackFullWidth = route.trackWidth * widthMultiplier;

        // 2. จัดตำแหน่งไปวางไว้กลางถนน
        startLineRenderer.transform.position = new Vector3(center.x, center.y, 0f);

        // 3. โหลดภาพจาก Resources/Line
        Sprite lineSprite = Resources.Load<Sprite>("Line");
        if (lineSprite != null)
        {
            startLineRenderer.sprite = lineSprite;
        }
        else
        {
            Debug.LogWarning("[RoutesRenderer] ไม่พบภาพ 'Line' ในโฟลเดอร์ Resources!");
        }

        // 4. ใส่สีและตั้งค่าการทับซ้อน (Sorting Order)
        startLineRenderer.color = startLineColor;
        startLineRenderer.sortingOrder = startLineSortingOrder;

        // 5. หมุนภาพให้ขวางถนนพอดี
        if (normal.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
            startLineRenderer.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // 6. ยืดภาพให้ยาวเท่ากับความกว้างถนน
        if (lineSprite != null && lineSprite.bounds.size.x > 0)
        {
            float currentImageWidth = lineSprite.bounds.size.x;
            float scaleX = trackFullWidth / currentImageWidth;
            // scaleX คือความกว้างให้พอดีถนน, startLineWidth คือความหนาของเส้นชัย
            startLineRenderer.transform.localScale = new Vector3(scaleX, startLineWidth, 1f);
        }
    }

    private void SpawnCheerleaders(RouteData route)
    {
        foreach (var obj in spawnedCheerleaders)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        spawnedCheerleaders.Clear();

        if (route == null || !route.IsValid()) return;

        GameObject cheerPrefab = Resources.Load<GameObject>(cheerPrefabPath);
        if (cheerPrefab == null)
        {
            Debug.LogWarning($"[RoutesRenderer] หา Prefab กองเชียร์ไม่เจอใน Resources! (ตรวจสอบโฟลเดอร์ {cheerPrefabPath})");
            return;
        }

        Vector2 center = route.EvaluatePosition(0f);
        Vector2 normal = route.EvaluateNormal(0f).normalized;

        float spawnDistance = (route.trackWidth * widthMultiplier * 0.5f) + distanceOutsideTrack;

        Vector3 leftPos = new Vector3(center.x - normal.x * spawnDistance, center.y - normal.y * spawnDistance, 0f);
        Vector3 rightPos = new Vector3(center.x + normal.x * spawnDistance, center.y + normal.y * spawnDistance, 0f);

        GameObject cheerLeft = Instantiate(cheerPrefab, leftPos, Quaternion.identity, transform);
        cheerLeft.name = "Cheerleaders_Left";

        GameObject cheerRight = Instantiate(cheerPrefab, rightPos, Quaternion.identity, transform);
        cheerRight.name = "Cheerleaders_Right";

        SpriteRenderer srLeft = cheerLeft.GetComponentInChildren<SpriteRenderer>();
        if (srLeft != null) srLeft.flipX = false;

        SpriteRenderer srRight = cheerRight.GetComponentInChildren<SpriteRenderer>();
        if (srRight != null) srRight.flipX = true;

        spawnedCheerleaders.Add(cheerLeft);
        spawnedCheerleaders.Add(cheerRight);
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