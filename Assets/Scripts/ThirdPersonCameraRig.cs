using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class ThirdPersonCameraRig : MonoBehaviour
{
    [Header("Referensi")]
    [Tooltip("Seret objek Cinemachine Camera ke sini")]
    public CinemachineThirdPersonFollow thirdPerson;

    [Header("Orbit")]
    public float mouseSensitivity = 0.12f;
    public float minPitch = -20f;
    public float maxPitch = 65f;
    public bool invertY = false;

    [Header("Zoom (scroll)")]
    public float minDistance = 2.5f;
    public float maxDistance = 10f;
    public float zoomStep = 0.8f;
    public float zoomSmooth = 10f;

    [Header("Kontrol")]
    // Nanti dimatikan saat dialog / inventaris / pause (pakai SetLookEnabled)
    public bool lookEnabled = true;

    float yaw;
    float pitch = 15f;
    float targetDistance = 6f;

    void Start()
    {
        yaw = transform.eulerAngles.y;
        if (thirdPerson != null) targetDistance = thirdPerson.CameraDistance;
        SetCursorLocked(lookEnabled);
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !lookEnabled) return;

        // Jika cursor terlepas (mis. tekan Esc di Editor), klik untuk mengunci lagi
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (mouse.leftButton.wasPressedThisFrame) SetCursorLocked(true);
            return;
        }

        // Orbit
        Vector2 delta = mouse.delta.ReadValue();
        yaw += delta.x * mouseSensitivity;
        pitch += (invertY ? delta.y : -delta.y) * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Zoom
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetDistance -= Mathf.Sign(scroll) * zoomStep;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
    }

    void LateUpdate()
    {
        // Set rotasi dunia, jadi tidak ikut berputar saat Player berputar
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        if (thirdPerson != null)
        {
            thirdPerson.CameraDistance = Mathf.Lerp(
                thirdPerson.CameraDistance, targetDistance, zoomSmooth * Time.deltaTime);
        }
    }

    // Dipanggil sistem dialog/inventaris/pause nanti
    public void SetLookEnabled(bool enabled)
    {
        lookEnabled = enabled;
        SetCursorLocked(enabled);
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
