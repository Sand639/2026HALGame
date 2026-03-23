using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f;

    private Vector3 direction;
    private float speed;
    private int damage;
    private bool initialized;

    public void Initialize(Vector3 dir, float moveSpeed, int attackPower)
    {
        direction = dir.normalized;
        speed = moveSpeed;
        damage = attackPower;
        initialized = true;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!initialized) return;

        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        Health health = other.GetComponentInParent<Health>();
        if (health != null && other.CompareTag("Player"))
        {
            health.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}