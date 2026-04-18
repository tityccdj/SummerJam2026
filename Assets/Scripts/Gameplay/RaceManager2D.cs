using System.Collections.Generic;
using System.Linq;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RaceManager2D : MonoBehaviour
{
    private const string CountSoundName = "count";
    private const string RaceSoundName = "race";
    private const string FailSoundName = "fail";

    [Header("Race")]
    [SerializeField] private int cpuRunnerCount = 4;
    [SerializeField] private int lapCount = 3;
    [SerializeField] private float startProgressSpacing = 0.018f;
    [SerializeField] private float laneSpacing = 0.42f;
    [SerializeField] private string playerPrefabResourcesPath = "characters/Player";
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
    [SerializeField] private float rearHitCameraShakeMagnitude = 0.13f;
    [SerializeField] private float rearHitCameraShakeDuration = 0.22f;
    [SerializeField] private float runnerCollisionDistance = 0.55f;
    [SerializeField] private float runnerCollisionCameraShakeMagnitude = 0.18f;
    [SerializeField] private float runnerCollisionCameraShakeDuration = 0.28f;
    [SerializeField] private int obstacleCount = 7;
    [SerializeField] private string obstaclePrefabResourcesPath = "prefabs/obstacles";
    [SerializeField] private int obstaclePrefabMinIndex = 0;
    [SerializeField] private int obstaclePrefabMaxIndex = 8;
    [SerializeField] private float obstacleScale = 0.3f;
    [SerializeField] private float obstacleRadius = 0.2f;
    [SerializeField] private float obstacleSpawnPaddingRatio = 0.72f;
    [SerializeField] private float obstacleSpacing = 0.95f;
    [SerializeField] private float obstacleScreenExitDuration = 0.65f;

    [Header("UI")]
    [SerializeField] private string rankTextName = "Text (Rank)";
    [SerializeField] private string lapTextName = "Text (Lap)";
    [SerializeField] private string countDownTextName = "CountDownText";
    [SerializeField] private string runnerNameTextPrefix = "Text (Runner)";
    [SerializeField] private string uiFontResourcesPath = "fonts/troika SDF";
    [SerializeField] private float countDownStepDuration = 0.9f;
    [SerializeField] private float goTextDuration = 0.75f;

    private readonly List<PlayerSplineRunner> runners = new List<PlayerSplineRunner>();
    private readonly List<RaceHazard2D> activeHazards = new List<RaceHazard2D>();
    private readonly List<RaceObstacle2D> activeObstacles = new List<RaceObstacle2D>();
    private TextMeshProUGUI rankText;
    private TextMeshProUGUI lapText;
    private TextMeshProUGUI countDownText;
    private PlayerSplineRunner humanRunner;
    private Canvas uiCanvas;
    private CameraFollow2D cameraFollow;
    private Coroutine countdownRoutine;
    private TMP_FontAsset uiFont;

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
        ClearObstacles();
        uiCanvas = canvas != null ? canvas : FindFirstObjectByType<Canvas>();
        cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;

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

        SpawnObstacles(route);
        SetAllRunnersRaceActive(false);
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
        }

        countdownRoutine = StartCoroutine(PlayCountdown());
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
        //UpdateRearHits();
        UpdateRunnerCollisions();
        UpdateObstacleHits();
        UpdateUi();
    }

    private PlayerSplineRunner CreateOrUsePlayerRunner(PlayerSplineRunner existingRunner, RoutesRenderer routesRenderer)
    {
        GameObject playerPrefab = LoadPlayerPrefab();
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
            Debug.LogWarning("[RaceManager2D] Player prefab was not found in Resources and there is no fallback player in the scene.");
        }

        return resolvedRunner;
    }

    private PlayerSplineRunner CreateCpuRunner(int prefabIndex, RoutesRenderer routesRenderer, float usableLaneSpacing)
    {
        GameObject cpuPrefab = LoadPlayerPrefab();
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

        Character character = cpuObject.GetComponentInChildren<Character>();
        if (character != null)
        {
            character.RandomizeAppearance();
        }

        float startProgress = Mathf.Repeat(1f - (prefabIndex * startProgressSpacing), 0.0001f);
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

        LoadUiFont();
        rankText = FindOrCreateText(canvas.transform, rankTextName, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), TextAlignmentOptions.TopRight);
        lapText = FindOrCreateText(canvas.transform, lapTextName, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), TextAlignmentOptions.TopLeft);
        countDownText = FindOrCreateText(canvas.transform, countDownTextName, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), TextAlignmentOptions.Center);
        ApplyUiFont(rankText);
        ApplyUiFont(lapText);
        if (countDownText != null)
        {
            RectTransform rectTransform = countDownText.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(480f, 140f);
            countDownText.fontSize = 84;
            countDownText.gameObject.SetActive(false);
        }
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

    private IEnumerator PlayCountdown()
    {
        if (countDownText == null)
        {
            SetAllRunnersRaceActive(true);
            yield break;
        }

        countDownText.gameObject.SetActive(true);
        //string[] steps = { "3", "2", "1" };
        string[] steps = {  "1" };

        foreach (string step in steps)
        {
            countDownText.text = step;
            AudioManager.Instance?.PlaySFXOneShot(CountSoundName);
            yield return new WaitForSeconds(countDownStepDuration);
        }

        countDownText.text = "GO!";
        AudioManager.Instance?.PlaySFXOneShot(RaceSoundName);
        SetAllRunnersRaceActive(true);
        yield return new WaitForSeconds(goTextDuration);

        countDownText.gameObject.SetActive(false);
        countdownRoutine = null;
    }

    private void SetAllRunnersRaceActive(bool active)
    {
        foreach (PlayerSplineRunner runner in runners)
        {
            if (runner == null)
            {
                continue;
            }

            runner.SetRaceActive(active);
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
                    if (runner == humanRunner)
                    {
                        AudioManager.Instance?.PlaySFXOneShot(FailSoundName);
                    }

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

                //victim.ApplyRearHitEffect(rearHitBoostAmount, rearHitBoostDuration, rearHitHeatMultiplier, rearHitHeatAmount);
                //TriggerRearHitCameraShake(attacker, victim);
            }
        }
    }

    private void TriggerRearHitCameraShake(PlayerSplineRunner attacker, PlayerSplineRunner victim)
    {
        if (cameraFollow == null)
        {
            cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
        }

        if (cameraFollow == null || humanRunner == null)
        {
            return;
        }

        if (attacker == humanRunner || victim == humanRunner)
        {
            AudioManager.Instance?.PlaySFXOneShot(FailSoundName);
            cameraFollow.TriggerShake(rearHitCameraShakeMagnitude, rearHitCameraShakeDuration);
        }
    }

    private void UpdateRunnerCollisions()
    {
        if (humanRunner == null || humanRunner.IsFinished)
        {
            return;
        }

        float sqrCollisionDistance = runnerCollisionDistance * runnerCollisionDistance;

        foreach (PlayerSplineRunner runner in runners)
        {
            if (runner == null || runner == humanRunner || runner.IsFinished)
            {
                continue;
            }

            Vector2 delta = runner.transform.position - humanRunner.transform.position;
            if (delta.sqrMagnitude > sqrCollisionDistance)
            {
                continue;
            }

            if (!humanRunner.CanReceiveRearHit || !runner.CanReceiveRearHit)
            {
                continue;
            }

            //humanRunner.TriggerInstantOverheat();
            //runner.TriggerInstantOverheat();
            //AudioManager.Instance?.PlaySFXOneShot(FailSoundName);
            //TriggerCollisionCameraShake();
        }
    }

    private void UpdateObstacleHits()
    {
        for (int obstacleIndex = activeObstacles.Count - 1; obstacleIndex >= 0; obstacleIndex--)
        {
            RaceObstacle2D obstacle = activeObstacles[obstacleIndex];
            if (obstacle == null)
            {
                activeObstacles.RemoveAt(obstacleIndex);
                continue;
            }

            if (obstacle.IsAnimating)
            {
                continue;
            }

            foreach (PlayerSplineRunner runner in runners)
            {
                if (runner == null || runner.IsFinished)
                {
                    continue;
                }

                float triggerDistance = obstacle.TriggerRadius + obstacleScale;
                if (Vector2.Distance(runner.transform.position, obstacle.transform.position) > triggerDistance)
                {
                    continue;
                }

                runner.TriggerInstantOverheat();
                obstacle.AnimateAwayAndDestroy(obstacleScreenExitDuration);
                activeObstacles.RemoveAt(obstacleIndex);
                if (runner == humanRunner)
                {
                    AudioManager.Instance?.PlaySFXOneShot(FailSoundName);
                }

                if (runner == humanRunner)
                {
                    TriggerCollisionCameraShake();
                }

                break;
            }
        }
    }

    private void TriggerCollisionCameraShake()
    {
        if (cameraFollow == null)
        {
            cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
        }

        if (cameraFollow != null)
        {
            cameraFollow.TriggerShake(runnerCollisionCameraShakeMagnitude, runnerCollisionCameraShakeDuration);
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

    private void LoadUiFont()
    {
        if (uiFont == null)
        {
            uiFont = Resources.Load<TMP_FontAsset>(uiFontResourcesPath);
        }
    }

    private void ApplyUiFont(TextMeshProUGUI text)
    {
        if (text != null && uiFont != null)
        {
            text.font = uiFont;
        }
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

    private void SpawnObstacles(RouteData route)
    {
        if (route == null || !route.IsValid() || obstacleCount <= 0)
        {
            return;
        }

        float halfTrackWidth = route.trackWidth * obstacleSpawnPaddingRatio * 0.5f;
        int attempts = obstacleCount * 8;
        List<Vector2> placedPositions = new List<Vector2>();

        for (int i = 0; i < attempts && activeObstacles.Count < obstacleCount; i++)
        {
            float progress = Random.Range(0.08f, 0.92f);
            Vector2 center = route.EvaluatePosition(progress);
            Vector2 normal = route.EvaluateNormal(progress);
            float lateral = Random.Range(-halfTrackWidth, halfTrackWidth);
            Vector2 obstaclePosition = center + normal * lateral;

            bool overlapsExisting = false;
            foreach (Vector2 placedPosition in placedPositions)
            {
                if (Vector2.Distance(placedPosition, obstaclePosition) < obstacleSpacing)
                {
                    overlapsExisting = true;
                    break;
                }
            }

            if (overlapsExisting)
            {
                continue;
            }

            placedPositions.Add(obstaclePosition);
            SpawnObstacle(obstaclePosition);
        }
    }

    private void SpawnObstacle(Vector2 worldPosition)
    {
        GameObject obstaclePrefab = LoadRandomObstaclePrefab();
        GameObject obstacleObject;

        if (obstaclePrefab != null)
        {
            obstacleObject = Instantiate(obstaclePrefab, transform);
            obstacleObject.name = obstaclePrefab.name;
        }
        else
        {
            obstacleObject = new GameObject("TrackObstacle");
            obstacleObject.transform.SetParent(transform, false);

            SpriteRenderer obstacleRenderer = obstacleObject.AddComponent<SpriteRenderer>();
            obstacleRenderer.sprite = GetSquareSprite();
            obstacleRenderer.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            obstacleRenderer.sortingOrder = 3;
        }

        obstacleObject.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
        obstacleObject.transform.localScale = Vector3.one * obstacleScale;

        EnsureObstacleSorting(obstacleObject);

        RaceObstacle2D obstacle = obstacleObject.GetComponent<RaceObstacle2D>();
        if (obstacle == null)
        {
            obstacle = obstacleObject.AddComponent<RaceObstacle2D>();
        }

        obstacle.Initialize(obstacleRadius);
        activeObstacles.Add(obstacle);
    }

    private GameObject LoadRandomObstaclePrefab()
    {
        int rangeMin = Mathf.Min(obstaclePrefabMinIndex, obstaclePrefabMaxIndex);
        int rangeMax = Mathf.Max(obstaclePrefabMinIndex, obstaclePrefabMaxIndex);
        int randomIndex = Random.Range(rangeMin, rangeMax + 1);
        return Resources.Load<GameObject>($"{obstaclePrefabResourcesPath}/{randomIndex}");
    }

    private static void EnsureObstacleSorting(GameObject obstacleObject)
    {
        SpriteRenderer[] renderers = obstacleObject.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 3);
        }
    }

    private void ClearObstacles()
    {
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            if (activeObstacles[i] != null)
            {
                Destroy(activeObstacles[i].gameObject);
            }
        }

        activeObstacles.Clear();
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
    }

    private GameObject LoadCharacterPrefab(int prefabIndex)
    {
        GameObject prefab = Resources.Load<GameObject>($"{characterPrefabResourcesPath}/{prefabIndex}");
        if (prefab != null)
        {
            return prefab;
        }

        return Resources.Load<GameObject>($"characters/{prefabIndex}");
    }

    private GameObject LoadPlayerPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>(playerPrefabResourcesPath);
        if (prefab != null)
        {
            return prefab;
        }

        prefab = Resources.Load<GameObject>("characters/Player");
        if (prefab != null)
        {
            return prefab;
        }

        return LoadCharacterPrefab(0);
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
