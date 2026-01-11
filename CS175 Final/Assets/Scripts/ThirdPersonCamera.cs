using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 2f, -6f);
    public float followSmooth = 12f;

    void LateUpdate()
    {
        if (!target) return;
        Vector3 desired = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSmooth * Time.deltaTime));
        transform.LookAt(target.position + Vector3.up * 1.2f);
    }
}
