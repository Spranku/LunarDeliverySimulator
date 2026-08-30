using UnityEngine;
using System.Collections;

public class RoverVisual : MonoBehaviour
{
    [Header("References")]
    public RoverData data;
    public Transform moonSurface;

    [Header("Movement")]
    public float baseMoveSpeed = 0.1f;
    public float maxWeightPenalty = 0.5f;
    public float weightThreshold = 100f;
    public float moveSpeedMultiplier = 1f;

    private float currentMoveSpeed = 0.1f;
    private bool isMoving = false;
    private Vector3 startPosition;
    private float moveProgress = 0f;
    private bool isReturning = false;
    private Vector3 basePosition;
    private Transform targetTransform;
    private Vector3 targetPosition;

    public System.Action onDeliveryComplete;
    public System.Action onReachedDestination;
    public System.Action onRoverDestroyed;

    private bool isDestroyed = false;
    private float riskCheckThreshold = 0.7f;
    private bool riskChecked = false;

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
        if (isDestroyed) return;
        if (!isMoving) return;

        if (!isReturning && targetTransform != null)
        {
            targetPosition = targetTransform.position;
        }

        /* Используем currentMoveSpeed вместо actualMoveSpeed */
        float speed = currentMoveSpeed * moveSpeedMultiplier;
        moveProgress += Time.deltaTime * speed;

        if (!isReturning && !isDestroyed && moveProgress >= riskCheckThreshold && !riskChecked)
        {
            CheckRisk(true);
            riskChecked = true;
        }

        if (isReturning && !isDestroyed && moveProgress >= riskCheckThreshold && !riskChecked)
        {
            CheckRisk(false);
            riskChecked = true;
        }

        if (moveProgress >= 1f)
        {
            moveProgress = 1f;

            Vector3 finalPos = GetPositionOnSurface(targetPosition);
            transform.position = finalPos;
            data.CurrentPosition = finalPos;

            if (isDestroyed)
            {
                isMoving = false;
                return;
            }

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

    void CheckRisk(bool onWayToOrder)
    {
        if (isDestroyed) return;
        if (data == null) return;

        float risk = 0f;
        string zoneName = "Unknown";

        if (targetTransform != null)
        {
            OrderPoint3D point = targetTransform.GetComponent<OrderPoint3D>();
            if (point != null && point.Order != null)
            {
                risk = point.Order.Risk;
                zoneName = point.Order.ZoneType;
            }
        }

        float roll = Random.Range(0f, 1f);

        Debug.Log($"⚠️ {data.Name}: zone={zoneName}, risk={risk * 100:F2}%, roll={roll * 100:F2}%");

        if (roll < risk)
        {
            isDestroyed = true;
            data.IsDestroyed = true;
            data.IsBusy = false;
            isMoving = false;
            riskChecked = true;

            string location = onWayToOrder ? "on the way to order" : "on the way back to base";
            Debug.Log($"💥 {data.Name} DESTROYED! (risk: {risk * 100:F2}%, roll: {roll * 100:F2}%)");

            StartCoroutine(DestroyRoverWithEffect());
            onRoverDestroyed?.Invoke();
        }
        else
        {
            Debug.Log($"✅ {data.Name} survived! (risk: {risk * 100:F2}%, roll: {roll * 100:F2}%)");
        }
    }

    IEnumerator DestroyRoverWithEffect()
    {
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }

    private Vector3 GetPositionOnSurface(Vector3 worldPos)
    {
        if (moonSurface == null) return worldPos;

        Vector3 direction = (worldPos - moonSurface.position).normalized;
        float radius = moonSurface.localScale.x * 0.5f;
        return moonSurface.position + direction * radius;
    }

    public void MoveTo(Transform target, float orderWeight)
    {
        if (isMoving) return;
        if (data.IsDestroyed) return;

        targetTransform = target;
        targetPosition = GetPositionOnSurface(target.position);

        /* ===== Speed by weight ===== */
        float weightFactor = Mathf.Clamp01(orderWeight / weightThreshold);
        float speedPenalty = weightFactor * maxWeightPenalty;
        currentMoveSpeed = baseMoveSpeed * (1f - speedPenalty);
        currentMoveSpeed = Mathf.Max(currentMoveSpeed, 0.01f);

        Debug.Log($"🚀 {data.Name}: weight={orderWeight}kg, penalty={speedPenalty * 100:F0}%, speed={currentMoveSpeed:F3}");

        isMoving = true;
        isReturning = false;
        startPosition = GetPositionOnSurface(data.CurrentPosition);
        moveProgress = 0f;
        data.IsBusy = true;
        isDestroyed = false;
        riskChecked = false;
        gameObject.SetActive(true);
    }

    IEnumerator ReturnToBase()
    {
        yield return new WaitForSeconds(0.2f);

        if (isDestroyed) yield break;

        startPosition = transform.position;
        targetPosition = GetPositionOnSurface(basePosition);

        moveProgress = 0f;
        isMoving = true;
        isReturning = true;
        riskChecked = false;
        targetTransform = null;

        Debug.Log($"↩️ {data.Name} returning to base with speed {currentMoveSpeed:F3}");
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