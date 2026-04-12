using UnityEngine;

[DisallowMultipleComponent]
public class RaceHazard2D : MonoBehaviour
{
    private PlayerSplineRunner owner;
    private float lifeTimer;
    private float triggerRadius;
    private float heatAmount;
    private float heatMultiplier;
    private float heatDuration;

    public PlayerSplineRunner Owner => owner;
    public float TriggerRadius => triggerRadius;

    public void Initialize(PlayerSplineRunner hazardOwner, float lifetime, float radius, float extraHeat, float extraHeatMultiplier, float extraHeatDuration)
    {
        owner = hazardOwner;
        lifeTimer = lifetime;
        triggerRadius = radius;
        heatAmount = extraHeat;
        heatMultiplier = extraHeatMultiplier;
        heatDuration = extraHeatDuration;
    }

    public bool CanHit(PlayerSplineRunner runner)
    {
        return runner != null && runner != owner && !runner.IsFinished;
    }

    public void ApplyTo(PlayerSplineRunner runner)
    {
        if (!CanHit(runner))
        {
            return;
        }

        runner.ApplyExternalHeat(heatAmount, heatMultiplier, heatDuration);
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
