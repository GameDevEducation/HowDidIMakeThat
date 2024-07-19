using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum EFaceOrientation
{
    Front,
    Left,
    Right,
    Back,
    Top,
    Bottom,
}

[System.Flags]
public enum EFaceFlags
{
    None = 0x00,

    Front = 0x01,
    Left = 0x02,
    Right = 0x04,
    Back = 0x08,
    Top = 0x10,
    Bottom = 0x20,

    All = Front | Left | Right | Back | Top | Bottom
}

public class BlockVisualAsset : ScriptableObject
{
    [SerializeField] Texture2D AtlasTexture;
    [SerializeField] SerializableDictionary<EFaceFlags, Mesh> Meshes;

    public SerializableDictionary<EFaceFlags, Mesh> GetMeshes()
    {
        return Meshes;
    }

    public Material GetMaterialInstanceForAtlas(Material InReferenceMaterial)
    {
        Material AtlasMaterialInstance = Material.Instantiate(InReferenceMaterial);
        AtlasMaterialInstance.mainTexture = AtlasTexture;

        return AtlasMaterialInstance;
    }

    public GameObject SpawnGameObject(EFaceFlags InFlags, Vector3 InPosition, Quaternion InRotation, Transform InParent, Material InMaterial)
    {
        var SelectedMesh = Meshes[InFlags];

        GameObject NewGO = new GameObject(SelectedMesh.name);
        NewGO.transform.SetParent(InParent, true);
        NewGO.transform.position = InPosition;
        NewGO.transform.rotation = InRotation;

        NewGO.AddComponent<MeshFilter>().mesh = SelectedMesh;
        NewGO.AddComponent<MeshRenderer>().material = InMaterial;

        return NewGO;
    }

#if UNITY_EDITOR
    static string FlagsToName(EFaceFlags InFlags)
    {
        List<string> NameElements = new();

        if (InFlags.HasFlag(EFaceFlags.Front))
            NameElements.Add("Front");
        if (InFlags.HasFlag(EFaceFlags.Left))
            NameElements.Add("Left");
        if (InFlags.HasFlag(EFaceFlags.Right))
            NameElements.Add("Right");
        if (InFlags.HasFlag(EFaceFlags.Back))
            NameElements.Add("Back");
        if (InFlags.HasFlag(EFaceFlags.Top))
            NameElements.Add("Top");
        if (InFlags.HasFlag(EFaceFlags.Bottom))
            NameElements.Add("Bottom");

        return string.Join("+", NameElements);
    }

    public static BlockVisualAsset Build(Texture2D InAtlasTexture, Rect[] InFaceTextureUVs, float InBlockSize)
    {
        var NewVA = ScriptableObject.CreateInstance<BlockVisualAsset>();
        NewVA.AtlasTexture = InAtlasTexture;
        NewVA.Meshes = new();

        float HalfBlockSize = InBlockSize * 0.5f;

        // generate all mesh combinations
        for (int FaceFlagInt = 1; FaceFlagInt <= (int)EFaceFlags.All; FaceFlagInt++)
        {
            EFaceFlags FaceFlags = (EFaceFlags)FaceFlagInt;

            List<Vector3> Vertices = new();
            List<Vector2> UVs = new();
            int NumFaces = 0;

            if (FaceFlags.HasFlag(EFaceFlags.Top))
            {
                AddTop(HalfBlockSize, InFaceTextureUVs[(int)EFaceOrientation.Top], Vertices, UVs);
                ++NumFaces;
            }
            if (FaceFlags.HasFlag(EFaceFlags.Back))
            {
                AddBack(HalfBlockSize, InFaceTextureUVs[(int)EFaceOrientation.Back], Vertices, UVs);
                ++NumFaces;
            }
            if (FaceFlags.HasFlag(EFaceFlags.Right))
            {
                AddRight(HalfBlockSize, InFaceTextureUVs[(int)EFaceOrientation.Right], Vertices, UVs);
                ++NumFaces;
            }
            if (FaceFlags.HasFlag(EFaceFlags.Front))
            {
                AddFront(HalfBlockSize, InFaceTextureUVs[(int)EFaceOrientation.Front], Vertices, UVs);
                ++NumFaces;
            }
            if (FaceFlags.HasFlag(EFaceFlags.Left))
            {
                AddLeft(HalfBlockSize, InFaceTextureUVs[(int)EFaceOrientation.Left], Vertices, UVs);
                ++NumFaces;
            }
            if (FaceFlags.HasFlag(EFaceFlags.Bottom))
            {
                AddBottom(HalfBlockSize, InFaceTextureUVs[(int)EFaceOrientation.Bottom], Vertices, UVs);
                ++NumFaces;
            }

            var WorkingMesh = new Mesh();
            NewVA.Meshes.EditorOnly_Add(FaceFlags, WorkingMesh);

            WorkingMesh.vertices = Vertices.ToArray();
            WorkingMesh.uv = UVs.ToArray();
            WorkingMesh.triangles = BuildTriangles(NumFaces);
        }

        NewVA.Meshes.SynchroniseToSerializedData();

        foreach (var KVP in NewVA.Meshes)
        {
            EFaceFlags ElementFlags = KVP.Key;
            Mesh ElementMesh = KVP.Value;

            ElementMesh.name = FlagsToName(ElementFlags);

            ElementMesh.RecalculateBounds();
            ElementMesh.RecalculateNormals();
        }

        return NewVA;
    }

    public void PostBuild()
    {
        AssetDatabase.AddObjectToAsset(AtlasTexture, this);

        foreach (var KVP in Meshes)
        {
            Mesh ElementMesh = KVP.Value;

            AssetDatabase.AddObjectToAsset(ElementMesh, this);
        }
    }

    // Builds triangles assuming 2 tris per face and 4 verts per face with a consistent winding
    static int[] BuildTriangles(int InNumFaces)
    {
        int[] Triangles = new int[InNumFaces * 2 * 3];

        int Offset = 0;
        for (int FaceIndex = 0; FaceIndex < InNumFaces; ++FaceIndex)
        {
            Triangles[Offset++] = FaceIndex * 4 + 0;
            Triangles[Offset++] = FaceIndex * 4 + 1;
            Triangles[Offset++] = FaceIndex * 4 + 3;
            Triangles[Offset++] = FaceIndex * 4 + 1;
            Triangles[Offset++] = FaceIndex * 4 + 2;
            Triangles[Offset++] = FaceIndex * 4 + 3;
        }

        return Triangles;
    }

    static void AddTop(float InFaceHalfSize, Rect InFaceTextureUVs, List<Vector3> InOutVertices, List<Vector2> InOutUVs)
    {
        InOutVertices.Add(new Vector3(-InFaceHalfSize, InFaceHalfSize, -InFaceHalfSize));
        InOutVertices.Add(new Vector3(-InFaceHalfSize, InFaceHalfSize, InFaceHalfSize));
        InOutVertices.Add(new Vector3(InFaceHalfSize, InFaceHalfSize, InFaceHalfSize));
        InOutVertices.Add(new Vector3(InFaceHalfSize, InFaceHalfSize, -InFaceHalfSize));

        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMax, InFaceTextureUVs.yMax));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMax, InFaceTextureUVs.yMin));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMin, InFaceTextureUVs.yMin));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMin, InFaceTextureUVs.yMax));
    }

    static void AddBack(float InFaceHalfSize, Rect InFaceTextureUVs, List<Vector3> InOutVertices, List<Vector2> InOutUVs)
    {
        InOutVertices.Add(new Vector3(-InFaceHalfSize, -InFaceHalfSize, -InFaceHalfSize));
        InOutVertices.Add(new Vector3(-InFaceHalfSize, InFaceHalfSize, -InFaceHalfSize));
        InOutVertices.Add(new Vector3(InFaceHalfSize, InFaceHalfSize, -InFaceHalfSize));
        InOutVertices.Add(new Vector3(InFaceHalfSize, -InFaceHalfSize, -InFaceHalfSize));

        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMin, InFaceTextureUVs.yMin));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMin, InFaceTextureUVs.yMax));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMax, InFaceTextureUVs.yMax));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMax, InFaceTextureUVs.yMin));
    }

    static void AddRight(float InFaceHalfSize, Rect InFaceTextureUVs, List<Vector3> InOutVertices, List<Vector2> InOutUVs)
    {
        InOutVertices.Add(new Vector3(InFaceHalfSize, -InFaceHalfSize, -InFaceHalfSize));
        InOutVertices.Add(new Vector3(InFaceHalfSize, InFaceHalfSize, -InFaceHalfSize));
        InOutVertices.Add(new Vector3(InFaceHalfSize, InFaceHalfSize, InFaceHalfSize));
        InOutVertices.Add(new Vector3(InFaceHalfSize, -InFaceHalfSize, InFaceHalfSize));

        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMin, InFaceTextureUVs.yMin));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMin, InFaceTextureUVs.yMax));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMax, InFaceTextureUVs.yMax));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMax, InFaceTextureUVs.yMin));
    }

    static void AddBottom(float InFaceHalfSize, Rect InFaceTextureUVs, List<Vector3> InOutVertices, List<Vector2> InOutUVs)
    {
        InOutVertices.Add(new Vector3(InFaceHalfSize, -InFaceHalfSize, -InFaceHalfSize));
        InOutVertices.Add(new Vector3(InFaceHalfSize, -InFaceHalfSize, InFaceHalfSize));
        InOutVertices.Add(new Vector3(-InFaceHalfSize, -InFaceHalfSize, InFaceHalfSize));
        InOutVertices.Add(new Vector3(-InFaceHalfSize, -InFaceHalfSize, -InFaceHalfSize));

        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMin, InFaceTextureUVs.yMin));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMin, InFaceTextureUVs.yMax));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMax, InFaceTextureUVs.yMax));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMax, InFaceTextureUVs.yMin));
    }

    static void AddFront(float InFaceHalfSize, Rect InFaceTextureUVs, List<Vector3> InOutVertices, List<Vector2> InOutUVs)
    {
        InOutVertices.Add(new Vector3(InFaceHalfSize, -InFaceHalfSize, InFaceHalfSize));
        InOutVertices.Add(new Vector3(InFaceHalfSize, InFaceHalfSize, InFaceHalfSize));
        InOutVertices.Add(new Vector3(-InFaceHalfSize, InFaceHalfSize, InFaceHalfSize));
        InOutVertices.Add(new Vector3(-InFaceHalfSize, -InFaceHalfSize, InFaceHalfSize));

        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMin, InFaceTextureUVs.yMin));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMin, InFaceTextureUVs.yMax));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMax, InFaceTextureUVs.yMax));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMax, InFaceTextureUVs.yMin));
    }

    static void AddLeft(float InFaceHalfSize, Rect InFaceTextureUVs, List<Vector3> InOutVertices, List<Vector2> InOutUVs)
    {
        InOutVertices.Add(new Vector3(-InFaceHalfSize, -InFaceHalfSize, InFaceHalfSize));
        InOutVertices.Add(new Vector3(-InFaceHalfSize, InFaceHalfSize, InFaceHalfSize));
        InOutVertices.Add(new Vector3(-InFaceHalfSize, InFaceHalfSize, -InFaceHalfSize));
        InOutVertices.Add(new Vector3(-InFaceHalfSize, -InFaceHalfSize, -InFaceHalfSize));

        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMin, InFaceTextureUVs.yMin));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMin, InFaceTextureUVs.yMax));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMax, InFaceTextureUVs.yMax));
        InOutUVs.Add(new Vector2(InFaceTextureUVs.xMax, InFaceTextureUVs.yMin));
    }

#endif // UNITY_EDITOR
}
