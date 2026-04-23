using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static RaceResult;

[DisallowMultipleComponent]
public class RaceManager2D : MonoBehaviour
{
    
    private const string CountSoundName = "count";
    private const string RaceSoundName = "race";
    private const string FailSoundName = "fail";
    private const string ChickenSoundName = "chicken";

    [Header("Race")]
    [SerializeField] private bool isRaceEnded = false;
    [SerializeField] private int cpuRunnerCount = 4;
    [SerializeField] private int lapCount = 3;//3
    [SerializeField] private float startProgressSpacing = 0.018f;
    [SerializeField] private float laneSpacing = 0.42f;
    [SerializeField] private string playerPrefabResourcesPath = "characters/Player";
    [SerializeField] private string characterPrefabResourcesPath = "prefabs/character";
    [SerializeField] private float hazardLifetime = 7f;
    [SerializeField] private float hazardRadius = 1f;
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
    [SerializeField] private int obstacleCount = 10;
    [SerializeField] private string obstaclePrefabResourcesPath = "prefabs/obstacles";
    [SerializeField] private int obstaclePrefabMinIndex = 1;
    [SerializeField] private int obstaclePrefabMaxIndex = 3;
    [SerializeField] private float obstacleScale = 0.2f;
    [SerializeField] private float obstacleRadius = 0.7f;
    [SerializeField] private float obstacleSpawnPaddingRatio = 0.72f;
    [SerializeField] private float obstacleSpacing = 8f;
    [SerializeField] private float obstacleScreenExitDuration = 0.65f;
    [SerializeField] private int minObstacleCount = 5;
    


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
    private readonly List<RaceItemBox2D> activeItemBoxes = new List<RaceItemBox2D>();
    private TextMeshProUGUI rankText;
    private TextMeshProUGUI MyrankText;
    private TextMeshProUGUI FirstPlace_name;
    private TextMeshProUGUI SecondPlace_name;
    private TextMeshProUGUI ThirdPlace_name;
    private TextMeshProUGUI lapText;
    private TextMeshProUGUI New_Laptext;
    private TextMeshProUGUI countDownText;
    private PlayerSplineRunner humanRunner;
    private Canvas uiCanvas;
    private CameraFollow2D cameraFollow;
    private Coroutine countdownRoutine;
    private TMP_FontAsset uiFont;
    private RouteData currentRouteData;

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
        activeItemBoxes.Clear();
        runners.Clear();
        activeHazards.Clear();
        ClearObstacles();
        uiCanvas = canvas != null ? canvas : GameObject.Find("Canvas").GetComponent<Canvas>();
        cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
        New_Laptext = GameObject.Find("round").GetComponent<TextMeshProUGUI>();
        MyrankText = GameObject.Find("my_ranktext").GetComponent<TextMeshProUGUI>();
        FirstPlace_name = GameObject.Find("first_place_name").GetComponent<TextMeshProUGUI>();
        SecondPlace_name = GameObject.Find("second_place_name").GetComponent<TextMeshProUGUI>();
        ThirdPlace_name = GameObject.Find("third_place_name").GetComponent<TextMeshProUGUI>();

        RouteData route = routesRenderer.GetRouteData();
        currentRouteData = route;
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
        SpawnItemBoxes(route);
        SetAllRunnersRaceActive(false);
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
        }

        countdownRoutine = StartCoroutine(PlayCountdown());
        UpdateUi();
    }
    private void SpawnItemBoxes(RouteData route)
    {
        if (route == null || !route.IsValid()) return;

        // �ҧ 3 �� ���������ʹ�� 25%, 50% ��� 75%
        float[] progressPoints = { 0.25f, 0.50f, 0.75f };
        int boxesPerRow = 5; // ���� 3 ���ͧ

        foreach (float progress in progressPoints)
        {
            Vector2 center = route.EvaluatePosition(progress);
            Vector2 normal = route.EvaluateNormal(progress);
            float halfWidth = route.trackWidth * 0.4f; // ��Ш�«��¢��

            for (int i = 0; i < boxesPerRow; i++)
            {
                float lateral = Mathf.Lerp(-halfWidth, halfWidth, i / (float)(boxesPerRow - 1));
                Vector2 pos = center + normal * lateral;

                CreateItemBox(pos);
            }
        }
    }
    private void UpdateItemBoxHits()
    {
        foreach (PlayerSplineRunner runner in runners)
        {
            if (runner == null || runner.IsFinished) continue;

            foreach (RaceItemBox2D box in activeItemBoxes)
            {
                if (box.IsAvailable)
                {
                    if (Vector2.Distance(runner.transform.position, box.transform.position) <= box.TriggerRadius + 0.2f)
                    {
                        box.Collect(runner);
                    }
                }
            }
        }
    }
    private void CreateItemBox(Vector2 worldPos)
    {
        GameObject boxObj = new GameObject("ItemBox");
        boxObj.transform.SetParent(transform, false);
        boxObj.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

        SpriteRenderer sr = boxObj.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Item/Box");
        sr.sortingOrder = 2;
        boxObj.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

        RaceItemBox2D box = boxObj.AddComponent<RaceItemBox2D>();
        activeItemBoxes.Add(box);
    }
    private void CheckFinishedCount()
    {
        // 1. นับจำนวนคนที่ IsFinished เป็น true
        int finishedCount = 0;
        foreach (var runner in runners)
        {
            if (runner != null && runner.IsFinished)
            {
                finishedCount++;
            }
        }

        // 2. ถ้าครบ 5 คน และยังไม่เคยสั่งจบเกม
        if (finishedCount >= 5 && !isRaceEnded)
        {
            isRaceEnded = true; // ล็อกไว้ไม่ให้เข้ามาทำงานซ้ำ

            Debug.Log("เข้าเส้นชัยครบ 5 คนแล้ว! กำลังทำสิ่งต่อไปนี้...");

            // 3. ใส่สิ่งที่คุณต้องการให้ทำตรงนี้ เช่น:
            SaveResultsAndLoadScene(); 
            // หรือเรียกฟังก์ชันจบเกมอื่นๆ
            StartCoroutine(GotoResult()); // แนะนำให้รอสักครู่ก่อนเปลี่ยนฉาก
        }
    }
    private void Update()
    {
        if (humanRunner == null || runners.Count == 0 || isRaceEnded)
        {
            return;
        }
        CheckFinishedCount();
        UpdateHazardDrops();
        UpdateHazardHits();
        //UpdateRearHits();
        //UpdateRunnerCollisions();
        UpdateObstacleHits();
        UpdateItemBoxHits();
        UpdateUi();
        MaintainObstacleCount();
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

        Character character = resolvedRunner.GetComponentInChildren<Character>();
        if (character != null)
        {
            character.LoadCharacterData();
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
        //rankText = FindOrCreateText(canvas.transform, rankTextName, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), TextAlignmentOptions.TopRight);
        //lapText = FindOrCreateText(canvas.transform, lapTextName, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), TextAlignmentOptions.TopLeft);
        countDownText = FindOrCreateText(canvas.transform, countDownTextName, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), TextAlignmentOptions.Center);
        Debug.Log($"countDownText: {countDownText}");
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
        if(FirstPlace_name != null)
        {
            //FirstPlace_name.text = orderedRunners.Count >= 1 && orderedRunners[0] != null ? orderedRunners[0].RunnerName : "???";
            FirstPlace_name.text = $"1st: {orderedRunners[0].RunnerName}";
        }
        if (SecondPlace_name != null)
        {
            //SecondPlace_name.text = orderedRunners.Count >= 2 && orderedRunners[1] != null ? orderedRunners[1].RunnerName : "???";
            SecondPlace_name.text = $"2nd: {orderedRunners[1].RunnerName}";
        }
        if (ThirdPlace_name != null)
        {
            //ThirdPlace_name.text = orderedRunners.Count >= 3 && orderedRunners[2] != null ? orderedRunners[2].RunnerName : "???";
            ThirdPlace_name.text = $"3rd: {orderedRunners[2].RunnerName}";
        }
        if (MyrankText != null)
        {
            string rankDisplay = "";
            if (humanRank == 1)
            {
                rankDisplay = "1st";
            }
            else if (humanRank == 2)
            {
                rankDisplay = "2nd";
            }
            else if (humanRank == 3)
            {
                rankDisplay = "3rd";
            }
            else
            {
                rankDisplay = $"{humanRank}th";
            }
            MyrankText.text = $"Your rank {rankDisplay}";
            //rankText.text = $"Rank {humanRank}/{orderedRunners.Count}";
        }

        if (New_Laptext != null && humanRunner != null)
        {
            int shownLap = Mathf.Min(humanRunner.CompletedLaps + 1, lapCount);
            if (humanRunner.IsFinished)
            {
                shownLap = lapCount;
            }
            New_Laptext.text=$"Lap {shownLap} / {lapCount}";
            //lapText.text = $"Lap {shownLap}/{lapCount}";
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
        string[] steps = { "3", "2", "1" };
        //string[] steps = {  "1" };

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
                    AudioManager.Instance?.PlaySFXOneShot(ChickenSoundName);
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
            Debug.Log($"Creating UI Text: {objectName}"); 
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
        hazardRenderer.sprite = Resources.Load<Sprite>("Item/Trap");
        hazardRenderer.sortingOrder = 2;
        hazardObject.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

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
    public void SaveResultsAndLoadScene()
    {
        // 1. เคลียร์กล่องข้อมูลเก่าทิ้งก่อน
        RaceResultData.FinalResults.Clear();

        // 2. ดึงนักแข่งมาเรียงลำดับ (ก๊อปปี้สูตรเรียงลำดับมาจาก UpdateUi เลย)
        List<PlayerSplineRunner> orderedRunners = runners
            .Where(runner => runner != null)
            .OrderByDescending(runner => runner.IsFinished)
            .ThenBy(runner => runner.IsFinished ? runner.FinishTime : float.PositiveInfinity)
            .ThenByDescending(runner => runner.RaceProgress)
            .ToList();

        // 3. เอาข้อมูลนักแข่งที่เรียงลำดับแล้ว ยัดใส่กล่องลอยฟ้าทีละคน
        for (int i = 0; i < orderedRunners.Count; i++)
        {
            PlayerSplineRunner r = orderedRunners[i];

            RunnerResult result = new RunnerResult();
            result.Name = r.RunnerName;
            result.Rank = i + 1; // ลำดับที่ 1, 2, 3...
            result.FinishTime = r.FinishTime;
            result.IsHuman = r.IsHuman;
            Character characterInfo = r.GetComponentInChildren<Character>();

            if (characterInfo != null)
            {
                // ถ้ามีสคริปต์ Character ให้จดข้อมูลทรงผม หน้าตา สีผมไว้
                result.HasCharacter = true;
                result.HairIndex = characterInfo.CurrentHairIndex;
                result.FaceIndex = characterInfo.CurrentFaceIndex;
                result.ClothIndex = characterInfo.CurrentClothIndex;
                result.HairColor = characterInfo.HairColor;
            }
            else
            {
                // ถ้าไม่มีสคริปต์ Character (เช่น โหลด Prefab ไม่ติด กลายเป็นกล่องสี่เหลี่ยมสีๆ)
                result.HasCharacter = false;
                SpriteRenderer sr = r.GetComponentInChildren<SpriteRenderer>();
                result.FallbackColor = sr != null ? sr.color : Color.white;
            }
            RaceResultData.FinalResults.Add(result);
        }

        // 4. โหลดหน้าต่างสรุปผล (เปลี่ยนชื่อ "ResultScene" เป็นชื่อ Scene สรุปผลของคุณ)
        SceneManager.LoadScene("Result");
    }
    private void MaintainObstacleCount()
    {
        if (currentRouteData == null || !currentRouteData.IsValid() || minObstacleCount <= 0)
        {
            return;
        }

        // ถ้าจำนวนสิ่งกีดขวางในด่านเหลือน้อยกว่า 5
        if (activeObstacles.Count < minObstacleCount)
        {
            float halfTrackWidth = currentRouteData.trackWidth * obstacleSpawnPaddingRatio * 0.5f;
            int attempts = 10; // ลองสุ่มหาจุดเกิด 10 ครั้งต่อเฟรมเพื่อหาจุดที่ว่างจริงๆ

            for (int i = 0; i < attempts; i++)
            {
                float progress = Random.value; // สุ่มจุดเกิดตลอดทั้งเส้นทาง (0 ถึง 1)
                Vector2 center = currentRouteData.EvaluatePosition(progress);
                Vector2 normal = currentRouteData.EvaluateNormal(progress);
                float lateral = Random.Range(-halfTrackWidth, halfTrackWidth);
                Vector2 candidatePos = center + normal * lateral;

                bool overlaps = false;

                // เช็ก 1: จุดที่เกิด ทับกับสิ่งกีดขวางอันอื่นไหม
                foreach (var obs in activeObstacles)
                {
                    if (obs != null && Vector2.Distance(obs.transform.position, candidatePos) < obstacleSpacing)
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (overlaps) continue;

                // เช็ก 2: จุดที่เกิด อยู่ใกล้รถแข่งเกินไปไหม (ป้องกันเกิดทับหน้าผู้เล่นกะทันหัน รัศมี 3 หน่วย)
                foreach (var runner in runners)
                {
                    if (runner != null && !runner.IsFinished && Vector2.Distance(runner.transform.position, candidatePos) < 3.0f)
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (overlaps) continue;

                // ถ้าที่ว่างปลอดภัย ให้สร้างสิ่งกีดขวางใหม่ แล้วออกจากลูปทันที (สร้างทีละ 1 อัน เพื่อไม่ให้เกมกระตุก)
                SpawnObstacle(candidatePos);
                break;
            }
        }
    }
    IEnumerator GotoResult()
    {
        // รอ 2 วินาทีเพื่อให้ผู้เล่นเห็นผลลัพธ์บนหน้าจอแข่งก่อน
        yield return new WaitForSeconds(3f);
        SaveResultsAndLoadScene();
    }
}
