using UnityEngine;

namespace MyGame
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class ItemEntity : Entity
    {
        [Header("Item")]
        [SerializeField] private string itemId = "Item";
        [SerializeField] private int amount = 1;

        [Header("Drop")]
        [SerializeField] private bool applyDropOnStart = true;
        [SerializeField] private Vector2 randomHorizontalForceRange = new Vector2(0.5f, 1.2f);
        [SerializeField] private float upwardForce = 2.5f;
        [SerializeField] private float maxStartTorque = 8.0f;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDistance = 0.15f;
        [SerializeField] private LayerMask groundLayer = ~0;

        [Header("Idle Motion")]
        [SerializeField] private bool enableIdleMotion = true;
        [SerializeField] private float floatAmplitude = 0.1f;
        [SerializeField] private float floatSpeed = 2.0f;
        [SerializeField] private float spinSpeed = 90.0f;

        private Rigidbody rb;
        private Collider col;

        private bool hasDropped;
        private bool landed;
        private float baseY;
        private float idleTime;

        public string ItemId => itemId;
        public int Amount => amount;

        public bool CanPickup => gameObject.activeInHierarchy && amount > 0;

        public override void InitEntity()
        {
            CacheComponents();
        }

        private void Awake()
        {
            CacheComponents();
        }

        private void Start()
        {
            if (applyDropOnStart)
            {
                ApplyDrop();
            }
        }

        private void Update()
        {
            if (!enableIdleMotion) return;
            if (!landed) return;
            if (!gameObject.activeInHierarchy) return;

            idleTime += Time.deltaTime;

            Vector3 pos = transform.position;
            pos.y = baseY + Mathf.Sin(idleTime * floatSpeed) * floatAmplitude;
            transform.position = pos;

            transform.Rotate(0.0f, spinSpeed * Time.deltaTime, 0.0f, Space.World);
        }

        private void FixedUpdate()
        {
            if (!hasDropped || landed == true) return;
            if (rb == null) return;

            if (rb.linearVelocity.sqrMagnitude > 0.01f) return;

            if (IsGrounded())
            {
                Land();
            }
        }

        public void Pickup()
        {
            if (!CanPickup) return;

            gameObject.SetActive(false);
        }

        public void ApplyDrop()
        {
            CacheComponents();

            hasDropped = true;
            landed = false;
            idleTime = 0.0f;

            if (rb == null) return;

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector2 circle = Random.insideUnitCircle.normalized;
            float horizontalForce = Random.Range(randomHorizontalForceRange.x, randomHorizontalForceRange.y);
            Vector3 impulse = new Vector3(circle.x, 0.0f, circle.y) * horizontalForce;
            impulse.y = upwardForce;

            rb.AddForce(impulse, ForceMode.Impulse);

            Vector3 randomTorque = new Vector3(
                Random.Range(-maxStartTorque, maxStartTorque),
                Random.Range(-maxStartTorque, maxStartTorque),
                Random.Range(-maxStartTorque, maxStartTorque)
            );
            rb.AddTorque(randomTorque, ForceMode.Impulse);
        }

        private void Land()
        {
            landed = true;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            Vector3 pos = transform.position;
            baseY = pos.y;
        }

        private bool IsGrounded()
        {
            if (col == null) return false;

            Bounds b = col.bounds;
            Vector3 origin = new Vector3(b.center.x, b.min.y + 0.02f, b.center.z);

            return Physics.Raycast(
                origin,
                Vector3.down,
                groundCheckDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
        }

        private void CacheComponents()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody>();

            if (col == null)
                col = GetComponent<Collider>();
        }

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c != null)
                c.isTrigger = false;

            var body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.useGravity = true;
                body.isKinematic = false;
                body.mass = 1.0f;
                body.linearDamping = 1.5f;
                body.angularDamping = 1.5f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }
    }
}