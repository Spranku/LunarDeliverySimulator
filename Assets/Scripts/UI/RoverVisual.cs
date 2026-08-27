using UnityEngine;
using System.Collections;

public class RoverVisual : MonoBehaviour
{
    [Header("References")]
    public RoverData data;
    public Transform moonSurface;
    public float moveSpeed = 0.5f;

    private bool isMoving = false;
    private Vector3 startPosition;
    private float moveProgress = 0f;
    private bool isReturning = false;
    private Vector3 basePosition;
    private Transform targetTransform; 
    private Vector3 fixedTargetPosition; 

    public System.Action onDeliveryComplete;

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
           /* Update position of order point */
            fixedTargetPosition = targetTransform.position;
        }

        moveProgress += Time.deltaTime * moveSpeed;

        if (moveProgress >= 1f)
        {
            moveProgress = 1f;
            isMoving = false;
            transform.position = fixedTargetPosition;
            data.CurrentPosition = fixedTargetPosition;

            if (isReturning)
            {
                /* Returned to the base */
                data.IsBusy = false;
                isReturning = false;
                Debug.Log($"Rover {data.Name} returned to base!");
                onDeliveryComplete?.Invoke();
                gameObject.SetActive(false);
            }
            else
            {
                /* Start back rover */
                Debug.Log($"Rover {data.Name} arrived at destination, returning to base...");
                StartCoroutine(ReturnToBase());
            }
            return;
        }

        float t = Mathf.SmoothStep(0f, 1f, moveProgress);
        Vector3 pos = Vector3.Lerp(startPosition, fixedTargetPosition, t);

        /* Rover on the moon surface */
        Vector3 direction = (pos - moonSurface.position).normalized;
        float radius = moonSurface.localScale.x * 0.5f + 0.1f;
        pos = moonSurface.position + direction * radius;

        transform.position = pos;
        transform.LookAt(moonSurface.position);
    }

    public void MoveTo(Transform target)
    {
        if (isMoving) return;

        targetTransform = target;
        fixedTargetPosition = target.position;
        isMoving = true;
        isReturning = false;
        startPosition = data.CurrentPosition;
        moveProgress = 0f;
        data.IsBusy = true;
        gameObject.SetActive(true);
    }

    IEnumerator ReturnToBase()
    {
        yield return new WaitForSeconds(0.3f);

        targetTransform = null; 
        fixedTargetPosition = basePosition;
        startPosition = data.CurrentPosition;
        moveProgress = 0f;
        isMoving = true;
        isReturning = true;

        Debug.Log($"Rover {data.Name} returning to base...");
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