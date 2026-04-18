using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float shakeFrequency = 34f;

    private float shakeTimer;
    private float shakeDuration;
    private float shakeMagnitude;
    private float shakeSeed;

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
        SnapToTarget();
    }

    public void TriggerShake(float magnitude, float duration)
    {
        if (magnitude <= 0f || duration <= 0f)
        {
            return;
        }

        shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
        shakeDuration = Mathf.Max(shakeDuration, duration);
        shakeTimer = Mathf.Max(shakeTimer, duration);
        shakeSeed = Random.Range(-1000f, 1000f);
    }

    private void LateUpdate()
    {
        SnapToTarget();
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 finalOffset = offset;

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float normalizedTime = 1f - Mathf.Clamp01(shakeTimer / shakeDuration);
            float damping = 1f - normalizedTime;
            float sampleX = Mathf.PerlinNoise(shakeSeed, Time.time * shakeFrequency) - 0.5f;
            float sampleY = Mathf.PerlinNoise(Time.time * shakeFrequency, shakeSeed) - 0.5f;
            finalOffset += new Vector3(sampleX, sampleY, 0f) * (shakeMagnitude * 2f * damping);

            if (shakeTimer <= 0f)
            {
                shakeDuration = 0f;
                shakeMagnitude = 0f;
            }
        }

        transform.position = target.position + finalOffset;
    }
}
