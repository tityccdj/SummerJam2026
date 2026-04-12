using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RaceManager2D : MonoBehaviour
{
    [Header("Race")]
    [SerializeField] private int cpuRunnerCount = 4;
    [SerializeField] private int lapCount = 3;
    [SerializeField] private float startProgressSpacing = 0.018f;
    [SerializeField] private float laneSpacing = 0.42f;
    [SerializeField] private string characterPrefabResourcesPath = "prefabs/character";
    [SerializeField] private float hazardLifetime = 7f;
    [SerializeField] private float hazardRadius = 0.75f;
    [SerializeField] private float hazardHeatAmount = 20f;
    [SerializeField] private float hazardHeatMultiplier = 1.5f;
    [SerializeField] private float hazardHeatDuration = 2.2f;
    [SerializeField] private float rearHitDistance = 0.9f;
    [SerializeField] private float rearHitBoostAmount = 0.11f;
    [SerializeField] private float rearHitBoostDuration = 2f;
    [SerializeField] private float rearHitHeatMultiplier = 1.55f;
    [SerializeField] private float rearHitHeatAmount = 10f;

    [Header("UI")]
    [SerializeField] private string rankTextName = "Text (Rank)";
    [SerializeField] private string lapTextName = "Text (Lap)";
    [SerializeField] private string runnerNameTextPrefix = "Text (Runner)";

    private readonly List<PlayerSplineRunner> runners = new List<PlayerSplineRunner>();
    private readonly List<RaceHazard2D> activeHazards = new List<RaceHazard2D>();
    private TextMeshProUGUI rankText;
    private TextMeshProUGUI lapText;
    private PlayerSplineRunner humanRunner;
    private Canvas uiCanvas;

    public PlayerSplineRunner HumanRunner => humanRunner;

    public void ConfigureRace(int totalCpuRunners, int totalLaps)
    {
        cpuRunnerCount = Mathf.Max(0, totalCpuRunners);
        lapCount = Mathf.Max(1, totalLaps);
    }

    public void SetupRace(PlayerSplineRunner playerRunner, RoutesRenderer routesRenderer, Canvas canvas)
    {
        if (routesRenderer == null)
        {
            return;
        }

        runners.Clear();
        activeHazards.Clear();
        uiCanvas = canvas != null ? canvas : FindFirstObjectByType<Canvas>();

        RouteData route = routesRenderer.GetRouteData();
        float trackWidth = route != null ? route.trackWidth : 2.5f;
        float usableLaneSpacing = Mathf.Clamp(laneSpacing, 0.18f, trackWidth * 0.25f);
        EnsureUi(uiCanvas);

        humanRunner = CreateOrUsePlayerRunner(playerRunner, routesRenderer);
        if (humanRunner != null)
        {
            humanRunner.ConfigureRunner("Player", true, routesRenderer, FindFirstObjectByType<Joystick>(), FindFirstObjectByType<Slider>(), lapCount);
            humanRunner.ResetRunnerState(0f, 0f);
            runners.Add(humanRunner);
            CreateRunnerNameTag(humanRunner, "Player");
        }

        for (int i = 1; i <= cpuRunnerCount; i++)
        {
            PlayerSplineRunner cpuRunner = CreateCpuRunner(i, routesRenderer, usableLaneSpacing);
            if (cpuRunner != null)
            {
                MatchRunnerVisualScale(cpuRunner, humanRunner);
                runners.Add(cpuRunner);
                CreateRunnerNameTag(cpuRunner, $"CPU {i}");
            }
        }

        UpdateUi();
    }

    private void Update()
    {
        if (humanRunner == null || runners.Count == 0)
        {
            return;
        }

        UpdateHazardDrops();
        UpdateHazardHits();
        UpdateRearHits();
        UpdateUi();
    }

    private PlayerSplineRunner CreateOrUsePlayerRunner(PlayerSplineRunner existingRunner, RoutesRenderer routesRenderer)
    {
        GameObject playerPrefab = LoadCharacterPrefab(0);
        PlayerSplineRunner resolvedRunner = null;

        if (playerPrefab != null)
        {
            GameObject playerObject = Instantiate(playerPrefab, transform);
            playerObject.name = "Player";
            playerObject.tag = "Player";
            resolvedRunner = playerObject.GetComponent<PlayerSplineRunner>();
            if (resolvedRunner == null)
            {
                resolvedRunner = playerObject.AddComponent<PlayerSplineRunner>();
            }

            if (existingRunner != null && existingRunner.gameObject != playerObject)
            {
                existingRunner.gameObject.SetActive(false);
            }
        }
        else
        {
            resolvedRunner = existingRunner;
        }

        if (resolvedRunner == null)
        {
            Debug.LogWarning("[RaceManager2D] Player prefab 'Resources/prefabs/character/0' was not found and there is no fallback player in the scene.");
        }

        return resolvedRunner;
    }

    private PlayerSplineRunner CreateCpuRunner(int prefabIndex, RoutesRenderer routesRenderer, float usableLaneSpacing)
    {
        GameObject cpuPrefab = LoadCharacterPrefab(prefabIndex);
        GameObject cpuObject;

        if (cpuPrefab != null)
        {
            cpuObject = Instantiate(cpuPrefab, transform);
            cpuObject.name = $"CPU Runner {prefabIndex}";
        }
        else
        {
            cpuObject = new GameObject($"CPU Runner {prefabIndex}");
            cpuObject.transform.SetParent(transform, false);

            SpriteRenderer cpuSprite = cpuObject.AddComponent<SpriteRenderer>();
            cpuSprite.sprite = GetSquareSprite();
            cpuSprite.color = GetCpuColor(prefabIndex - 1);
            cpuSprite.sortingOrder = 5 + prefabIndex;
            cpuObject.transform.localScale = new Vector3(0.48f, 0.48f, 1f);
        }

        PlayerSplineRunner cpuRunner = cpuObject.GetComponent<PlayerSplineRunner>();
        if (cpuRunner == null)
        {
            cpuRunner = cpuObject.AddComponent<PlayerSplineRunner>();
        }

        float startProgress = Mathf.Repeat(1f - (prefabIndex * startProgressSpacing), 1f);
        float lateralOffset = GetCpuLaneOffset(prefabIndex - 1, usableLaneSpacing);

        cpuRunner.ConfigureRunner($"CPU {prefabIndex}", false, routesRenderer, null, null, lapCount);
        cpuRunner.ResetRunnerState(startProgress, lateralOffset);
        return cpuRunner;
    }

    private void EnsureUi(Canvas canvas)
    {
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            return;
        }

        rankText = FindOrCreateText(canvas.transform, rankTextName, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), TextAlignmentOptions.TopRight);
        lapText = FindOrCreateText(canvas.transform, lapTextName, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), TextAlignmentOptions.TopLeft);
    }

    private void CreateRunnerNameTag(PlayerSplineRunner runner, string displayName)
    {
        if (runner == null || uiCanvas == null)
        {
            return;
        }

        string objectName = $"{runnerNameTextPrefix} {displayName}";
        Transform existing = uiCanvas.transform.Find(objectName);
        RunnerNameTag nameTag = existing != null ? existing.GetComponent<RunnerNameTag>() : null;

        if (nameTag == null)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(RunnerNameTag));
            textObject.transform.SetParent(uiCanvas.transform, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;

            RectTransform rectTransform = text.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160f, 28f);

            nameTag = textObject.GetComponent<RunnerNameTag>();
        }

        nameTag.Bind(runner.transform, displayName, Camera.main);
    }

    private void UpdateUi()
    {
        List<PlayerSplineRunner> orderedRunners = runners
            .Where(runner => runner != null)
            .OrderByDescending(runner => runner.IsFinished)
            .ThenBy(runner => runner.IsFinished ? runner.FinishTime : float.PositiveInfinity)
            .ThenByDescending(runner => runner.RaceProgress)
            .ToList();

        int humanRank = Mathf.Max(1, orderedRunners.IndexOf(humanRunner) + 1);

        if (rankText != null)
        {
            rankText.text = $"Rank {humanRank}/{orderedRunners.Count}";
        }

        if (lapText != null && humanRunner != null)
        {
            int shownLap = Mathf.Min(humanRunner.CompletedLaps + 1, lapCount);
            if (humanRunner.IsFinished)
            {
                shownLap = lapCount;
            }

            lapText.text = $"Lap {shownLap}/{lapCount}";
        }
    }

    private void UpdateHazardDrops()
    {
        foreach (PlayerSplineRunner runner in runners)
        {
            if (runner == null)
            {
                continue;
            }

            if (runner.TryConsumeHazardDrop(out Vector3 worldPosition))
            {
                SpawnHazard(runner, worldPosition);
            }
        }
    }

    private void UpdateHazardHits()
    {
        for (int hazardIndex = activeHazards.Count - 1; hazardIndex >= 0; hazardIndex--)
        {
            RaceHazard2D hazard = activeHazards[hazardIndex];
            if (hazard == null)
            {
                activeHazards.RemoveAt(hazardIndex);
                continue;
            }

            bool consumed = false;
            foreach (PlayerSplineRunner runner in runners)
            {
                if (runner == null || !hazard.CanHit(runner))
                {
                    continue;
                }

                if (Vector2.Distance(runner.transform.position, hazard.transform.position) <= hazard.TriggerRadius)
                {
                    hazard.ApplyTo(runner);
                    Destroy(hazard.gameObject);
                    activeHazards.RemoveAt(hazardIndex);
                    consumed = true;
                    break;
                }
            }

            if (consumed)
            {
                continue;
            }
        }
    }

    private void UpdateRearHits()
    {
        for (int i = 0; i < runners.Count; i++)
        {
            PlayerSplineRunner attacker = runners[i];
            if (attacker == null || attacker.IsFinished)
            {
                continue;
            }

            for (int j = 0; j < runners.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                PlayerSplineRunner victim = runners[j];
                if (victim == null || !victim.CanReceiveRearHit)
                {
                    continue;
                }

                if (attacker.CurrentForwardSpeed <= victim.CurrentForwardSpeed)
                {
                    continue;
                }

                Vector2 delta = victim.transform.position - attacker.transform.position;
                if (delta.sqrMagnitude > rearHitDistance * rearHitDistance)
                {
                    continue;
                }

                Vector2 victimForward = victim.CurrentRouteTangent.sqrMagnitude > 0.001f ? victim.CurrentRouteTangent.normalized : Vector2.right;
                Vector2 fromVictimToAttacker = -delta.normalized;
                if (Vector2.Dot(victimForward, fromVictimToAttacker) <= 0.25f)
                {
                    continue;
                }

                victim.ApplyRearHitEffect(rearHitBoostAmount, rearHitBoostDuration, rearHitHeatMultiplier, rearHitHeatAmount);
            }
        }
    }

    private static TextMeshProUGUI FindOrCreateText(Transform canvasTransform, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, TextAlignmentOptions alignment)
    {
        Transform existing = canvasTransform.Find(objectName);
        TextMeshProUGUI text = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;

        if (text == null)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvasTransform, false);
            text = textObject.GetComponent<TextMeshProUGUI>();
        }

        RectTransform rectTransform = text.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = anchorMax;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(220f, 40f);

        text.fontSize = 26;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;

        return text;
    }

    private float GetCpuLaneOffset(int index, float usableLaneSpacing)
    {
        int laneIndex = index + 1;
        float direction = laneIndex % 2 == 0 ? 1f : -1f;
        int depth = (laneIndex + 1) / 2;
        return direction * depth * usableLaneSpacing;
    }

    private static Color GetCpuColor(int index)
    {
        Color[] colors =
        {
            new Color(0.95f, 0.57f, 0.18f, 1f),
            new Color(0.2f, 0.9f, 0.6f, 1f),
            new Color(0.35f, 0.75f, 1f, 1f),
            new Color(0.95f, 0.35f, 0.7f, 1f)
        };

        return colors[index % colors.Length];
    }

    private static Sprite GetSquareSprite()
    {
        return SquareSpriteCache.Sprite;
    }

    private void SpawnHazard(PlayerSplineRunner owner, Vector3 worldPosition)
    {
        GameObject hazardObject = new GameObject($"{owner.RunnerName} Hazard");
        hazardObject.transform.SetParent(transform, false);
        hazardObject.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);

        SpriteRenderer hazardRenderer = hazardObject.AddComponent<SpriteRenderer>();
        hazardRenderer.sprite = GetSquareSprite();
        hazardRenderer.color = new Color(0.22f, 0.1f, 0.08f, 1f);
        hazardRenderer.sortingOrder = 2;
        hazardObject.transform.localScale = new Vector3(0.38f, 0.38f, 1f);

        RaceHazard2D hazard = hazardObject.AddComponent<RaceHazard2D>();
        hazard.Initialize(owner, hazardLifetime, hazardRadius, hazardHeatAmount, hazardHeatMultiplier, hazardHeatDuration);
        activeHazards.Add(hazard);
    }

    private static void MatchRunnerVisualScale(PlayerSplineRunner targetRunner, PlayerSplineRunner referenceRunner)
    {
        if (targetRunner == null || referenceRunner == null)
        {
            return;
        }

        targetRunner.transform.localScale = referenceRunner.transform.localScale;

        SpriteRenderer referenceRenderer = referenceRunner.GetComponent<SpriteRenderer>();
        SpriteRenderer targetRenderer = targetRunner.GetComponent<SpriteRenderer>();
        if (referenceRenderer == null || targetRenderer == null)
        {
            return;
        }

        Vector2 referenceSize = referenceRenderer.sprite != null ? referenceRenderer.sprite.bounds.size : Vector2.one;
        Vector2 targetSize = targetRenderer.sprite != null ? targetRenderer.sprite.bounds.size : Vector2.one;

        if (referenceSize.y <= 0.0001f || targetSize.y <= 0.0001f)
        {
            return;
        }

        float heightRatio = referenceSize.y / targetSize.y;
        targetRunner.transform.localScale *= heightRatio;
    }

    private GameObject LoadCharacterPrefab(int prefabIndex)
    {
        return Resources.Load<GameObject>($"{characterPrefabResourcesPath}/{prefabIndex}");
    }

    private static class SquareSpriteCache
    {
        private static Sprite cachedSprite;

        public static Sprite Sprite
        {
            get
            {
                if (cachedSprite == null)
                {
                    cachedSprite = Sprite.Create(
                        Texture2D.whiteTexture,
                        new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }

                return cachedSprite;
            }
        }
    }
}
