using System.Collections.Generic;
using UnityEngine;

public class SliceFracturePiece
{
    public Mesh mesh;
    public Bounds bounds;
}

public static class SliceFractureGenerator
{
    private class WorkPiece
    {
        public Mesh mesh;
        public Bounds bounds;
    }

    public static List<SliceFracturePiece> Generate(
    Mesh sourceMesh,
    SliceFractureSettings settings)
    {
        List<SliceFracturePiece> result = new List<SliceFracturePiece>();

        if (sourceMesh == null)
        {
            Debug.LogError("SliceFractureGenerator: sourceMesh is null");
            return result;
        }

        Debug.Log($"SliceFractureGenerator start: vertices={sourceMesh.vertexCount}, triangles={sourceMesh.triangles.Length / 3}");

        List<WorkPiece> workPieces = new List<WorkPiece>();

        Mesh initialMesh = Object.Instantiate(sourceMesh);
        initialMesh.RecalculateBounds();

        workPieces.Add(new WorkPiece
        {
            mesh = initialMesh,
            bounds = initialMesh.bounds
        });

        Debug.Log($"Initial bounds size = {initialMesh.bounds.size}");

        System.Random rng = new System.Random(settings.randomSeed);

        int targetSplits = Mathf.Max(0, settings.sliceCount);

        for (int splitIndex = 0; splitIndex < targetSplits; splitIndex++)
        {
            int pieceIndex = FindLargestPieceIndex(workPieces);
            if (pieceIndex < 0)
            {
                Debug.LogWarning("No largest piece found");
                break;
            }

            WorkPiece targetPiece = workPieces[pieceIndex];
            if (targetPiece.mesh == null)
            {
                Debug.LogWarning("Target piece mesh is null");
                break;
            }

            Debug.Log($"Split {splitIndex}: target bounds size = {targetPiece.bounds.size}");

            if (!CanSliceBounds(targetPiece.bounds, settings))
            {
                Debug.LogWarning("CanSliceBounds == false");
                continue;
            }

            bool sliced = false;

            for (int retry = 0; retry < settings.maxRetryPerSlice; retry++)
            {
                Plane localPlane = BuildRandomLocalPlane(targetPiece.bounds, settings, rng);

                MeshSliceResult sliceResult = MeshSlicer.Slice(
                    targetPiece.mesh,
                    localPlane,
                    Matrix4x4.identity,
                    Matrix4x4.identity);

                Debug.Log($"  retry {retry}: slice success = {sliceResult.success}");

                if (!sliceResult.success ||
                    sliceResult.positiveMesh == null ||
                    sliceResult.negativeMesh == null)
                {
                    continue;
                }

                sliceResult.positiveMesh.RecalculateBounds();
                sliceResult.negativeMesh.RecalculateBounds();

                Bounds pb = sliceResult.positiveMesh.bounds;
                Bounds nb = sliceResult.negativeMesh.bounds;

                Debug.Log($"  positive bounds = {pb.size}, negative bounds = {nb.size}");

                if (!IsValidPiece(pb, settings) || !IsValidPiece(nb, settings))
                {
                    Debug.LogWarning("  one of pieces is invalid");
                    continue;
                }

                workPieces.RemoveAt(pieceIndex);

                workPieces.Add(new WorkPiece
                {
                    mesh = sliceResult.positiveMesh,
                    bounds = pb
                });

                workPieces.Add(new WorkPiece
                {
                    mesh = sliceResult.negativeMesh,
                    bounds = nb
                });

                sliced = true;
                break;
            }

            if (!sliced)
            {
                Debug.LogWarning($"Split {splitIndex}: failed all retries");
                continue;
            }
        }

        for (int i = 0; i < workPieces.Count; i++)
        {
            if (workPieces[i].mesh == null) continue;

            result.Add(new SliceFracturePiece
            {
                mesh = workPieces[i].mesh,
                bounds = workPieces[i].bounds
            });
        }

        Debug.Log($"SliceFractureGenerator end: result count = {result.Count}");
        return result;
    }


    private static bool CanSliceBounds(Bounds bounds, SliceFractureSettings settings)
    {
        Vector3 s = bounds.size;
        return s.x >= settings.minAxisSize ||
               s.y >= settings.minAxisSize ||
               s.z >= settings.minAxisSize;
    }

    private static bool IsValidPiece(Bounds bounds, SliceFractureSettings settings)
    {
        Vector3 s = bounds.size;
        float volume = s.x * s.y * s.z;

        if (volume < settings.minPieceVolume)
        {
            return false;
        }

        if (s.x < settings.minAxisSize &&
            s.y < settings.minAxisSize &&
            s.z < settings.minAxisSize)
        {
            return false;
        }

        return true;
    }

    private static int FindLargestPieceIndex(List<WorkPiece> pieces)
    {
        if (pieces == null || pieces.Count == 0) return -1;

        int bestIndex = -1;
        float bestVolume = -1f;

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null || pieces[i].mesh == null) continue;

            Vector3 s = pieces[i].bounds.size;
            float volume = s.x * s.y * s.z;

            if (volume > bestVolume)
            {
                bestVolume = volume;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static Plane BuildRandomLocalPlane(
        Bounds bounds,
        SliceFractureSettings settings,
        System.Random rng)
    {
        Vector3 bias = settings.axisBias.sqrMagnitude > 0.0001f
            ? settings.axisBias.normalized
            : Vector3.up;

        Quaternion jitter = Quaternion.Euler(
            RandomRange(rng, -settings.angleJitter, settings.angleJitter),
            RandomRange(rng, -settings.angleJitter, settings.angleJitter),
            RandomRange(rng, -settings.angleJitter, settings.angleJitter));

        Vector3 normal = (jitter * bias).normalized;

        Vector3 center = bounds.center;
        Vector3 extent = bounds.extents * 0.35f;

        Vector3 point = new Vector3(
            RandomRange(rng, center.x - extent.x, center.x + extent.x),
            RandomRange(rng, center.y - extent.y, center.y + extent.y),
            RandomRange(rng, center.z - extent.z, center.z + extent.z)
        );

        return new Plane(normal, point);
    }

    private static float RandomRange(System.Random rng, float min, float max)
    {
        return (float)(min + (max - min) * rng.NextDouble());
    }
}