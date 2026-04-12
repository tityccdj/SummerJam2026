using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class RunnerNameTag : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.9f, 0f);
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 18f);

    private Transform target;
    private Camera targetCamera;
    private TextMeshProUGUI label;
    private RectTransform rectTransform;

    public void Bind(Transform followTarget, string displayName, Camera worldCamera = null)
    {
        target = followTarget;
        targetCamera = worldCamera != null ? worldCamera : Camera.main;

        if (label == null)
        {
            label = GetComponent<TextMeshProUGUI>();
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (label != null)
        {
            label.text = displayName;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                return;
            }
        }

        Vector3 screenPosition = targetCamera.WorldToScreenPoint(target.position + worldOffset);
        bool visible = screenPosition.z > 0f;

        if (label != null && label.gameObject.activeSelf != visible)
        {
            label.gameObject.SetActive(visible);
        }

        if (!visible || rectTransform == null)
        {
            return;
        }

        rectTransform.position = screenPosition + (Vector3)screenOffset;
    }
}
