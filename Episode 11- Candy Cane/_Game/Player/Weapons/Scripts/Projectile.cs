using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    protected AmmoSO CurrentAmmunition;
    protected bool PrimaryFire;
    protected bool Detonated = false;

    protected float TimeUntilActive = -1f;

    protected FireModeConfig CurrentConfig
    {
        get
        {
            return PrimaryFire ? CurrentAmmunition.PrimaryFire : CurrentAmmunition.SecondaryFire;
        }
    }

    protected bool CanActivate
    {
        get
        {
            return !Detonated && (TimeUntilActive <= 0);
        }
    }

    public void SetAmmunition(AmmoSO ammunition, bool primaryFire)
    {
        CurrentAmmunition = ammunition;
        PrimaryFire = primaryFire;
        TimeUntilActive = CurrentConfig.ActivationTime;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (TimeUntilActive > 0)
            TimeUntilActive -= Time.deltaTime;

        // below kill floor?
        if (transform.position.y < -100f)
        {
            Destroy(gameObject);
            return;
        }

        // unable to activate?
        if (!CanActivate)
            return;

        // is this a cluster bomb?
        if (CurrentConfig.Behaviour == AmmoBehaviour.ClusterBomb)
        {
            // did we hit an object we can interact with?
            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position, Vector3.down, out hitInfo, CurrentConfig.DetonationHeight, CurrentConfig.DetonationLayerMask))
            {
                Detonate();
            }
        }
    }

    void OnCollisionEnter(Collision other)
    {
        // unable to activate?
        if (!CanActivate)
            return;

        if (CurrentConfig.Behaviour == AmmoBehaviour.Explode)
        {
            Detonate();
        }
    }

    void Detonate()
    {
        Detonated = true;

        // play any effects
        if (CurrentConfig.ImpactEffect)
        {
            Instantiate(CurrentConfig.ImpactEffect, transform.position, Quaternion.identity);
        }

        // is this a cluster bomb?
        if (CurrentConfig.Behaviour == AmmoBehaviour.ClusterBomb)
        {
            // spawn the clusters
            for (int index = 0; index < CurrentConfig.NumPerCluster; ++index)
            {
                // spawn the bomblet
                GameObject bomblet = Instantiate(CurrentConfig.AmmoToSpawn.PrimaryFire.ProjectilePrefab, transform.position, Quaternion.identity);

                // launch the bomblet
                Rigidbody bombletRB = bomblet.GetComponent<Rigidbody>();

                // randomise the direction
                float azimuthAngle = Random.Range(0f, 360f);
                float elevationAngle = Random.Range(-30f, 60f);

                Vector3 direction = new Vector3(Mathf.Sin(azimuthAngle * Mathf.Deg2Rad) * Mathf.Cos(elevationAngle * Mathf.Deg2Rad), 
                                                Mathf.Sin(elevationAngle * Mathf.Deg2Rad), 
                                                Mathf.Cos(azimuthAngle * Mathf.Deg2Rad) * Mathf.Cos(elevationAngle * Mathf.Deg2Rad));

                // launch the projectile
                bombletRB.AddForce(direction.normalized * CurrentConfig.DetonationForce, ForceMode.VelocityChange);

                // link to the right projectile (always primary for bomblets)
                Projectile bombletLogic = bomblet.GetComponent<Projectile>();
                bombletLogic.SetAmmunition(CurrentConfig.AmmoToSpawn, true);
            }
        }
        else if (CurrentConfig.Behaviour == AmmoBehaviour.Explode)
        {
            Collider[] objectsHit = Physics.OverlapSphere(transform.position, CurrentConfig.RadiusOfEffect);

            // apply damage to any enemies
            foreach(Collider collider in objectsHit)
            {
                // skip anything not an enemy
                if (!collider.CompareTag("Enemy"))
                    continue;

                EnemyAI enemyLogic = collider.gameObject.GetComponent<EnemyAI>();

                if (enemyLogic)
                {
                    // determine the damage
                    float workingDamage = CurrentConfig.BaseDamage;

                    // check for a critical hit
                    if (Random.Range(0f, 1f) < CurrentConfig.CriticalHitChance)
                        workingDamage *= CurrentConfig.CriticalHitMultiplier;

                    // apply the damage
                    enemyLogic.OnTakeDamage(Mathf.RoundToInt(workingDamage));

                    // apply status effects
                    if (CurrentConfig.StatusEffects.Count > 0)
                        enemyLogic.ApplyStatusEffects(CurrentConfig.StatusEffects);
                }
            }
        }

        AkSoundEngine.PostEvent(CurrentConfig.ImpactSound, gameObject);

        Destroy(gameObject);
    }
}
