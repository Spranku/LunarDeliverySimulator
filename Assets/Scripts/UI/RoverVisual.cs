using UnityEngine;
using System.Collections;

public class RoverVisual : MonoBehaviour
{
    [Header("References")]
    public RoverData data;
    public Transform moonSurface;

    [Header("Movement")]
    [Tooltip("Speed multiplier: 1 = 0.1 actual speed")]
    public float moveSpeedMultiplier = 1f;

    private float actualMoveSpeed = 0.1f; 
    private bool isMoving = false;
    private Vector3 startPosition;
    private float moveProgress = 0f;
    private bool isReturning = false;
    private Vector3 basePosition;
    private Transform targetTransform;
    private Vector3 targetPosition;

    public System.Action onDeliveryComplete;
    public System.Action onReachedDestination;

    public void Initialize(RoverData roverData, Transform moon, Vector3 basePos)
    {
        data = roverData;
        moonSurface = moon;
        basePosition = basePos;
        transform.position = data.CurrentPosition;
        UpdateVisual();
    }

    void Update()
    {
        if (!isMoving) return;

        if (!isReturning && targetTransform != null)
        {
            targetPosition = targetTransform.position;
        }

        
        float speed = actualMoveSpeed * moveSpeedMultiplier;
        moveProgress += Time.deltaTime * speed;

        if (moveProgress >= 1f)
        {
            moveProgress = 1f;

            Vector3 finalPos = GetPositionOnSurface(targetPosition);

            transform.position = finalPos;
            data.CurrentPosition = finalPos;

            if (isReturning)
            {
                isMoving = false;
                isReturning = false;
                data.IsBusy = false;
                onDeliveryComplete?.Invoke();
                gameObject.SetActive(false);
            }
            else
            {
                isMoving = false;
                onReachedDestination?.Invoke();
                StartCoroutine(ReturnToBase());
            }
            return;
        }

        float t = Mathf.SmoothStep(0f, 1f, moveProgress);
        Vector3 pos = Vector3.Lerp(startPosition, targetPosition, t);

        pos = GetPositionOnSurface(pos);

        transform.position = pos;

        Vector3 direction = (targetPosition - startPosition).normalized;
        if (direction.magnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction, (pos - moonSurface.position).normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
        else
        {
            transform.LookAt(moonSurface.position);
        }
    }

    private Vector3 GetPositionOnSurface(Vector3 worldPos)
    {
        if (moonSurface == null) return worldPos;

        Vector3 direction = (worldPos - moonSurface.position).normalized;
        float radius = moonSurface.localScale.x * 0.5f;
        return moonSurface.position + direction * radius;
    }

    public void MoveTo(Transform target)
    {
        if (isMoving) return;

        targetTransform = target;
        targetPosition = GetPositionOnSurface(target.position);
        isMoving = true;
        isReturning = false;
        startPosition = GetPositionOnSurface(data.CurrentPosition);
        moveProgress = 0f;
        data.IsBusy = true;
        gameObject.SetActive(true);
    }

    IEnumerator ReturnToBase()
    {
        yield return new WaitForSeconds(0.2f);

        startPosition = transform.position;
        targetPosition = GetPositionOnSurface(basePosition);

        moveProgress = 0f;
        isMoving = true;
        isReturning = true;
        targetTransform = null;
    }

    void UpdateVisual()
    {
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