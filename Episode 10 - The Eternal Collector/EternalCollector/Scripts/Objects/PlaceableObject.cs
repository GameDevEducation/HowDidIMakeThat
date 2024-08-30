using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EObjectType
{
    Scrap
}

public enum EObjectSize
{
    Small,
    Medium,
    Large
}

public class PlaceableObject : MonoBehaviour
{
    [SerializeField] string _Name;
    [SerializeField] EObjectType _Type;
    [SerializeField] EObjectSize _Size;
    [SerializeField] float _MinWeight = 0f;
    [SerializeField] float _MaxWeight = 0f;
    [SerializeField] GameObject MeshGO;

    public string DisplayName => $"{_Name} ({ChosenWeight:0.0} kg)";

    public string Name => _Name;
    public EObjectType Type => _Type;
    public EObjectSize Size => _Size;
    public float MinWeight => _MinWeight;
    public float MaxWeight => _MaxWeight;
    public float ChosenWeight { get; private set; } = 0f;

    public bool IsScrap => _Type == EObjectType.Scrap;
    public bool IsBeingSiphoned { get; private set; } = false;

    protected Rigidbody LinkedRB = null;

    // Start is called before the first frame update
    void Start()
    {
        ChosenWeight = Random.Range(_MinWeight, _MaxWeight);
        MeshGO.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BeginSiphoning()
    {
        if (LinkedRB != null)
        {
            Destroy(LinkedRB);
            LinkedRB = null;
        }

        IsBeingSiphoned = true;
    }

    public void PerformFall()
    {
        LinkedRB = gameObject.AddComponent<Rigidbody>();
        LinkedRB.mass = ChosenWeight;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("TerrainTile"))
        {
            Destroy(LinkedRB);
            LinkedRB = null;
        }
    }
}
