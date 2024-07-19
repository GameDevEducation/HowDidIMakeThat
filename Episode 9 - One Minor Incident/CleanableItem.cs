using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleanableItem : MonoBehaviour
{
    [Range(0f, 1f)]
    public float Dirtiness = 0.5f;

    protected MeshRenderer Renderer;
    protected MaterialPropertyBlock[] PropertyBlocks;
    protected Rigidbody RB;

    public SpriteRenderer[] AttachedSprites;

    public string OnHitSound = "Play_Bubble_Hit_Other";
    public string OnHitOtherObjectSound = "";

    // Start is called before the first frame update
    void Start()
    {
        // retrieve the renderer
        Renderer = GetComponent<MeshRenderer>();

        // get the rigid body
        RB = GetComponent<Rigidbody>();

        // setup the property blocks
        PropertyBlocks = new MaterialPropertyBlock[Renderer.sharedMaterials.Length];
        for (int index = 0; index < PropertyBlocks.Length; ++index)
        {
            PropertyBlocks[index] = new MaterialPropertyBlock();
        }

        // register the cleanable object
        DirtManager.Instance?.RegisterCleanableObject(this);
    }

    // Update is called once per frame
    void Update()
    {
        // update the material
        for (int index = 0; index < PropertyBlocks.Length; ++index)
        {
            Renderer.GetPropertyBlock(PropertyBlocks[index], index);

            PropertyBlocks[index].SetFloat("_Dirtiness", Dirtiness);

            Renderer.SetPropertyBlock(PropertyBlocks[index], index);
        }
    }

    public void CleanerImpact(Vector3 impactForce, float cleaningStrength)
    {
        Dirtiness = Mathf.Clamp01(Dirtiness - cleaningStrength);

        if (RB != null)
            RB.AddForce(impactForce, ForceMode.Impulse);
    }

    public void OnCollisionEnter(Collision other)
    {
        // if shoved out into the kill zone then forcibly clean
        if (other.gameObject.CompareTag("KillZone"))
            Dirtiness = 0f;

        if (Time.timeSinceLevelLoad > 1f && OnHitOtherObjectSound.Length > 0)
            AkSoundEngine.PostEvent(OnHitOtherObjectSound, gameObject);
    }
}
