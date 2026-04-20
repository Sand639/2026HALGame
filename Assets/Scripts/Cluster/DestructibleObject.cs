using UnityEngine;

public class DestructibleObject : MonoBehaviour
{
    [SerializeField] private GameObject intactRoot;
    [SerializeField] private GameObject fracturedRoot;
    [SerializeField] private ConnectedClusterDestruction cluster;
    [SerializeField] private bool broken = false;

    private bool initialized = false;

    private void Awake()
    {
        if (cluster == null && fracturedRoot != null)
        {
            cluster = fracturedRoot.GetComponent<ConnectedClusterDestruction>();
        }

        if (cluster != null)
        {
            cluster.BuildCluster();
        }

        SetBrokenState(false);
        initialized = true;
    }

    public void BreakAt(Vector3 hitPoint)
    {
        if (!broken)
        {
            SetBrokenState(true);
        }

        if (cluster != null)
        {
            cluster.DamageAt(hitPoint);
        }
    }

    public void SetBrokenState(bool isBroken)
    {
        broken = isBroken;

        if (intactRoot != null)
        {
            intactRoot.SetActive(!isBroken);
        }

        if (fracturedRoot != null)
        {
            fracturedRoot.SetActive(isBroken);
        }
    }
}