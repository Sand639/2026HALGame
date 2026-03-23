using System.Collections.Generic;
using UnityEngine;

public class ConnectedClusterDestruction : MonoBehaviour
{
    [Header("Auto Build")]
    [SerializeField] private bool autoCollectChildren = true;
    [SerializeField] private float neighborDistance = 1.1f;
    [SerializeField] private bool autoMarkLowestAsSupport = true;
    [SerializeField] private float supportHeightTolerance = 0.05f;

    [Header("Damage")]
    [SerializeField] private float breakRadius = 1.5f;
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
            ClusterPiece[] found = GetComponentsInChildren<ClusterPiece>();
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i].transform == transform) continue;
                pieces.Add(found[i]);
            }
        }

        for (int i = 0; i < pieces.Count; i++)
        {
            pieces[i].pieceId = i;
            pieces[i].neighbors.Clear();
            pieces[i].brokenLinks.Clear();
            pieces[i].isDetached = false;

            if (pieces[i].rb != null)
            {
                pieces[i].rb.isKinematic = true;
                pieces[i].rb.useGravity = false;
                pieces[i].rb.linearVelocity = Vector3.zero;
                pieces[i].rb.angularVelocity = Vector3.zero;
            }
        }

        BuildNeighbors();

        if (autoMarkLowestAsSupport)
        {
            MarkLowestPiecesAsSupport();
        }
    }

    private void BuildNeighbors()
    {
        float sqr = neighborDistance * neighborDistance;

        for (int i = 0; i < pieces.Count; i++)
        {
            Vector3 a = pieces[i].transform.position;

            for (int j = i + 1; j < pieces.Count; j++)
            {
                Vector3 b = pieces[j].transform.position;
                float distSqr = (a - b).sqrMagnitude;

                if (distSqr <= sqr)
                {
                    pieces[i].neighbors.Add(j);
                    pieces[j].neighbors.Add(i);
                }
            }
        }
    }

    private void MarkLowestPiecesAsSupport()
    {
        if (pieces.Count == 0) return;

        float minY = float.MaxValue;

        for (int i = 0; i < pieces.Count; i++)
        {
            float y = pieces[i].transform.position.y;
            if (y < minY) minY = y;
        }

        for (int i = 0; i < pieces.Count; i++)
        {
            float y = pieces[i].transform.position.y;
            pieces[i].isSupport = y <= minY + supportHeightTolerance;
        }
    }

    public void DamageAt(Vector3 hitPoint)
    {
        BreakLinksNear(hitPoint, breakRadius);
        RecalculateConnectivity(hitPoint);
    }

    private void BreakLinksNear(Vector3 hitPoint, float radius)
    {
        float radiusSqr = radius * radius;

        for (int i = 0; i < pieces.Count; i++)
        {
            ClusterPiece a = pieces[i];
            Vector3 aPos = a.transform.position;

            for (int n = 0; n < a.neighbors.Count; n++)
            {
                int otherId = a.neighbors[n];

                if (otherId <= i) continue;

                ClusterPiece b = pieces[otherId];
                if (!a.IsLinkedTo(otherId)) continue;

                Vector3 bPos = b.transform.position;
                Vector3 mid = (aPos + bPos) * 0.5f;

                if ((mid - hitPoint).sqrMagnitude <= radiusSqr)
                {
                    a.BreakLink(otherId);
                    b.BreakLink(i);
                }
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

            for (int i = 0; i < piece.neighbors.Count; i++)
            {
                int next = piece.neighbors[i];

                if (visited[next]) continue;
                if (!piece.IsLinkedTo(next)) continue;
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

            Vector3 dir = (piece.transform.position - hitPoint);
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

        ClusterPiece[] all = GetComponentsInChildren<ClusterPiece>();
        if (all == null) return;

        for (int i = 0; i < all.Length; i++)
        {
            ClusterPiece a = all[i];
            if (a == null) continue;

            for (int n = 0; n < a.neighbors.Count; n++)
            {
                int id = a.neighbors[n];
                if (id < 0 || id >= all.Length) continue;

                ClusterPiece b = all[id];
                if (b == null) continue;

                bool linked = !a.brokenLinks.Contains(id);

                if (!drawBrokenLinks && !linked) continue;

                Gizmos.color = linked ? Color.cyan : Color.red;
                Gizmos.DrawLine(a.transform.position, b.transform.position);
            }

            Gizmos.color = a.isSupport ? Color.green : Color.yellow;
            Gizmos.DrawSphere(a.transform.position, 0.05f);
        }
    }
}