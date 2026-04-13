using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RaceObstacle2D : MonoBehaviour
{
    private float triggerRadius;
    private readonly List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();

    public float TriggerRadius => triggerRadius;
    public bool IsAnimating { get; private set; }

    public void Initialize(float radius)
    {
        triggerRadius = radius;
        spriteRenderers.Clear();
        spriteRenderers.AddRange(GetComponentsInChildren<SpriteRenderer>(true));
    }

    public void AnimateAwayAndDestroy(float duration)
    {
        if (IsAnimating)
        {
            return;
        }

        StartCoroutine(AnimateRoutine(Mathf.Max(0.15f, duration)));
    }

    private IEnumerator AnimateRoutine(float duration)
    {
        IsAnimating = true;

        Camera mainCamera = Camera.main;
        Vector3 startPosition = transform.position;
        Vector3 randomDirection = Random.insideUnitCircle.normalized;
        if (randomDirection.sqrMagnitude < 0.001f)
        {
            randomDirection = new Vector3(1f, 0.6f, 0f).normalized;
        }

        float travelDistance = 12f;
        if (mainCamera != null)
        {
            float halfHeight = mainCamera.orthographicSize;
            float halfWidth = halfHeight * mainCamera.aspect;
            travelDistance = Mathf.Max(halfWidth, halfHeight) * 2.6f;
        }

        Vector3 targetPosition = startPosition + randomDirection * travelDistance;
        float spinDirection = Random.value < 0.5f ? -1f : 1f;
        float startRotation = transform.eulerAngles.z;
        float targetRotation = startRotation + spinDirection * Random.Range(360f, 720f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            transform.position = Vector3.Lerp(startPosition, targetPosition, eased);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startRotation, targetRotation, eased));

            for (int i = 0; i < spriteRenderers.Count; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];
                if (spriteRenderer == null)
                {
                    continue;
                }

                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = color;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
