using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Attack Input")]
    [SerializeField] private bool useMouseLeftClick = true;

    [Header("Attack Settings")]
    [SerializeField] private int damage = 20;
    [SerializeField] private float attackDistance = 2.5f;
    [SerializeField, Range(1f, 180f)] private float attackAngle = 90f;
    [SerializeField] private float attackDuration = 0.2f;
    [SerializeField] private float attackInterval = 0.02f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Reference")]
    [SerializeField] private Transform attackOrigin;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private bool isAttacking;

    private void Reset()
    {
        attackOrigin = transform;
    }

    private void Update()
    {
        if (!useMouseLeftClick) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryAttack();
        }
    }

    public void TryAttack()
    {
        if (isAttacking) return;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        float elapsed = 0f;
        HashSet<Health> hitTargets = new HashSet<Health>();

        while (elapsed < attackDuration)
        {
            DoAttackCheck(hitTargets);

            yield return new WaitForSeconds(attackInterval);
            elapsed += attackInterval;
        }

        isAttacking = false;
    }

    private void DoAttackCheck(HashSet<Health> hitTargets)
    {
        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Vector3 center = origin.position;

        Collider[] cols = Physics.OverlapSphere(center, attackDistance, targetLayer, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < cols.Length; i++)
        {
            Collider col = cols[i];
            if (col == null) continue;

            Health targetHealth = col.GetComponentInParent<Health>();
            if (targetHealth == null) continue;
            if (targetHealth.IsDead) continue;
            if (hitTargets.Contains(targetHealth)) continue;

            Vector3 toTarget = targetHealth.transform.position - center;
            toTarget.y = 0f;

            Vector3 forward = origin.forward;
            forward.y = 0f;

            if (toTarget.sqrMagnitude <= 0.0001f) continue;

            float angle = Vector3.Angle(forward.normalized, toTarget.normalized);
            if (angle > attackAngle * 0.5f) continue;

            float distance = toTarget.magnitude;
            if (distance > attackDistance) continue;

            targetHealth.TakeDamage(damage);
            hitTargets.Add(targetHealth);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Vector3 center = origin.position;
        Vector3 forward = origin.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackDistance);

        Vector3 left = Quaternion.Euler(0f, -attackAngle * 0.5f, 0f) * forward;
        Vector3 right = Quaternion.Euler(0f, attackAngle * 0.5f, 0f) * forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(center, center + left.normalized * attackDistance);
        Gizmos.DrawLine(center, center + right.normalized * attackDistance);
        Gizmos.DrawLine(center, center + forward.normalized * attackDistance);
    }
}