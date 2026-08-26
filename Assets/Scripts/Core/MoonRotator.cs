using UnityEngine;

public class MoonRotator : MonoBehaviour
{
    [Header("Rotation")]
    public float rotationSpeed = 1f;
    public bool autoRotate = true;

    void Update()
    {
        if (autoRotate)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}