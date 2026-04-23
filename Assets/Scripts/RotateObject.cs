using UnityEngine;

public class RotateObject : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 20f;

    void Update()
    {
        // rotate z
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}
