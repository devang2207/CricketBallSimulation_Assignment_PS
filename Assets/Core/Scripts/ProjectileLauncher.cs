using UnityEngine;
using UnityEngine.InputSystem;

public class ProjectileLauncher : MonoBehaviour
{
    public enum DeviationMode
    {
        Swing,
        Spin
    }

    [Header("Mode")]
    public DeviationMode mode = DeviationMode.Swing;

    [Header("References")]
    public GameObject projectilePrefab;
    public Transform target;

    [Header("Arm Spawn Points")]
    public Transform leftArmSpawn;
    public Transform rightArmSpawn;
    public bool useRightArm = true;

    [Header("Trajectory")]
    public float apexHeight = 2f;

    [Header("Swing (In Air)")]
    [Range(-20f, 20f)]
    public float curve = 0f;

    [Header("Spin (Post-Bounce Impulse)")]
    [Range(-10f, 10f)]
    public float spin = 0f;

    [Header("Trajectory Preview")]
    public LineRenderer trajectoryLine;
    public int trajectoryResolution = 50;

    [Header("Accuracy Meter (optional)")]
    public AccuracyMeter accuracyMeter;

    Rigidbody activeProjectileRb;
    Vector3 sideAcceleration;
    bool applySideForce;

    Vector3 spinDirection;

    // Two-phase launch state
    enum LaunchPhase { Aiming, Locked }
    LaunchPhase phase = LaunchPhase.Aiming;
    float cachedAccuracy = 1f;

    void Update()
    {
        DrawTrajectory();

        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            HandleSpacePress();
        }
    }

    void HandleSpacePress()
    {
        // No meter attached → just launch immediately
        if (accuracyMeter == null)
        {
            cachedAccuracy = 1f;
            Launch();
            return;
        }

        switch (phase)
        {
            case LaunchPhase.Aiming:
                cachedAccuracy = accuracyMeter.LockAndGetAccuracy();
                phase = LaunchPhase.Locked;
                Debug.Log($"Accuracy locked: {cachedAccuracy * 100f:F0}%");
                break;

            case LaunchPhase.Locked:
                Launch();
                phase = LaunchPhase.Aiming;
                accuracyMeter.ResetMeter();
                break;
        }
    }

    void FixedUpdate()
    {
        if (mode == DeviationMode.Swing &&
            applySideForce &&
            activeProjectileRb != null)
        {
            activeProjectileRb.AddForce(
                sideAcceleration * activeProjectileRb.mass,
                ForceMode.Force
            );
        }
    }

    public Vector3 SpawnPosition
    {
        get
        {
            if (useRightArm && rightArmSpawn != null) return rightArmSpawn.position;
            if (!useRightArm && leftArmSpawn != null) return leftArmSpawn.position;
            return transform.position;
        }
    }

    void Launch()
    {
        GameObject projectile = Instantiate(
            projectilePrefab,
            SpawnPosition,
            Quaternion.identity
        );

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Projectile prefab must have a Rigidbody.");
            return;
        }

        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.useGravity = true;
        rb.isKinematic = false;

        float usedCurve = (mode == DeviationMode.Swing) ? curve * cachedAccuracy : 0f;

        Vector3 impulse = CalculateImpulseWithCurve(
            rb.mass,
            SpawnPosition,
            target.position,
            apexHeight,
            usedCurve,
            out sideAcceleration
        );

        rb.AddForce(impulse, ForceMode.Impulse);

        activeProjectileRb = rb;
        applySideForce = (mode == DeviationMode.Swing);

        // spin direction 
        Vector3 displacementXZ = new Vector3(
            target.position.x - SpawnPosition.x,
            0f,
            target.position.z - SpawnPosition.z
        );

        Vector3 forward = displacementXZ.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        spinDirection = right * spin * cachedAccuracy;

        CollisionNotifier notifier = projectile.AddComponent<CollisionNotifier>();
        notifier.OnFirstCollision = StopSideForce;
    }

    void StopSideForce()
    {
        applySideForce = false;

        if (mode == DeviationMode.Spin &&
            activeProjectileRb != null &&
            spin != 0f)
        {
            activeProjectileRb.AddForce(
                spinDirection * activeProjectileRb.mass,
                ForceMode.Impulse
            );
        }
    }

    Vector3 CalculateImpulseWithCurve(
        float mass,
        Vector3 start,
        Vector3 end,
        float apexHeight,
        float curve,
        out Vector3 sideAccel
    )
    {
        float g = Mathf.Abs(Physics.gravity.y);

        float heightDifference = end.y - start.y;
        if (apexHeight <= heightDifference)
        {
            sideAccel = Vector3.zero;
            return Vector3.zero;
        }

        float vy = Mathf.Sqrt(2f * g * apexHeight);
        float timeUp = vy / g;

        float dropFromApex = apexHeight - heightDifference;
        float timeDown = Mathf.Sqrt(2f * dropFromApex / g);

        float totalTime = timeUp + timeDown;

        Vector3 displacementXZ = new Vector3(
            end.x - start.x,
            0f,
            end.z - start.z
        );

        Vector3 forward = displacementXZ.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        sideAccel = right * curve;

        Vector3 sideDrift = 0.5f * sideAccel * totalTime * totalTime;
        Vector3 velocityXZ = (displacementXZ - sideDrift) / totalTime;

        Vector3 launchVelocity = new Vector3(
            velocityXZ.x,
            vy,
            velocityXZ.z
        );

        return mass * launchVelocity;
    }

    void DrawTrajectory()
    {
        if (trajectoryLine == null || target == null)
            return;

        float g = Mathf.Abs(Physics.gravity.y);

        Vector3 start = SpawnPosition;
        Vector3 end = target.position;

        float usedCurve = (mode == DeviationMode.Swing) ? curve : 0f;

        Vector3 sideAccel;
        Vector3 impulse = CalculateImpulseWithCurve(
            1f,
            start,
            end,
            apexHeight,
            usedCurve,
            out sideAccel
        );

        Vector3 v0 = impulse;

        float heightDifference = end.y - start.y;
        float vy = v0.y;

        float timeUp = vy / g;
        float dropFromApex = apexHeight - heightDifference;
        float timeDown = Mathf.Sqrt(2f * dropFromApex / g);
        float totalTime = timeUp + timeDown;

        trajectoryLine.positionCount = trajectoryResolution;

        for (int i = 0; i < trajectoryResolution; i++)
        {
            float t = (i / (float)(trajectoryResolution - 1)) * totalTime;

            Vector3 pos =
                start +
                v0 * t +
                0.5f * new Vector3(
                    sideAccel.x,
                    -g,
                    sideAccel.z
                ) * t * t;

            trajectoryLine.SetPosition(i, pos);
        }
    }

    class CollisionNotifier : MonoBehaviour
    {
        public System.Action OnFirstCollision;
        bool collided;

        void OnCollisionEnter(Collision collision)
        {
            if (collided) return;
            collided = true;
            OnFirstCollision?.Invoke();
        }
    }
}
