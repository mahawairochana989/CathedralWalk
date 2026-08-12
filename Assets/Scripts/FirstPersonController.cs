using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public float walkSpeed = 6f;
    public float runSpeed = 11f;
    public float lookSensitivity = 2.2f;
    public float gravity = -20f;
    public float jumpHeight = 1.2f;
    public Transform cameraPivot;

    CharacterController controller;
    float pitch;
    float verticalVelocity;
    bool cursorLocked = true;

    float spawnGrace;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraPivot == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraPivot = cam.transform;
        }
        LockCursor(true);
        spawnGrace = 0.35f;
        verticalVelocity = -1f;
    }

    void Start()
    {
        // Only adjust height if already outside on the bridge — never pull into palace floor
        if (transform.position.z > -50f) return;

        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 30f,
                ~0, QueryTriggerInteraction.Ignore))
        {
            controller.enabled = false;
            transform.position = new Vector3(transform.position.x, hit.point.y + 0.05f, transform.position.z);
            controller.enabled = true;
            Physics.SyncTransforms();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            LockCursor(!cursorLocked);

        // Allow looking only when locked; gate clicks use center reticle while locked
        if (!cursorLocked) return;

        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;
        transform.Rotate(0f, mouseX, 0f);
        pitch = Mathf.Clamp(pitch - mouseY, -85f, 85f);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 move = (transform.right * h + transform.forward * v).normalized;
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        if (spawnGrace > 0f) spawnGrace -= Time.deltaTime;

        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
            if (Input.GetButtonDown("Jump"))
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else if (spawnGrace > 0f)
        {
            // Soft settle at spawn — avoid rocket-fall through thin bridge collider
            verticalVelocity = Mathf.Max(verticalVelocity + gravity * Time.deltaTime, -6f);
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    void LockCursor(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
