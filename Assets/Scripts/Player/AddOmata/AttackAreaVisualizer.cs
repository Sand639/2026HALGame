using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AttackAreaVisualizer : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] private float radius = 2.5f;
    [SerializeField, Range(1f, 180f)] private float angle = 90f;
    [SerializeField] private int segmentCount = 24;
    [SerializeField] private float yOffset = 0.02f;

    [Header("Follow")]
    [SerializeField] private Transform followTarget;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh();
        mesh.name = "AttackAreaMesh";
        meshFilter.mesh = mesh;

        BuildMesh();
        Hide();
    }

    private void LateUpdate()
    {
        if (followTarget != null)
        {
            transform.position = new Vector3(
                followTarget.position.x,
                followTarget.position.y + yOffset,
                followTarget.position.z
            );

            Vector3 euler = followTarget.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }
    }

    public void SetShape(float newRadius, float newAngle)
    {
        radius = newRadius;
        angle = newAngle;
        BuildMesh();
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    public void Show()
    {
        meshRenderer.enabled = true;
    }

    public void Hide()
    {
        meshRenderer.enabled = false;
    }

    private void BuildMesh()
    {
        if (segmentCount < 1)
        {
            segmentCount = 1;
        }

        mesh.Clear();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        vertices.Add(Vector3.zero);
        uvs.Add(new Vector2(0.5f, 0.5f));

        float startAngle = -angle * 0.5f;
        float step = angle / segmentCount;

        for (int i = 0; i <= segmentCount; i++)
        {
            float currentAngle = startAngle + step * i;
            float rad = currentAngle * Mathf.Deg2Rad;

            Vector3 point = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * radius;
            vertices.Add(point);

            float u = (point.x / (radius * 2f)) + 0.5f;
            float v = (point.z / (radius * 2f)) + 0.5f;
            uvs.Add(new Vector2(u, v));
        }

        for (int i = 1; i <= segmentCount; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}