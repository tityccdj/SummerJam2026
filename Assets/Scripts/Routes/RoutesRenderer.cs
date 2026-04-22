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
    [SerializeField] private float startLineWidth = 0.28f;
    [SerializeField] private int sortingOrder = 0;
    [SerializeField] private int centerLineSortingOrder = 1;
    [SerializeField] private int startLineSortingOrder = 2;

    // 🌟 ส่วนที่เพิ่มใหม่สำหรับกองเชียร์
    [Header("Props (Cheerleaders)")]
    [SerializeField] private string cheerPrefabPath = "prefabs/cheer";
    [SerializeField] private float distanceOutsideTrack = 2.0f; // ระยะห่างจากขอบถนน
    private List<GameObject> spawnedCheerleaders = new List<GameObject>();

    private LineRenderer trackRenderer;
    private LineRenderer centerLineRenderer;
    private LineRenderer startLineRenderer;
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
        ApplyStartLine(route);

        // 🌟 เรียกใช้ฟังก์ชันสร้างกองเชียร์
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

        if (startLineRenderer == null)
        {
            startLineRenderer = GetOrCreateRenderer("StartLine");
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

    private void ApplyStartLine(RouteData route)
    {
        if (startLineRenderer == null || route == null || !route.IsValid())
        {
            return;
        }

        Vector2 center = route.EvaluatePosition(0f);
        Vector2 normal = route.EvaluateNormal(0f).normalized;
        float halfWidth = route.trackWidth * widthMultiplier * 0.5f;

        Vector3[] points =
        {
            new Vector3(center.x - normal.x * halfWidth, center.y - normal.y * halfWidth, 0f),
            new Vector3(center.x + normal.x * halfWidth, center.y + normal.y * halfWidth, 0f)
        };

        ApplyToRenderer(startLineRenderer, points, startLineWidth, startLineColor, startLineSortingOrder, false);
    }

    // 🌟 ฟังก์ชันสร้างกองเชียร์ข้างสนาม
    private void SpawnCheerleaders(RouteData route)
    {
        // ลบของเก่าทิ้งก่อน (ถ้ามีการกดยืนยัน Refresh ด่านใหม่)
        foreach (var obj in spawnedCheerleaders)
        {
            if (obj != null) DestroyImmediate(obj); // ใช้ DestroyImmediate เพื่อให้ลบในโหมด Edit ได้
        }
        spawnedCheerleaders.Clear();

        if (route == null || !route.IsValid()) return;

        // โหลด Prefab ตามชื่อที่ตั้งไว้
        GameObject cheerPrefab = Resources.Load<GameObject>(cheerPrefabPath);
        if (cheerPrefab == null)
        {
            Debug.LogWarning($"[RoutesRenderer] หา Prefab กองเชียร์ไม่เจอใน Resources! (ตรวจสอบโฟลเดอร์ {cheerPrefabPath})");
            return;
        }

        // หาจุดกึ่งกลางและทิศทางของเส้นชัย (ที่จุด progress 0f)
        Vector2 center = route.EvaluatePosition(0f);
        Vector2 normal = route.EvaluateNormal(0f).normalized;

        // คำนวณความกว้างครึ่งหนึ่งของถนน + ระยะห่างออกมานอกถนน
        float spawnDistance = (route.trackWidth * widthMultiplier * 0.5f) + distanceOutsideTrack;

        // จุดเกิดด้านซ้ายและขวา
        Vector3 leftPos = new Vector3(center.x - normal.x * spawnDistance, center.y - normal.y * spawnDistance, 0f);
        Vector3 rightPos = new Vector3(center.x + normal.x * spawnDistance, center.y + normal.y * spawnDistance, 0f);

        // สร้างกองเชียร์ฝั่งซ้าย
        GameObject cheerLeft = Instantiate(cheerPrefab, leftPos, Quaternion.identity, transform);
        cheerLeft.name = "Cheerleaders_Left";

        // สร้างกองเชียร์ฝั่งขวา
        GameObject cheerRight = Instantiate(cheerPrefab, rightPos, Quaternion.identity, transform);
        cheerRight.name = "Cheerleaders_Right";

        // (เผื่อไว้) ถ้าอยากให้ฝั่งซ้ายหันหน้าเข้าหาถนน สลับ FlipX ได้
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