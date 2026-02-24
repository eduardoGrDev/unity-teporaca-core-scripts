using System.Collections;
using UnityEngine;

public class AttackState : State<EnemyController>
{
   [SerializeField] float attackDistance = 1f;

   bool isAttacking;
   EnemyController enemy;

   public override void Enter (EnemyController ownwer)
   {
        enemy = ownwer;
        enemy.NavAgent.stoppingDistance = attackDistance;
   }

   public override void Execute ()
   {
         if (isAttacking) return;

         if (!enemy.Target) return;
         enemy.NavAgent.SetDestination(enemy.Target.transform.position);

         if (Vector3.Distance(enemy.Target.transform.position, enemy.transform.position) <= attackDistance + 0.03f)
            StartCoroutine(Attack(Random.Range(0, enemy.Fighter.Attacks.Count + 1)));
   }

   IEnumerator Attack(int comboCount = 1) 
   {
      isAttacking = true;
      enemy.Animator.applyRootMotion = true;

      enemy.Fighter.TryToAttack();

      for (int i = 1; i < comboCount; i++)
      {
         yield return new WaitUntil(() => enemy.Fighter.AttackStates == AttackStates.Cooldown);
         enemy.Fighter.TryToAttack();
      }

      yield return new WaitUntil(() => enemy.Fighter.AttackStates == AttackStates.Idle);

      enemy.Animator.applyRootMotion = false;
      isAttacking = false;


      if (enemy.IsInState(EnemyStates.Attack))
         enemy.ChangeState(EnemyStates.RetreatAfterAttack);
   }

   public override void Exit ()
   {
      enemy.NavAgent.ResetPath();
   }
}
