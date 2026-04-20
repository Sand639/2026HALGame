using UnityEngine;

public static class MeshPieceObjectFactory
{
    public static GameObject CreatePieceObject(
        Transform parent,
        Mesh sourceMesh,
        int pieceId,
        Material material,
        bool createConvexMeshCollider = true,
        bool activatePhysicsImmediately = false)
    {
        if (sourceMesh == null)
        {
            return null;
        }

        Mesh mesh = Object.Instantiate(sourceMesh);
        mesh.RecalculateBounds();

        Bounds bounds = mesh.bounds;
        Vector3 pivot = bounds.center;

        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= pivot;
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        GameObject go = new GameObject("Piece_" + pieceId);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pivot;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        if (material != null)
        {
            mr.sharedMaterial = material;
        }

        if (createConvexMeshCollider)
        {
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
        }
        else
        {
            BoxCollider bc = go.AddComponent<BoxCollider>();
            Bounds b = mesh.bounds;
            bc.center = b.center;
            bc.size = b.size;
        }

        Rigidbody rb = go.AddComponent<Rigidbody>();

        if (activatePhysicsImmediately)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        else
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        ClusterPiece piece = go.AddComponent<ClusterPiece>();
        piece.CaptureInitialTransform();

        return go;
    }
}