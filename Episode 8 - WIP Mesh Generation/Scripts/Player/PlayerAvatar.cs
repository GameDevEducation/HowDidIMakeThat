using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAvatar : MonoBehaviour
{
    [SerializeField] Transform VisualMeshTransform;
    [SerializeField] Rigidbody AvatarRB;
    [SerializeField] PlayerConfig PlayerConfig;

    Vector2 MoveInput;

    Vector2 Velocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    void FixedUpdate()
    {
        {
            bool bHasHorizontalInput = !Mathf.Approximately(MoveInput.x, 0f);
            float InputStrength = bHasHorizontalInput ? MoveInput.x : (-Velocity.x * PlayerConfig.HorizontalDrag);
            float HorizontalAccel = InputStrength * PlayerConfig.HorizontalAcceleration;

            if (bHasHorizontalInput && (Mathf.Sign(HorizontalAccel) != Mathf.Sign(Velocity.x)))
                HorizontalAccel *= PlayerConfig.HorizontalCounterBoost;

            Velocity.x = Mathf.Clamp(Velocity.x + HorizontalAccel * Time.deltaTime, -PlayerConfig.MaxHorizontalVelocity, PlayerConfig.MaxHorizontalVelocity);
        }

        {
            bool bHasVerticalInput = !Mathf.Approximately(MoveInput.y, 0f);
            float InputStrength = bHasVerticalInput ? MoveInput.y : (-Velocity.y * PlayerConfig.VerticalDrag);
            float VerticalAccel = InputStrength * PlayerConfig.VerticalAcceleration;

            if (bHasVerticalInput && (Mathf.Sign(VerticalAccel) != Mathf.Sign(Velocity.y)))
                VerticalAccel *= PlayerConfig.VerticalCounterBoost;

            Velocity.y = Mathf.Clamp(Velocity.y + VerticalAccel * Time.deltaTime, -PlayerConfig.MaxVerticalVelocity, PlayerConfig.MaxVerticalVelocity);
        }

        VisualMeshTransform.eulerAngles = new Vector3(Velocity.y * PlayerConfig.VelocityToRollScale, 0f, 0f);
        AvatarRB.linearVelocity = new Vector3(Velocity.x, 0, Velocity.y);// * Time.deltaTime;
    }

    void OnMove(InputValue InMoveInput)
    {
        MoveInput = InMoveInput.Get<Vector2>();
    }

    private void OnCollisionEnter(Collision InCollision)
    {
        Debug.Log(InCollision.gameObject.name);
    }
}
