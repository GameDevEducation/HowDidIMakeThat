using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    [Header("General")]
    public GameObject LaunchPoint;
    public TMPro.TextMeshPro AmmoText;
    public AmmoSO CurrentAmmunition;

    [Header("Fire Behaviour")]
    public KeyCode PrimaryFireKey = KeyCode.Mouse0;
    public KeyCode SecondaryFireKey = KeyCode.Mouse1;
    public float LaunchForce_Primary = 50f;
    public float LaunchForce_Secondary = 50f;

    protected float PrimaryFireCooldown = -1f;
    protected float SecondaryFireCooldown = -1f;

    public bool IgnoreFirstFire = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (AIManager.Instance.IsPaused)
            return;

        // update the cooldown times
        if (PrimaryFireCooldown > 0)
            PrimaryFireCooldown -= Time.deltaTime;
        if (SecondaryFireCooldown > 0)
            SecondaryFireCooldown -= Time.deltaTime;

        if (Input.GetKeyDown(PrimaryFireKey) && PrimaryFireCooldown <= 0)
        {
            Fire(true);
        }
        if (Input.GetKeyDown(SecondaryFireKey) && SecondaryFireCooldown <= 0)
        {
            Fire(false);
        }
    }

    public void SetAmmunition(AmmoSO ammunition)
    {
        CurrentAmmunition = ammunition;
        AmmoText.text = CurrentAmmunition.Name;
        IgnoreFirstFire = true;
    }

    protected void Fire(bool isPrimary)
    {
        if (IgnoreFirstFire)
        {
            IgnoreFirstFire = false;
            return;
        }
        
        // instantiate the projectile
        GameObject newProjectile = Instantiate(isPrimary ? CurrentAmmunition.PrimaryFire.ProjectilePrefab : CurrentAmmunition.SecondaryFire.ProjectilePrefab, LaunchPoint.transform.position, LaunchPoint.transform.rotation);

        // retrieve the rigid body
        Rigidbody rb = newProjectile.GetComponent<Rigidbody>();

        // launch the projectile
        float launchForce = isPrimary ? LaunchForce_Primary : LaunchForce_Secondary;
        rb.AddForce(LaunchPoint.transform.forward * launchForce, ForceMode.VelocityChange);

        // set the projectile behaviour
        newProjectile.GetComponent<Projectile>().SetAmmunition(CurrentAmmunition, isPrimary);

        // play the fire sound
        AkSoundEngine.PostEvent(isPrimary ? CurrentAmmunition.PrimaryFire.FireSound : CurrentAmmunition.SecondaryFire.FireSound, gameObject);

        // apply cooldown
        if (isPrimary)
            PrimaryFireCooldown = CurrentAmmunition.PrimaryFire.Cooldown;
        else
            SecondaryFireCooldown = CurrentAmmunition.SecondaryFire.Cooldown;
    }
}
