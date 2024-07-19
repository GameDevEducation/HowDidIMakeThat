using UnityEditor;
using UnityEngine;

public class BlockModelProcessor : MonoBehaviour
{
    public class CaptureInfo
    {
        public Camera LinkedCamera;
        public EFaceOrientation Orientation;
    }

    [SerializeField] Transform StageOrigin;

    [SerializeField] Camera Camera_Front;
    [SerializeField] Camera Camera_Left;
    [SerializeField] Camera Camera_Right;
    [SerializeField] Camera Camera_Back;
    [SerializeField] Camera Camera_Top;
    [SerializeField] Camera Camera_Bottom;

    [SerializeField] Material PresentationMaterial;
    [SerializeField] Material TestMaterial;

    [SerializeField] int TextureSize = 256;
    [SerializeField] float BlockSize = 1f;

    [SerializeField] bool bDebug_SpawnAfterGeneration = false;

    Object[] AllPrefabs = null;
    CaptureInfo[] CaptureSet = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CaptureSet = new CaptureInfo[]
        {
            new CaptureInfo { Orientation = EFaceOrientation.Front, LinkedCamera = Camera_Front },
            new CaptureInfo { Orientation = EFaceOrientation.Left, LinkedCamera = Camera_Left },
            new CaptureInfo { Orientation = EFaceOrientation.Right, LinkedCamera = Camera_Right },
            new CaptureInfo { Orientation = EFaceOrientation.Back, LinkedCamera = Camera_Back },
            new CaptureInfo { Orientation = EFaceOrientation.Top, LinkedCamera = Camera_Top },
            new CaptureInfo { Orientation = EFaceOrientation.Bottom, LinkedCamera = Camera_Bottom },
        };

        AllPrefabs = Resources.LoadAll("BlockModels/", typeof(GameObject));

        if ((AllPrefabs != null) && (AllPrefabs.Length > 0))
        {
            ConfigureStage(AllPrefabs[0]);

            PerformCapture(AllPrefabs[0].name);
        }
    }

    void ResetStage()
    {
        // destroy all children on the stage
        for (int Index = StageOrigin.childCount - 1; Index >= 0; Index--)
            Destroy(StageOrigin.GetChild(Index));
    }

    void ConfigureStage(Object InPrefab)
    {
        ResetStage();

        GameObject NewModelGO = (GameObject)Instantiate(InPrefab, Vector3.zero, Quaternion.identity, StageOrigin);

        MeshRenderer NewModelMeshRenderer = NewModelGO.GetComponent<MeshRenderer>();
        Texture CurrentModelTexture = NewModelMeshRenderer.material.mainTexture;

        Material NewModelMaterial = Material.Instantiate(PresentationMaterial);
        NewModelMaterial.mainTexture = CurrentModelTexture;

        NewModelMeshRenderer.material = NewModelMaterial;
    }

    void PerformCapture(string InSourceAssetName)
    {
        Texture2D[] Textures = new Texture2D[6];

        foreach (var Config in CaptureSet)
        {
            Textures[(int)Config.Orientation] = PerformFaceCapture(Config);
        }

        Texture2D AtlasTexture = new Texture2D(TextureSize * 4, TextureSize * 4, TextureFormat.RGBA32, false);
        Rect[] FaceTextureUVs = AtlasTexture.PackTextures(Textures, 2);
        AtlasTexture.name = "Texture Atlas";

        var VisualAsset = BlockVisualAsset.Build(AtlasTexture, FaceTextureUVs, BlockSize);
        Undo.RegisterFullObjectHierarchyUndo(VisualAsset, "Create Visual Asset");

        AssetDatabase.CreateAsset(VisualAsset, $"Assets/Duality/Content/BlockVisualAssets/{InSourceAssetName}.asset");

        VisualAsset.PostBuild();

        AssetDatabase.SaveAssets();

        if (bDebug_SpawnAfterGeneration)
        {
            Material MaterialInstance = VisualAsset.GetMaterialInstanceForAtlas(TestMaterial);

            foreach (var KVP in VisualAsset.GetMeshes())
            {
                GameObject VATest = new GameObject(KVP.Value.name);
                VATest.transform.SetParent(StageOrigin, true);
                VATest.transform.position = Vector3.zero;
                VATest.transform.rotation = Quaternion.identity;

                VATest.AddComponent<MeshFilter>().mesh = KVP.Value;
                VATest.AddComponent<MeshRenderer>().material = MaterialInstance;
            }
        }
    }

    Texture2D PerformFaceCapture(CaptureInfo InConfig)
    {
        ShutOffAllCameras();

        RenderTexture OutputRT = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32);
        OutputRT.isPowerOfTwo = true;
        OutputRT.Create();

        Texture2D FaceTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);

        InConfig.LinkedCamera.targetTexture = OutputRT;
        InConfig.LinkedCamera.gameObject.SetActive(true);
        InConfig.LinkedCamera.Render();

        InConfig.LinkedCamera.targetTexture = null;

        // Can't use Graphics.CopyTexture as we need the data CPU side to pack the atlas for saving
        var PreviousActive = RenderTexture.active;
        RenderTexture.active = OutputRT;

        FaceTexture.ReadPixels(new Rect(0, 0, TextureSize, TextureSize), 0, 0);
        FaceTexture.Apply();

        RenderTexture.active = PreviousActive;

        ShutOffAllCameras();

        return FaceTexture;
    }

    void ShutOffAllCameras()
    {
        foreach (var Config in CaptureSet)
            Config.LinkedCamera.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
