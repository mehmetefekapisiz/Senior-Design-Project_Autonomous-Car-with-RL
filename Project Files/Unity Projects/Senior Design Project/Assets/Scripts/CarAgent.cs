using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CarAgent : Agent
{
    [Header("Movement")]
    public float maxSpeed = 10f;
    public float accelerationForce = 5f;
    public float brakeForce = 8f;
    public float turnSpeed = 100f;

    [Header("Waypoints")]
    public Transform[] waypoints;
    private int nextWaypointIndex = 0;

    [Header("Finish")]
    public Transform finish;

    private Rigidbody rb;
    private Vector3 startPosition;
    private Quaternion startRotation; // x, y, z, w
    private float currentSpeed = 0f;
    private float previousDistance;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;

        /*
        // Parallel Training için;

        // Kendi Environment'ından waypoint ve finish'i otomatik bul
        Transform env = transform.parent;

        // Waypoint'leri sıraya göre bul
        List<Transform> foundWaypoints = new List<Transform>();
        for (int i = 1; i <= 10; i++)
        {
            Transform wp = env.Find("Waypoint (" + i + ")");
            if (wp != null) foundWaypoints.Add(wp);
        }
        waypoints = foundWaypoints.ToArray();

        // Finish'i bul
        finish = env.Find("Finish");
        */
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.localPosition = startPosition;
        transform.localRotation = startRotation;
        currentSpeed = 0f;
        nextWaypointIndex = 0;

        Transform target;
        if (waypoints.Length > 0)
            target = waypoints[0];
        else
            target = finish;
        if (target != null)
            previousDistance = Vector3.Distance(transform.localPosition, target.position);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(rb.linearVelocity.x);
        sensor.AddObservation(rb.linearVelocity.z);
        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);

        // Tüm waypoint'ler geçildiyse Finish'e yönel
        Transform target;
        if (nextWaypointIndex < waypoints.Length)
            target = waypoints[nextWaypointIndex];
        else
            target = finish;

        if (target != null)
        {
            // Sıradaki waypoint'e yön
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            sensor.AddObservation(dirToTarget.x);
            sensor.AddObservation(dirToTarget.z);

            // Dot Product
            float dot = Vector3.Dot(transform.right, dirToTarget);
            sensor.AddObservation(dot);

            // Arabanın waypoint'e olan mesafesi (normalize edilmiş)
            float distance = Vector3.Distance(transform.position, target.position);
            sensor.AddObservation(Mathf.Clamp01(distance / 50f));
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveInput = actions.ContinuousActions[0];
        float turnInput = actions.ContinuousActions[1];

        if (moveInput > 0f)
            currentSpeed += accelerationForce * moveInput * Time.fixedDeltaTime;
        else if (moveInput < 0f)
            currentSpeed += brakeForce * moveInput * Time.fixedDeltaTime;
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, 4f * Time.fixedDeltaTime);

        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed * 0.5f, maxSpeed);

        Vector3 move = transform.forward * currentSpeed * Time.fixedDeltaTime; // Araç hareketi.
        rb.MovePosition(rb.position + move);

        float speedFactor = Mathf.Abs(currentSpeed) / maxSpeed;
        float turn = turnInput * turnSpeed * speedFactor * Time.fixedDeltaTime; // Araç ne kadar hızlıysa o kadar iyi döner.
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f)); // Sadece y ekseninde döner.

        Transform target;
        if (nextWaypointIndex < waypoints.Length)
            target = waypoints[nextWaypointIndex];
        else
            target = finish;

        if (target != null)
        {
            float currentDistance = Vector3.Distance(transform.localPosition, target.position);
            float distanceDelta = previousDistance - currentDistance;
            AddReward(distanceDelta * 0.1f);
            previousDistance = currentDistance;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        actions[0] = 0f;
        actions[1] = 0f;

        if (Keyboard.current.wKey.isPressed) actions[0] = 1f;
        if (Keyboard.current.sKey.isPressed) actions[0] = -1f;
        if (Keyboard.current.dKey.isPressed) actions[1] = 1f;
        if (Keyboard.current.aKey.isPressed) actions[1] = -1f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Waypoint"))
        {
            if (nextWaypointIndex < waypoints.Length &&
                other.gameObject == waypoints[nextWaypointIndex].gameObject)
            {
                AddReward(0.3f);
                nextWaypointIndex++;

                if (nextWaypointIndex < waypoints.Length)
                    previousDistance = Vector3.Distance(transform.localPosition, waypoints[nextWaypointIndex].position);
            }
        }
        else if (other.CompareTag("Finish"))
        {
            float finalReward = GetCumulativeReward() + 2.0f;
            AddReward(2.0f);
            FindFirstObjectByType<CarUI>()?.UpdateBestReward(finalReward);
            FindFirstObjectByType<CarUI>()?.AddLap();
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            float finalReward = GetCumulativeReward() - 0.5f;
            FindFirstObjectByType<CarUI>()?.UpdateBestReward(finalReward);
            FindFirstObjectByType<CarUI>()?.AddCollision();
            AddReward(-0.5f);
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            float finalReward = GetCumulativeReward() - 1.0f;
            FindFirstObjectByType<CarUI>()?.UpdateBestReward(finalReward);
            FindFirstObjectByType<CarUI>()?.AddCollision();
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
}

/*
Velocity.x = Arabanın sağ-sol eksenindeki mevcut hız bileşeni
Velocity.z = Arabanın ileri-geri eksenindeki mevcut hız bileşeni
Forward.x  = Arabanın burnunun baktığı yönün X eksenindeki izdüşümü
Forward.z  = Arabanın burnunun baktığı yönün Z eksenindeki izdüşümü
Target Direction.x  = Bir sonraki hedef noktasına olan yönün X bileşeni (checkpoint nerede)
Target Direction.z  = Bir sonraki hedef noktasına olan yönün Z bileşeni(checkpoint nerede)
Dot Product         = Arabanın baktığı yön ile hedefin yönü arasındaki açı (1 ise hedefe -1 ise ters yöne)
Normalised distance = Araç ile sıradaki hedef arasındaki mesafenin mesafesi.
*/