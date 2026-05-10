using System;
using System.Collections.Generic;
using Config;
using MainMenu;
using UnityEngine;


[Serializable]
public struct LevelTheme
{
    public string themeName;
    public Color safeColor;
    public Color dangerColor;
    public Color poleColor;
    public Color ballColor;
}

public class LevelGenerator : MonoBehaviour
{
    
    
    public static LevelGenerator instance {private set; get;}

    private void Awake()
    {
        instance = this;
    }

    [Header("Level Settings")] 
    public int numberOfLevels = 10;
    public float levelHeight = 4f;
    
    [Range(0.1f,2.0f)] public float poleRadius;

    [Header("Themes")] public List<LevelTheme> themes;
    
    [Header("Mesh Smoothness")] 
    [Range(40, 120)] public int segments = 80;
    [Range(0, 1)] public float gapPercentage = 0.2f;

    [Header("Dimensions")] 
    public float innerRadius = 0.5f;
    public float outerRadius = 2.8f;
    public float thickness = 0.2f;
    
    [Header("Danger Zone Settings")]
    [Range(1,3)] public int maxDangerZones = 2;
    [Range(5, 30)] public int dangerZoneSize = 10;

    [Header("Materials")]
    public Material safeMaterial;
    public Material dangerMaterial;
    public Material poleMaterial;
    public Material goalMaterial;
    
    private void SpawnCenterPole()
    {
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Center Pole";
        pole.transform.SetParent(this.transform);
        float totalHeight =  numberOfLevels * levelHeight;
        pole.transform.localScale = new Vector3(poleRadius *2, totalHeight /2f, poleRadius * 2);
        pole.transform.position = new Vector3(0, -(totalHeight/2) + (levelHeight / 2f), 0);
        if (poleMaterial) pole.GetComponent<Renderer>().material = poleMaterial;
    }

    private void ApplyRandomTheme()
    {
        if (themes != null && themes.Count ==0)
        {
            return;
        }
        
        LevelTheme selected = themes[UnityEngine.Random.Range(0, themes.Count)];
        
        safeMaterial.color = selected.safeColor;
        dangerMaterial.color = selected.dangerColor;
        poleMaterial.color = selected.poleColor;
        
        GameObject ball = GameObject.FindGameObjectWithTag("Player");

        if (ball != null)
        {
            ball.GetComponent<Renderer>().material.color = selected.ballColor;
        }
    }

    private void CreateLevelFloor(int levelIndex)
    {
        GameObject floorObj = new GameObject($"Floor {levelIndex}");
        floorObj.transform.parent = this.transform;
        floorObj.transform.position = new Vector3(0, -levelIndex * levelHeight, 0);
        
        // gap settings
        int gapStart = UnityEngine.Random.Range(0, segments);
        int gapCount = Mathf.RoundToInt(segments * gapPercentage);
        floorObj.transform.rotation = Quaternion.Euler(0,UnityEngine.Random.Range(0,360f), 0);
        floorObj.tag = "Platform";

        MeshFilter mf = floorObj.AddComponent<MeshFilter>();
        MeshRenderer mr = floorObj.AddComponent<MeshRenderer>();
        MeshCollider mc = floorObj.AddComponent<MeshCollider>();

        mr.materials = new Material [] {safeMaterial, dangerMaterial};

        mf.mesh = GenerateDonutMesh(gapStart, gapCount);
        mc.sharedMesh = mf.mesh;
        
        AddPassTrigger(floorObj,gapStart, gapCount);
    }

    private Mesh GenerateDonutMesh(int gapStart, int gapCount)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> safeTris = new List<int>();
        List<int> dangerTris = new List<int>();
        
        float angleStep = 360f / segments;
        bool[] dangerMap = GenerateDangerMap();

        for (int i = 0; i <= segments; i++)
        {
            float rad = i * angleStep * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            
            vertices.Add(new Vector3(sin * innerRadius,  thickness / 2,  cos * innerRadius ));
            vertices.Add(new Vector3(sin * outerRadius,  thickness / 2,  cos * outerRadius ));
            vertices.Add(new Vector3(sin * innerRadius,  -thickness / 2,  cos * innerRadius ));
            vertices.Add(new Vector3(sin * outerRadius,  -thickness / 2,  cos * outerRadius ));
            
        }

        for (int i = 0; i < segments; i++)
        {
            if (IsIndexInGap(i,gapStart,gapCount)) continue;
            
            List<int> targetTris = dangerMap[i] ? dangerTris : safeTris;
            int curr = i * 4;
            int next = (i + 1) * 4;

            AddQuad(targetTris, curr + 0, curr + 1, next + 0, next+1);
            AddQuad(targetTris, next + 2, next + 3, curr + 2, curr+3);
            AddQuad(targetTris, curr + 1, curr + 3, next + 1, next+3);
            AddQuad(targetTris, next + 0, next + 2, curr + 0, curr+2);
            
            if (IsIndexInGap(i+1, gapStart,gapCount)) AddQuad(targetTris, next + 0, next + 1, next + 2, next+3);
            if (IsIndexInGap(i-1, gapStart,gapCount)) AddQuad(targetTris, curr + 1, curr + 0, curr + 3, curr+2);
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 2;
        mesh.SetTriangles(safeTris,0);
        mesh.SetTriangles(dangerTris,1);
        mesh.RecalculateNormals();
        return mesh;
    }

    public Mesh GenerateShatterMesh(int start, int count)
    {
        Mesh mesh = new Mesh();
        List <Vector3> verts = new List<Vector3>();
        List <int> tris = new List<int>();
        
        float angleStep = 360f / count;

        for (int i = 0; i <= count; i++)
        {
            float rad = (start +i) * angleStep * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            
            verts.Add(new Vector3(sin * innerRadius, thickness / 2, cos * innerRadius));
            verts.Add(new Vector3(sin * outerRadius, thickness / 2, cos * outerRadius));
            verts.Add(new Vector3(sin * innerRadius, -thickness / 2, cos * innerRadius));
            verts.Add(new Vector3(sin * outerRadius, -thickness / 2, cos * outerRadius));
        }

        for (int i = 0; i < count; i++)
        {
            int b = i * 4;
            AddQuad(tris,b+0, b+1, b+4, b+5);
            AddQuad(tris,b+6, b+7, b+2, b+3);
            AddQuad(tris,b+1, b+5, b+3, b+7);
            AddQuad(tris,b+2, b+6, b+0, b+4);
            
            if (i == 0) AddQuad(tris,b+1, b+0, b+3, b+2);
            if (i == count-1)  AddQuad(tris,b+4, b+5, b+6, b+7);
        }
        
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    private void AddPassTrigger(GameObject floor, int gapStart, int gapCount)
    {
        GameObject triggerObj = new GameObject("PassTrigger");
        triggerObj.transform.SetParent(floor.transform);
        triggerObj.transform.localPosition = new Vector3(0,-1,0);

        float angleStep = 360f / segments;
        float centerAngle = (gapStart + (gapCount / 2f)) * angleStep;
        triggerObj.transform.localRotation  = Quaternion.Euler(0,centerAngle,0);
        
        BoxCollider box = triggerObj.AddComponent<BoxCollider>();
        box.isTrigger = true;

        float gapWidth = outerRadius - innerRadius;
        box.size = new Vector3(gapWidth * 2.5f, 2f, 1f);
        box.center = new Vector3(0, 0, (innerRadius + outerRadius) / 2f);

        triggerObj.AddComponent<PassDetector>();

    } 
    

    void AddQuad(List<int> tris, int v0, int v1, int v2, int v3)
    {
        tris.Add(v0); tris.Add(v1); tris.Add(v2);
        tris.Add(v1); tris.Add(v3); tris.Add(v2);
    }

    private bool IsIndexInGap(int i, int start,int count)
    {
        i = (i+segments) % segments;
        for (int j = 0; j < count; j++)
        {
            if ((start + j) % segments == i)
            {
                return true;
            } 
        }

        return false;
    }

    private bool[] GenerateDangerMap()
    {
        bool [] map = new bool[segments];
        int zones = UnityEngine.Random.Range(1,maxDangerZones);

        for (int z = 0; z < zones; z++)
        {
            int start = UnityEngine.Random.Range(0, segments);
            for (int s = 0; s < dangerZoneSize; s++)
            {
                map[(start + s)%segments] = true;
            }
        }
        
        return map;
    }

    private void Start()
    {
        ApplySelectedLevelSettings();
        GenerateLevel();
    }

    private void ApplySelectedLevelSettings()
    {
        if (GameLauncher.Instance == null)
            return;

        LevelData levelData = GameLauncher.Instance.GetCurrentLevelData();
        if (levelData == null || levelData.EffectiveGameType != LevelGameType.Helix)
            return;

        ApplySettings(levelData.helix);
    }

    public void ApplySettings(HelixLevelSettings settings)
    {
        if (settings == null)
            return;

        numberOfLevels = Mathf.Max(1, settings.numberOfLevels);
        levelHeight = Mathf.Max(0.1f, settings.levelHeight);
        poleRadius = Mathf.Clamp(settings.poleRadius, 0.1f, 2f);
        segments = Mathf.Clamp(settings.segments, 40, 120);
        gapPercentage = Mathf.Clamp01(settings.gapPercentage);
        innerRadius = Mathf.Max(0.1f, settings.innerRadius);
        outerRadius = Mathf.Max(innerRadius + 0.1f, settings.outerRadius);
        thickness = Mathf.Max(0.01f, settings.thickness);
        maxDangerZones = Mathf.Clamp(settings.maxDangerZones, 1, 3);
        dangerZoneSize = Mathf.Clamp(settings.dangerZoneSize, 5, 30);
    }

    private void SpawnGoalTrigger()
    {
        GameObject triggerObj = new GameObject("GoalTrigger");
        triggerObj.transform.SetParent(transform);
        triggerObj.transform.position = new Vector3(0, -(numberOfLevels * levelHeight), 0);
        
        BoxCollider box = triggerObj.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(outerRadius * 5f,2f,outerRadius * 5f);
        triggerObj.tag = "Goal";
        
        GameObject visualPad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visualPad.name = "GoalVisual";
        visualPad.transform.SetParent(transform);
        visualPad.transform.position = new Vector3(0,-(numberOfLevels * levelHeight),0);
        visualPad.transform.localScale = new Vector3(outerRadius*2.5f,0.1f,outerRadius * 2.5f);
        
        Destroy(visualPad.GetComponent<Collider>());

        if (goalMaterial != null)
        {
            visualPad.GetComponent<Renderer>().material = goalMaterial;
        }
    }
    

    public void GenerateLevel()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        
        for (int i = 0; i < numberOfLevels; i++)
        {
            CreateLevelFloor(i);
        }
        
        SpawnCenterPole();
        SpawnGoalTrigger();
        
        ApplyRandomTheme();
    }
}
