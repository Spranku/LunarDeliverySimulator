using UnityEngine;
using System.Collections;

public class RoverVisual : MonoBehaviour
{
    [Header("References")]
    public RoverData data;
    public Transform moonSurface;
    public float moveSpeed = 2f;

    private bool isMoving = false;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private float moveProgress = 0f;
    private bool isReturning = false;

    public System.Action onDeliveryComplete; 

    public void Initialize(RoverData roverData, Transform moon)
    {
        data = roverData;
        moonSurface = moon;
        transform.position = data.CurrentPosition;
        UpdateVisual();
    }

    void Update()
    {
        if (isMoving)
        {
            moveProgress += Time.deltaTime * moveSpeed;

            if (moveProgress >= 1f)
            {
                moveProgress = 1f;
                isMoving = false;
                transform.position = targetPosition;
                data.CurrentPosition = targetPosition;

                if (isReturning)
                {
                    /* Returned */
                    data.IsBusy = false;
                    isReturning = false;
                    Debug.Log($"Rover {data.Name} returned to base!");
                }
                else
                {
                    /* Return after delivery */
                    StartCoroutine(ReturnToBase());
                }
                return;
            }

            float t = Mathf.SmoothStep(0f, 1f, moveProgress);
            Vector3 pos = Vector3.Lerp(startPosition, targetPosition, t);

            /* Rover on sphere */
            Vector3 direction = (pos - moonSurface.position).normalized;
            float radius = moonSurface.localScale.x * 0.5f + 0.1f;
            pos = moonSurface.position + direction * radius;

            transform.position = pos;
            transform.LookAt(moonSurface.position);
        }
    }

    public void MoveTo(Vector3 target)
    {
        if (isMoving) return;

        isMoving = true;
        isReturning = false;
        startPosition = data.CurrentPosition;
        targetPosition = target;
        moveProgress = 0f;
        data.IsBusy = true;
    }

    IEnumerator ReturnToBase()
    {
        yield return new WaitForSeconds(0.5f);

        isMoving = true;
        isReturning = true;
        startPosition = data.CurrentPosition;
        targetPosition = GetBasePosition();
        moveProgress = 0f;

        if (isReturning)
        {
            data.IsBusy = false;
            isReturning = false;
            Debug.Log($"Rover {data.Name} returned to base!");

            onDeliveryComplete?.Invoke();
        }
    }

    Vector3 GetBasePosition()
    {
        BasePoint basePoint = FindFirstObjectByType<BasePoint>();
        if (basePoint != null)
        {
            return basePoint.transform.position;
        }
        return Vector3.zero;
    }

    void UpdateVisual()
    {
        /* Change color */
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            if (data.IsDestroyed)
                rend.material.color = Color.red;
            else if (data.IsBusy)
                rend.material.color = Color.yellow;
            else
                rend.material.color = Color.green;
        }
    }
}