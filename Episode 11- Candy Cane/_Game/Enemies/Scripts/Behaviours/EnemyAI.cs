using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;

[System.Serializable]
public class EnemyDestroyedEvent : UnityEvent<EnemyAI> {}
[System.Serializable]
public class ReachedDestinationEvent : UnityEvent<EnemyAI> {}

public class EnemyAI : MonoBehaviour
{
    public enum State
    {
        None,

        Idle,
        MovingToTarget,
        WaitingToAttack,
        Attacking,
        Dying
    }

    [Header("General Settings")]
    public EnemyConfig Config;
    public State CurrentState = State.None;
    public int CurrentHealth = -1;
    public float MovementSpeed = 1f;

    public AttackPoint CurrentAttackPoint = null;

    public EnemyDestroyedEvent OnDestroyed;
    public ReachedDestinationEvent OnReachedDestination;

    protected NavMeshAgent Agent;
    protected Animator AnimController;
    protected bool ReachedDestination = false;
    protected BuildingPiece CurrentTarget;
    protected float NextAttackCooldown = -1;
    protected bool IsStunned = false;
    protected float EffectDamage = 0f;

    protected List<StatusEffect> ActiveEffects = new List<StatusEffect>();
    protected List<float> ActiveEffectsTimeRemaining = new List<float>();

    public bool CanTakeDamage
    {
        get
        {
            return CurrentHealth > 0;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        CurrentHealth = Mathf.CeilToInt(Config.HitPoints * AIManager.Instance.CurrentHealthScale);
        Agent = GetComponent<NavMeshAgent>();
        AnimController = GetComponent<Animator>();

        Agent.speed = MovementSpeed;

        ClearTarget();
    }

    protected bool WasAllowedToMove = true;
    protected bool _IsAllowedToMove = true;
    protected bool IsAllowedToMove
    {
        get
        {
            return _IsAllowedToMove;
        }
        set
        {
            if (WasAllowedToMove && !value)
                Agent.isStopped = true;
            else if (!WasAllowedToMove && value)
                Agent.isStopped = false;

            WasAllowedToMove = IsAllowedToMove;
            _IsAllowedToMove = value;
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool isMoving = false;
        bool isAttacking = false;

        // do nothing if dying
        if (CurrentState == State.Dying)
            return;

        // has the game paused?
        if (AIManager.Instance.IsPaused)
        {
            IsAllowedToMove = false;
            return;
        }

        // if stunned update effects
        if (IsStunned)
        {
            UpdateStatusEffects();
            IsAllowedToMove = false;
            return;
        }

        IsAllowedToMove = true;

        if (CurrentState == State.MovingToTarget)
        {
            // check if we currently have a valid path
            if (Agent.hasPath && Agent.pathStatus == NavMeshPathStatus.PathComplete)
            {
                // moving along the path?
                if (Agent.remainingDistance > 0.1f)
                {
                    if (Agent.velocity.sqrMagnitude > 0.1f)
                        isMoving = true;
                }
                else if (!ReachedDestination)
                {
                    ReachedDestination = true;
                    OnReachedDestination?.Invoke(this);
                }
            }
        }
        else if (CurrentState == State.Attacking && CurrentTarget != null)
        {
            NextAttackCooldown -= Time.deltaTime;
            isAttacking = true;

            // time to attack?
            if (NextAttackCooldown <= 0)
            {
                AkSoundEngine.PostEvent("Play_SnowmanAttack", gameObject);
                CurrentTarget.OnTakeDamage(Config.DamagePerAttack);

                NextAttackCooldown = Config.AttackInterval;
            }
        }

        AnimController.SetBool("Moving", isMoving);
        AnimController.SetBool("Attacking", isAttacking);
    }

    void UpdateStatusEffects()
    {
        // update the remaining time on effects
        for (int index = 0; index < ActiveEffects.Count; ++index)
        {
            ActiveEffectsTimeRemaining[index] -= Time.deltaTime;

            // effect has expired?
            if (ActiveEffectsTimeRemaining[index] <= 0)
            {
                // remove the effect
                ActiveEffects.RemoveAt(index);
                ActiveEffectsTimeRemaining.RemoveAt(index);

                --index;
            }
        }
        
        // apply each effect
        float speedScale = 1f;
        bool wasStunned = IsStunned;
        IsStunned = false;
        foreach(StatusEffect effect in ActiveEffects)
        {
            if (effect.Type == StatusEffect.Types.Burn)
            {
                EffectDamage += effect.Strength * Time.deltaTime;
            }
            else if (effect.Type == StatusEffect.Types.Stun)
            {
                IsStunned = true;
            }
            else if (effect.Type == StatusEffect.Types.Slow)
            {
                speedScale *= effect.Strength;
            }
        }

        Agent.speed = MovementSpeed * speedScale;

        // accumulated enough damage to apply it?
        int damageToApply = Mathf.FloorToInt(EffectDamage);
        if (damageToApply > 0)
        {
            EffectDamage -= damageToApply;

            OnTakeDamage(damageToApply);
        }
    }

    public void OnJump()
    {
        AkSoundEngine.PostEvent("Play_Snowman_Jump", gameObject);
    }

    public void OnLand()
    {
        AkSoundEngine.PostEvent("Play_Snowman_Land", gameObject);
    }

    public void OnTakeDamage(int amount)
    {
        // exit if cannot take damage
        if (!CanTakeDamage)
            return;

        CurrentHealth -= amount;

        // now dead?
        if (CurrentHealth <= 0)
        {
            AkSoundEngine.PostEvent("Play_Snowman_Death", gameObject);

            ClearTarget();

            OnDestroyed?.Invoke(this);

            CurrentState = State.Dying;
            Destroy(gameObject);
        }
        else
        {
            AkSoundEngine.PostEvent("Play_Snowman_Hurt", gameObject);
        }
    }

    public void ApplyStatusEffects(List<StatusEffect> effects)
    {
        foreach(StatusEffect effect in effects)
        {
            ActiveEffects.Add(effect);
            ActiveEffectsTimeRemaining.Add(effect.Duration);
        }
    }

    public void SetTarget(Vector3 target)
    {
        ReachedDestination = false;
        Agent.SetDestination(target);
        CurrentState = State.MovingToTarget;
    }

    public void SetAttackTarget(BuildingPiece target)
    {
        CurrentTarget = target;
        CurrentState = State.Attacking;
    }

    public void ClearTarget()
    {
        // clear the attack point
        if (CurrentAttackPoint != null)
            CurrentAttackPoint.IsOccupied = false;
        CurrentAttackPoint = null;
        CurrentTarget = null;

        Agent.ResetPath();

        CurrentState = State.Idle;
    }
}
