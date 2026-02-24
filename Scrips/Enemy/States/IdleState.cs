// Este script define un ScriptableObject que almacena datos sobre ataques en un sistema de combate.
// Este script es parte de un sistema de combate en Unity y se utiliza para definir los datos de un ataque.

public class IdleState : State<EnemyController>
{
    EnemyController enemy;
    public override void Enter(EnemyController owner)
    {
        enemy = owner;

        enemy.Animator.SetInteger("weaponType", 0);
    }

    public override void Execute()
    {
        enemy.Target = enemy.FindTarget();
        if (enemy.Target != null)
        {
            enemy.AlertNearbyEnemies();
            enemy.ChangeState(EnemyStates.CombatMovement);
        }
    }

    public override void Exit()
    {
        
    }
}
