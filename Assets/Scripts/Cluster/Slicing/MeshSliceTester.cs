using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshSliceTester : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Material capMaterial;
    [SerializeField] private bool useCameraUpAsNormal = true;
    [SerializeField] private Vector3 sliceNormal = Vector3.up;
    [SerializeField] private Key triggerKey = Key.Space;

    private Keyboard keyboard;

    private void Awake()
    {
        keyboard = Keyboard.current;
    }

    private void Update()
    {
        if (keyboard == null)
        {
            keyboard = Keyboard.current;
        }

        if (keyboard != null && keyboard[triggerKey].wasPressedThisFrame)
        {
            SliceNow();
        }
    }

    [ContextMenu("Slice Now")]
    public void SliceNow()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("MeshFilter or Mesh missing.");
            return;
        }

        Vector3 normal = useCameraUpAsNormal && cam != null
            ? cam.transform.up
            : sliceNormal.normalized;

        Plane plane = new Plane(normal, transform.position);

        MeshSliceResult result = MeshSlicer.Slice(
            mf.sharedMesh,
            plane,
            transform.localToWorldMatrix,
            transform.worldToLocalMatrix);

        if (!result.success)
        {
            Debug.LogWarning("Slice failed.");
            return;
        }

        CreatePieceObject("PositivePiece", result.positiveMesh, mr.sharedMaterial);
        CreatePieceObject("NegativePiece", result.negativeMesh, mr.sharedMaterial);

        gameObject.SetActive(false);
    }

    private void CreatePieceObject(string objName, Mesh mesh, Material baseMaterial)
    {
        GameObject go = new GameObject(objName);
        go.transform.position = transform.position;
        go.transform.rotation = transform.rotation;
        go.transform.localScale = transform.localScale;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = capMaterial != null ? capMaterial : baseMaterial;

        MeshCollider mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = true;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = 1f;
    }
}