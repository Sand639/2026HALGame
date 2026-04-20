using System.Collections.Generic;
using UnityEngine;

public class ConnectedClusterDestruction : MonoBehaviour
{
    [Header("Auto Build")]
    [SerializeField] private bool autoCollectChildren = true;
    [SerializeField] private float neighborDistance = 1.1f;
    [SerializeField] private bool autoMarkLowestAsSupport = true;
    [SerializeField] private float supportHeightTolerance = 0.05f;

    [Header("Link Strength")]
    [SerializeField] private float baseLinkHp = 10f;
    [SerializeField] private float supportBonusMultiplier = 2f;

    [Header("Damage")]
    [SerializeField] private float breakRadius = 1.5f;
    [SerializeField] private float damageAmount = 6f;
    [SerializeField] private AnimationCurve damageFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] private float detachImpulse = 2.0f;
    [SerializeField] private float upwardImpulse = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool drawNeighborGizmos = true;
    [SerializeField] private bool drawBrokenLinks = false;

    private readonly List<ClusterPiece> pieces = new List<ClusterPiece>();

    private void Awake()
    {
        BuildCluster();
    }

    [ContextMenu("Build Cluster")]
    public void BuildCluster()
    {
        pieces.Clear();

        if (autoCollectChildren)
        {
            ClusterPiece[] found = GetComponentsInChildren<ClusterPiece>(true);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i].transform == transform) continue;
                pieces.Add(found[i]);
            }
        }

        for (int i = 0; i < pieces.Count; i++)
        {
            pieces[i].pieceId = i;
            pieces[i].CaptureInitialTransform();
            pieces[i].ResetRuntimeState();
        }

        if (autoMarkLowestAsSupport)
        {
            MarkLowestPiecesAsSupport();
        }

        BuildLinks();
    }

    private void BuildLinks()
    {
        float sqr = neighborDistance * neighborDistance;

        for (int i = 0; i < pieces.Count; i++)
        {
            Vector3 aPos = pieces[i].transform.position;
            Bounds aBounds = GetPieceWorldBounds(pieces[i]);

            for (int j = i + 1; j < pieces.Count; j++)
            {
                Vector3 bPos = pieces[j].transform.position;
                Bounds bBounds = GetPieceWorldBounds(pieces[j]);

                bool linked = false;

                float distSqr = (aPos - bPos).sqrMagnitude;
                if (distSqr <= sqr)
                {
                    linked = true;
                }
                else
                {
                    float boundsGap = BoundsGap(aBounds, bBounds);
                    if (boundsGap <= neighborDistance)
                    {
                        linked = true;
                    }
                }

                if (!linked) continue;

                float hp = CalculateLinkHp(pieces[i], pieces[j]);
                pieces[i].AddLink(j, hp);
                pieces[j].AddLink(i, hp);
            }
        }
    }

    private Bounds GetPieceWorldBounds(ClusterPiece piece)
    {
        Collider c = piece.col;
        if (c != null)
        {
            return c.bounds;
        }

        Renderer r = piece.GetComponent<Renderer>();
        if (r != null)
        {
            return r.bounds;
        }

        return new Bounds(piece.transform.position, Vector3.zero);
    }

    private float BoundsGap(Bounds a, Bounds b)
    {
        float dx = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
        float dy = Mathf.Max(0f, Mathf.Max(a.min.y - b.max.y, b.min.y - a.max.y));
        float dz = Mathf.Max(0f, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));

        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private float CalculateLinkHp(ClusterPiece a, ClusterPiece b)
    {
        float hp = baseLinkHp;
        hp *= 0.5f * (a.strengthMultiplier + b.strengthMultiplier);

        if (a.isSupport || b.isSupport)
        {
            hp *= supportBonusMultiplier;
        }

        return hp;
    }

    private void MarkLowestPiecesAsSupport()
    {
        if (pieces.Count == 0) return;

        float minY = float.MaxValue;

        for (int i = 0; i < pieces.Count; i++)
        {
            float y = GetPieceWorldBounds(pieces[i]).min.y;
            if (y < minY) minY = y;
        }

        for (int i = 0; i < pieces.Count; i++)
        {
            float y = GetPieceWorldBounds(pieces[i]).min.y;
            pieces[i].isSupport = y <= minY + supportHeightTolerance;
        }
    }

    public void DamageAt(Vector3 hitPoint)
    {
        DamageLinksNear(hitPoint, breakRadius, damageAmount);
        RecalculateConnectivity(hitPoint);
    }

    private void DamageLinksNear(Vector3 hitPoint, float radius, float maxDamage)
    {
        float radiusSqr = radius * radius;

        for (int i = 0; i < pieces.Count; i++)
        {
            ClusterPiece a = pieces[i];
            Vector3 aPos = a.transform.position;

            for (int n = 0; n < a.links.Count; n++)
            {
                PieceLink link = a.links[n];
                int otherId = link.otherId;

                if (otherId <= i) continue;
                if (link.broken) continue;

                ClusterPiece b = pieces[otherId];
                Vector3 bPos = b.transform.position;
                Vector3 mid = (aPos + bPos) * 0.5f;

                float distSqr = (mid - hitPoint).sqrMagnitude;
                if (distSqr > radiusSqr) continue;

                float dist = Mathf.Sqrt(distSqr);
                float t = Mathf.Clamp01(dist / radius);
                float falloff = damageFalloff.Evaluate(t);
                float damage = maxDamage * falloff;

                a.ApplyDamageToLink(otherId, damage);
                b.ApplyDamageToLink(i, damage);
            }
        }
    }

    private void RecalculateConnectivity(Vector3 hitPoint)
    {
        bool[] visited = new bool[pieces.Count];
        Queue<int> queue = new Queue<int>();

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].isSupport && !pieces[i].isDetached)
            {
                visited[i] = true;
                queue.Enqueue(i);
            }
        }

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            ClusterPiece piece = pieces[current];

            for (int i = 0; i < piece.links.Count; i++)
            {
                PieceLink link = piece.links[i];
                int next = link.otherId;

                if (visited[next]) continue;
                if (link.broken) continue;
                if (pieces[next].isDetached) continue;

                visited[next] = true;
                queue.Enqueue(next);
            }
        }

        List<int> detachedNow = new List<int>();

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].isDetached) continue;
            if (visited[i]) continue;

            detachedNow.Add(i);
            pieces[i].isDetached = true;
        }

        ActivateDetachedPieces(detachedNow, hitPoint);
    }

    private void ActivateDetachedPieces(List<int> detachedIds, Vector3 hitPoint)
    {
        for (int i = 0; i < detachedIds.Count; i++)
        {
            ClusterPiece piece = pieces[detachedIds[i]];
            piece.ActivatePhysics();

            Vector3 dir = piece.transform.position - hitPoint;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Random.onUnitSphere;
            }

            dir.Normalize();
            dir.y += upwardImpulse;
            dir.Normalize();

            piece.rb.AddForce(dir * detachImpulse, ForceMode.Impulse);
            piece.rb.AddTorque(Random.insideUnitSphere * detachImpulse, ForceMode.Impulse);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawNeighborGizmos) return;

        ClusterPiece[] all = GetComponentsInChildren<ClusterPiece>(true);
        if (all == null) return;

        for (int i = 0; i < all.Length; i++)
        {
            ClusterPiece a = all[i];
            if (a == null) continue;

            for (int n = 0; n < a.links.Count; n++)
            {
                PieceLink link = a.links[n];
                int id = link.otherId;
                if (id < 0 || id >= all.Length) continue;

                ClusterPiece b = all[id];
                if (b == null) continue;
                if (id <= a.pieceId) continue;

                bool linked = !link.broken;

                if (!drawBrokenLinks && !linked) continue;

                float ratio = link.maxHp > 0f ? link.hp / link.maxHp : 0f;
                Gizmos.color = linked ? Color.Lerp(Color.red, Color.cyan, ratio) : Color.red;
                Gizmos.DrawLine(a.transform.position, b.transform.position);
            }

            Gizmos.color = a.isSupport ? Color.green : Color.yellow;
            Gizmos.DrawSphere(a.transform.position, 0.05f);
        }
    }
}