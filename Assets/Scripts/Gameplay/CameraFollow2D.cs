using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
        SnapToTarget();
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

        transform.position = target.position + offset;
    }
}
