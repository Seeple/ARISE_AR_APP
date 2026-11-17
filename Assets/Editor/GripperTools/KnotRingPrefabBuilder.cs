using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class KnotRingPrefabBuilder
{
    public const string PrefabPath = "Assets/Resources/Prefab/KnotRing.prefab";
    const string MeshPath = "Assets/Resources/GripperModels/KnotRing.asset";
    const string MaterialPath = "Assets/Resources/Material/KnotRing.mat";

    [MenuItem("Tools/ARISE/Build Knot Ring Prefab")]
    public static void Build()
    {
        Mesh mesh = SaveAsset(BuildRing(), MeshPath);
        Shader shader = Shader.Find("ARISE/Gripper Vertex Color");
        var material = new Material(shader) { name = "Knot Ring", color = new Color(1f, 1f, 1f, .5f) };
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        material = SaveAsset(material, MaterialPath);

        var ring = new GameObject("KnotRing");
        try
        {
            ring.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            PrefabUtility.SaveAsPrefabAsset(ring, PrefabPath);
        }
        finally { Object.DestroyImmediate(ring); }
        AssetDatabase.SaveAssets();
    }

    static Mesh BuildRing()
    {
        const int along = 64, around = 12;
        float tube = KnotGraspPose.CordDiameter * .5f;
        float a = KnotGraspPose.OuterMinorDiameter * .5f - tube;
        float b = KnotGraspPose.OuterMajorDiameter * .5f - tube;
        var vertices = new Vector3[along * around];
        var normals = new Vector3[vertices.Length];
        var colors = new Color32[vertices.Length];
        var indices = new int[vertices.Length * 6];

        for (int i = 0; i < along; i++)
        {
            float t = i * 2f * Mathf.PI / along;
            float x = Mathf.Cos(t), y = Mathf.Sin(t);
            Vector3 center = new Vector3(a * x, b * y, 0f);
            Vector3 outward = new Vector3(x / a, y / b, 0f).normalized;
            for (int j = 0; j < around; j++)
            {
                float s = j * 2f * Mathf.PI / around;
                int index = i * around + j;
                normals[index] = Mathf.Cos(s) * outward + Mathf.Sin(s) * Vector3.forward;
                vertices[index] = center + tube * normals[index];
                float twist = .5f + .5f * Mathf.Cos(3f * s - 14f * t);
                colors[index] = Color32.Lerp(new Color32(178, 19, 36, 0), new Color32(239, 47, 60, 0), twist);

                int across = i * around + (j + 1) % around;
                int next = ((i + 1) % along) * around;
                int start = index * 6;
                indices[start] = index;
                indices[start + 1] = next + j;
                indices[start + 2] = across;
                indices[start + 3] = across;
                indices[start + 4] = next + j;
                indices[start + 5] = next + (j + 1) % around;
            }
        }

        var mesh = new Mesh { name = "Knot ring (60 x 50 x 9 mm)", vertices = vertices, normals = normals,
            colors32 = colors, triangles = indices };
        mesh.RecalculateBounds();
        return mesh;
    }

    static T SaveAsset<T>(T value, string path) where T : Object
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(value, path);
            return value;
        }
        EditorUtility.CopySerialized(value, existing);
        Object.DestroyImmediate(value);
        return existing;
    }
}
