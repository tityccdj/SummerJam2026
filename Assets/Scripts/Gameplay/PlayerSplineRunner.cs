using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerSplineRunner : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private RoutesRenderer routesRenderer;
    [SerializeField] private Joystick joystick;
    [SerializeField] private Slider overheatSlider;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Movement")]
    [SerializeField] private float pumpImpulse = 0.1f;
    [SerializeField] private float joystickAcceleration = 0.08f;
    [SerializeField] private float maxForwardSpeed = 0.35f;
    [SerializeField] private float speedDamping = 0.18f;
    [SerializeField] private float lateralMoveSpeed = 2.4f;
    [SerializeField] private float lateralReturnSpeed = 1.4f;
    [SerializeField] private float lateralTrackPadding = 0.75f;

    [Header("OverHeat")]
    [SerializeField] private float maxOverHeat = 100f;
    [SerializeField] private float runHeatGain = 12f;
    [SerializeField] private float passiveCoolRate = 6f;
    [SerializeField] private float coolDownTapAmount = 9f;
    [SerializeField] private float overHeatRecoverThreshold = 35f;
    [SerializeField] private Color overHeatColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float flashSpeed = 14f;

    private float progress;
    private float forwardSpeed;
    private float lateralOffset;
    private float currentOverHeat;
    private bool isOverHeated;
    private Color defaultColor = Color.white;

    public void BindSceneReferences(RoutesRenderer renderer, Joystick movementJoystick, Slider heatSlider)
    {
        routesRenderer = renderer;
        joystick = movementJoystick;
        overheatSlider = heatSlider;
        CacheComponents();
        SyncSlider();
        SnapToRoute();
    }

    public void PlayerRun()
    {
        if (isOverHeated)
        {
            return;
        }

        forwardSpeed = Mathf.Min(maxForwardSpeed, forwardSpeed + pumpImpulse);
        currentOverHeat = Mathf.Clamp(currentOverHeat + runHeatGain, 0f, maxOverHeat);

        if (currentOverHeat >= maxOverHeat)
        {
            isOverHeated = true;
        }

        SyncSlider();
    }

    public void CoolDown()
    {
        currentOverHeat = Mathf.Max(0f, currentOverHeat - coolDownTapAmount);

        if (isOverHeated && currentOverHeat <= overHeatRecoverThreshold)
        {
            isOverHeated = false;
        }

        SyncSlider();
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void Start()
    {
        SyncSlider();
        SnapToRoute();
    }

    private void Update()
    {
        RouteData route = routesRenderer != null ? routesRenderer.GetRouteData() : null;
        if (route == null)
        {
            return;
        }

        ApplyHeat();
        UpdateMovement(route);
        UpdateVisuals();
        SyncSlider();
    }

    private void CacheComponents()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            defaultColor = spriteRenderer.color;
        }
    }

    private void ApplyHeat()
    {
        if (currentOverHeat > 0f)
        {
            currentOverHeat = Mathf.Max(0f, currentOverHeat - passiveCoolRate * Time.deltaTime);
        }

        if (isOverHeated && currentOverHeat <= overHeatRecoverThreshold)
        {
            isOverHeated = false;
        }
    }

    private void UpdateMovement(RouteData route)
    {
        float joystickVertical = joystick != null ? joystick.Vertical : 0f;
        float joystickHorizontal = joystick != null ? joystick.Horizontal : 0f;
        float accelerationScale = isOverHeated ? 0.15f : 1f;

        forwardSpeed += joystickVertical * joystickAcceleration * accelerationScale * Time.deltaTime;
        forwardSpeed = Mathf.Clamp(forwardSpeed, -maxForwardSpeed * 0.35f, maxForwardSpeed);

        if (Mathf.Abs(joystickVertical) < 0.01f)
        {
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, speedDamping * Time.deltaTime);
        }

        progress = route.closed ? Mathf.Repeat(progress + forwardSpeed * Time.deltaTime, 1f) : Mathf.Clamp01(progress + forwardSpeed * Time.deltaTime);

        float offsetLimit = Mathf.Max(0.15f, route.trackWidth * lateralTrackPadding * 0.5f);
        if (Mathf.Abs(joystickHorizontal) > 0.01f)
        {
            lateralOffset += joystickHorizontal * lateralMoveSpeed * Time.deltaTime;
        }
        else
        {
            lateralOffset = Mathf.MoveTowards(lateralOffset, 0f, lateralReturnSpeed * Time.deltaTime);
        }

        lateralOffset = Mathf.Clamp(lateralOffset, -offsetLimit, offsetLimit);

        Vector2 routePosition = route.EvaluatePosition(progress);
        Vector2 routeNormal = route.EvaluateNormal(progress);
        Vector2 routeTangent = route.EvaluateTangent(progress);
        Vector2 finalPosition = routePosition + routeNormal * lateralOffset;

        transform.position = new Vector3(finalPosition.x, finalPosition.y, transform.position.z);

        if (routeTangent.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(routeTangent.y, routeTangent.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (isOverHeated)
        {
            float lerp = Mathf.PingPong(Time.time * flashSpeed, 1f);
            spriteRenderer.color = Color.Lerp(defaultColor, overHeatColor, lerp);
        }
        else
        {
            float heatRatio = maxOverHeat > 0f ? currentOverHeat / maxOverHeat : 0f;
            spriteRenderer.color = Color.Lerp(defaultColor, overHeatColor, heatRatio * 0.35f);
        }
    }

    private void SyncSlider()
    {
        if (overheatSlider == null)
        {
            return;
        }

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
        transform.position = new Vector3(routePosition.x, routePosition.y, transform.position.z);
    }
}
