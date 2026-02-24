// Este script define un ScriptableObject que almacena datos sobre ataques en un sistema de combate.
// Este script es parte de un sistema de combate en Unity y se utiliza para definir los datos de un ataque.

public class DeadState : State<EnemyController>
{
    override public void Enter(EnemyController owner)
    {
        owner.VisionSensor.gameObject.SetActive(false);
        EnemyManager.I.RemoveEnemyInRange(owner);

        owner.NavAgent.enabled = false;
        owner.CharacterController.enabled = false;
    }
}
