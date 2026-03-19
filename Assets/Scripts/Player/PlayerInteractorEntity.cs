using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGame
{
    public class PlayerInteractorEntity : Entity
    {
        [Header("Ray")]
        [SerializeField] private Transform cameraTransform;

        // Rayの開始点：プレイヤー側（未設定なら transform + 高さ）
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private float originHeight = 1.2f;

        [SerializeField] private float interactDistance = 3.0f;
        [SerializeField] private LayerMask itemLayer = ~0;

        [Header("UI")]
        [SerializeField] private ReticleEntity reticle;

        [Header("Debug")]
        [SerializeField] private bool debugRay = true;

        private bool interactQueued;
        private ItemEntity currentItem;

        public override void InitEntity()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        public override void UpdateEntity()
        {
            if (cameraTransform == null) return;

            Vector3 origin = GetRayOrigin();
            Vector3 dir = cameraTransform.forward;

            if (debugRay)
                Debug.DrawRay(origin, dir * interactDistance, Color.red);

            currentItem = FindTargetItem(origin, dir);

            bool canPickup = currentItem != null && currentItem.CanPickup;

            if (reticle != null)
                reticle.SetActive(canPickup);

            if (canPickup && interactQueued)
            {
                currentItem.Pickup();
            }

            interactQueued = false;
        }

        private Vector3 GetRayOrigin()
        {
            if (rayOrigin != null)
                return rayOrigin.position;

            return transform.position + Vector3.up * originHeight;
        }

        private ItemEntity FindTargetItem(Vector3 origin, Vector3 dir)
        {
            Ray ray = new Ray(origin, dir);

            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, itemLayer, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.TryGetComponent<ItemEntity>(out var item))
                    return item;

                return hit.collider.GetComponentInParent<ItemEntity>();
            }

            return null;
        }

        // PlayerInput: Send Messages 用
        public void OnInteract(InputValue v)
        {
            if (v.isPressed)
                interactQueued = true;
        }

        // PlayerInput: Invoke Unity Events 用
        public void OnInteract(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
                interactQueued = true;
        }
    }
}
