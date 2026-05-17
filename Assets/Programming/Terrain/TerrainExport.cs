using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class TerrainToObjExporter
{
    [MenuItem("Tools/Terrain/Export Selected Terrain To OBJ")]
    private static void ExportSelectedTerrain()
    {
        Terrain terrain = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<Terrain>()
            : null;

        if (terrain == null)
        {
            Debug.LogError("Выдели объект с Terrain.");
            return;
        }

        string path = EditorUtility.SaveFilePanel(
            "Export Terrain To OBJ",
            Application.dataPath,
            terrain.name + "_Terrain.obj",
            "obj"
        );

        if (string.IsNullOrEmpty(path))
            return;

        ExportTerrainToObj(terrain, path);

        Debug.Log("Terrain exported: " + path);
    }

    private static void ExportTerrainToObj(Terrain terrain, string path)
    {
        TerrainData data = terrain.terrainData;

        int heightmapResolution = data.heightmapResolution;

        // Чем больше step, тем легче меш.
        // 1 = максимум деталей, 2 = в 4 раза легче, 4 = в 16 раз легче.
        int step = 2;

        int vertexCountX = (heightmapResolution - 1) / step + 1;
        int vertexCountZ = (heightmapResolution - 1) / step + 1;

        Vector3 terrainSize = data.size;
        Vector3 terrainPosition = terrain.transform.position;

        StringBuilder obj = new StringBuilder();

        obj.AppendLine("# Exported Unity Terrain");
        obj.AppendLine("o " + terrain.name);

        // Vertices
        for (int z = 0; z < vertexCountZ; z++)
        {
            for (int x = 0; x < vertexCountX; x++)
            {
                int heightX = Mathf.Min(x * step, heightmapResolution - 1);
                int heightZ = Mathf.Min(z * step, heightmapResolution - 1);

                float normalizedHeight = data.GetHeight(heightX, heightZ);

                float worldX = ((float)heightX / (heightmapResolution - 1)) * terrainSize.x;
                float worldZ = ((float)heightZ / (heightmapResolution - 1)) * terrainSize.z;
                float worldY = normalizedHeight;

                Vector3 vertex = new Vector3(
                    worldX + terrainPosition.x,
                    worldY + terrainPosition.y,
                    worldZ + terrainPosition.z
                );

                // OBJ: X Y Z
                obj.AppendLine($"v {vertex.x} {vertex.y} {vertex.z}");
            }
        }

        // UV
        for (int z = 0; z < vertexCountZ; z++)
        {
            for (int x = 0; x < vertexCountX; x++)
            {
                float u = x / (float)(vertexCountX - 1);
                float v = z / (float)(vertexCountZ - 1);

                obj.AppendLine($"vt {u} {v}");
            }
        }

        // Faces
        for (int z = 0; z < vertexCountZ - 1; z++)
        {
            for (int x = 0; x < vertexCountX - 1; x++)
            {
                int bottomLeft = z * vertexCountX + x + 1;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + vertexCountX;
                int topRight = topLeft + 1;

                obj.AppendLine($"f {bottomLeft}/{bottomLeft} {topLeft}/{topLeft} {bottomRight}/{bottomRight}");
                obj.AppendLine($"f {bottomRight}/{bottomRight} {topLeft}/{topLeft} {topRight}/{topRight}");
            }
        }

        File.WriteAllText(path, obj.ToString());
    }
}