using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

[System.Serializable]
public class SquadDestroyedEvent : UnityEvent<EnemySquad> {}

public class EnemySquad : MonoBehaviour
{
    public enum State
    {
        None,

        Idle,
        SearchingForTarget,
        MoveToTerminalRange,
        Attacking,
        Dying
    }
    public State CurrentState = State.None;

    // range to give precise marker
    public float TerminalRange = 30f;

    public SquadDestroyedEvent OnSquadDestroyed;

    protected List<EnemyAI> SquadMembers = new List<EnemyAI>();
    protected SpawnerLocation StartDirection;

    protected BuildingPiece CurrentTarget;

    public void Initialise(List<EnemyAI> squad, SpawnerLocation spawnerLocation)
    {
        StartDirection = spawnerLocation;

        // associated the squad
        SquadMembers = squad;

        foreach(EnemyAI member in SquadMembers)
        {
            //member.transform.SetParent(transform);
            member.OnDestroyed.AddListener(OnSquadMemberDestroyed);
            member.OnReachedDestination.AddListener(OnSquadMemberAtDestination);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (AIManager.Instance.IsPaused)
            return;

        if (CurrentState == State.Dying)
            return;

        // update the squad location
        Vector3 newLocation = Vector3.zero;
        foreach(EnemyAI member in SquadMembers)
        {
            newLocation += member.transform.position;
        }
        
        // average and update the squad location
        if (SquadMembers.Count > 0)
            newLocation /= SquadMembers.Count;
        transform.position = newLocation;

        // dispatch based on state
        switch(CurrentState)
        {
            case State.Idle:
                SwitchState(State.SearchingForTarget);
            break;

            case State.SearchingForTarget:
                State_SearchingForTarget();
            break;

            case State.MoveToTerminalRange:
                State_MoveToTerminalRange();
            break;

            case State.Attacking:
            break;
        }
    }

    protected void SwitchState(State newState)
    {
        // nothing to do
        if (CurrentState == newState)
            return;

        // update the state
        CurrentState = newState;

        // switching to the idle state?
        if (CurrentState == State.Idle)
        {
            foreach(EnemyAI member in SquadMembers)
            {
                member.ClearTarget();
            }
        } // entered move to terminal range?
        else if (CurrentState == State.MoveToTerminalRange)
        {
            CurrentTarget.OnPieceDestroyed.AddListener(OnTargetDestroyed);

            foreach(EnemyAI member in SquadMembers)
            {
                member.ClearTarget();
                member.SetTarget(CurrentTarget.transform.position);
            }
        }
    }

    protected void State_SearchingForTarget()
    {
        // find the closest target
        List<BuildingPiece> sortedTargets = AIManager.Instance.AvailableTargets.OrderBy(target => Mathf.Pow(target.transform.position.x - transform.position.x, 2) + Mathf.Pow(target.transform.position.z - transform.position.z, 2)).ToList();

        // no targets
        if (sortedTargets.Count == 0)
            return;

        // attempt to extract any gates - prioritise clearing the path
        List<BuildingPiece> gates = sortedTargets.Where(target => target.Config.IsPathBlocker && target.Direction == StartDirection).ToList();
        if (gates != null && gates.Count > 0)
        {
            CurrentTarget = gates[0];
            SwitchState(State.MoveToTerminalRange);
            return;
        }

        // build the foundation list
        List<BuildingPiece> foundations = sortedTargets.Where(target => !target.Config.IsPathBlocker).ToList();

        // no foundations
        if (foundations.Count == 0)
            return;

        // set the target
        CurrentTarget = foundations[0];
        SwitchState(State.MoveToTerminalRange);
    }

    protected void State_MoveToTerminalRange()
    {
        bool anyWithoutAttackPoint = false;

        // check if any are within terminal range and have no marker
        foreach(EnemyAI member in SquadMembers)
        {
            // skip if already has an attack point
            if (member.CurrentAttackPoint != null)
                continue;

            // calculate the distance
            Vector3 vectorToTarget = member.transform.position - CurrentTarget.transform.position;
            vectorToTarget.y = 0;

            // within terminal range
            if (vectorToTarget.sqrMagnitude <= (TerminalRange * TerminalRange))
            {
                AssignAttackPoint(member);
            }
            else
                anyWithoutAttackPoint = true;
        }

        if (!anyWithoutAttackPoint)
            SwitchState(State.Attacking);
    }

    protected void AssignAttackPoint(EnemyAI member)
    {
        // get the available attack points
        List<AttackPoint> availablePoints = CurrentTarget.AttackPoints.Where(attackPoint => !attackPoint.IsOccupied).ToList();

        // no attack points?
        if (availablePoints.Count == 0)
            return;

        // are their slots we can attack from free?
        List<AttackPoint> attackablePoints = availablePoints.Where(attackPoint => attackPoint.IsFrontLine).ToList();

        // are there ones we can attack from?
        if (attackablePoints.Count > 0)
        {
            // pick a random attackable point
            member.CurrentAttackPoint = attackablePoints[Random.Range(0, attackablePoints.Count)];
        }
        else
        {
            // sort the points and pick one
            List<AttackPoint> sortedPoints = availablePoints.OrderBy(attackPoint => Vector3.Distance(attackPoint.Location, member.transform.position)).ToList();

            member.CurrentAttackPoint = sortedPoints[0];
        }

        // lock the attack point and update the member
        member.CurrentAttackPoint.IsOccupied = true;
        member.SetTarget(member.CurrentAttackPoint.Location);
    }
    
    public void ApplyDamageToSquad(int amount)
    {
        // apply damage to every squad member (reverse order in case they are destroyed)
        for (int index = SquadMembers.Count - 1; index >= 0; --index)
        {
            SquadMembers[index].OnTakeDamage(amount);
        }
    }

    void OnSquadMemberDestroyed(EnemyAI member)
    {
        // remove from squad
        SquadMembers.Remove(member);

        // squad finished?
        if (SquadMembers.Count == 0)
        {
            AkSoundEngine.PostEvent("Play_SquadDestroyed", gameObject);
            
            OnSquadDestroyed?.Invoke(this);

            SwitchState(State.Dying);
            Destroy(gameObject, 0.1f);
        } // if we were attacking then we may have members that can now attack
        else if (CurrentState == State.Attacking)
        {
            // build the list of members with no attack point
            List<EnemyAI> membersWithNoAttackPoint = SquadMembers.Where(squadMember => squadMember.CurrentAttackPoint == null).ToList();

            // build the list of attack points
            List<AttackPoint> attackPoints = CurrentTarget.AttackPoints.Where(attackPoint => attackPoint.IsFrontLine && !attackPoint.IsOccupied).ToList();

            // are there free slots and free squad members?
            if (membersWithNoAttackPoint.Count > 0 && attackPoints.Count > 0)
            {
                foreach(AttackPoint attackPoint in attackPoints)
                {
                    // pick a squad member
                    EnemyAI squadMember = membersWithNoAttackPoint[Random.Range(0, membersWithNoAttackPoint.Count)];

                    // set the target
                    attackPoint.IsOccupied = true;
                    squadMember.CurrentAttackPoint = attackPoint;
                    squadMember.SetTarget(attackPoint.Location);

                    // remove the squad member
                    membersWithNoAttackPoint.Remove(squadMember);
                    if (membersWithNoAttackPoint.Count == 0)
                        break;
                }
            }
        }
    }

    void OnSquadMemberAtDestination(EnemyAI member)
    {
        // are they able to attack?
        if (member.CurrentAttackPoint != null && member.CurrentAttackPoint.IsFrontLine)
        {
            member.SetAttackTarget(CurrentTarget);
        }
    }

    void OnTargetDestroyed(BuildingPiece target)
    {
        // target was destroyed - reset the AI
        if (target == CurrentTarget)
        {
            // kill the squad?
            if (CurrentTarget.Config.KillsAttackersOnDestruction && CurrentState == State.Attacking)
            {
                ApplyDamageToSquad(int.MaxValue);
                return;
            }

            CurrentTarget = null;
            SwitchState(State.Idle);
        }
    }
}
