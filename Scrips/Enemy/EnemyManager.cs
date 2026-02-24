using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] Vector2 timeRangeBetweenAttack = new Vector2(1,4);
    [SerializeField] CombatController player;
    [field: SerializeField] public LayerMask EnemyLayer { get; private set; }
    public static EnemyManager I { get; private set; }
    private void Awake()
    {
        I = this;
    }
    public List <EnemyController> enemiesInRange = new List<EnemyController>();
    float notAttackingTimer = 2f;
    
    public void AddEnemyInRange (EnemyController enemy)
    {
        if (!enemiesInRange.Contains(enemy))
            enemiesInRange.Add(enemy);
    }

    public void RemoveEnemyInRange (EnemyController enemy)
    {
        enemiesInRange.Remove(enemy);

        if (enemy == player.TargetEnemy)
            {
                enemy.MeshHighlighter.HighlightMesh(false);
                player.TargetEnemy = GetClosesEnemyToDirection(player.GetTargetingDir());
                player.TargetEnemy?.MeshHighlighter?.HighlightMesh(true);
            }
    }

    float timer = 0f;

    private void Update()
    {
        if (enemiesInRange.Count == 0) return;

        if (!enemiesInRange.Any(e => e.IsInState(EnemyStates.Attack)))
        {
            if (notAttackingTimer > 0)
                notAttackingTimer -= Time.deltaTime;

            if (notAttackingTimer <= 0)
            {
                var attackingEnemy = SelectEnemyForAttack();
                if (attackingEnemy != null)
                {
                    attackingEnemy.ChangeState(EnemyStates.Attack);
                    notAttackingTimer = Random.Range(timeRangeBetweenAttack.x,timeRangeBetweenAttack.y);
                }
            }
        }

        if (timer >= 0.1f)
        {
            timer = 0f;
            var closestEnemy = GetClosesEnemyToDirection(player.GetTargetingDir());
            if (closestEnemy != null && closestEnemy != player.TargetEnemy)
            {
                var prevEnemy = player.TargetEnemy;
                player.TargetEnemy = closestEnemy;

                player?.TargetEnemy?.MeshHighlighter.HighlightMesh(true);
                prevEnemy?.MeshHighlighter?.HighlightMesh(false);
            }
        }

        timer += Time.deltaTime;
    }

    EnemyController SelectEnemyForAttack ()
    {
        return enemiesInRange.OrderByDescending(e => e.CombatMovementTimer).FirstOrDefault(e => e.Target != null && e.IsInState(EnemyStates.CombatMovement));
    }

    public EnemyController GetAttackingEnemy ()
    {
        return enemiesInRange.FirstOrDefault(e => e.IsInState(EnemyStates.Attack));
    }

    public EnemyController GetClosesEnemyToDirection(Vector3 direction)
    {
        float minDistance = Mathf.Infinity;
        EnemyController closestEnemy = null;

        foreach (var enemy in enemiesInRange)
        {
            var vecToEnemy = enemy.transform.position - player.transform.position;
            vecToEnemy.y = 0; // Ignora la componente vertical (altura)

            float angle = Vector3.Angle(direction, vecToEnemy); // Calcula el ángulo entre la dirección de la cámara y el enemigo
            float distance = vecToEnemy.magnitude * Mathf.Sin(angle * Mathf.Deg2Rad); // Calcula la distancia al enemigo en el plano horizontal

            if (distance < minDistance)
            {
                minDistance = distance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }
}
