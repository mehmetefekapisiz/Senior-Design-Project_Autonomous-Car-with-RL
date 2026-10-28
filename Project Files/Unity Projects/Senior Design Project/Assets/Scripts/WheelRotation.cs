using UnityEngine;

public class WheelRotation : MonoBehaviour
{
    [Header("Wheels")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    [Header("Settings")]
    public float wheelRadius = 0.3f;

    private CarAgent carAgent;

    void Start()
    {
        carAgent = GetComponentInParent<CarAgent>();
    }

    void Update()
    {
        if (carAgent == null) return;

        float speed = carAgent.GetCurrentSpeed();
        float rotationAngle = (speed / wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;

        frontLeftWheel.Rotate(rotationAngle, 0f, 0f);
        frontRightWheel.Rotate(rotationAngle, 0f, 0f);
        rearLeftWheel.Rotate(rotationAngle, 0f, 0f);
        rearRightWheel.Rotate(rotationAngle, 0f, 0f);
    }
}