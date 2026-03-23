using UnityEngine;

public class SimpleDeath : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private bool destroyOnDeath = true;

    private void Awake()
    {
        if (health == null)
        {
            health = GetComponent<Health>();
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDead += OnDead;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDead -= OnDead;
        }
    }

    private void OnDead(Health h)
    {
        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}