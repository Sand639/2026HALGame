using System.Collections;
using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Shoot Timing")]
    [SerializeField] private float minShootInterval = 1.5f;
    [SerializeField] private float maxShootInterval = 3.5f;

    [Header("Shoot Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private int projectileDamage = 10;

    [Header("Optional")]
    [SerializeField] private bool stopWhenDead = true;

    private Health myHealth;

    private void Awake()
    {
        myHealth = GetComponent<Health>();
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        StartCoroutine(ShootRoutine());
    }

    private IEnumerator ShootRoutine()
    {
        while (true)
        {
            if (stopWhenDead && myHealth != null && myHealth.IsDead)
            {
                yield break;
            }

            float wait = Random.Range(minShootInterval, maxShootInterval);
            yield return new WaitForSeconds(wait);

            Shoot();
        }
    }

    private void Shoot()
    {
        if (player == null || projectilePrefab == null || shootPoint == null) return;

        Vector3 dir = (player.position - shootPoint.position).normalized;
        GameObject obj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.LookRotation(dir));

        EnemyProjectile projectile = obj.GetComponent<EnemyProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(dir, projectileSpeed, projectileDamage);
        }
    }
}