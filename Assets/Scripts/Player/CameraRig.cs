using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGame
{
    public class TPSCameraEntity : Entity
    {
        [Header("References")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform cameraTransform;

        [Header("Orbit")]
        [SerializeField] private float yawSpeed = 180f;
        [SerializeField] private float pitchSpeed = 140f;
        [SerializeField] private float pitchMin = -35f;
        [SerializeField] private float pitchMax = 70f;

        [Header("Distance")]
        [SerializeField] private float distance = 3.5f;
        [SerializeField] private float height = 1.6f;

        [Header("Smoothing")]
        [SerializeField] private float followLerp = 14f;

        private Vector2 lookInput;
        private float yaw;
        private float pitch;

        public override void InitEntity()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (followTarget != null)
            {
                Vector3 e = transform.rotation.eulerAngles;
                yaw = e.y;
                pitch = e.x;
                transform.position = followTarget.position;
            }
        }

        public override void LateUpdateEntity()
        {
            if (followTarget == null || cameraTransform == null) return;

            float dt = Time.deltaTime;

            yaw += lookInput.x * yawSpeed * dt;

            pitch -= lookInput.y * pitchSpeed * dt;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

            Vector3 targetPos = followTarget.position;
            transform.position = Vector3.Lerp(
                transform.position,
                targetPos,
                1f - Mathf.Exp(-followLerp * dt)
            );

            Vector3 pivot = transform.position + Vector3.up * height;
            Vector3 camPos = pivot - rot * Vector3.forward * distance;

            cameraTransform.position = camPos;
            cameraTransform.rotation = rot;
        }

        // Send Messages 用　ああ
        public void OnLook(InputValue v)
        {
            lookInput = v.Get<Vector2>();
        }

        // Invoke Unity Events 用
        public void OnLook(InputAction.CallbackContext ctx)
        {
            lookInput = ctx.ReadValue<Vector2>();
        }
    }
}
