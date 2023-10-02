using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class DistanceUpdatedEvent : UnityEvent<float, float> {}

public class ShaftManager : MonoBehaviour
{
    [Header("Shaft Prefab")]
    public GameObject ShaftPrefab;

    [Header("Shaft Movement")]
    public float MaximumSpeed = 1f;
    public float SpawnY = 25f;
    public float DespawnY = -100f;
    public Vector3 SpawnOffset = new Vector3(0, 50, 0);

    [Header("Cable Movement")]
    public float TextureSpeed = 1f;
    private List<Cable> AllCables;
    private Material CableMaterial;

    [Header("Events")]
    public DistanceUpdatedEvent OnDistanceUpdated;

    private Platform PlatformManager;

    private List<GameObject> ShaftSegments = new List<GameObject>();
    private float DistanceTravelled = 0f;
    private float PreviousBestScore = -1f;
    
    // Start is called before the first frame update
    void Start()
    {
        PlatformManager = FindObjectOfType<Platform>();

        // populate the list of current shaft segments
        for (int index = 0; index < transform.childCount; ++index)
        {
            ShaftSegments.Add(transform.GetChild(index).gameObject);
        }

        AllCables = new List<Cable>(FindObjectsOfType<Cable>());
        CableMaterial = AllCables[0].gameObject.GetComponent<MeshRenderer>().sharedMaterial;

        PreviousBestScore = PlayerPrefs.GetFloat("BestScore", -1f);
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseManager.IsPaused)
            return;

        UpdateInternal_Shaft();

        UpdateInternal_Cables();
    }

    void UpdateInternal_Cables()
    {
        Vector2 textureOffset = CableMaterial.GetTextureOffset("_MainTex");
        textureOffset += Vector2.up * TextureSpeed * PlatformManager.CurrentSpeed * Time.deltaTime;        
        CableMaterial.SetTextureOffset("_MainTex", textureOffset);

        textureOffset = CableMaterial.GetTextureOffset("_BumpMap");
        textureOffset += Vector2.up * TextureSpeed * PlatformManager.CurrentSpeed * Time.deltaTime;        
        CableMaterial.SetTextureOffset("_BumpMap", textureOffset);
    }

    void UpdateInternal_Shaft()
    {
        // work out the movement delta
        float movementDelta = MaximumSpeed * PlatformManager.CurrentSpeed * Time.deltaTime;

        DistanceTravelled += movementDelta;

        OnDistanceUpdated?.Invoke(DistanceTravelled, PreviousBestScore);

        bool needToSpawn = true;

        // move all shaft segments
        for (int index = 0; index < ShaftSegments.Count; ++index)
        {
            GameObject segment = ShaftSegments[index];

            segment.transform.position = segment.transform.position + Vector3.down * movementDelta;

            // below the despawn point?
            if (segment.transform.position.y < DespawnY)
            {
                GameObject.Destroy(segment);
                ShaftSegments.RemoveAt(index);
                --index;
            }
            else if (segment.transform.position.y > SpawnY)
            {
                needToSpawn = false;
            }
        }

        // need to spawn a new segment?
        if (needToSpawn)
        {
            GameObject newSegment = Instantiate(ShaftPrefab, ShaftSegments[ShaftSegments.Count - 1].transform.position + SpawnOffset, Quaternion.identity);
            //newSegment.transform.eulerAngles = new Vector3(0f, Random.Range(0f, 360f), 0f);
            newSegment.transform.parent = transform;

            ShaftSegments.Add(newSegment);
        }
    }

    public void OnEndGameTriggered()
    {
        if (DistanceTravelled > PreviousBestScore)
        {
            PlayerPrefs.SetFloat("BestScore", DistanceTravelled);
            PlayerPrefs.Save();
        }
    }
}
