using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class BuildingPieceDestroyed : UnityEvent<BuildingPiece> {}

[System.Serializable]
public class AttackPoint
{
    public GameObject Marker;
    public bool IsFrontLine = false;
    public bool IsOccupied = false;

    public AttackPoint(GameObject _Marker, bool _IsFrontLine)
    {
        Marker = _Marker;
        IsFrontLine = _IsFrontLine;
    }

    public Vector3 Location
    {
        get
        {
            return Marker.transform.position;
        }
    }
}

public class BuildingPiece : MonoBehaviour
{
    public BuildingPieceSO Config;
    public BuildingPieceDestroyed OnPieceDestroyed;
    public SpawnerLocation Direction;

    public int CurrentHealth = -1;

    public List<AttackPoint> AttackPoints;

    public float ScanRange = 12f;
    public bool OnlyInFront = false;
    public float ScanStepDistance = 2f;
    public float ScanDepth = 5f;

    public bool CanEmitSound = false;

    public bool CanBeDamaged
    {
        get
        {
            return Config.HitPoints > 0 && CurrentHealth > 0;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        CurrentHealth = Config.HitPoints;

        // building is attackable?
        if (Config.HitPoints > 0)
        {
            AIManager.Instance.RegisterTarget(this);

            int numSteps = Mathf.CeilToInt(ScanRange / ScanStepDistance);

            float xOffset = (OnlyInFront ? 0 : (numSteps / 2f));
            float zOffset = (numSteps / 2f);

            // perform the scan
            for(int x = 0; x < numSteps; ++x)
            {
                for (int z = 0; z < numSteps; ++z)
                {
                    // calculate the location
                    Vector3 location = transform.position;
                    location += transform.forward * (ScanStepDistance * 0.5f + ScanStepDistance * (z - zOffset));
                    location += transform.right * (ScanStepDistance * 0.5f + ScanStepDistance * (x - xOffset));
                    location += Vector3.up * ScanDepth * 0.5f;

                    // run a raycast to make sure it hits terrain
                    RaycastHit hitResult;
                    if (Physics.Raycast(location, Vector3.down, out hitResult, ScanDepth))
                    {
                        if (hitResult.collider.gameObject.CompareTag("Ground"))
                        {
                            bool canAttack = false;
                            
                            if (OnlyInFront)
                                canAttack = x == 0;
                            else
                                canAttack = (Mathf.Pow(location.x - transform.position.x, 2f) + Mathf.Pow(location.z - transform.position.z, 2f)) < Mathf.Pow(ScanStepDistance * 1.5f, 2f);

                            GameObject marker = GameObject.CreatePrimitive(canAttack ? PrimitiveType.Cube : PrimitiveType.Sphere);

                            // position the marker point
                            marker.transform.position = hitResult.point;
                            marker.transform.rotation = transform.rotation;
                            marker.transform.SetParent(transform);
                            marker.SetActive(false);

                            AttackPoints.Add(new AttackPoint(marker, canAttack));
                        }
                    }
                }
            }        
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void OnTakeDamage(int amount)
    {
        // able to be damaged?
        if (!CanBeDamaged)
            return;

        // update the amount of health
        CurrentHealth -= amount;

        // if the piece is destroyed the invoke the evnet
        if (CurrentHealth <= 0)
        {
            OnPieceDestroyed?.Invoke(this);

            // turn off the game object?
            if (Config.CollapseMode == CollapseModeEnum.Disappear)
            {
                gameObject.SetActive(false);
            }
            else if (Config.CollapseMode == CollapseModeEnum.Explode)
            {
                // build up the list of all of the renderers
                MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();

                // deparent the objects first
                foreach(MeshRenderer renderer in renderers)
                {
                    renderer.gameObject.transform.SetParent(null);
                }

                // make each one explode
                foreach(MeshRenderer renderer in renderers)
                {
                    // move to the effects layer
                    renderer.gameObject.layer = LayerMask.NameToLayer("Effects");

                    // add a despawner to the object
                    Despawner despawner = renderer.gameObject.AddComponent<Despawner>();
                    despawner.DespawnTime = 30f;

                    // add a rigid body to the object
                    Rigidbody componentRB = renderer.gameObject.AddComponent<Rigidbody>();

                    // determine the launch direction
                    Vector3 direction = (renderer.gameObject.transform.position - transform.position).normalized + Vector3.up;
                    componentRB.AddForce(direction * Config.ExplosionForce, ForceMode.VelocityChange);
                }
            }
        }
    }

    public void OnCollisionEnter(Collision other)
    {
        if (CanEmitSound && (other.gameObject.CompareTag("Ground")))
            AkSoundEngine.PostEvent("Play_Building_Collide", gameObject);
    }
}
