using UnityEngine;

public class SantaAnimDriver : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Animator anim;
    public Transform skin;

    [Header("Rotation")]
    public float turnSpeed = 12f;
    public float minSpeedToTurn = 0.2f;
    public float modelYawOffset = 0f;

    [Header("Ground Check")]
    public LayerMask groundMask = ~0;
    public float groundCheckDist = 0.2f;

    bool prevGrounded;

    CapsuleCollider col;

    void Awake()
    {
        skin = skin ? skin : transform;
        anim = anim ? anim : GetComponentInChildren<Animator>();
        rb = rb ? rb : GetComponentInParent<Rigidbody>();
        col = rb ? rb.GetComponent<CapsuleCollider>() : null;
    }

    void Reset()
    {
        skin = transform;
        anim = GetComponentInChildren<Animator>();
        rb = GetComponentInParent<Rigidbody>();
        col = GetComponentInParent<CapsuleCollider>();
    }

    void Update()
    {
        if (!rb || !anim || !skin) return;

        // --- Animation parameters ---
        Vector3 v = rb.linearVelocity;
        float horizSpeed = new Vector3(v.x, 0f, v.z).magnitude;

        bool grounded = IsGrounded();

        anim.SetFloat("Speed", horizSpeed);
        anim.SetBool("Grounded", grounded);
        anim.SetFloat("VerticalSpeed", rb.linearVelocity.y);

        if (!prevGrounded && grounded) anim.SetTrigger("Land");

        prevGrounded = grounded;

        // --- Rotate the visual to face movement direction ---
        Vector3 hv = v; 
        hv.y = 0f;

        if (hv.magnitude > minSpeedToTurn)
        {
            Quaternion target =
                Quaternion.LookRotation(hv.normalized, Vector3.up) *
                Quaternion.Euler(0f, modelYawOffset, 0f);

            skin.rotation = Quaternion.Slerp(
                skin.rotation,
                target,
                1f - Mathf.Exp(-turnSpeed * Time.deltaTime)
            );
        }
    }

    bool IsGrounded()
    {
        if (!rb) return false;

        if (!col) col = rb.GetComponent<CapsuleCollider>();

        Vector3 origin = col ? col.bounds.center : rb.position + Vector3.up * 0.1f;
        if (col) origin.y = col.bounds.min.y + 0.02f;
        float dist = Mathf.Max(groundCheckDist, 0.05f);
        return Physics.Raycast(origin, Vector3.down, dist, groundMask, QueryTriggerInteraction.Ignore);
    }
}
