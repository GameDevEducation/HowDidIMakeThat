using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableChecker : MonoBehaviour
{
    public float MaxRange = 3f;
    public LayerMask RaycastMask;
    public KeyCode DropItem = KeyCode.F;

    public Transform AttachmentPoint;
    public float AttachmentCentreSpeed = 1f;
    public float AttachmentReorientSpeed = 60f;

    public KeyCode UseItem = KeyCode.P;

    public InteractableItemBase AttachedItem = null;

    protected Vector3 PreviousAttachPointLocation;
    protected float TimeSinceLastMoveSound = -1f;
    public float MoveSoundRepeatInterval = 1f;
    public float MinimumMoveDistanceForSound = 0.5f;

    public float ChanceToDropOnCough = 0.25f;
    public float ChanceToPourTooMuch = 0.25f;

    protected InteractableItemBase FocusItem = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }
    
    public void ToggleEnabled(bool newEnabled)
    {
        enabled = newEnabled;
    }

    // Update is called once per frame
    void Update()
    {
        // do nothing if paused
        if (GameManager.Instance.IsPaused)
            return;

        // can only pickup in manufacturing phase
        if (GameManager.Instance.Phase != GameManager.GamePhases.Manufacturing)
            return;

        // update the move sound cooldown
        if (TimeSinceLastMoveSound > 0)
        {
            TimeSinceLastMoveSound -= Time.deltaTime;
        }

        // do we have an attached item
        if (AttachedItem != null)
        {
            // attempting to detach
            if (Input.GetKeyDown(DropItem))
            {
                Detach();
                return;
            }
            else
            {
                // local position not yet at attachment point?
                if (AttachedItem.transform.localPosition.sqrMagnitude > float.Epsilon)
                {
                    AttachedItem.transform.localPosition = Vector3.Lerp(AttachedItem.transform.localPosition, Vector3.zero, AttachmentCentreSpeed * Time.deltaTime);
                }

                // local rotation not yet zeroed
                if (AttachedItem.transform.localEulerAngles.sqrMagnitude > float.Epsilon)
                {
                    AttachedItem.transform.localRotation = Quaternion.RotateTowards(AttachedItem.transform.localRotation, Quaternion.identity, AttachmentReorientSpeed * Time.deltaTime);
                }

                // moved enough to trigger sound?
                if ((AttachmentPoint.transform.position - PreviousAttachPointLocation).sqrMagnitude > (MinimumMoveDistanceForSound * MinimumMoveDistanceForSound))
                {
                    // hasn't played sound recently
                    if (TimeSinceLastMoveSound <= 0)
                    {
                        TimeSinceLastMoveSound = MoveSoundRepeatInterval;

                        // play the pickup sound if needed
                        if (!string.IsNullOrEmpty(AttachedItem.MoveSound))
                            AkSoundEngine.PostEvent(AttachedItem.MoveSound, AttachedItem.gameObject);
                    }

                    PreviousAttachPointLocation = AttachmentPoint.transform.position;
                }
            }
        }

        Vector3 centrePoint = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        // did we hit anything?
        RaycastHit hitInfo;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(centrePoint), out hitInfo, MaxRange, RaycastMask, QueryTriggerInteraction.Collide))
        {
            InteractableItemBase item = hitInfo.transform.gameObject.GetComponent<InteractableItemBase>();

            // show the prompt
            if (item != null)
            {
                // if item is attachable but we alreay have one
                if (item.IsAttachable && AttachedItem != null)
                    return;

                // requires an attached item to use
                if (item.RequiresAttachedItem && AttachedItem == null)
                    return;

                FocusItem = item;

                // make sure the prompt is visible
                item.ShowPrompt();

                // asked to pickup?
                if (Input.GetKeyDown(item.ActivationKey))
                {
                    item.Pickup(true);

                    if (item.IsAttachable)
                    {
                        Attach(item);
                    }
                }
                else if (item.HasSecondaryActivation && Input.GetKeyDown(item.SecondaryActivationKey))
                {
                    item.Pickup(false);
                }
            }
        }        
    }

    public void OnCough()
    {
        AkSoundEngine.PostEvent("Play_Cough", gameObject);

        // do we have an attached item?
        if (AttachedItem != null)
        {
            if (Random.Range(0f, 1f) < ChanceToDropOnCough)
                Detach();
            else if (Random.Range(0f, 1f) < ChanceToPourTooMuch)
            {
                // looking at a chute
                if (FocusItem && FocusItem.HasSecondaryActivation && FocusItem.IsShowingUsePrompt)
                {
                    FocusItem.Pickup(true);
                }
            }
        }
    }

    void Detach()
    {
        // turn the rigid body on the object to non-kinematic
        AttachedItem.gameObject.GetComponent<Rigidbody>().isKinematic = false;

        // detach the item
        AttachedItem.transform.SetParent(null);

        // re-enable being picked up
        AttachedItem.CanBePickedUp = true;
        AttachedItem.enabled = true;

        AttachedItem = null;
    }

    void Attach(InteractableItemBase item)
    {
        AttachedItem = item;

        // play the pickup sound if needed
        if (!string.IsNullOrEmpty(AttachedItem.PickupSound))
            AkSoundEngine.PostEvent(AttachedItem.PickupSound, AttachedItem.gameObject);

        // turn the rigid body on the object to kinematic
        AttachedItem.gameObject.GetComponent<Rigidbody>().isKinematic = true;

        PreviousAttachPointLocation = AttachmentPoint.transform.position;
        TimeSinceLastMoveSound = MoveSoundRepeatInterval;

        // attach the item
        AttachedItem.transform.SetParent(AttachmentPoint);
    }
}
