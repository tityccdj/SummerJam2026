using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro; // 🌟 จำเป็นต้องใช้สำหรับ TextMeshPro

[DisallowMultipleComponent]
public class PlayerSplineRunner : MonoBehaviour
{
    private const string StepSoundName = "step";
    private const string GetItemSoundName = "get";
    private const string SunSoundName = "sun";
    private const string BananaSoundName = "fail";
    private const string IceSoundName = "ice";
    private const string GunSoundName = "gun";
    private const string SodaSoundName = "soda";
    private const string BalloonSoundName = "balloon";
    private const string CooldownSoundName = "cooldown";
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
    [SerializeField] private Image itemUIImage;

    // 🌟 1. เพิ่มช่องใส่ UI สำหรับแสดงคนที่โจมตีเรา
    [Header("Hit Feedback UI")]
    [SerializeField] private Image hitFeedbackImage; // UI Image รูปไอเทมที่โดน
    [SerializeField] private TextMeshProUGUI hitFeedbackText; // UI Text ข้อความ From...
    [SerializeField] private GameObject CooldownAlertPanel;
    private Coroutine feedbackCoroutine;

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
    [SerializeField] private float maxForwardSpeed = 0.05f;
    [SerializeField] private float speedDamping = 0.04f;
    [SerializeField] private float lateralMoveSpeed = 3.4f;
    [SerializeField] private float lateralReturnSpeed = 1.4f;
    [SerializeField] private float lateralTrackPadding = 0.75f;

    [Header("OverHeat")]
    [SerializeField] private float maxOverHeat = 100f;
    [SerializeField] private float runHeatGain = 15f;
    [SerializeField] private float passiveCoolRate = 30f;
    [SerializeField] private float coolDownTapAmount = 10f;
    [SerializeField] private float overHeatRecoverThreshold = 25f;
    [SerializeField] private float globalSpeedMultiplier = 0.9f;
    [SerializeField] private float overHeatTopSpeedMultiplier = 0.42f;
    [SerializeField] private float overHeatAccelerationMultiplier = 0.25f;
    [SerializeField] private float randomHeatBurstChance = 0.82f;
    [SerializeField] private Vector2 randomHeatBurstIntervalRange = new Vector2(1.1f, 2.3f);
    [SerializeField] private Vector2 randomHeatBurstAmountRange = new Vector2(10f, 22f);
    [SerializeField] private Color overHeatColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float flashSpeed = 14f;

    [Header("Human Input")]
    [SerializeField] private KeyCode runKey = KeyCode.O;
    [SerializeField] private KeyCode altRunKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode coolDownKey = KeyCode.P;
    [SerializeField] private KeyCode altCoolDownKey = KeyCode.Z;
    [SerializeField] private KeyCode useItemKey = KeyCode.Space;
    [SerializeField] private KeyCode altUseItemKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;
    [SerializeField] private KeyCode upKey = KeyCode.W;
    [SerializeField] private KeyCode downKey = KeyCode.S;
    [SerializeField] private KeyCode altLeftKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode altRightKey = KeyCode.RightArrow;

    [Header("Inventory")]
    public RaceItemType currentItem = RaceItemType.None;
    private float heatImmunityEndTime = 0f;
    private float speedDampingMultiplier = 1f;
    private Coroutine colorEffectCoroutine;
    private float coolDownDebuffEndTime = 0f;

    [Header("CPU AI")]
    [SerializeField] private Vector2 aiPumpIntervalRange = new Vector2(0.32f, 0.75f);
    [SerializeField] private Vector2 aiCoolDownIntervalRange = new Vector2(0.85f, 1.2f);
    [SerializeField] private Vector2 aiSteerChangeIntervalRange = new Vector2(0.5f, 1.6f);
    [SerializeField] private Vector2 aiRestDurationRange = new Vector2(0.1f, 0.2f);
    [SerializeField] private Vector2 aiDropIntervalRange = new Vector2(2.2f, 4.8f);
    [SerializeField] private float aiDangerHeatRatio = 0.82f;
    [SerializeField] private float aiEmergencyHeatRatio = 0.92f;

    [Header("Race Effects")]
    [SerializeField] private float hazardDropCooldown = 1.8f;
    [SerializeField] private float hazardDropOffset = 0.85f;

    [Header("VFX References")]
    [SerializeField] private ParticleSystem sodaSpeedVfx; // ลาก Particle สปีดมาใส่
    [SerializeField] private ParticleSystem iceAuraVfx;   // ลาก Particle ไอเย็นมาใส่
    [SerializeField] private ParticleSystem sunBurnVfx;    // 🌟 สำหรับโดน Sun
    [SerializeField] private ParticleSystem weightHeavyVfx; // 🌟 สำหรับโดน Weight

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

    private float speedBoostEndTime;
    private float speedBoostMultiplier = 1f;

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

    private void UpdateItemUI()
    {
        if (!isHuman || itemUIImage == null) return;

        if (currentItem == RaceItemType.None)
        {
            itemUIImage.enabled = false;
            itemUIImage.sprite = null;
        }
        else
        {
            Sprite loadedSprite = Resources.Load<Sprite>($"Item/{currentItem.ToString()}");

            if (loadedSprite != null)
            {
                itemUIImage.sprite = loadedSprite;
                itemUIImage.enabled = true;
            }
            else
            {
                Debug.LogWarning($"หาภาพไอเทมไม่เจอ! กรุณาเช็กว่ามีไฟล์ชื่อ {currentItem} อยู่ใน Resources/Item/ หรือไม่");
            }
        }
    }

    public void SetItemUI(Image uiImage)
    {
        itemUIImage = uiImage;
    }

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

        if (isHuman)
        {
            hitFeedbackImage = GameObject.Find("HitFeedbackImage")?.GetComponent<Image>();
            hitFeedbackText = GameObject.Find("HitFeedbackText")?.GetComponent<TextMeshProUGUI>();

            // ถ้าหา CooldownAlertPanel แบบปกติไม่เจอ ให้ลองหาในลูกของ Canvas
            if (CooldownAlertPanel == null)
            {
                // วิธีที่ชัวร์กว่า: หา Canvas ก่อนแล้วค่อยหาลูกข้างใน
                GameObject canvasObj = GameObject.Find("Canvas");
                if (canvasObj != null)
                {
                    // ค้นหาลูกชื่อ CooldownAlertPanel แม้ว่ามันจะปิด (Inactive) อยู่ก็ตาม
                    Transform t = canvasObj.transform.Find("CharacterStatus/AlertPanel");
                    if (t != null) CooldownAlertPanel = t.gameObject;
                }
            }
        }
        // 🌟 ปิดการแสดงผล UI แจ้งเตือนตอนเริ่มด่าน
        if (isHuman)
        {
            if (hitFeedbackImage != null) hitFeedbackImage.enabled = false;
            if (hitFeedbackText != null) hitFeedbackText.enabled = false;
            if (CooldownAlertPanel != null) CooldownAlertPanel.SetActive(false);
        }

        if (sodaSpeedVfx == null)
            sodaSpeedVfx = FindChildParticleByName("Soda_VFX");

        if (iceAuraVfx == null)
            iceAuraVfx = FindChildParticleByName("Ice_VFX");
        if (sunBurnVfx == null) sunBurnVfx = FindChildParticleByName("Sun_VFX");
        if (weightHeavyVfx == null) weightHeavyVfx = FindChildParticleByName("Weight_VFX");
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

    public bool ReceiveItem(RaceItemType newItem)
    {
        if (currentItem != RaceItemType.None)
        {
            return false;
        }
        if (isHuman) AudioManager.Instance?.PlaySFXOneShot(GetItemSoundName);
        currentItem = newItem;
        UpdateItemUI();
        return true;
    }

    public void UseItem()
    {
        if (currentItem == RaceItemType.None) return;

        if (colorEffectCoroutine != null)
        {
            StopCoroutine(colorEffectCoroutine);
        }

        PlayerSplineRunner targetAhead = GetTargetAhead();

        switch (currentItem)
        {
            case RaceItemType.Soda:
                ApplySpeedBoost(1.5f, 2f);
                heatImmunityEndTime = Time.time + 2f;
                speedDampingMultiplier = 0.5f;

                if (isHuman) AudioManager.Instance?.PlaySFXOneShot(SodaSoundName);
                if (sodaSpeedVfx != null) sodaSpeedVfx.Play();

                //colorEffectCoroutine = StartCoroutine(ItemColorEffect(new Color(1f, 0.8f, 0.2f), 3f));
                Debug.Log($"<color=lime> [{RunnerName}] </color> กดใช้: <color=orange> Soda  </color>");
                EffectManager.Instance?.PlayEffect("soda", transform.position);

                break;

            case RaceItemType.Banana:
                QueueHazardDrop();

                if (isHuman) AudioManager.Instance?.PlaySFXOneShot(BananaSoundName);
                //colorEffectCoroutine = StartCoroutine(ItemColorEffect(Color.green, 1f));
                colorEffectCoroutine = StartCoroutine(ItemColorEffect(Color.green, 1f));
                Debug.Log($"<color=lime> [{RunnerName}] </color> กดใช้: <color=yellow> Banana  </color>");
                EffectManager.Instance?.PlayEffect("powder", transform.position);

                break;

            case RaceItemType.Ice:
                ResetHeat();
                heatImmunityEndTime = Time.time + 3f;

                if (iceAuraVfx != null) iceAuraVfx.Play();

                if (isHuman) AudioManager.Instance?.PlaySFXOneShot(IceSoundName);

                colorEffectCoroutine = StartCoroutine(ItemColorEffect(Color.cyan, 3f));
                EffectManager.Instance?.PlayEffect("ice", transform.position);
                if (isHuman)
                {
                    AudioManager.Instance?.PlaySFXOneShot(IceSoundName);
                }
                Debug.Log($"<color=lime> [{RunnerName}] </color> กดใช้: <color=cyan> Ice  </color>");
                break;

            case RaceItemType.Sun:
                if (targetAhead != null)
                {
                    // 🌟 ส่งชื่อตัวเอง (คนยิง) เข้าไปด้วย
                    targetAhead.ApplySunDebuff(RunnerName);
                }
                break;

            case RaceItemType.Gun:
                FireGunProjectile();
                if (isHuman) AudioManager.Instance?.PlaySFXOneShot(GunSoundName);
                break;

            case RaceItemType.Weight:
                if (targetAhead != null)
                {

                    // 🌟 ส่งชื่อตัวเอง (คนยิง) เข้าไปด้วย
                    targetAhead.ApplyWeightDebuff(RunnerName);
                    if (isHuman) AudioManager.Instance?.PlaySFXOneShot(BalloonSoundName);

                    targetAhead.ApplySpeedBoost(0.8f, 3f); // วิ่งได้แค่ 80% เป็นเวลา 3 วินาที
                    targetAhead.colorEffectCoroutine = targetAhead.StartCoroutine(targetAhead.ItemColorEffect(Color.magenta, 3f));
                    if (isHuman)
                    {
                        AudioManager.Instance?.PlaySFXOneShot(BalloonSoundName);
                    }
                    EffectManager.Instance?.PlayEffect("water_drop", targetAhead.transform.position);
                    Debug.Log($"<color=lime> [{RunnerName}] </color> ลดสปีดใส่: <color=magenta> {targetAhead.RunnerName} </color>");
                }
                break;
        }

        currentItem = RaceItemType.None;
        UpdateItemUI();
    }

    private PlayerSplineRunner GetTargetAhead()
    {
        var allRunners = FindObjectsByType<PlayerSplineRunner>(FindObjectsSortMode.None)
            .Where(r => r != null)
            .OrderByDescending(r => r.IsFinished)
            .ThenBy(r => r.IsFinished ? r.FinishTime : float.PositiveInfinity)
            .ThenByDescending(r => r.RaceProgress)
            .ToList();

        int myIndex = allRunners.IndexOf(this);
        if (myIndex > 0)
        {
            return allRunners[myIndex - 1];
        }
        return null;
    }

    private void FireGunProjectile()
    {
        GameObject bullet = new GameObject("GunBullet");
        bullet.transform.position = transform.position;

        SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
        Sprite bulletSprite = Resources.Load<Sprite>("Item/Bullet");

        if (bulletSprite != null)
        {
            sr.sprite = bulletSprite;
            sr.color = Color.white;
        }
        else
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
            sr.color = Color.red;
        }

        sr.sortingOrder = 50;
        bullet.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

        GunBullet logic = bullet.AddComponent<GunBullet>();
        PlayerSplineRunner targetAhead = GetTargetAhead();
        Vector2 fallbackDirection = currentRouteTangent.sqrMagnitude > 0.001f ? currentRouteTangent.normalized : Vector2.right;

        if (currentFacingTangentX < 0) fallbackDirection = -fallbackDirection;

        logic.Initialize(this, targetAhead, fallbackDirection, 20);
    }

    // 🌟 ฟังก์ชันแสดง UI เตือนว่าโดนใครตี
    public void ShowHitFeedback(RaceItemType itemType, string attackerName)
    {
        if (!isHuman) return; // ไม่ต้องโชว์ให้บอทดู

        if (hitFeedbackImage != null)
        {
            Sprite loadedSprite = Resources.Load<Sprite>($"Item/{itemType.ToString()}");
            if (loadedSprite != null)
            {
                hitFeedbackImage.sprite = loadedSprite;
                hitFeedbackImage.enabled = true;
            }
        }

        if (hitFeedbackText != null)
        {
            hitFeedbackText.text = $"From {attackerName}";
            hitFeedbackText.enabled = true;
        }

        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = StartCoroutine(HideFeedbackRoutine());
    }

    private System.Collections.IEnumerator HideFeedbackRoutine()
    {
        yield return new WaitForSeconds(2f); // แสดง 2 วินาที
        if (hitFeedbackImage != null) hitFeedbackImage.enabled = false;
        if (hitFeedbackText != null) hitFeedbackText.enabled = false;
    }

    // 🌟 รับสถานะโดนพระอาทิตย์เผา พร้อมรับชื่อคนยิง
    public void ApplySunDebuff(string attackerName = "")
    {
        if (isHuman)
        {
            AudioManager.Instance?.PlaySFXOneShot(SunSoundName);
        }
        TriggerInstantOverheat();
        coolDownDebuffEndTime = Time.time + 1f;
        if (sunBurnVfx != null) sunBurnVfx.Play();
        //if (colorEffectCoroutine != null) StopCoroutine(colorEffectCoroutine);
        //colorEffectCoroutine = StartCoroutine(ItemColorEffect(new Color(1f, 0.5f, 0f), 1f));

        if (!string.IsNullOrEmpty(attackerName)) ShowHitFeedback(RaceItemType.Sun, attackerName);
    }

    // 🌟 แยกฟังก์ชันของ Weight ออกมาเพื่อให้รับชื่อคนยิงได้
    public void ApplyWeightDebuff(string attackerName = "")
    {
        ApplySpeedBoost(0.8f, 3f);
        //if (colorEffectCoroutine != null) StopCoroutine(colorEffectCoroutine);
        //colorEffectCoroutine = StartCoroutine(ItemColorEffect(Color.magenta, 3f));
        if (weightHeavyVfx != null) weightHeavyVfx.Play();
        if (!string.IsNullOrEmpty(attackerName)) ShowHitFeedback(RaceItemType.Weight, attackerName);

        if (colorEffectCoroutine != null) StopCoroutine(colorEffectCoroutine);
        colorEffectCoroutine = StartCoroutine(ItemColorEffect(new Color(1f, 0.5f, 0f), 1f)); // ตัวเป็นสีส้มแดง
        EffectManager.Instance?.PlayEffect("fire", transform.position);

    }

    public void PlayerRun()
    {
        if (!raceActive || isFinished) return;

        float currentPump = isOverHeated ? (pumpImpulse * overHeatAccelerationMultiplier) : pumpImpulse;
        forwardSpeed = Mathf.Min(maxForwardSpeed * speedBoostMultiplier, forwardSpeed + currentPump);

        if (Time.time > heatImmunityEndTime)
        {
            currentOverHeat = Mathf.Clamp(currentOverHeat + (runHeatGain * currentHeatMultiplier), 0f, maxOverHeat);
            if (currentOverHeat >= maxOverHeat) isOverHeated = true;
        }

        if (isHuman) AudioManager.Instance?.PlaySFXWithPitchVariation(StepSoundName, 0.08f);

        RefreshHumanOverheatAudio();
        SyncSlider();
    }

    public void CoolDown()
    {
        if (!raceActive || isFinished) return;

        float tapAmount = (Time.time < coolDownDebuffEndTime) ? coolDownTapAmount * 0.5f : coolDownTapAmount;
        currentOverHeat = Mathf.Max(0f, currentOverHeat - tapAmount);

        if (isOverHeated && currentOverHeat <= overHeatRecoverThreshold) isOverHeated = false;

        if (isHuman) AudioManager.Instance?.PlaySFXWithPitchVariation(CooldownSoundName, 0.08f);

        RefreshHumanOverheatAudio();
        SyncSlider();
    }

    public bool TryConsumeHazardDrop(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (!hasPendingHazardDrop) return false;

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

        if (currentOverHeat >= maxOverHeat) isOverHeated = true;

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

    public void ResetHeat()
    {
        currentOverHeat = 0f;
        isOverHeated = false;
        RefreshHumanOverheatAudio();
        SyncSlider();
    }

    public void ApplySpeedBoost(float speedMultiplier, float duration)
    {
        speedBoostEndTime = Time.time + duration;
        speedBoostMultiplier = speedMultiplier;
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
        if (route == null) return;

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
            if (isHuman) HandleHumanActionInput();
            else HandleCpuDropInput();

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
        if (isHuman && CooldownAlertPanel != null)
        {
            // ถ้า Overheat ให้เปิด ถ้าไม่ให้ปิด (เขียนแบบสั้น)
            CooldownAlertPanel.SetActive(isOverHeated);
        }
        if (currentOverHeat > 0f)
        {
            float currentPassiveCool = (Time.time < coolDownDebuffEndTime) ? passiveCoolRate * 0.5f : passiveCoolRate;
            currentOverHeat = Mathf.Max(0f, currentOverHeat - currentPassiveCool * Time.deltaTime);
        }
        if (Time.time >= coolDownDebuffEndTime)
        {
            if (sunBurnVfx != null && sunBurnVfx.isPlaying) sunBurnVfx.Stop();
        }

        if (Time.time >= speedBoostEndTime)
        {
            speedBoostMultiplier = 1f;
            speedDampingMultiplier = 1f;
            if (weightHeavyVfx != null && weightHeavyVfx.isPlaying) weightHeavyVfx.Stop();
            if (sodaSpeedVfx != null && sodaSpeedVfx.isPlaying) sodaSpeedVfx.Stop();
        }
        if (Time.time >= heatImmunityEndTime)
        {
            // สั่งปิด Particle ไอเย็น
            if (iceAuraVfx != null && iceAuraVfx.isPlaying) iceAuraVfx.Stop();
        }

        if (heatMultiplierTimer > 0f)
        {
            heatMultiplierTimer -= Time.deltaTime;
            if (heatMultiplierTimer <= 0f) currentHeatMultiplier = 1f;
        }

        if (rearHitImmunityTimer > 0f) rearHitImmunityTimer -= Time.deltaTime;
        if (collisionAnimationTimer > 0f) collisionAnimationTimer -= Time.deltaTime;
        if (hazardDropTimer > 0f) hazardDropTimer -= Time.deltaTime;

        if (isOverHeated && currentOverHeat <= overHeatRecoverThreshold) isOverHeated = false;

        RefreshHumanOverheatAudio();
    }

    private void UpdateMovement(RouteData route)
    {
        float forwardInput = isHuman ? GetHumanForwardInput() : GetCpuForwardInput();
        float accelerationScale = isOverHeated ? overHeatAccelerationMultiplier : 1f;
        float targetTopSpeed = maxForwardSpeed * globalSpeedMultiplier * (isOverHeated ? overHeatTopSpeedMultiplier : 1f) * speedBoostMultiplier;
        float reverseSpeedLimit = maxForwardSpeed * globalSpeedMultiplier * 0.2f;
        float sprintCap = targetTopSpeed * 1.45f;

        forwardSpeed += forwardInput * joystickAcceleration * accelerationScale * Time.deltaTime;
        forwardSpeed = Mathf.Clamp(forwardSpeed, -reverseSpeedLimit, sprintCap);
        forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, speedDamping * speedDampingMultiplier * Time.deltaTime);

        if (forwardSpeed > targetTopSpeed)
        {
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, targetTopSpeed, speedDamping * 1.8f * Time.deltaTime);
        }

        float previousProgress = progress;
        progress = route.closed
            ? Mathf.Repeat(progress + forwardSpeed * Time.deltaTime, 1f)
            : Mathf.Clamp01(progress + forwardSpeed * Time.deltaTime);

        Vector2 routePosition = route.EvaluatePosition(progress);
        Vector2 routeNormal = route.EvaluateNormal(progress);
        Vector2 routeTangent = route.EvaluateTangent(progress);

        float lateralInput = isHuman ? GetHumanLateralInput(routeNormal) : ResolveScreenConsistentHorizontal(GetCpuHorizontalInput(), route);

        float offsetLimit = Mathf.Max(0.15f, route.trackWidth * lateralTrackPadding * 0.5f);
        if (Mathf.Abs(lateralInput) > 0.01f) lateralOffset += lateralInput * lateralMoveSpeed * Time.deltaTime;

        lateralOffset = Mathf.Clamp(lateralOffset, -offsetLimit, offsetLimit);

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
        if (!route.closed) return;

        if (lastProgressSample > 0.85f && progress < 0.15f && forwardSpeed > 0f)
        {
            completedLaps++;
            LapCompleted?.Invoke(this);
            if (isHuman) AudioManager.Instance?.PlaySFXOneShot(RoundSoundName);

            if (completedLaps >= targetLapCount) FinishRace();
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
        if (isHuman) AudioManager.Instance?.PlaySFXOneShot(WinSoundName);
    }

    private float GetHumanForwardInput() => 0f;

    private float GetHumanLateralInput(Vector2 routeNormal)
    {
        Vector2 inputDir = Vector2.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) { inputDir.y += 1f; }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) { inputDir.y -= 1f; }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) { inputDir.x += 1f; }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) { inputDir.x -= 1f; }

        if (joystick != null)
        {
            inputDir.x += joystick.Horizontal;
            inputDir.y += joystick.Vertical;
        }

        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        return Vector2.Dot(inputDir, routeNormal);
    }

    private void HandleHumanActionInput()
    {
        if (Input.GetKeyDown(runKey) || Input.GetKeyDown(altRunKey)) PlayerRun();
        if (Input.GetKeyDown(coolDownKey) || Input.GetKeyDown(altCoolDownKey)) CoolDown();
        if (Input.GetKeyDown(useItemKey) || Input.GetKeyDown(altUseItemKey)) UseItem();
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

        if (isOverHeated || heatRatio >= aiEmergencyHeatRatio || heatRatio >= aiDangerHeatRatio)
        {
            if (isOverHeated || heatRatio >= aiEmergencyHeatRatio) aiRestTimer = Random.Range(aiRestDurationRange.x, aiRestDurationRange.y);

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
        if (aiDropTimer < aiDropCooldown) return;

        aiDropTimer = 0f;
        aiDropCooldown = Random.Range(aiDropIntervalRange.x, aiDropIntervalRange.y);

        if (currentOverHeat < maxOverHeat * 0.88f) UseItem();
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
        if (!screenSpaceLateralControls || Mathf.Abs(horizontalInput) <= 0.01f) return horizontalInput;

        Vector2 routeNormal = route.EvaluateNormal(progress);
        float horizontalSign = Vector2.Dot(routeNormal, Vector2.right);

        if (Mathf.Abs(horizontalSign) > 0.15f) lastHorizontalSign = Mathf.Sign(horizontalSign);

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

    private float GetNextRandomHeatCooldown() => Random.Range(randomHeatBurstIntervalRange.x, randomHeatBurstIntervalRange.y);

    private void UpdateVisuals()
    {
        UpdateAnimationState();
        UpdateSpriteFacing();
    }

    private void SyncSlider()
    {
        if (overheatSlider == null) return;
        overheatSlider.gameObject.SetActive(isHuman);
        overheatSlider.minValue = 0f;
        overheatSlider.maxValue = maxOverHeat;
        overheatSlider.value = currentOverHeat;
    }

    private void SnapToRoute()
    {
        RouteData route = routesRenderer != null ? routesRenderer.GetRouteData() : null;
        if (route == null) return;

        Vector2 routePosition = route.EvaluatePosition(progress);
        Vector2 routeNormal = route.EvaluateNormal(progress);
        currentRouteTangent = route.EvaluateTangent(progress);

        if (currentRouteTangent.sqrMagnitude > 0.001f) currentFacingTangentX = currentRouteTangent.normalized.x;

        Vector2 finalPosition = routePosition + routeNormal * lateralOffset;
        transform.position = new Vector3(finalPosition.x, finalPosition.y, transform.position.z);
    }

    private void QueueHazardDrop()
    {
        if (!raceActive || hazardDropTimer > 0f || isFinished) return;
        hasPendingHazardDrop = true;
        hazardDropTimer = hazardDropCooldown;
    }

    private void RefreshHumanOverheatAudio()
    {
        if (!isHuman || AudioManager.Instance == null) return;

        if (raceActive && !isFinished && isOverHeated) AudioManager.Instance.PlayLoop(GaspSoundName, HumanGaspChannel);
        else StopHumanGaspLoop();
    }

    private void StopHumanGaspLoop()
    {
        if (!isHuman || AudioManager.Instance == null) return;
        AudioManager.Instance.StopLoop(HumanGaspChannel);
    }

    private void TriggerCollisionAnimation(float durationOverride = -1f)
    {
        float duration = durationOverride > 0f ? durationOverride : collisionAnimationDuration;
        collisionAnimationTimer = Mathf.Max(collisionAnimationTimer, duration);
    }

    private void UpdateAnimationState()
    {
        if (!useAnimationSet) return;

        string targetState = IdleAnimationStateName;
        if (collisionAnimationTimer > 0f || isOverHeated) targetState = OverheatAnimationStateName;
        else if (raceActive && !isFinished && Mathf.Abs(forwardSpeed) > runAnimationSpeedThreshold) targetState = RunAnimationStateName;

        if (currentAnimationState == targetState) return;

        if (character != null) character.PlayAnimation(targetState);
        currentAnimationState = targetState;
    }

    private void UpdateSpriteFacing()
    {
        if (character == null) return;

        if (currentFacingTangentX >= tangentFlipThreshold) character.SetFlipX(defaultSpriteFlipX);
        else if (currentFacingTangentX <= -tangentFlipThreshold) character.SetFlipX(!defaultSpriteFlipX);
    }

    public System.Collections.IEnumerator ItemColorEffect(Color effectColor, float duration)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Color[] originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
            renderers[i].color = effectColor;
        }

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].color = originalColors[i];
        }
    }
    private ParticleSystem FindChildParticleByName(string childName)
    {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps.gameObject.name == childName) return ps;
        }
        return null;
    }
}

// 🌟 ระบบกระสุนปืน ยิงเป็นเส้นตรง และโชว์ Feedback
public class GunBullet : MonoBehaviour
{
    public PlayerSplineRunner owner;
    public PlayerSplineRunner target;
    public Vector2 fallbackDirection;
    public float speed;
    private float lifeTimer = 2.5f;

    public void Initialize(PlayerSplineRunner owner, PlayerSplineRunner target, Vector2 fallbackDir, float spd)
    {
        this.owner = owner;
        this.target = target;
        this.fallbackDirection = fallbackDir.normalized;
        this.speed = spd;
    }

    void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 moveDirection;

        if (target != null && !target.IsFinished)
        {
            moveDirection = (target.transform.position - transform.position).normalized;
        }
        else
        {
            moveDirection = fallbackDirection;
        }

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        transform.position += moveDirection * speed * Time.deltaTime;

        foreach (var runner in FindObjectsByType<PlayerSplineRunner>(FindObjectsSortMode.None))
        {
            if (runner == owner || runner.IsFinished) continue;

            if (Vector2.Distance(transform.position, runner.transform.position) < 0.8f)
            {
                EffectManager.Instance?.PlayEffect("water_splash", runner.transform.position);
                runner.TriggerInstantOverheat();

                // 🌟 เมื่อยิงโดน สั่งให้แสดง UI Feedback และส่งชื่อเจ้าของปืนไปบอก
                runner.ShowHitFeedback(RaceItemType.Gun, owner.RunnerName);

                Destroy(gameObject);
                return;
            }
        }

    }

}