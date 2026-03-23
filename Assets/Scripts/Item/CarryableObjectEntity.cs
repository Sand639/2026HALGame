using UnityEngine;

namespace MyGame
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class CarryableObjectEntity : Entity
    {
        [SerializeField] private bool canCarry = true;

        private Rigidbody rb;
        private Collider[] cols;
        private Transform originalParent;
        private bool isCarried;

        public bool CanCarry => canCarry && gameObject.activeInHierarchy && !isCarried;
        public bool IsCarried => isCarried;

        public override void InitEntity()
        {
            rb = GetComponent<Rigidbody>();
            cols = GetComponentsInChildren<Collider>();
            originalParent = transform.parent;
        }

        private void Awake()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody>();

            if (cols == null || cols.Length == 0)
                cols = GetComponentsInChildren<Collider>();

            originalParent = transform.parent;
        }

        public void PickUp()
        {
            if (!CanCarry) return;

            isCarried = true;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            SetCollidersEnabled(false);
        }

        public void FollowHoldPoint(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (!isCarried) return;

            transform.position = worldPosition;
            transform.rotation = worldRotation;
        }

        public void Drop()
        {
            if (!isCarried) return;

            isCarried = false;
            transform.SetParent(originalParent);

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            SetCollidersEnabled(true);
        }

        public void Throw(Vector3 direction, float force)
        {
            if (!isCarried) return;

            isCarried = false;
            transform.SetParent(originalParent);

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(direction * force, ForceMode.Impulse);
            }

            SetCollidersEnabled(true);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (cols == null) return;

            for (int i = 0; i < cols.Length; i++)
            {
                cols[i].enabled = enabled;
            }
        }
    }
}