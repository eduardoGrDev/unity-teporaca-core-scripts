using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyStates { Idle, CombatMovement, Attack, RetreatAfterAttack, Dead, GettingHit }

public class EnemyController : MonoBehaviour
{
    [field: SerializeField] public float Fov { get; private set; } = 180f;
    [field: SerializeField] public float AlertRange { get; private set; } = 20f;
    [field: SerializeField] public WeaponType Weapon { get; private set; }

    public List<MeeleFighter> TargetsInRange {get; set;} = new List<MeeleFighter>();
    public MeeleFighter Target { get; set; }
    public SkinnedMeshHighlighter MeshHighlighter { get; private set; }
    public float CombatMovementTimer { get; set; } = 0f;

    public StateMachine<EnemyController> StateMachine { get; private set;}

    Dictionary<EnemyStates, State<EnemyController>> stateDict;

    public NavMeshAgent NavAgent { get; private set; }
    public CharacterController CharacterController { get; private set; }
    public Animator Animator { get; private set; }
    public MeeleFighter Fighter { get; private set; }
    public VisionSensor VisionSensor { get; set; }



    private void Start()
    {
        NavAgent = GetComponent<NavMeshAgent>();
        CharacterController = GetComponent<CharacterController>();
        Animator = GetComponent<Animator>();
        Fighter = GetComponent<MeeleFighter>();
        MeshHighlighter = GetComponent<SkinnedMeshHighlighter>();

        stateDict = new Dictionary<EnemyStates, State<EnemyController>>();
        stateDict[EnemyStates.Idle] = GetComponent<IdleState>();
        stateDict[EnemyStates.CombatMovement] = GetComponent<CombatMovementState>();
        stateDict[EnemyStates.Attack] = GetComponent<AttackState>();
        stateDict[EnemyStates.RetreatAfterAttack] = GetComponent<RetreatAfterAttackState>();
        stateDict[EnemyStates.Dead] = GetComponent<DeadState>();
        stateDict[EnemyStates.GettingHit] = GetComponent<GettingHitState>();


        StateMachine = new StateMachine<EnemyController>(this);
        StateMachine.ChangeState(stateDict[EnemyStates.Idle]);

        Fighter.OnGotHit += (MeeleFighter attacker) => 
        {
            if (Fighter.Health > 0)
            {
                if (Target == null)
                {
                    Target = attacker;
                    AlertNearbyEnemies();
                }
                
                ChangeState(EnemyStates.GettingHit);
            }
            else
                ChangeState(EnemyStates.Dead);
        };

        Animator.SetInteger("weaponType", 0);
    }

    public void ChangeState(EnemyStates state)
    {
        StateMachine.ChangeState(stateDict[state]);
    }

    public bool IsInState (EnemyStates state)
    {
        return StateMachine.CurrentState == stateDict[state];
    }

    Vector3 prevPos;
    private void Update()
    {
        StateMachine.Execute();

        var deltaPos = Animator.applyRootMotion? Vector3.zero : transform.position - prevPos;
        var velocity = deltaPos / Time.deltaTime;

        float forwardSpeed = Vector3.Dot(velocity, transform.forward);
        Animator.SetFloat("forwardSpeed", forwardSpeed / NavAgent.speed, 0.2f, Time.deltaTime);

        float angle = Vector3.SignedAngle(transform.forward, velocity, Vector3.up);
        float strafeSpeed = Mathf.Sin(angle * Mathf.Deg2Rad);
        Animator.SetFloat("strafeSpeed", strafeSpeed, 0.2f, Time.deltaTime);

        if (Target?.Health <= 0)
        {
            TargetsInRange.Remove(Target);
            EnemyManager.I.RemoveEnemyInRange(this);
        }

        prevPos = transform.position;
    }

    public MeeleFighter FindTarget()
    {
        foreach (var target in TargetsInRange)
        {
            var vecToTarget = target.transform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, vecToTarget);

            if (angle <= Fov / 2 )
            {
                return target;
            }

        }

        return null;
    }

    public void AlertNearbyEnemies()
    {
        var colliders = Physics.OverlapBox(transform.position, new Vector3(AlertRange / 2f, 1f, AlertRange / 2f), Quaternion.identity,  EnemyManager.I.EnemyLayer);

        foreach (var collider in colliders)
        {
            if (collider.gameObject == gameObject) continue;

            var nearbyEnemy = collider.GetComponent<EnemyController>();
            if (nearbyEnemy != null && nearbyEnemy.Target == null)
            {
                nearbyEnemy.Target = Target;
                nearbyEnemy.ChangeState(EnemyStates.CombatMovement);
            }
        }
    }

}
