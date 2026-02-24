using UnityEngine; // Importa las funciones básicas de Unity
using UnityEngine.InputSystem; // Importa el nuevo sistema de entrada (Input System)

// Este script controla el sistema de combate (golpes/ataques) del personaje
public class CombatController : MonoBehaviour
{
    EnemyController targetEnemy; // Enemigo objetivo actual
    public EnemyController TargetEnemy
    {
        get => targetEnemy; // Devuelve el enemigo objetivo actual
        set
        {
            targetEnemy = value; // Establece el nuevo enemigo objetivo

            if (targetEnemy == null)
                CombatMode = false; // Si no hay enemigo objetivo, desactiva el modo combate
        }
    }

    bool combatMode;

    public bool CombatMode
    {
        get => combatMode; // Devuelve el estado actual del modo combate
        set
        {
            combatMode = value;

            if (TargetEnemy == null)
                combatMode = false; // Si no hay enemigo objetivo, el modo combate se desactiva
            
            if (animator != null) // Asegurarse que el animator no sea null
                animator.SetBool("combatMode", combatMode); // Cambia el parámetro "combatMode" del Animator
        }
    }

    // Referencia al componente MeeleFighter, encargado de gestionar los ataques cuerpo a cuerpo
    MeeleFighter meeleFighter;

    Animator animator; // Referencia al Animator del personaje

    CameraControllerE cam; // Referencia al controlador de cámara

    // Awake se llama antes de Start, ideal para obtener referencias a componentes en el mismo GameObject
    private void Awake()
    {
        // Obtiene el componente MeeleFighter del mismo GameObject donde esté este script
        meeleFighter = GetComponent<MeeleFighter>();
        animator = GetComponent<Animator>();
        
        // Es más seguro buscar la cámara principal y luego su componente
        if (Camera.main != null)
        {
            cam = Camera.main.GetComponent<CameraControllerE>(); 
        }
        else
        {
            Debug.LogError("No se encontró la cámara principal (MainCamera) en la escena.");
        }
    }

    private void Start()
    {
        if (meeleFighter == null)
        {
            Debug.LogError("MeeleFighter no encontrado en el GameObject: " + gameObject.name);
            return;
        }

        meeleFighter.OnGotHit += (MeeleFighter attacker) => 
        {
            // Asegurarse que attacker y TargetEnemy no sean null antes de acceder a sus componentes
            if (attacker != null && CombatMode && attacker != (TargetEnemy != null ? TargetEnemy.Fighter : null))
            {
                EnemyController enemyCtrl = attacker.GetComponent<EnemyController>();
                if (enemyCtrl != null)
                {
                    TargetEnemy = enemyCtrl; // Si el atacante no es el enemigo objetivo, lo establece como nuevo objetivo
                }
            }
        };
    }

    // Esta función se conecta al nuevo sistema de entrada (Input System)
    // Se debe enlazar en el Input Action con el evento "performed" o "started"
    public void OnAttack(InputAction.CallbackContext context)
    {
        // *** INICIO DE LA CORRECCIÓN DEL BUG ***
        // Verifica si el personaje está vivo antes de procesar el ataque
        if (meeleFighter != null && meeleFighter.Health <= 0)
        {
            return; // Si está muerto, no hacer nada
        }
        // *** FIN DE LA CORRECCIÓN DEL BUG ***

        // Verifica si el botón de ataque acaba de ser presionado (fase "started")
        if (context.started && meeleFighter != null && !meeleFighter.IsTakingHit)
        {
            // Llama al método TryToAttack() del componente MeeleFighter
            // Este método intentará realizar un ataque si las condiciones lo permiten
           var enemy = EnemyManager.I.GetAttackingEnemy();
        
            if (enemy != null && enemy.Fighter != null && enemy.Fighter.IsCounterable && !meeleFighter.InAction)
            {
                StartCoroutine(meeleFighter.PerformCounterAttack(enemy)); // Realiza un contraataque si el enemigo es atacable y el personaje no está en acción
            }
            else
            {
                // Asegurarse que PlayerControllerE.Instance no sea null
                var playerInstance = PlayerControllerE.Instance;
                if (playerInstance != null) {
                    var enemyToAttack = EnemyManager.I.GetClosesEnemyToDirection(playerInstance.GetIntentDirection()); // Obtiene el enemigo más cercano a la dirección de entrada del jugador
                    meeleFighter.TryToAttack(enemyToAttack?.Fighter); // Intenta realizar un ataque hacia el enemigo
                } else {
                     meeleFighter.TryToAttack(); // Ataca sin un objetivo específico si no se puede determinar la dirección
                }


                CombatMode = true; // Activa el modo combate al atacar
            }
            
        }
    }

    public void combatModeOn(InputAction.CallbackContext context)
    {
        // Verifica si el botón de combate acaba de ser presionado (fase "started")
        if (context.started)
        {
            CombatMode = !CombatMode; // Cambia el estado del modo combate
        }
    }


    void OnAnimatorMove()
    {
        if (animator == null || meeleFighter == null) return;

        // Si el personaje está en medio de un contraataque, no se mueve
        if (!meeleFighter.InCounter)
        {
            // Solo aplicar animator.deltaPosition si el CharacterController está habilitado
            if (GetComponent<CharacterController>() != null && GetComponent<CharacterController>().enabled)
            {
                transform.position += animator.deltaPosition;
            }
        }
        
        transform.rotation *= animator.deltaRotation;
    }

    public Vector3 GetTargetingDir()
    {
        if (cam == null) // Si no hay cámara, devuelve la dirección hacia adelante del personaje
        {
            return transform.forward;
        }

        if (!CombatMode) // Si no está en modo combate, devuelve la dirección desde la cámara
        {
            var vecFromCam = transform.position - cam.transform.position; // Calcula la dirección desde la cámara hacia el personaje
            vecFromCam.y = 0f; // Ignora la componente vertical (altura)
            return vecFromCam.normalized; // Devuelve la dirección normalizada (vector unitario)
        }
        else
        {
            return transform.forward; // Si está en modo combate, devuelve la dirección hacia adelante del personaje
        }
    }
}
