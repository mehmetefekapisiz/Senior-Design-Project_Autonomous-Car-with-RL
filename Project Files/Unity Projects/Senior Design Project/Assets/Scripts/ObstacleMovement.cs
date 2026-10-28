using UnityEngine;

public class ObstacleMovement : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float moveDistance = 4f;

    private Vector3 startPosition;
    private int direction = 1;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        transform.position += transform.right * moveSpeed * direction * Time.deltaTime;

        float distance = Vector3.Distance(transform.position, startPosition);
        if (distance >= moveDistance)
        {
            direction *= -1;
        }
    }
}