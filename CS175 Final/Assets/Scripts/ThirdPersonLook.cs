using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonLook : MonoBehaviour
{
    public Transform target;
    public Transform cam;
    public Transform handPoint;

    public Vector3 camOffset = new Vector3(0f, 2f, -6f);
    public float sensitivity = 0.12f;
    public float followSmooth = 18f;
    public float minPitch = -35f;
    public float maxPitch = 70f;

    Vector2 look;
    float yaw;
    float pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw = transform.eulerAngles.y;
        pitch = 15f;
    }

    public void OnLook(InputValue v) => look = v.Get<Vector2>();

    void LateUpdate()
    {
        if (!target || !cam) return;

        yaw += look.x * sensitivity;
        pitch -= look.y * sensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Rig rotation from yaw/pitch
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        // Follow target smoothly
        Vector3 desiredRigPos = target.position;
        transform.position = Vector3.Lerp(transform.position, desiredRigPos,
            1f - Mathf.Exp(-followSmooth * Time.deltaTime));

        // Position camera using offset in rotated space
        cam.position = transform.position + rot * camOffset;
        cam.rotation = rot;

        // Hand points where the camera points (yaw/pitch)
        if (handPoint)
            handPoint.rotation = rot;
    }
}
