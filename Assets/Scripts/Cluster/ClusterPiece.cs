using System.Collections.Generic;
using UnityEngine;

public class ClusterPiece : MonoBehaviour
{
    [HideInInspector] public int pieceId = -1;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Collider col;

    [Header("Support")]
    public bool isSupport = false;

    [HideInInspector] public bool isDetached = false;

    [HideInInspector] public List<int> neighbors = new List<int>();
    [HideInInspector] public HashSet<int> brokenLinks = new HashSet<int>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public bool IsLinkedTo(int otherId)
    {
        if (!neighbors.Contains(otherId)) return false;
        if (brokenLinks.Contains(otherId)) return false;
        return true;
    }

    public void BreakLink(int otherId)
    {
        if (!brokenLinks.Contains(otherId))
        {
            brokenLinks.Add(otherId);
        }
    }

    public void ActivatePhysics()
    {
        if (rb == null) return;

        rb.isKinematic = false;
        rb.useGravity = true;
    }
}