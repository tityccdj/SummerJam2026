using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerSplineRunner : MonoBehaviour
{
    private const string StepSoundName = "step";
    private const string GaspSoundName = "gasp";
    private const string RoundSoundName = "round";
    private const string WinSoundName = "win";
    private const string HumanGaspChannel = "player_gasp";
    public const string IdleAnimationStateName = "idle";
    public const string RunAnimationStateName = "run";
    public const string OverheatAnimationStateName = "overheat";

    [Header("Scene References")]
    [SerializeField] private RoutesRenderer routesRenderer;
    [SerializeField] private Joystick joystick;
    [SerializeField] private Slider overheatSlider;
    [SerializeField] private Character character;

    [Header("Runner")]
    [SerializeField] private string runnerName = "Runner";
    [SerializeField] private bool isHuman = true;
    [SerializeField] private int targetLapCount = 3;
    [SerializeField] private bool screenSpaceLateralControls = true;
    [SerializeField] private bool useAnimationSet = true;
    [SerializeField] private float runAnimationSpeedThreshold = 0.02f;
    [SerializeField] private float collisionAnimationDuration = 0.45f;
    [SerializeField] private float tangentFlipThreshold = 0.001f;

    [Header("Movement")]
    [SerializeField] private float pumpImpulse = 0.1f;
    [SerializeField] private float joystickAcceleration = 0.08f;
    [SerializeField] private float maxForwardSpeed = 0.15f;
    [SerializeField] private float speedDamping = 0.05f;
    [SerializeField] private float lateralMoveSpeed = 2.4f;
    [SerializeField] private float lateralReturnSpeed = 1.4f;
    [SerializeField] private float lateralTrackPadding = 0.75f;

    [Header("OverHeat")]
    [SerializeField] private float maxOverHeat = 150f;
    [SerializeField] private float runHeatGain = 12f;
    [SerializeField] private float passiveCoolRate = 10f;
    [SerializeField] private float coolDownTapAmount = 30f;
    [SerializeField] private float overHeatRecoverThreshold = 55f;
    [SerializeField] private float globalSpeedMultiplier = 0.9f;
    [SerializeField] private float overHeatTopSpeedMultiplier = 0.42f;
    [SerializeField] private float overHeatAccelerationMultiplier = 0.12f;
    [SerializeField] private float randomHeatBurstChance = 0.82f;
    [SerializeField] private Vector2 randomHeatBurstIntervalRange = new Vector2(1.1f, 2.3f);
    [SerializeField] private Vector2 randomHeatBurstAmountRange = new Vector2(10f, 22f);
    [SerializeField] private Color overHeatColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float flashSpeed = 14f;

    [Header("Human Input")]
    [SerializeField] private KeyCode runKey = KeyCode.O;
    [SerializeField] private KeyCode coolDownKey = KeyCode.P;
    [SerializeField] private KeyCode dropHazardKey = KeyCode.I;
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;
    [SerializeField] private KeyCode altLeftKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode altRightKey = KeyCode.RightArrow;

    [Header("CPU AI")]
    [SerializeField] private Vector2 aiPumpIntervalRange = new Vector2(0.22f, 0.65f);
    [SerializeField] private Vector2 aiCoolDownIntervalRange = new Vector2(0.45f, 1.2f);
    [SerializeField] private Vector2 aiSteerChangeIntervalRange = new Vector2(0.5f, 1.6f);
    [SerializeField] private Vector2 aiRestDurationRange = new Vector2(0.5f, 1.4f);
    [SerializeField] private Vector2 aiDropIntervalRange = new Vector2(2.2f, 4.8f);
    [SerializeField] private float aiDangerHeatRatio = 0.72f;
    [SerializeField] private float aiEmergencyHeatRatio = 0.92f;

    [Header("Race Effects")]
    [SerializeField] private float hazardDropCooldown = 1.8f;
    [SerializeField] private float hazardDropOffset = 0.85f;

    private float progress;
    private float forwardSpeed;
    private float lateralOffset;
    private float currentOverHeat;
    private bool isOverHeated;
    private bool isFinished;
    private float finishTime = float.PositiveInfinity;
    private int completedLaps;
    private float lastHorizontalSign = 1f;
    private float lastProgressSample;
    private bool hasProgressSample;
    private Vector2 currentRouteTangent = Vector2.right;

    private float aiPumpTimer;
    private float aiPumpCooldown;
    private float aiCoolTimer;
    private float aiCoolCooldown;
    private float aiSteerTimer;
    private float aiSteerCooldown;
    private float aiRestTimer;
    private float aiSteerTarget;
    private float aiDropTimer;
    private float aiDropCooldown;

    private float hazardDropTimer;
    private bool hasPendingHazardDrop;
    private float currentHeatMultiplier = 1f;
    private float heatMultiplierTimer;
    private float rearHitImmunityTimer;
    private float randomHeatTimer;
    private float randomHeatCooldown;
    private bool raceActive = true;
    private float collisionAnimationTimer;
    private string currentAnimationState;
    private float currentFacingTangentX;
    private bool defaultSpriteFlipX;

    public string RunnerName => runnerName;
    public bool IsHuman => isHuman;
    public bool IsFinished => isFinished;
    public int CompletedLaps => completedLaps;
    public int TargetLapCount => targetLapCount;
    public float LapProgress => progress;
    public float FinishTime => finishTime;
    public float RaceProgress => isFinished ? targetLapCount : completedLaps + progress;
    public float CurrentForwardSpeed => forwardSpeed;
    public Vector2 CurrentRouteTangent => currentRouteTangent;
    public bool CanReceiveRearHit => rearHitImmunityTimer <= 0f && !isFinished;
    public event System.Action<PlayerSplineRunner> LapCompleted;
    public event System.Action<PlayerSplineRunner> RaceFinished;

    public void BindSceneReferences(RoutesRenderer renderer, Joystick movementJoystick, Slider heatSlider)
    {
        routesRenderer = renderer;
        joystick = movementJoystick;
        overheatSlider = heatSlider;
        CacheComponents();
        SyncSlider();
        SnapToRoute();
    }

    public void ConfigureRunner(string displayName, bool humanControlled, RoutesRenderer renderer, Joystick movementJoystick, Slider heatSlider, int totalLaps)
    {
        runnerName = string.IsNullOrWhiteSpace(displayName) ? runnerName : displayName;
        isHuman = humanControlled;
        targetLapCount = Mathf.Max(1, totalLaps);
        BindSceneReferences(renderer, movementJoystick, heatSlider);
        ResetRunnerState(progress, lateralOffset);
    }

    public void ResetRunnerState(float startProgress, float startLateralOffset)
    {
        progress = Mathf.Repeat(startProgress, 1f);
        lateralOffset = startLateralOffset;
        forwardSpeed = 0f;
        currentOverHeat = 0f;
        isOverHeated = false;
        isFinished = false;
        finishTime = float.PositiveInfinity;
        completedLaps = 0;
        hasProgressSample = false;
        hasPendingHazardDrop = false;
        currentHeatMultiplier = 1f;
        heatMultiplierTimer = 0f;
        rearHitImmunityTimer = 0f;
        hazardDropTimer = 0f;
        randomHeatTimer = 0f;
        randomHeatCooldown = GetNextRandomHeatCooldown();
        collisionAnimationTimer = 0f;
        currentAnimationState = null;
        currentFacingTangentX = currentRouteTangent.x;
        ResetAiState();
        SyncSlider();
        SnapToRoute();
        UpdateVisuals();
        StopHumanGaspLoop();
    }

    public void SetTargetLapCount(int totalLaps)
    {
        targetLapCount = Mathf.Max(1, totalLaps);
    }

    public void SetRaceActive(bool active)
    {
        raceActive = active;
        if (!raceActive)
        {
            forwardSpeed = 0f;
            StopHumanGaspLoop();
        }
    }

    public void PlayerRun()
    {
        if (!raceActive || isFinished || isOverHeated)
        {
            return;
        }

        forwardSpeed = Mathf.Min(maxForwardSpeed, forwardSpeed + pumpImpulse);
        currentOverHeat = Mathf.Clamp(currentOverHeat + (runHeatGain * currentHeatMultiplier), 0f, maxOverHeat);

        if (currentOverHeat >= maxOverHeat)
        {
            isOverHeated = true;
        }

        if (isHuman)
        {
            AudioManager.Instance?.PlaySFXWithPitchVariation(StepSoundName, 0.08f);
        }

        RefreshHumanOverheatAudio();
        SyncSlider();
    }

    public void CoolDown()
    {
        if (!raceActive || isFinished)
        {
            return;
        }

        currentOverHeat = Mathf.Max(0f, currentOverHeat - coolDownTapAmount);

        if (isOverHeated && currentOverHeat <= overHeatRecoverThreshold)
        {
            isOverHeated = false;
        }

        RefreshHumanOverheatAudio();
        SyncSlider();
    }

    public bool TryConsumeHazardDrop(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (!hasPendingHazardDrop)
        {
            return false;
        }

        hasPendingHazardDrop = false;
        Vector2 tangent = currentRouteTangent.sqrMagnitude > 0.001f ? currentRouteTangent.normalized : Vector2.right;
        worldPosition = transform.position - new Vector3(tangent.x, tangent.y, 0f) * hazardDropOffset;
        return true;
    }

    public void ApplyExternalHeat(float heatAmount, float heatMultiplier, float multiplierDuration)
    {
        currentOverHeat = Mathf.Clamp(currentOverHeat + heatAmount, 0f, maxOverHeat);
        currentHeatMultiplier = Mathf.Max(currentHeatMultiplier, heatMultiplier);
        heatMultiplierTimer = Mathf.Max(heatMultiplierTimer, multiplierDuration);
        TriggerCollisionAnimation();

        if (currentOverHeat >= maxOverHeat)
        {
            isOverHeated = true;
        }

        RefreshHumanOverheatAudio();
        SyncSlider();
    }

    public void TriggerInstantOverheat(float immunityDuration = 0.45f)
    {
        currentOverHeat = maxOverHeat;
        isOverHeated = true;
        rearHitImmunityTimer = Mathf.Max(rearHitImmunityTimer, immunityDuration);
        TriggerCollisionAnimation(immunityDuration);
        RefreshHumanOverheatAudio();
        SyncSlider();
    }

    public void ApplyRearHitEffect(float speedBoostAmount, float duration, float heatMultiplier, float extraHeat)
    {
        forwardSpeed = Mathf.Min(maxForwardSpeed * 1.45f, forwardSpeed + speedBoostAmount);
        TriggerCollisionAnimation(duration);
        ApplyExternalHeat(extraHeat, heatMultiplier, duration);
        rearHitImmunityTimer = Mathf.Max(rearHitImmunityTimer, duration * 0.5f);
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void Start()
    {
        SyncSlider();
        SnapToRoute();
        ResetAiState();
    }

    private void Update()
    {
        RouteData route = routesRenderer != null ? routesRenderer.GetRouteData() : null;
        if (route == null)
        {
            return;
        }

        if (!raceActive)
        {
            forwardSpeed = 0f;
            RefreshHumanOverheatAudio();
            UpdateVisuals();
            SyncSlider();
            return;
        }

        if (!isFinished)
        {
            if (isHuman)
            {
                HandleHumanActionInput();
            }
            else
            {
                HandleCpuDropInput();
            }

            ApplyHeat();
            UpdateMovement(route);
            UpdateLapState(route);
        }

        UpdateVisuals();
        SyncSlider();
    }

    private void CacheComponents()
    {
        if (character == null)
        {
            character = GetComponentInChildren<Character>();
            character.SetFlipX(defaultSpriteFlipX);
        }
    }

    private void ApplyHeat()
    {
        if (currentOverHeat > 0f)
        {
            currentOverHeat = Mathf.Max(0f, currentOverHeat - passiveCoolRate * Time.deltaTime);
        }

        if (heatMultiplierTimer > 0f)
        {
            heatMultiplierTimer -= Time.deltaTime;
            if (heatMultiplierTimer <= 0f)
            {
                currentHeatMultiplier = 1f;
            }
        }

        if (rearHitImmunityTimer > 0f)
        {
            rearHitImmunityTimer -= Time.deltaTime;
        }

        if (collisionAnimationTimer > 0f)
        {
            collisionAnimationTimer -= Time.deltaTime;
        }

        if (hazardDropTimer > 0f)
        {
            hazardDropTimer -= Time.deltaTime;
        }

        randomHeatTimer += Time.deltaTime;
        if (!isFinished && randomHeatTimer >= randomHeatCooldown)
        {
            randomHeatTimer = 0f;
            randomHeatCooldown = GetNextRandomHeatCooldown();

            if (Random.value <= randomHeatBurstChance)
            {
                float extraHeat = Random.Range(randomHeatBurstAmountRange.x, randomHeatBurstAmountRange.y);
                ApplyExternalHeat(extraHeat, 1f, 0f);
            }
        }

        if (isOverHeated && currentOverHeat <= overHeatRecoverThreshold)
        {
            isOverHeated = false;
        }

        RefreshHumanOverheatAudio();
    }

    private void UpdateMovement(RouteData route)
    {
        float forwardInput = isHuman ? GetHumanForwardInput() : GetCpuForwardInput();
        float horizontalInput = isHuman ? GetHumanHorizontalInput() : GetCpuHorizontalInput();
        float accelerationScale = isOverHeated ? overHeatAccelerationMultiplier : 1f;
        float targetTopSpeed = maxForwardSpeed * globalSpeedMultiplier * (isOverHeated ? overHeatTopSpeedMultiplier : 1f);
        float reverseSpeedLimit = maxForwardSpeed * globalSpeedMultiplier * 0.2f;
        float sprintCap = targetTopSpeed * 1.45f;

        forwardSpeed += forwardInput * joystickAcceleration * accelerationScale * Time.deltaTime;
        forwardSpeed = Mathf.Clamp(forwardSpeed, -reverseSpeedLimit, sprintCap);
        forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, speedDamping * Time.deltaTime);
        if (forwardSpeed > targetTopSpeed)
        {
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, targetTopSpeed, speedDamping * 1.8f * Time.deltaTime);
        }

        float previousProgress = progress;
        progress = route.closed
            ? Mathf.Repeat(progress + forwardSpeed * Time.deltaTime, 1f)
            : Mathf.Clamp01(progress + forwardSpeed * Time.deltaTime);

        float offsetLimit = Mathf.Max(0.15f, route.trackWidth * lateralTrackPadding * 0.5f);
        float signedHorizontalInput = ResolveScreenConsistentHorizontal(horizontalInput, route);
        if (Mathf.Abs(signedHorizontalInput) > 0.01f)
        {
            lateralOffset += signedHorizontalInput * lateralMoveSpeed * Time.deltaTime;
        }

        lateralOffset = Mathf.Clamp(lateralOffset, -offsetLimit, offsetLimit);

        Vector2 routePosition = route.EvaluatePosition(progress);
        Vector2 routeNormal = route.EvaluateNormal(progress);
        Vector2 routeTangent = route.EvaluateTangent(progress);
        currentRouteTangent = routeTangent.sqrMagnitude > 0.001f ? routeTangent.normalized : Vector2.right;
        currentFacingTangentX = currentRouteTangent.x;
        Vector2 finalPosition = routePosition + routeNormal * lateralOffset;

        transform.position = new Vector3(finalPosition.x, finalPosition.y, transform.position.z);
        transform.rotation = Quaternion.identity;

        if (!hasProgressSample)
        {
            lastProgressSample = previousProgress;
            hasProgressSample = true;
        }
    }

    private void UpdateLapState(RouteData route)
    {
        if (!route.closed)
        {
            return;
        }

        if (lastProgressSample > 0.85f && progress < 0.15f && forwardSpeed > 0f)
        {
            completedLaps++;
            LapCompleted?.Invoke(this);
            if (isHuman)
            {
                AudioManager.Instance?.PlaySFXOneShot(RoundSoundName);
            }

            if (completedLaps >= targetLapCount)
            {
                FinishRace();
            }
        }

        lastProgressSample = progress;
    }

    private void FinishRace()
    {
        isFinished = true;
        finishTime = Time.time;
        forwardSpeed = 0f;
        StopHumanGaspLoop();
        RaceFinished?.Invoke(this);
        if (isHuman)
        {
            AudioManager.Instance?.PlaySFXOneShot(WinSoundName);
        }
    }

    private float GetHumanForwardInput()
    {
        return joystick != null ? joystick.Vertical : 0f;
    }

    private float GetHumanHorizontalInput()
    {
        float keyboardHorizontal = 0f;

        if (Input.GetKey(leftKey) || Input.GetKey(altLeftKey))
        {
            keyboardHorizontal -= 1f;
        }

        if (Input.GetKey(rightKey) || Input.GetKey(altRightKey))
        {
            keyboardHorizontal += 1f;
        }

        float joystickHorizontal = joystick != null ? joystick.Horizontal : 0f;
        return Mathf.Clamp(keyboardHorizontal + joystickHorizontal, -1f, 1f);
    }

    private void HandleHumanActionInput()
    {
        if (Input.GetKeyDown(runKey))
        {
            PlayerRun();
        }

        if (Input.GetKeyDown(coolDownKey))
        {
            CoolDown();
        }

        if (Input.GetKeyDown(dropHazardKey))
        {
            QueueHazardDrop();
        }
    }

    private float GetCpuForwardInput()
    {
        aiPumpTimer += Time.deltaTime;
        aiCoolTimer += Time.deltaTime;

        if (aiRestTimer > 0f)
        {
            aiRestTimer -= Time.deltaTime;

            if (aiCoolTimer >= aiCoolCooldown)
            {
                CoolDown();
                aiCoolTimer = 0f;
                aiCoolCooldown = Random.Range(aiCoolDownIntervalRange.x, aiCoolDownIntervalRange.y);
            }

            return 0f;
        }

        float heatRatio = maxOverHeat > 0f ? currentOverHeat / maxOverHeat : 0f;

        if (isOverHeated || heatRatio >= aiEmergencyHeatRatio)
        {
            aiRestTimer = Random.Range(aiRestDurationRange.x, aiRestDurationRange.y);
            if (aiCoolTimer >= aiCoolCooldown)
            {
                CoolDown();
                aiCoolTimer = 0f;
                aiCoolCooldown = Random.Range(aiCoolDownIntervalRange.x, aiCoolDownIntervalRange.y);
            }

            return 0f;
        }

        if (heatRatio >= aiDangerHeatRatio)
        {
            if (aiCoolTimer >= aiCoolCooldown)
            {
                CoolDown();
                aiCoolTimer = 0f;
                aiCoolCooldown = Random.Range(aiCoolDownIntervalRange.x, aiCoolDownIntervalRange.y);
            }

            return 0f;
        }

        if (aiPumpTimer >= aiPumpCooldown)
        {
            PlayerRun();
            aiPumpTimer = 0f;
            aiPumpCooldown = Random.Range(aiPumpIntervalRange.x, aiPumpIntervalRange.y);
        }

        return 0f;
    }

    private void HandleCpuDropInput()
    {
        aiDropTimer += Time.deltaTime;
        if (aiDropTimer < aiDropCooldown)
        {
            return;
        }

        aiDropTimer = 0f;
        aiDropCooldown = Random.Range(aiDropIntervalRange.x, aiDropIntervalRange.y);

        if (currentOverHeat < maxOverHeat * 0.88f)
        {
            QueueHazardDrop();
        }
    }

    private float GetCpuHorizontalInput()
    {
        aiSteerTimer += Time.deltaTime;
        if (aiSteerTimer >= aiSteerCooldown)
        {
            aiSteerTimer = 0f;
            aiSteerCooldown = Random.Range(aiSteerChangeIntervalRange.x, aiSteerChangeIntervalRange.y);
            aiSteerTarget = Random.Range(-1f, 1f);
        }

        if (Mathf.Abs(lateralOffset) > 1.25f)
        {
            aiSteerTarget = -Mathf.Sign(lateralOffset) * Random.Range(0.35f, 1f);
        }

        return aiSteerTarget;
    }

    private float ResolveScreenConsistentHorizontal(float horizontalInput, RouteData route)
    {
        if (!screenSpaceLateralControls || Mathf.Abs(horizontalInput) <= 0.01f)
        {
            return horizontalInput;
        }

        Vector2 routeNormal = route.EvaluateNormal(progress);
        float horizontalSign = Vector2.Dot(routeNormal, Vector2.right);

        if (Mathf.Abs(horizontalSign) > 0.15f)
        {
            lastHorizontalSign = Mathf.Sign(horizontalSign);
        }

        return horizontalInput * lastHorizontalSign;
    }

    private void ResetAiState()
    {
        aiPumpTimer = 0f;
        aiCoolTimer = 0f;
        aiSteerTimer = 0f;
        aiRestTimer = 0f;
        aiSteerTarget = 0f;
        aiDropTimer = 0f;
        aiPumpCooldown = Random.Range(aiPumpIntervalRange.x, aiPumpIntervalRange.y);
        aiCoolCooldown = Random.Range(aiCoolDownIntervalRange.x, aiCoolDownIntervalRange.y);
        aiSteerCooldown = Random.Range(aiSteerChangeIntervalRange.x, aiSteerChangeIntervalRange.y);
        aiDropCooldown = Random.Range(aiDropIntervalRange.x, aiDropIntervalRange.y);
    }

    private float GetNextRandomHeatCooldown()
    {
        return Random.Range(randomHeatBurstIntervalRange.x, randomHeatBurstIntervalRange.y);
    }

    private void UpdateVisuals()
    {
        UpdateAnimationState();
        UpdateSpriteFacing();
    }

    private void SyncSlider()
    {
        if (overheatSlider == null)
        {
            return;
        }

        overheatSlider.gameObject.SetActive(isHuman);
        overheatSlider.minValue = 0f;
        overheatSlider.maxValue = maxOverHeat;
        overheatSlider.value = currentOverHeat;
    }

    private void SnapToRoute()
    {
        RouteData route = routesRenderer != null ? routesRenderer.GetRouteData() : null;
        if (route == null)
        {
            return;
        }

        Vector2 routePosition = route.EvaluatePosition(progress);
        Vector2 routeNormal = route.EvaluateNormal(progress);
        currentRouteTangent = route.EvaluateTangent(progress);
        if (currentRouteTangent.sqrMagnitude > 0.001f)
        {
            currentFacingTangentX = currentRouteTangent.normalized.x;
        }
        Vector2 finalPosition = routePosition + routeNormal * lateralOffset;
        transform.position = new Vector3(finalPosition.x, finalPosition.y, transform.position.z);
    }

    private void QueueHazardDrop()
    {
        if (!raceActive || hazardDropTimer > 0f || isFinished)
        {
            return;
        }

        hasPendingHazardDrop = true;
        hazardDropTimer = hazardDropCooldown;
    }

    private void RefreshHumanOverheatAudio()
    {
        if (!isHuman || AudioManager.Instance == null)
        {
            return;
        }

        if (raceActive && !isFinished && isOverHeated)
        {
            AudioManager.Instance.PlayLoop(GaspSoundName, HumanGaspChannel);
        }
        else
        {
            StopHumanGaspLoop();
        }
    }

    private void StopHumanGaspLoop()
    {
        if (!isHuman || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.StopLoop(HumanGaspChannel);
    }

    private void TriggerCollisionAnimation(float durationOverride = -1f)
    {
        float duration = durationOverride > 0f ? durationOverride : collisionAnimationDuration;
        collisionAnimationTimer = Mathf.Max(collisionAnimationTimer, duration);
    }

    private void UpdateAnimationState()
    {
        if (!useAnimationSet)
        {
            return;
        }


        string targetState = IdleAnimationStateName;
        if (collisionAnimationTimer > 0f || isOverHeated)
        {
            targetState = OverheatAnimationStateName;
        }
        else if (raceActive && !isFinished && Mathf.Abs(forwardSpeed) > runAnimationSpeedThreshold)
        {
            targetState = RunAnimationStateName;
        }

        if (currentAnimationState == targetState)
        {
            return;
        }

        if (character != null) character.PlayAnimation(targetState);
        currentAnimationState = targetState;
    }

    private void UpdateSpriteFacing()
    {
        if (character == null)
        {
            return;
        }

        if (currentFacingTangentX >= tangentFlipThreshold)
        {
            character.SetFlipX(defaultSpriteFlipX);
        }
        else if (currentFacingTangentX <= -tangentFlipThreshold)
        {
            character.SetFlipX(!defaultSpriteFlipX);
        }
    }
}
