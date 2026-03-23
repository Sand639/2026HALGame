using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGame
{
    public class PlayerInteractorEntity : Entity
    {
        [Header("Camera")]
        [SerializeField] private Transform cameraTransform;

        [Header("Ray")]
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private float originHeight = 1.2f;
        [SerializeField] private float interactDistance = 3.0f;
        [SerializeField] private LayerMask interactLayer = ~0;

        [Header("Auto Pickup")]
        [SerializeField] private float autoPickupRadius = 1.5f;
        [SerializeField] private LayerMask itemLayer = ~0;

        [Header("Carry Position")]
        [SerializeField] private Vector3 holdLocalOffset = new Vector3(0.0f, 2.0f, 0.5f);
        [SerializeField] private Vector3 holdLocalEuler = Vector3.zero;

        [Header("Throw")]
        [SerializeField] private float minThrowForce = 4.0f;
        [SerializeField] private float maxThrowForce = 12.0f;
        [SerializeField] private float maxChargeTime = 1.5f;

        [Header("UI")]
        [SerializeField] private ReticleEntity reticle;
        [SerializeField] private ThrowChargeGaugeEntity throwGauge;

        [Header("Debug")]
        [SerializeField] private bool debugRay = true;
        [SerializeField] private bool debugAutoPickup = true;

        private bool interactQueued;
        private bool isChargingThrow;
        private float throwChargeTime;

        private CarryableObjectEntity currentCarryTarget;
        private CarryableObjectEntity carryingObject;

        public float ThrowChargeRate
        {
            get
            {
                if (maxChargeTime <= 0.0f) return 1.0f;
                return Mathf.Clamp01(throwChargeTime / maxChargeTime);
            }
        }

        public float CurrentThrowForce => Mathf.Lerp(minThrowForce, maxThrowForce, ThrowChargeRate);

        public override void InitEntity()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (throwGauge != null)
            {
                throwGauge.SetVisible(false);
                throwGauge.SetRate(0.0f);
            }
        }

        public override void UpdateEntity()
        {
            if (cameraTransform == null) return;

            AutoPickupNearbyItems();

            Vector3 origin = GetRayOrigin();
            Vector3 dir = cameraTransform.forward;

            if (debugRay)
                Debug.DrawRay(origin, dir * interactDistance, Color.red);

            currentCarryTarget = FindTargetCarryable(origin, dir);

            bool canInteractCarry = carryingObject != null ||
                                    (currentCarryTarget != null && currentCarryTarget.CanCarry);

            if (reticle != null)
                reticle.SetActive(canInteractCarry);

            if (interactQueued)
            {
                HandleInteract();
            }

            if (carryingObject != null)
            {
                UpdateCarriedObjectTransform();
                UpdateThrowByMouse();
            }
            else
            {
                isChargingThrow = false;
                throwChargeTime = 0.0f;
                UpdateThrowGauge(false, 0.0f);
            }

            interactQueued = false;
        }

        private Vector3 GetRayOrigin()
        {
            if (rayOrigin != null)
                return rayOrigin.position;

            return transform.position + Vector3.up * originHeight;
        }

        private void AutoPickupNearbyItems()
        {
            Vector3 center = transform.position;

            if (debugAutoPickup)
                Debug.DrawLine(center, center + Vector3.up * 1.5f, Color.green);

            Collider[] hits = Physics.OverlapSphere(center, autoPickupRadius, itemLayer, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                ItemEntity item = hits[i].GetComponent<ItemEntity>();
                if (item == null)
                    item = hits[i].GetComponentInParent<ItemEntity>();

                if (item != null && item.CanPickup)
                    item.Pickup();
            }
        }

        private CarryableObjectEntity FindTargetCarryable(Vector3 origin, Vector3 dir)
        {
            Ray ray = new Ray(origin, dir);

            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.TryGetComponent<CarryableObjectEntity>(out var carryable))
                    return carryable;

                return hit.collider.GetComponentInParent<CarryableObjectEntity>();
            }

            return null;
        }

        private void HandleInteract()
        {
            if (carryingObject != null)
            {
                carryingObject.Drop();
                carryingObject = null;
                isChargingThrow = false;
                throwChargeTime = 0.0f;
                UpdateThrowGauge(false, 0.0f);
                return;
            }

            if (currentCarryTarget != null && currentCarryTarget.CanCarry)
            {
                currentCarryTarget.PickUp();
                carryingObject = currentCarryTarget;
                UpdateCarriedObjectTransform();
                UpdateThrowGauge(false, 0.0f);
            }
        }

        private void UpdateCarriedObjectTransform()
        {
            if (carryingObject == null) return;

            Vector3 worldPos = transform.TransformPoint(holdLocalOffset);
            Quaternion worldRot = transform.rotation * Quaternion.Euler(holdLocalEuler);

            carryingObject.FollowHoldPoint(worldPos, worldRot);
        }

        private void UpdateThrowByMouse()
        {
            if (Mouse.current == null) return;

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                isChargingThrow = true;
                throwChargeTime = 0.0f;
                UpdateThrowGauge(true, 0.0f);
            }

            if (isChargingThrow && Mouse.current.rightButton.isPressed)
            {
                throwChargeTime += Time.deltaTime;
                if (throwChargeTime > maxChargeTime)
                    throwChargeTime = maxChargeTime;

                UpdateThrowGauge(true, ThrowChargeRate);
            }

            if (isChargingThrow && Mouse.current.rightButton.wasReleasedThisFrame)
            {
                float force = CurrentThrowForce;
                Vector3 dir = cameraTransform.forward;

                carryingObject.Throw(dir, force);
                carryingObject = null;
                isChargingThrow = false;
                throwChargeTime = 0.0f;
                UpdateThrowGauge(false, 0.0f);
            }
        }

        private void UpdateThrowGauge(bool visible, float rate)
        {
            if (throwGauge == null) return;

            throwGauge.SetVisible(visible);
            throwGauge.SetRate(rate);
        }

        public void OnInteract(InputValue v)
        {
            if (v.isPressed)
                interactQueued = true;
        }

        public void OnInteract(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
                interactQueued = true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, autoPickupRadius);

            Gizmos.color = Color.cyan;
            Vector3 holdPos = transform.TransformPoint(holdLocalOffset);
            Gizmos.DrawWireSphere(holdPos, 0.2f);
        }
    }
}