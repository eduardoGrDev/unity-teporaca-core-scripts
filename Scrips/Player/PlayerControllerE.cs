using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

// Controla el movimiento, salto, carrera y evasión del jugador.
public class PlayerControllerE : MonoBehaviour
{
    // --- CONFIGURACIÓN DE MOVIMIENTO ---
    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad base de movimiento del jugador.")]
    [SerializeField] float moveSpeed = 5f; 
    [Tooltip("Multiplicador aplicado a la velocidad base cuando el jugador corre.")]
    [SerializeField] float runSpeedMultiplier = 1.8f;
    [Tooltip("Velocidad a la que el personaje rota para encarar la dirección de movimiento o al enemigo.")]
    [SerializeField] float rotationSpeed = 500f; 

    // --- CONFIGURACIÓN DE SALTO ---
    [Header("Configuración de Salto")]
    [Tooltip("Fuerza aplicada al jugador al saltar.")]
    [SerializeField] float jumpForce = 8f; 
    [Tooltip("Costo de estamina para realizar un salto.")]
    [SerializeField] float jumpStaminaCost = 15f; 

    // --- CONFIGURACIÓN DE EVASIÓN ---
    [Header("Configuración de Evasión")]
    [Tooltip("Costo de estamina para realizar una evasión.")]
    [SerializeField] float dodgeStaminaCost = 25f;
    [Tooltip("Nombre del estado de animación de evasión en el Animator.")]
    [SerializeField] string dodgeAnimName = "Dodge"; 
    [Tooltip("Duración total de la animación de evasión (usada para el timeout de la corrutina).")]
    [SerializeField] float dodgeAnimationDuration = 0.7f; 
    [Tooltip("Retraso desde el inicio de la evasión hasta que comienzan los fotogramas de invulnerabilidad (i-frames).")]
    [SerializeField] float dodgeInvulnerabilityStartTime = 0.05f;
    [Tooltip("Duración de los fotogramas de invulnerabilidad durante la evasión.")]
    [SerializeField] float dodgeInvulnerabilityDuration = 0.4f;
    [Tooltip("Velocidad de movimiento durante la evasión si no se usa Root Motion.")]
    [SerializeField] float dodgeSpeed = 8f;
    [Tooltip("Indica si la animación de evasión utiliza Root Motion para su desplazamiento.")]
    [SerializeField] bool dodgeUsesRootMotion = true;

    // --- OTROS COSTOS DE ESTAMINA ---
    [Header("Otros Costos de Estamina")]
    [Tooltip("Costo de estamina por segundo mientras se corre.")]
    [SerializeField] float runStaminaCostPerSecond = 10f; 

    // --- CONFIGURACIÓN DE DETECCIÓN DE SUELO ---
    [Header("Configuración de Detección de Suelo")] 
    [Tooltip("Radio de la esfera usada para detectar si el jugador está en el suelo.")]
    [SerializeField] float groundCheckRadius = 0.2f; 
    [Tooltip("Desplazamiento vertical desde el centro del jugador para la esfera de detección de suelo.")]
    [SerializeField] Vector3 groundCheckOffset; 
    [Tooltip("Máscara de capas que se consideran 'suelo'.")]
    [SerializeField] LayerMask groundLayer; 

    // --- ESTADOS Y VARIABLES INTERNAS DE MOVIMIENTO ---
    bool isGrounded; // ¿Está el jugador tocando el suelo?
    float ySpeed; // Velocidad vertical actual del jugador (para gravedad y salto).
    Quaternion targetRotation; // Rotación objetivo hacia la que el personaje debe girar.
    private Vector3 currentInputDirection; // Dirección de input actual, orientada por la cámara.

    // --- ESTADOS DEL JUGADOR ---
    private bool isDodging = false; // ¿Está el jugador esquivando actualmente?
    private bool isRunning = false; // ¿Está el jugador corriendo actualmente?
    private bool isJumping = false; // ¿Está el jugador en el aire debido a un salto?

    // --- REFERENCIAS A COMPONENTES Y SINGLETONS ---
    CameraControllerE cameraController; 
    Animator animator; 
    CharacterController characterController; 
    MeeleFighter meeleFighter; 
    CombatController combatController; 
    public static PlayerControllerE Instance { get; private set; } // Singleton para fácil acceso.

    // --- SISTEMA DE INPUT ---
    InputActions inputActions; 
    Vector2 movementInput; // Input raw de movimiento (ej. joystick, WASD).
    bool runButtonPressed = false; // ¿Está presionado el botón de correr?

    // Constante para la capa de acción del Animator (si se usa para CrossFade)
    private const int ACTION_ANIMATOR_LAYER = 1;

    // --- MÉTODOS DE UNITY ---
    private void Awake()
    {
        // Obtener referencias a componentes.
        cameraController = Camera.main.GetComponent<CameraControllerE>(); 
        animator = GetComponent<Animator>(); 
        characterController = GetComponent<CharacterController>();  
        meeleFighter = GetComponent<MeeleFighter>(); 
        combatController = GetComponent<CombatController>(); 
        Instance = this; // Establecer instancia del Singleton.
        inputActions = new InputActions(); // Crear instancia de las acciones de input.
    }

    private void OnEnable()
    {
        // Habilitar el mapa de acciones del jugador y suscribirse a los eventos.
        inputActions.Player.Enable(); 
        inputActions.Player.Move.performed += OnMoveInput; 
        inputActions.Player.Move.canceled += OnMoveInput;  
        inputActions.Player.Jump.performed += OnJumpInput; 
        inputActions.Player.Dodge.performed += OnDodgeInput; 
        inputActions.Player.Run.performed += OnRunInputPerformed; 
        inputActions.Player.Run.canceled += OnRunInputCanceled;   
    }

    private void OnDisable()
    {
        // Desuscribirse de los eventos y deshabilitar el mapa de acciones para evitar errores.
        inputActions.Player.Move.performed -= OnMoveInput; 
        inputActions.Player.Move.canceled -= OnMoveInput;  
        inputActions.Player.Jump.performed -= OnJumpInput; 
        inputActions.Player.Dodge.performed -= OnDodgeInput; 
        inputActions.Player.Run.performed -= OnRunInputPerformed;
        inputActions.Player.Run.canceled -= OnRunInputCanceled;
        inputActions.Player.Disable(); 
    }

    // --- MANEJADORES DE INPUT (LLAMADOS POR EVENTOS) ---

    /// <summary>Se llama cuando el input de movimiento cambia.</summary>
    public void OnMoveInput(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>(); 
    }

    /// <summary>Se llama cuando se presiona el botón de salto.</summary>
    public void OnJumpInput(InputAction.CallbackContext context)
    {
        // Condiciones para saltar: botón presionado, en el suelo, no esquivando, no en otra acción, no recibiendo golpe, y vivo.
        if (context.performed && isGrounded && !isDodging && 
            (meeleFighter != null ? !meeleFighter.InAction : true) && 
            (meeleFighter != null ? !meeleFighter.IsTakingHit : true) && 
            HealthCheck())
        {
            if (meeleFighter.ConsumeStamina(jumpStaminaCost))
            {
                isJumping = true; 
                ySpeed = jumpForce; // Aplicar fuerza de salto.
                animator.SetTrigger("Jump"); // Activar animación de salto.
            }
            // else { Debug.Log... } // Log eliminado
        }
    }

    /// <summary>Se llama cuando se presiona el botón de evasión.</summary>
    public void OnDodgeInput(InputAction.CallbackContext context)
    {
        if (context.performed) // Solo actuar cuando se presiona el botón.
        {
            // Verificar si el estado actual del personaje permite la evasión.
            bool canDodgeBasedOnState = !isDodging && 
                                        (meeleFighter != null ? !meeleFighter.InAction : true) && 
                                        (meeleFighter != null ? !meeleFighter.IsTakingHit : true) && 
                                        HealthCheck();
            // Considerar añadir 'isGrounded' aquí si no se quiere evasión en el aire.

            if (canDodgeBasedOnState)
            {
                if (meeleFighter.ConsumeStamina(dodgeStaminaCost)) // Consumir estamina.
                {
                    StartCoroutine(PerformDodge()); // Iniciar corrutina de evasión.
                }
                // else { Debug.Log... } // Log eliminado
            }
            // else { Debug.LogWarning... } // Log eliminado
        }
    }
    
    /// <summary>Se llama cuando se presiona el botón de correr.</summary>
    private void OnRunInputPerformed(InputAction.CallbackContext context) 
    {
        runButtonPressed = true;
    }
    /// <summary>Se llama cuando se suelta el botón de correr.</summary>
    private void OnRunInputCanceled(InputAction.CallbackContext context)
    {
        runButtonPressed = false;
    }
    
    // --- LÓGICA DE ACCIONES ---

    /// <summary>Corrutina que maneja la lógica de la evasión.</summary>
    private IEnumerator PerformDodge()
    {
        isDodging = true; // Marcar que está esquivando.
        Vector3 dodgeDirection = GetDodgeDirectionInternal(); // Obtener dirección de la evasión.

        // Rotar si no está en modo combate fijado.
        if (combatController != null && (!combatController.CombatMode || combatController.TargetEnemy == null))
        {
            if (dodgeDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dodgeDirection);
            }
        }
        
        // Iniciar animación de evasión (sin especificar capa, se asume configuración en Animator).
        animator.CrossFade(dodgeAnimName, 0.1f); 

        float elapsedTime = 0f;
        bool invulnerabilityActivated = false; 
        bool originalApplyRootMotionState = animator.applyRootMotion; // Guardar estado de root motion.

        // Determinar si se usará root motion efectivo para este esquive.
        bool effectivelyUseRootMotion = dodgeUsesRootMotion;
        if (combatController != null && combatController.CombatMode && combatController.TargetEnemy != null)
        {
            effectivelyUseRootMotion = false; // Forzar movimiento por script si está fijado a un enemigo.
        }

        if (effectivelyUseRootMotion)
        {
            animator.applyRootMotion = true;
        }
        else
        {
            animator.applyRootMotion = false; // Asegurar que esté desactivado para movimiento por script.
        }

        // Bucle de la evasión.
        while (elapsedTime < dodgeAnimationDuration)
        {
            // Activar invulnerabilidad.
            if (!invulnerabilityActivated && elapsedTime >= dodgeInvulnerabilityStartTime)
            {
                if(meeleFighter != null) meeleFighter.SetInvulnerable(true);
                invulnerabilityActivated = true;
            }

            // Desactivar invulnerabilidad.
            if (invulnerabilityActivated && (meeleFighter != null && meeleFighter.IsInvulnerable) && (elapsedTime >= dodgeInvulnerabilityStartTime + dodgeInvulnerabilityDuration))
            {
                if(meeleFighter != null) meeleFighter.SetInvulnerable(false);
            }

            // Mover por script si no se usa root motion efectivo.
            if (!effectivelyUseRootMotion && characterController.enabled)
            {
                // Se podría quitar isGrounded de aquí si se permite evasión en el aire y que siga la trayectoria.
                characterController.Move(dodgeDirection * dodgeSpeed * Time.deltaTime);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null; // Esperar al siguiente frame.
        }

        // Asegurar desactivación de invulnerabilidad al final.
        if (meeleFighter != null && meeleFighter.IsInvulnerable)
        {
            meeleFighter.SetInvulnerable(false);
        }
        
        // Restaurar estado de root motion.
        animator.applyRootMotion = originalApplyRootMotionState; 

        isDodging = false; // Terminar estado de esquivar.
    }

    // --- ACTUALIZACIÓN PRINCIPAL Y MOVIMIENTO ---
    private void Update()
    {
        // Calcular dirección de input actual.
        float h_update = movementInput.x;
        float v_update = movementInput.y;
        if (cameraController != null) 
        {
            currentInputDirection = cameraController.PlanarRotation * new Vector3(h_update, 0, v_update).normalized;
        }
        else // Fallback si no hay cámara.
        {
            currentInputDirection = new Vector3(h_update, 0, v_update).normalized; 
        }

        // Si está muerto, en acción, recibiendo golpe o esquivando, solo aplicar gravedad.
        if (!HealthCheck() || 
            (meeleFighter != null && (meeleFighter.InAction || meeleFighter.IsTakingHit)) || 
            isDodging) 
        {
            if (!HealthCheck()) // Si está muerto, resetear animaciones de movimiento.
            { 
                animator.SetFloat("forwardSpeed", 0); 
                animator.SetFloat("strafeSpeed", 0); 
                animator.SetBool("IsRunning", false); 
            }
            ApplyGravity(); 
            return; // No procesar HandleMovement.
        }
        
        HandleMovement(); // Procesar movimiento normal.
        ApplyGravity(); // Aplicar gravedad.
    }

    /// <summary>Maneja la lógica de movimiento y rotación del jugador.</summary>
    private void HandleMovement()
    {
        var moveDir = currentInputDirection; // Usar dirección ya calculada.
        float moveAmount = Mathf.Clamp01(Mathf.Abs(movementInput.x) + Mathf.Abs(movementInput.y));

        float currentSpeed = moveSpeed;
        isRunning = false; 

        // Lógica de correr.
        if (runButtonPressed && moveAmount > 0.1f && isGrounded && !isDodging) 
        {
            if (meeleFighter != null && meeleFighter.ConsumeStamina(runStaminaCostPerSecond * Time.deltaTime))
            {
                currentSpeed *= runSpeedMultiplier;
                isRunning = true;
            }
            // else { Debug.Log... } // Log eliminado
        }
        animator.SetBool("IsRunning", isRunning); // Actualizar parámetro del Animator.

        var velocity = moveDir * currentSpeed * moveAmount; // Calcular velocidad final.

        // Lógica de rotación y animación en modo combate.
        if (combatController != null && combatController.CombatMode && combatController.TargetEnemy != null)
        {
            var targetVec = combatController.TargetEnemy.transform.position - transform.position;
            targetVec.y = 0; // Ignorar diferencia de altura.
            // Rotar hacia el enemigo.
            if (moveAmount > 0.01f || (moveAmount < 0.01f && combatController.TargetEnemy != null) ) 
            {
                targetRotation = Quaternion.LookRotation(targetVec.normalized);
            }
            // Calcular velocidad local para animaciones de strafing.
            Vector3 localVel = transform.InverseTransformDirection(velocity);
            animator.SetFloat("forwardSpeed", localVel.z / (isRunning ? moveSpeed * runSpeedMultiplier : moveSpeed), 0.1f, Time.deltaTime);
            animator.SetFloat("strafeSpeed", localVel.x / (isRunning ? moveSpeed * runSpeedMultiplier : moveSpeed), 0.1f, Time.deltaTime);
        }
        else // Lógica de rotación y animación en modo exploración.
        {
            if (moveAmount > 0.01f) // Rotar en la dirección del movimiento.
            {
                targetRotation = Quaternion.LookRotation(moveDir);
            }
            animator.SetFloat("forwardSpeed", moveAmount * (isRunning ? runSpeedMultiplier : 1f), 0.1f, Time.deltaTime); 
            animator.SetFloat("strafeSpeed", 0, 0.1f, Time.deltaTime); // No hay strafing en exploración.
        }
        
        // Aplicar rotación si hay movimiento o está en modo combate con objetivo, y no está esquivando.
        if ((moveAmount > 0.01f || (combatController != null && combatController.CombatMode && combatController.TargetEnemy != null)) && !isDodging)
        {
             transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Mover el CharacterController.
        if (characterController.enabled) 
        {
            characterController.Move(velocity * Time.deltaTime);
        }
    }

    /// <summary>Aplica la gravedad al jugador.</summary>
    private void ApplyGravity()
    {
        GroundCheck(); // Verificar si está en el suelo.
        if (isGrounded && ySpeed < 0) 
        {
            ySpeed = -0.5f; // Pequeña fuerza hacia abajo para mantenerlo pegado al suelo.
            isJumping = false; // Resetear estado de salto.
        }
        else
        {
            ySpeed += Physics.gravity.y * Time.deltaTime; // Aplicar gravedad estándar.
        }
        
        // Mover verticalmente.
        if (characterController.enabled)
        {
            characterController.Move(new Vector3(0, ySpeed, 0) * Time.deltaTime);
        }
    }

    /// <summary>Verifica si el jugador está tocando el suelo.</summary>
    public void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(
            transform.TransformPoint(groundCheckOffset), // Posición de la esfera de chequeo.
            groundCheckRadius, // Radio de la esfera.
            groundLayer // Capas consideradas como suelo.
        );
        animator.SetBool("IsGrounded", isGrounded); // Actualizar parámetro del Animator.
        // Resetear estado de salto si aterriza.
        if (isGrounded && isJumping && ySpeed < 0.1f) { isJumping = false; }
    }
    
    /// <summary>Verifica si el MeeleFighter existe y tiene salud.</summary>
    private bool HealthCheck()
    {
        return meeleFighter != null && meeleFighter.Health > 0;
    }

    /// <summary>Obtiene la dirección de intención actual del jugador (para ataques, etc.).</summary>
    public Vector3 GetIntentDirection()
    {
        // Devuelve la dirección de input actual si hay movimiento,
        // o la dirección hacia adelante del personaje si no hay input.
        if (movementInput.sqrMagnitude > 0.01f) // Si hay input de movimiento significativo.
        {
            return currentInputDirection.normalized; // currentInputDirection ya está orientado por la cámara.
        }
        else
        {
            return transform.forward; // Hacia adelante del personaje.
        }
    }

    /// <summary>Método interno para obtener la dirección específica de la evasión.</summary>
    private Vector3 GetDodgeDirectionInternal()
    {
        if (movementInput.sqrMagnitude > 0.01f) // Si hay input de movimiento.
        {
            return currentInputDirection.normalized; // Usar dirección de input actual.
        }
        else
        {
            return -transform.forward; // Esquivar hacia atrás si no hay input.
        }
    }

    // --- DEBUG ---
    /// <summary>Dibuja la esfera de GroundCheck en el Editor para visualización.</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f); // Color verde semitransparente.
        if (characterController != null) 
        {
            Gizmos.DrawSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius);
        }
    }
}
