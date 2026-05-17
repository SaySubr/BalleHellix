using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HeightmapMeshTerrain : MonoBehaviour
{
    [Header("Heightmap")]
    [SerializeField] private Texture2D heightmap;
    [SerializeField] private int resolution = 128;
    [SerializeField] private float heightMultiplier = 20f;

    [Header("Size")]
    [SerializeField] private float width = 100f;
    [SerializeField] private float length = 100f;

    [Header("Transform")]
    [SerializeField] private Vector3 position;
    [SerializeField] private Vector3 rotation;
    [SerializeField] private Vector3 scale = Vector3.one;

    [Header("Apply")]
    [SerializeField] private bool generateNow;

    private void OnValidate()
    {
        if (generateNow)
        {
            generateNow = false;
            Generate();
        }

        ApplyTransform();
    }

    private void ApplyTransform()
    {
        transform.position = position;
        transform.rotation = Quaternion.Euler(rotation);
        transform.localScale = scale;
    }

    public void Generate()
    {
        if (heightmap == null)
            return;

        resolution = Mathf.Clamp(resolution, 2, 255);

        Mesh mesh = new Mesh();
        mesh.name = "Heightmap Mesh Terrain";

        Vector3[] vertices = new Vector3[resolution * resolution];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u = x / (float)(resolution - 1);
                float v = z / (float)(resolution - 1);

                float pixelHeight = heightmap.GetPixelBilinear(u, v).grayscale;
                float y = pixelHeight * heightMultiplier;

                int index = z * resolution + x;

                vertices[index] = new Vector3(
                    u * width - width * 0.5f,
                    y,
                    v * length - length * 0.5f
                );

                uvs[index] = new Vector2(u, v);
            }
        }

        int triangleIndex = 0;

        for (int z = 0; z < resolution - 1; z++)
        {
            for (int x = 0; x < resolution - 1; x++)
            {
                int bottomLeft = z * resolution + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + resolution;
                int topRight = topLeft + 1;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;

                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
            }
        }

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;

        ApplyTransform();
    }
}