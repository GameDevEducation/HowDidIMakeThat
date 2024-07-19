using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DirtManager : MonoBehaviour
{
    protected List<CleanableItem> AllCleanables = new List<CleanableItem>();

    public float MaximumDirtLevel {private set; get;} = 0f;
    public float CurrentDirtLevel {private set; get;} = 0f;

    protected static DirtManager _Instance = null;
    public static DirtManager Instance
    {
        get
        {
            return _Instance;
        }
    }

    void Awake()
    {
        if (_Instance != null)
        {
            Destroy(this);
            return;
        }

        _Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // update the dirtiness
        CurrentDirtLevel = AllCleanables.Sum(cleanable => cleanable.Dirtiness);
    }

    public void RegisterCleanableObject(CleanableItem item)
    {
        // register the item
        AllCleanables.Add(item);

        // update the maximum and current dirt level
        CurrentDirtLevel += item.Dirtiness;
        MaximumDirtLevel += item.Dirtiness;
    }
}
