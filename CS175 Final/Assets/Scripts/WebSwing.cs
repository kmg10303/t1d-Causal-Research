using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class WebSwing : MonoBehaviour
{
    public Camera cam;
    public Transform handPoint;
    public LineRenderer line;

    public LayerMask attachMask;
    public float maxDistance = 120f;

    [Header("Rope Elasticity")]
    public float maxStretch = 10f;

    [Header("Rope Length")]
    public float ropeMin = 3.5f;
    public float ropeMax = 80f;
    public float reelInSpeed = 12f;

    [Header("Settle to Ground")]
    public float settleSpeedThresh = 3f;
    public float settleTangentialDamp = 1.5f;
    
    [Header("Shoot Animation")]
    public float webSpeed = 80f;
    public int shootSegments = 20;
    public float arcHeight = 2.0f;
    public float arcShape = 1.0f;
    public float shotGravity = 9f;
    public float maxDrop = 25f;


    Rigidbody rb;

    bool fireHeld;
    bool attached;
    Vector3 anchor;
    float ropeLen;
    bool shooting;
    Vector3 shotStart;
    Vector3 shotTarget;
    float shotT;
    bool isSprinting;

    void OnSprint(InputValue value) => isSprinting = value.isPressed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (line) line.enabled = false;
    }

    void StartShot()
    {
        Ray r = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(r, out RaycastHit hit, maxDistance, attachMask, QueryTriggerInteraction.Ignore))
            shotTarget = hit.point;
        else
            shotTarget = r.origin + r.direction * maxDistance;

        // Either using the handPoint or position, apply a ballistic drop to the rope.
        float d = Vector3.Distance(handPoint ? handPoint.position : transform.position, shotTarget);

        // Adapting found shot trajectory math at https://selectfiretrainingcenter.com/bullet-drop/
        float t = d / Mathf.Max(1f, webSpeed);
        float drop = Mathf.Min(maxDrop, 0.5f * shotGravity * t * t);

        // Apply drop
        shotTarget += Vector3.down * drop;

        shotStart = handPoint ? handPoint.position : transform.position;
        shotT = 0f;
        shooting = true;

        attached = false;
        if (line) line.enabled = true;
    }


    void Detach()
    {
        attached = false;
        if (line) line.enabled = false;
    }

    void Update()
    {
        bool nowHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;

        if (nowHeld && !fireHeld)
        {
            fireHeld = true;
            if (!attached) StartShot();
        }
        else if (!nowHeld && fireHeld)
        {
            fireHeld = false;
            if (shooting)
            {
                shooting = false;
                if (line) line.enabled = false;
            }
            else
            {
                Detach();
            }

        }

        if (shooting)
        {
            Vector3 start = handPoint ? handPoint.position : transform.position;
            float dist = Vector3.Distance(start, shotTarget);
            float dt = (dist > 1e-4f) ? (webSpeed * Time.deltaTime / dist) : 1f;
            shotT = Mathf.Min(1f, shotT + dt);

            Vector3 tip = Vector3.Lerp(start, shotTarget, shotT);

            if (line)
            {
                line.enabled = true;
                line.positionCount = shootSegments;
                
                // Craft the rope in segments to create an arc shape.
                for (int i = 0; i < shootSegments; i++)
                {
                    float s = i / (shootSegments - 1f);
                    Vector3 p = Vector3.Lerp(start, tip, s);
                    float h = arcHeight * Mathf.Pow(4f * s * (1f - s), arcShape);
                    p += Vector3.up * h;
                    line.SetPosition(i, p);
                }
            }

            if (shotT >= 1f)
            {
                float distNow = Vector3.Distance(rb.position, shotTarget);

                if (distNow > ropeMax)
                {
                    shooting = false;
                    if (line) line.enabled = false;
                    return;
                }

                shooting = false;
                attached = true;
                anchor = shotTarget;
                ropeLen = Mathf.Max(ropeMin, distNow);
            }
        }

    }


    void FixedUpdate()
    {
        if (!attached) return;

        float speed = rb.linearVelocity.magnitude;

        // reel in while holding shift
        if (isSprinting && speed >= settleSpeedThresh)
            ropeLen = Mathf.Max(ropeMin, ropeLen - reelInSpeed * Time.fixedDeltaTime);

        Vector3 pos = rb.position;
        Vector3 toPlayer = pos - anchor;
        float dist = toPlayer.magnitude;
        if (dist < 1e-4f) return;

        Vector3 dir = toPlayer / dist;

        float maxLen = ropeLen + maxStretch;
        float enforceLen = maxLen;

        // when rope is taut, pull toward anchor (spring + damping)
        if (dist > maxLen)  
        {
            // Put player back on the allowed radius (pivot)
            Vector3 corrected = anchor + dir * enforceLen;
            rb.MovePosition(corrected);

            // Remove radial velocity so you keep tangential momentum around the anchor
            Vector3 v = rb.linearVelocity;
            float vr = Vector3.Dot(v, dir);
            if (vr > 0f) rb.linearVelocity = v - vr * dir;
        }

        // when velocity is low, bring the player down to the ground.
        if (speed < settleSpeedThresh)
        {
            // Add force downwards.
            // rb.AddForce(Vector3.down * settleDownAccel, ForceMode.Acceleration);

            // Reduce swinging
            Vector3 v = rb.linearVelocity;
            Vector3 tangent = Vector3.ProjectOnPlane(v, dir);
            rb.AddForce(-tangent * settleTangentialDamp, ForceMode.Acceleration);
        }


    }

    void LateUpdate()
    {
        if (!line) return;

        if (shooting) return;

        if (!attached)
        {
            line.enabled = false;
            return;
        }

        line.enabled = true;
        line.positionCount = 2;
        line.SetPosition(0, handPoint ? handPoint.position : transform.position);
        line.SetPosition(1, anchor);
    }
    public bool IsAttached() => attached;
}
