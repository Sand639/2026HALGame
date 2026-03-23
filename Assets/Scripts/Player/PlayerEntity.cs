using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGame
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerEntity : Entity
    {
        [Header("Move")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float airControl = 0.6f;

        [Header("Jump / Gravity")]
        [SerializeField] private float jumpHeight = 1.6f;
        [SerializeField] private float gravity = -25f;

        [Header("Camera Based Move (optional)")]
        [SerializeField] private Transform cameraTransform;


        private CharacterController cc;

        private Vector2 moveInput;
        private bool jumpQueued;
        private Vector3 velocity;

        public override void InitEntity()
        {
            cc = GetComponent<CharacterController>();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            
        }

        public override void UpdateEntity()
        {
            if (cc == null) return;

            bool grounded = cc.isGrounded;

            if (grounded && velocity.y < 0f)
                velocity.y = -2f;

            Vector2 m = Vector2.ClampMagnitude(moveInput, 1f);

            Vector3 forward, right;
            if (cameraTransform != null)
            {
                forward = cameraTransform.forward; forward.y = 0f; forward.Normalize();
                right = cameraTransform.right; right.y = 0f; right.Normalize();
            }
            else
            {
                forward = transform.forward; forward.y = 0f; forward.Normalize();
                right = transform.right; right.y = 0f; right.Normalize();
            }

            Vector3 moveDir = right * m.x + forward * m.y;

            float control = grounded ? 1f : airControl;

            cc.Move(moveDir * (moveSpeed * control) * Time.deltaTime);

            if (grounded && jumpQueued)
            {
                velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }
            jumpQueued = false;

            velocity.y += gravity * Time.deltaTime;
            cc.Move(velocity * Time.deltaTime);

            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
            }
           
        }

       

        // Invoke Unity Events 用
        public void OnMove(InputAction.CallbackContext ctx)
        {
            moveInput = ctx.ReadValue<Vector2>();
        }

        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
                jumpQueued = true;
        }

        // Send Messages 用
        public void OnMove(InputValue v)
        {
            moveInput = v.Get<Vector2>();
        }

        public void OnJump(InputValue v)
        {
            if (v.isPressed)
                jumpQueued = true;
        }
    }
}
