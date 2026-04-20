using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DestructibleBuilderWindow : EditorWindow
{
    private GameObject sourceObject;
    private Material pieceMaterial;

    private FractureMode fractureMode = FractureMode.Voxel;

    private bool useColliderFilter = true;
    private float surfaceTolerance = 0.15f;
    private float pieceSpacing = 0.01f;
    private bool autoSupportLowest = true;

    private VoxelFractureSettings voxelSettings = new VoxelFractureSettings();
    private VoronoiFractureSettings voronoiSettings = new VoronoiFractureSettings();
    private SliceFractureSettings sliceSettings = new SliceFractureSettings();

    [MenuItem("Tools/Cluster Destruction/Destructible Builder")]
    public static void Open()
    {
        GetWindow<DestructibleBuilderWindow>("Destructible Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Source", EditorStyles.boldLabel);
        sourceObject = (GameObject)EditorGUILayout.ObjectField(
            "Source Object",
            sourceObject,
            typeof(GameObject),
            true);

        pieceMaterial = (Material)EditorGUILayout.ObjectField(
            "Piece Material",
            pieceMaterial,
            typeof(Material),
            false);

        GUILayout.Space(8);
        GUILayout.Label("Common", EditorStyles.boldLabel);

        fractureMode = (FractureMode)EditorGUILayout.EnumPopup("Fracture Mode", fractureMode);
        useColliderFilter = EditorGUILayout.Toggle("Use Collider Filter", useColliderFilter);
        surfaceTolerance = Mathf.Max(0f, EditorGUILayout.FloatField("Surface Tolerance", surfaceTolerance));
        pieceSpacing = Mathf.Max(0f, EditorGUILayout.FloatField("Piece Spacing", pieceSpacing));
        autoSupportLowest = EditorGUILayout.Toggle("Auto Support Lowest", autoSupportLowest);

        GUILayout.Space(8);
        GUILayout.Label("Mode Settings", EditorStyles.boldLabel);

        switch (fractureMode)
        {
            case FractureMode.Voxel:
                voxelSettings.gridX = Mathf.Max(1, EditorGUILayout.IntField("Grid X", voxelSettings.gridX));
                voxelSettings.gridY = Mathf.Max(1, EditorGUILayout.IntField("Grid Y", voxelSettings.gridY));
                voxelSettings.gridZ = Mathf.Max(1, EditorGUILayout.IntField("Grid Z", voxelSettings.gridZ));
                break;

            case FractureMode.Voronoi:
                voronoiSettings.seedCount = Mathf.Max(1, EditorGUILayout.IntField("Seed Count", voronoiSettings.seedCount));
                voronoiSettings.gridX = Mathf.Max(1, EditorGUILayout.IntField("Grid X", voronoiSettings.gridX));
                voronoiSettings.gridY = Mathf.Max(1, EditorGUILayout.IntField("Grid Y", voronoiSettings.gridY));
                voronoiSettings.gridZ = Mathf.Max(1, EditorGUILayout.IntField("Grid Z", voronoiSettings.gridZ));
                voronoiSettings.randomSeed = EditorGUILayout.IntField("Random Seed", voronoiSettings.randomSeed);
                break;

            case FractureMode.Slice:
                sliceSettings.sliceCount = Mathf.Clamp(EditorGUILayout.IntField("Slice Count", sliceSettings.sliceCount), 1, 16);
                sliceSettings.gridX = Mathf.Max(1, EditorGUILayout.IntField("Grid X", sliceSettings.gridX));
                sliceSettings.gridY = Mathf.Max(1, EditorGUILayout.IntField("Grid Y", sliceSettings.gridY));
                sliceSettings.gridZ = Mathf.Max(1, EditorGUILayout.IntField("Grid Z", sliceSettings.gridZ));
                sliceSettings.randomSeed = EditorGUILayout.IntField("Random Seed", sliceSettings.randomSeed);
                sliceSettings.angleJitter = Mathf.Max(0f, EditorGUILayout.FloatField("Angle Jitter", sliceSettings.angleJitter));
                sliceSettings.axisBias = EditorGUILayout.Vector3Field("Axis Bias", sliceSettings.axisBias);
                sliceSettings.minPieceVolume = Mathf.Max(0.000001f, EditorGUILayout.FloatField("Min Piece Volume", sliceSettings.minPieceVolume));
                sliceSettings.minAxisSize = Mathf.Max(0.0001f, EditorGUILayout.FloatField("Min Axis Size", sliceSettings.minAxisSize));
                sliceSettings.maxRetryPerSlice = Mathf.Max(1, EditorGUILayout.IntField("Max Retry Per Slice", sliceSettings.maxRetryPerSlice));
                break;
        }

        GUILayout.Space(16);

        GUI.enabled = sourceObject != null;
        if (GUILayout.Button("Build Destructible"))
        {
            BuildDestructible();
        }
        GUI.enabled = true;
    }

    private void BuildDestructible()
    {
        if (sourceObject == null) return;

        Bounds localBounds = FractureBuilderCore.CalculateLocalBounds(sourceObject);
        Collider[] sourceColliders = sourceObject.GetComponentsInChildren<Collider>();

        FractureBuildContext context = new FractureBuildContext
        {
            sourceObject = sourceObject,
            localBounds = localBounds,
            sourceColliders = sourceColliders,
            useColliderFilter = useColliderFilter,
            surfaceTolerance = surfaceTolerance,
            pieceSpacing = pieceSpacing,
            pieceMaterial = pieceMaterial
        };

        List<FracturePieceData> pieces = null;
        List<SliceFracturePiece> slicePieces = null;

        if (fractureMode == FractureMode.Slice)
        {
            Mesh combinedMesh = MeshCombineUtility.CombineMeshesToLocal(sourceObject);
            if (combinedMesh == null)
            {
                Debug.LogError("combinedMesh is null");
                EditorUtility.DisplayDialog("Build Failed", "Slice モード用の結合メッシュを作れなかった。", "OK");
                return;
            }

            Debug.Log($"Combined Mesh: vertices={combinedMesh.vertexCount}, triangles={combinedMesh.triangles.Length / 3}, bounds={combinedMesh.bounds.size}");

            slicePieces = SliceFractureGenerator.Generate(
                combinedMesh,
                sliceSettings);

            Debug.Log($"Slice piece count = {(slicePieces == null ? -1 : slicePieces.Count)}");

            if (slicePieces == null || slicePieces.Count == 0)
            {
                EditorUtility.DisplayDialog("Build Failed", "Slice 破片の生成に失敗した。", "OK");
                return;
            }
        }
        else
        {
            pieces = FractureBuilderCore.GeneratePieces(
                fractureMode,
                context,
                voxelSettings,
                voronoiSettings,
                sliceSettings);

            if (pieces == null || pieces.Count == 0)
            {
                EditorUtility.DisplayDialog("Build Failed", "破片が1つも生成されなかった。Collider Filter や解像度を見直して。", "OK");
                return;
            }
        }

        GameObject root = new GameObject("Destructible_" + sourceObject.name);
        Undo.RegisterCreatedObjectUndo(root, "Create Destructible Root");

        root.transform.position = sourceObject.transform.position;
        root.transform.rotation = sourceObject.transform.rotation;
        root.transform.localScale = sourceObject.transform.lossyScale;

        GameObject intactRoot = new GameObject("IntactRoot");
        GameObject fracturedRoot = new GameObject("FracturedRoot");

        Undo.RegisterCreatedObjectUndo(intactRoot, "Create Intact Root");
        Undo.RegisterCreatedObjectUndo(fracturedRoot, "Create Fractured Root");

        intactRoot.transform.SetParent(root.transform, false);
        fracturedRoot.transform.SetParent(root.transform, false);

        intactRoot.transform.localPosition = Vector3.zero;
        intactRoot.transform.localRotation = Quaternion.identity;
        intactRoot.transform.localScale = Vector3.one;

        fracturedRoot.transform.localPosition = Vector3.zero;
        fracturedRoot.transform.localRotation = Quaternion.identity;
        fracturedRoot.transform.localScale = Vector3.one;

        GameObject intactCopy = Instantiate(sourceObject, intactRoot.transform);
        Undo.RegisterCreatedObjectUndo(intactCopy, "Create Intact Copy");
        intactCopy.name = sourceObject.name;
        intactCopy.transform.localPosition = Vector3.zero;
        intactCopy.transform.localRotation = Quaternion.identity;
        intactCopy.transform.localScale = Vector3.one;

        if (fractureMode == FractureMode.Slice)
        {
            for (int i = 0; i < slicePieces.Count; i++)
            {
                GameObject pieceObject = MeshPieceObjectFactory.CreatePieceObject(
                    fracturedRoot.transform,
                    slicePieces[i].mesh,
                    i,
                    pieceMaterial,
                    true);

                Undo.RegisterCreatedObjectUndo(pieceObject, "Create Slice Piece");
            }
        }
        else
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                GameObject pieceObject = FractureBuilderCore.CreatePieceObject(
                    fracturedRoot.transform,
                    pieces[i],
                    pieceMaterial,
                    pieceSpacing);

                Undo.RegisterCreatedObjectUndo(pieceObject, "Create Fracture Piece");
            }
        }

        ConnectedClusterDestruction cluster = fracturedRoot.GetComponent<ConnectedClusterDestruction>();
        if (cluster == null)
        {
            cluster = fracturedRoot.AddComponent<ConnectedClusterDestruction>();
        }

        SetBoolProperty(cluster, "autoCollectChildren", true);
        SetBoolProperty(cluster, "autoMarkLowestAsSupport", autoSupportLowest);

        cluster.BuildCluster();

        DestructibleObject destructible = root.GetComponent<DestructibleObject>();
        if (destructible == null)
        {
            destructible = root.AddComponent<DestructibleObject>();
        }

        AssignDestructibleFields(destructible, intactRoot, fracturedRoot, cluster);

        fracturedRoot.SetActive(false);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
    }

    private void AssignDestructibleFields(
        DestructibleObject destructible,
        GameObject intactRoot,
        GameObject fracturedRoot,
        ConnectedClusterDestruction cluster)
    {
        SerializedObject so = new SerializedObject(destructible);

        so.FindProperty("intactRoot").objectReferenceValue = intactRoot;
        so.FindProperty("fracturedRoot").objectReferenceValue = fracturedRoot;
        so.FindProperty("cluster").objectReferenceValue = cluster;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private void SetBoolProperty(Object target, string propertyName, bool value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}