using System.Collections.Generic;
using UnityEngine;

public static class MeshCombineUtility
{
    public static Mesh CombineMeshesToLocal(GameObject sourceRoot)
    {
        if (sourceRoot == null) return null;

        MeshFilter[] meshFilters = sourceRoot.GetComponentsInChildren<MeshFilter>(true);
        if (meshFilters == null || meshFilters.Length == 0) return null;

        List<CombineInstance> combines = new List<CombineInstance>();
        Matrix4x4 rootWorldToLocal = sourceRoot.transform.worldToLocalMatrix;

        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter mf = meshFilters[i];
            if (mf == null || mf.sharedMesh == null) continue;

            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null) continue;

            Mesh mesh = mf.sharedMesh;

            int subMeshCount = Mathf.Max(1, mesh.subMeshCount);
            for (int sub = 0; sub < subMeshCount; sub++)
            {
                CombineInstance ci = new CombineInstance();
                ci.mesh = mesh;
                ci.subMeshIndex = sub;

                // sourceRoot ローカル空間へ変換
                ci.transform = rootWorldToLocal * mf.transform.localToWorldMatrix;
                combines.Add(ci);
            }
        }

        if (combines.Count == 0) return null;

        Mesh combined = new Mesh();
        combined.name = sourceRoot.name + "_CombinedForSlice";

        if (NeedUInt32IndexFormat(combines))
        {
            combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        // 1つのsubmeshにまとめる
        combined.CombineMeshes(combines.ToArray(), true, true, false);

        if (combined.normals == null || combined.normals.Length == 0)
        {
            combined.RecalculateNormals();
        }

        if (combined.uv == null || combined.uv.Length == 0)
        {
            Vector2[] uvs = new Vector2[combined.vertexCount];
            combined.uv = uvs;
        }

        combined.RecalculateBounds();
        return combined;
    }

    private static bool NeedUInt32IndexFormat(List<CombineInstance> combines)
    {
        long totalVertexCount = 0;

        for (int i = 0; i < combines.Count; i++)
        {
            if (combines[i].mesh == null) continue;
            totalVertexCount += combines[i].mesh.vertexCount;
            if (totalVertexCount > 65000)
            {
                return true;
            }
        }

        return false;
    }
}