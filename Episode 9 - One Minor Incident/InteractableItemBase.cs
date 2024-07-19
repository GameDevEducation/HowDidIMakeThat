using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractableItemBase : MonoBehaviour
{
    public UnityEvent OnPickup;

    public GameObject PickupPrompt;

    public bool CanBePickedUp = false;
    public float PromptLifetime = 1f;

    public bool DestroyOnPickup = true;

    protected float PromptKillTime = -1f;

    public float DestroyDelayTime = 1f;
    protected bool DelayedDestroyInProgress = false;

    public KeyCode ActivationKey = KeyCode.L;

    public bool HasSecondaryActivation = false;
    public KeyCode SecondaryActivationKey = KeyCode.L;

    public bool IsAttachable = false;
    public bool SingleUse = true;
    public bool RequiresAttachedItem = false;

    public string PickupSound = "";
    public string MoveSound = "";

    public bool IsShowingUsePrompt
    {
        get
        {
            return PickupPrompt.activeInHierarchy;
        }
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (PickupPrompt.activeInHierarchy)
        {
            // make the prompt look at the camera
            PickupPrompt.transform.LookAt(Camera.main.transform);
            Vector3 currentEulerAngles = PickupPrompt.transform.eulerAngles;
            currentEulerAngles.x = currentEulerAngles.z = 0f;
            currentEulerAngles.y += 180f;
            PickupPrompt.transform.eulerAngles = currentEulerAngles;

            // update the countdown
            PromptKillTime -= Time.deltaTime;
            if (PromptKillTime <= 0f)
            {
                PickupPrompt.SetActive(false);
            }
        }
    }

    public void ShowPrompt()
    {
        if (DelayedDestroyInProgress)
            return;
            
        if (!CanBePickedUp)
            return;

        PickupPrompt.SetActive(true);
        PromptKillTime = PromptLifetime;
    }

    public virtual void Pickup(bool isPrimaryActivation)
    {
        if (DelayedDestroyInProgress)
            return;
            
        if (!CanBePickedUp)
            return;
            
        OnPickup?.Invoke();

        if (DestroyOnPickup)
        {
            if (DestroyDelayTime > 0)
                Invoke("DestroyPickup", DestroyDelayTime);
            else
                Destroy(gameObject);
        }
        else
        {
            if (SingleUse)
            {
                PickupPrompt.SetActive(false);

                CanBePickedUp = false;
                this.enabled = false;
            }
        }

        DidPickup(isPrimaryActivation);
    }

    protected virtual void DidPickup(bool isPrimaryActivation)
    {

    }

    public void DestroyPickup()
    {
        Destroy(gameObject);
    }
}
