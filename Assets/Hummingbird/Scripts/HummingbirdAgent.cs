using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")]
    public float pitchSpeed = 100f;

    [Tooltip("Spped to rotate around the axis")]
    public float yawSpeed = 100f;

    [Tooltip("Tip of the beek")]
    public Transform beakTip;

    [Tooltip("Agent's Camera")]
    public Camera agentCamera;

    [Tooltip("Train or gameplay mode")]
    public bool trainingMode;

    new private Rigidbody rigidbody;
    private FlowerArea flowerArea;
    private Flower nearestFlower;
    private float smoothPitchChange = 0f;
    private float smoothYawChange = 0f;
    private const float MaxPitchAngle = 80f;
    private const float BeakTipRadius = 0.008f;
    private bool isFrozen = false;

    /// <summary>
    /// nectar obtained in the episode
    /// </summary>
    public float NectarObtained { get; private set; }

    /// <summary>
    /// Initialize the agent
    /// </summary>
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        // if not training mode, no max steps, play forever
        if (!trainingMode)
        {
            MaxStep = 0;
        }
    }

    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            // reset flowers and one agent only
            flowerArea.ResetFlowers();
        }

        // reset nectar obtained
        NectarObtained = 0f;

        // zero out velocities for new episode
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        // default to spawning i.f.o a flower
        bool inFrontOfFlower = true;

        if (trainingMode)
        {
            inFrontOfFlower = UnityEngine.Random.value > 0.5f;
        }

        // move to safe random position
        MoveToSafeRandomPosition(inFrontOfFlower);

        // Recalculate nearest flower
        UpdateNearestFlower();

    }

    /// <summary>
    /// Called when action is received from player or neural-net
    /// index 0: move vector x (+1 right, -1 left, 0 same)
    /// index 1: move vector y (+1 up, -1 down, 0 same)
    /// index 2: move vector z (+1 forward, -1 backward, 0 same)
    /// index 3: pitch angle (+1 up, -1 down)
    /// index 4: yaw angle (+1 right, -1 left) 
    /// </summary>
    /// <param name="vectorAction">The actions to take</param>
    public override void OnActionReceived(float[] vectorAction)
    {
        if (isFrozen) return;
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);
        // add force
        rigidbody.AddForce(move * moveForce);
        // rotation
        // current rotation
        Vector3 rotationVector = transform.rotation.eulerAngles;
        // calculate pitch and yaw
        float pitchChange = vectorAction[3];
        float yawchange   = vectorAction[4];
        // smooth rotation
        smoothPitchChange = Mathf.MoveTowards(
            smoothPitchChange, pitchChange, 2f*Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(
            smoothYawChange, yawchange, 2f * Time.fixedDeltaTime);
        // new pitch and new
        // clamp pitch to avoid flipping
        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, - MaxPitchAngle, +MaxPitchAngle);

        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        // apply rotation
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

    }

    /// <summary>
    /// Collect vecotr observations from the environment
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        // Observe the local rotation
        sensor.AddObservation(transform.localRotation.normalized);
        Vector3 toFlower = nearestFlower.FlowerCenterVector - beakTip.position;
        // pointing to nearest flower
        sensor.AddObservation(toFlower.normalized);
        // dot product observation - beak tip in front of flower?
        // +1 -> infront, -1 -> behind
        sensor.AddObservation(
            Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector.normalized));
        // beak tip point to flower
        sensor.AddObservation(
            Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));
        // relative distance from beek tip to flower
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);
        // 10 total observations

    }
    /// <summary>
    /// no neural net, use it
    /// </summary>
    /// <param name="actionsOut">output action array</param>
    public override void Heuristic(float[] actionsOut)
    {
        // create placeholder
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        // keyboard input to control
        // forward / backward
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;
        // left / right
        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;
        // up / down
        if (Input.GetKey(KeyCode.E)) up = transform.up;
        else if (Input.GetKey(KeyCode.C)) up = -transform.up;
        // pitch up / down
        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;
        // yaw left / down
        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        // combine and normalize
        Vector3 combined = (forward + left + up).normalized;
        actionsOut[0] = combined.x;
        actionsOut[1] = combined.y;
        actionsOut[2] = combined.z;
        actionsOut[3] = pitch;
        actionsOut[4] = yaw;


    }
    /// <summary>
    /// prevent from moving
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/unfreeze not supported in training");
        isFrozen = true;
        rigidbody.Sleep();
    }
    /// <summary>
    /// resume movement
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/unfreeze not supported in training");
        isFrozen = false;
        rigidbody.WakeUp();
    }



    /// <summary>
    /// Move the agent to safe random position, no collision
    /// or flower with beek
    /// </summary>
    /// <param name="inFrontOfFlower"></param>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        // loop until safe position
        while (!safePositionFound && attemptsRemaining > 0)
        {
            --attemptsRemaining;
            if (inFrontOfFlower)
            {
                Flower randomFlower = flowerArea.Flowers[
                    UnityEngine.Random.Range(0, flowerArea.Flowers.Count)];
                // position in front of flower
                float distanceFromFlower = UnityEngine.Random.Range(0.1f, 0.2f);
                potentialPosition = randomFlower.transform.position
                    + randomFlower.FlowerUpVector * distanceFromFlower;

                // Point beek at flower
                Vector3 toFlower = randomFlower.FlowerCenterVector - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);

            }
            else
            {
                float height = UnityEngine.Random.Range(1.2f, 2.5f);
                float radius = UnityEngine.Random.Range(2f, 7f);
                Quaternion direction = Quaternion.Euler(
                    0, UnityEngine.Random.Range(-180f, 180f), 0f);
                potentialPosition = flowerArea.transform.position
                    + Vector3.up * height + direction * Vector3.forward * radius;
                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEngine.Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            // agent collision
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);
            // safe position
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not found a safe position");

        // set position, rotation
        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }


    /// <summary>
    /// update the nearest flower to agent
    /// </summary>
    private void UpdateNearestFlower()
    {
        foreach (Flower flower in flowerArea.Flowers)
        {
            if (nearestFlower == null && flower.HasNectar)
            {
                // no current nearest flower
                nearestFlower = flower;
            }
            else if (flower.HasNectar)
            {
                // calculate distance to this flower and current nearest
                float distanceToFlower = Vector3.Distance(
                    flower.transform.position, beakTip.position);
                float distanceToCurrentNearestFlower = Vector3.Distance(
                    nearestFlower.transform.position, beakTip.position);
                // current flower empty, update nearest flower
                if (!nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower)
                {
                    nearestFlower = flower;
                }

            }
        }
    }
    /// <summary>
    /// Called when the agent's collider enters a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }
    /// <summary>
    /// Called when the agent's collider stays a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }
    /// <summary>
    /// Enter or stay in a trigger collider
    /// </summary>
    /// <param name="collider"></param>
    private void TriggerEnterOrStay(Collider collider)
    {
        // check if colliding with nectar
        if (collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beakTip.position);
            // check if closest is close to tip
            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < BeakTipRadius)
            {
                Flower flower = flowerArea.GetFlowerFromNectar(collider);
                // Attemp to take 0.01 nectar
                float nectarReceived = flower.Feed(0.01f);
                // nectar obtained
                NectarObtained += nectarReceived;
                if (trainingMode)
                {
                    // calculate reward for getting nectar
                    float bonus = 0.02f * Mathf.Clamp01(
                        Vector3.Dot(transform.forward.normalized,
                            -nearestFlower.FlowerUpVector.normalized));
                    AddReward(0.01f + bonus); // experiment, balance reward

                }
                // if flower empty, update nearest flower
                if (!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    /// <summary>
    /// when collides with something solid
    /// </summary>
    /// <param name="collision">collision info</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            // boundary negative reward
            AddReward(-0.5f); // discourage getting outside
        }
    }
    /// <summary>
    /// call every frame
    /// </summary>
    private void Update()
    {
        // Beektip to flower-line debug
        if (nearestFlower != null)
        {
            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterVector, Color.green);
        }
    }
    /// <summary>
    /// called every 0.02 seconds
    /// </summary>
    private void FixedUpdate()
    {
        // avoid stolen nearest flower
        if (nearestFlower != null && !nearestFlower.HasNectar)
        {
            UpdateNearestFlower();
        }
    }
}
