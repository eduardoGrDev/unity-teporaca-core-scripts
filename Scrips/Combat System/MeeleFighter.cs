using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Enumerador para los estados de ataque del personaje.
public enum AttackStates { Idle, Windup, Impact, Cooldown }

// Gestiona la lógica de combate cuerpo a cuerpo, salud, estamina y reacciones del personaje.
public class MeeleFighter : MonoBehaviour
{
    // --- ESTADÍSTICAS DEL PERSONAJE ---
    [Header("Estadísticas Principales")] 
    [Tooltip("Salud máxima que puede tener el personaje.")] 
    [field: SerializeField] public float maxHealth { get; private set; } = 100f; 
    [Tooltip("Salud actual del personaje. Se inicializa a maxHealth.")]
    [field: SerializeField] public float Health { get; private set; } 

    [Tooltip("Estamina máxima que puede tener el personaje.")]
    [field: SerializeField] public float MaxStamina { get; private set; } = 100f; 
    [Tooltip("Estamina actual del personaje. Se inicializa a MaxStamina.")]
    [field: SerializeField] public float CurrentStamina { get; private set; } 
    
    [Tooltip("Cantidad de estamina regenerada por segundo.")]
    [SerializeField] private float staminaRegenRate = 15f; 
    [Tooltip("Tiempo en segundos a esperar después de usar estamina antes de que comience la regeneración.")]
    [SerializeField] private float staminaRegenDelay = 1.5f; 
    [Tooltip("Costo de estamina para cada ataque.")]
    [SerializeField] private float attackStaminaCost = 10f; 
    [Tooltip("Daño base que inflige este personaje con sus ataques.")]
    [SerializeField] private float baseDamage = 10f; 


    // --- CONFIGURACIÓN DE COMBATE ---
    [Header("Configuración de Combate")]
    [Tooltip("Lista de ataques normales disponibles para el personaje.")]
    [SerializeField] List<AttackData> attacks; 
    [Tooltip("Lista de ataques a larga distancia (si aplica).")]
    [SerializeField] List<AttackData> longRangeAttacks; 
    [Tooltip("Umbral de distancia para considerar el uso de un ataque a larga distancia.")]
    [SerializeField] float longRangeAttackThreshold = 1.5f; 
    [Tooltip("Referencia al GameObject del arma (si el personaje usa una).")]
    [SerializeField] GameObject weapon; 
    [Tooltip("Velocidad a la que el personaje rota hacia su objetivo durante un ataque.")]
    [SerializeField] float rotationSpeed = 500f; 
    
    [Header("Nombres de Animaciones (Exactos)")]
    [Tooltip("Nombre del estado de animación para la reacción al golpe en el Animator.")]
    [SerializeField] string hitReactionAnimName = "Impact"; 
    [Tooltip("Nombre del estado de animación para la muerte en el Animator.")]
    [SerializeField] string deathAnimName = "DeathBackward01";

    // --- ESTADOS Y EVENTOS INTERNOS ---
    public bool IsTakingHit { get; private set; } = false; 
    public bool IsInvulnerable { get; private set; } = false;

    public event Action<MeeleFighter> OnGotHit; 
    public event Action OnHitComplete; 
    public event Action<float, float> OnStaminaChanged;
    public event Action<float, float> OnHealthChanged;

    BoxCollider weaponCollider; 
    SphereCollider leftHandCollider, rightHandCollider, leftFootCollider, rightFootCollider; 
    
    Animator animator;
    CharacterController characterController; 
    
    Coroutine _playHitReactionCoroutine; 
    private float lastStaminaUseTime;
    
    private const int ACTION_ANIMATOR_LAYER = 1; 
    private const int BASE_ANIMATOR_LAYER = 0;

    public AttackStates AttackStates { get; private set; }
    bool doCombo; 
    int comboCount = 0; 
    public bool InAction { get; private set; } = false; 
    public bool InCounter { get; set; } = false;


    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>(); 
        
        if (maxHealth <= 0) maxHealth = 100f; // Asegurar un valor por defecto positivo para maxHealth
        Health = maxHealth; 
        CurrentStamina = MaxStamina; 
    }

    private void Start()
    {
        // Es crucial que 'animator' se haya obtenido en Awake antes de llamar a InitializeColliders.
        if (animator == null)
        {
            // Log de error eliminado según solicitud, pero esto sería un punto crítico.
            // Considera añadir un return aquí o desactivar el componente si el animator es esencial.
        }
        InitializeColliders();
        
        OnHealthChanged?.Invoke(Health, maxHealth);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }
    
    /// <summary>
    /// Inicializa los colliders de las extremidades y el arma.
    /// </summary>
    void InitializeColliders()
    {
        if (weapon != null)
        {
            weaponCollider = weapon.GetComponent<BoxCollider>();
        }

        // Solo intentar obtener colliders de huesos si el animator existe y es humanoide.
        if (animator != null && animator.isHuman)
        {
                Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                if (leftFoot != null) leftFootCollider = leftFoot.GetComponent<SphereCollider>();

                Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if (rightFoot != null) rightFootCollider = rightFoot.GetComponent<SphereCollider>();
                
                Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                if (leftHand != null) leftHandCollider = leftHand.GetComponent<SphereCollider>();

                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (rightHand != null) rightHandCollider = rightHand.GetComponent<SphereCollider>();

        }
        // else if (animator == null) { /* Animator es null */ }
        // else { /* Animator no es humanoide */ }
        
        DisableHitboxes(); // Desactivar todos al inicio.
    }
    
    private void Update()
    {
        if (CurrentStamina < MaxStamina && 
            Time.time > lastStaminaUseTime + staminaRegenDelay && 
            Health > 0 &&            
            !InAction &&             
            !IsTakingHit &&          
            !IsInvulnerable)         
        {
            CurrentStamina += staminaRegenRate * Time.deltaTime; 
            CurrentStamina = Mathf.Clamp(CurrentStamina, 0, MaxStamina); 
            OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina); 
        }
    }

    public void SetInvulnerable(bool state)
    {
        IsInvulnerable = state;
    }

    public bool ConsumeStamina(float amount) 
    {
        if (CurrentStamina >= amount)
        {
            CurrentStamina -= amount;
            lastStaminaUseTime = Time.time; 
            OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
            return true;
        }
        return false; 
    }

    public void TryToAttack(MeeleFighter target = null)
    {
        if (Health <= 0 || IsInvulnerable) return; 
        
        if (InAction)
        {
            if (AttackStates == AttackStates.Impact || AttackStates == AttackStates.Cooldown)
            {
                doCombo = true; 
            }
        }
        else 
        {
            if (ConsumeStamina(attackStaminaCost)) 
            {
                comboCount = 0; 
                StartCoroutine(Attack(target)); 
            }
        }
    }

    IEnumerator Attack(MeeleFighter target = null)
    {
        InAction = true; 
        AttackStates = AttackStates.Windup; 

        var attack = attacks[comboCount]; 
        var attackDir = transform.forward; 
        Vector3 startPos = transform.position; 
        Vector3 targetPos = Vector3.zero; 

        if (target != null)
        {
            var vecToTarget = target.transform.position - transform.position; 
            attackDir = vecToTarget.normalized; 
            float distance = vecToTarget.magnitude - attack.DistanceFromTarget; 
            if (distance > longRangeAttackThreshold && longRangeAttacks != null && longRangeAttacks.Count > 0)
            {
                attack = longRangeAttacks[Mathf.Min(comboCount, longRangeAttacks.Count -1)];
            }
            if (attack.MoveToTarget) 
            {
                if (distance <= attack.MaxMoveDistance) 
                    targetPos = target.transform.position - attackDir * attack.DistanceFromTarget; 
                else
                    targetPos = startPos + attackDir * attack.MaxMoveDistance;          
            }
        }
        
        if (animator != null) animator.CrossFade(attack.AnimName, 0.2f, ACTION_ANIMATOR_LAYER); 
        yield return null; 

        AnimatorStateInfo animState = animator != null ? animator.GetNextAnimatorStateInfo(ACTION_ANIMATOR_LAYER) : new AnimatorStateInfo(); 

        float timer = 0f; 
        float animationLength = animState.length > 0.01f ? animState.length : 1f;

        while (timer <= animationLength)
        {
            if (IsTakingHit || Health <= 0 || IsInvulnerable) { 
                InAction = false; 
                AttackStates = AttackStates.Idle; 
                comboCount = 0; 
                doCombo = false; 
                DisableHitboxes(); 
                yield break; 
            }
            timer += Time.deltaTime; 
            float normalizedTime = Mathf.Clamp01(timer / animationLength); 

            if (target != null && attack.MoveToTarget && targetPos != Vector3.zero) 
            {
                if (attack.MoveEndTime > attack.MoveStartTime) 
                {
                    float percTime = (normalizedTime - attack.MoveStartTime) / (attack.MoveEndTime - attack.MoveStartTime); 
                    transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Clamp01(percTime)); 
                }
            }

            if (attackDir != Vector3.zero)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(attackDir), rotationSpeed * Time.deltaTime); 
            }

            if (AttackStates == AttackStates.Windup) 
            {
                if (InCounter) break; 
                if (normalizedTime >= attack.ImpactStartTime) 
                { AttackStates = AttackStates.Impact; EnableHitBox(attack); } 
            }
            else if (AttackStates == AttackStates.Impact) 
            {
                if (normalizedTime >= attack.ImpactEndTime) 
                { AttackStates = AttackStates.Cooldown; DisableHitboxes(); } 
            }
            else if (AttackStates == AttackStates.Cooldown) 
            {
                if (doCombo)
                {
                    doCombo = false; 
                    comboCount = (comboCount + 1) % attacks.Count; 
                    if (ConsumeStamina(attackStaminaCost)) 
                    { 
                        StartCoroutine(Attack()); 
                        yield break; 
                    } 
                    else 
                    { 
                        break; 
                    }
                }
            }
            yield return null; 
        }

        AttackStates = AttackStates.Idle; 
        comboCount = 0; 
        InAction = false; 
        doCombo = false; 
        DisableHitboxes();
    }
    
    public void RequestCombo()
    {
        if (InAction && (AttackStates == AttackStates.Impact || AttackStates == AttackStates.Cooldown))
        {
            doCombo = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsInvulnerable) return;
        if (Health <= 0 || InCounter) return; 
        
        if (other.CompareTag("Hitbox")) 
        {
            MeeleFighter attacker = other.GetComponentInParent<MeeleFighter>(); 
            if (attacker != null && attacker != this) 
            {
                TakeDamage(attacker.baseDamage, attacker); 
                OnGotHit?.Invoke(attacker); 
                
                if (Health > 0) 
                {
                    if (_playHitReactionCoroutine != null)
                    {
                        StopCoroutine(_playHitReactionCoroutine);
                    }
                    _playHitReactionCoroutine = StartCoroutine(PlayHitReaction(attacker)); 
                }
            }
        }
    }

    public void TakeDamage(float damage, MeeleFighter attacker = null) 
    {
        if (IsInvulnerable) return; 
        if (Health <= 0) return; 

        Health = Mathf.Clamp(Health - damage, 0, maxHealth); 
        OnHealthChanged?.Invoke(Health, maxHealth);

        if (Health <= 0)
        {
            Die(attacker);
        }
    }

    public UnityEvent onDieEvent=new UnityEvent();

    public void Die(MeeleFighter attacker = null)
    {
        PlayDeathAnimation(attacker);
        onDieEvent?.Invoke();
    }

    IEnumerator PlayHitReaction(MeeleFighter attacker)
    {
        InAction = true;  
        IsTakingHit = true; 

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true; 
        }

        if (attacker != null) 
        {
            var dispVec = attacker.transform.position - transform.position; 
            dispVec.y = 0; 
            if (dispVec != Vector3.zero) 
            {
                 transform.rotation = Quaternion.LookRotation(dispVec); 
            }
        }
        
        if(animator != null) animator.CrossFade(hitReactionAnimName, 0.2f); 
        yield return null; 

        AnimatorStateInfo animStateLayer1 = animator != null ? animator.GetNextAnimatorStateInfo(ACTION_ANIMATOR_LAYER) : new AnimatorStateInfo(); 
        
        float timeToWait = 0.3f; 

        if (animStateLayer1.length > 0.01f) 
        {
            timeToWait = animStateLayer1.length * 0.8f; 
        }
        
        yield return new WaitForSeconds(timeToWait); 
        
        OnHitComplete?.Invoke(); 

        InAction = false; 
        IsTakingHit = false; 

        if (agent != null && agent.enabled)
        {
            agent.isStopped = false; 
        }
        _playHitReactionCoroutine = null; 
    }

    void PlayDeathAnimation(MeeleFighter attacker) 
    {
        if (animator == null) return; // Comprobación de nulidad para animator

        if (animator.GetCurrentAnimatorStateInfo(BASE_ANIMATOR_LAYER).IsName(deathAnimName) || 
            animator.GetNextAnimatorStateInfo(BASE_ANIMATOR_LAYER).IsName(deathAnimName)) { return; }

        InAction = true; IsTakingHit = false; 
        if (characterController != null) { characterController.enabled = false; }
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) { agent.enabled = false; }

        if (attacker != null)
        {
            var dispVec = attacker.transform.position - transform.position;
            dispVec.y = 0;
            if (dispVec != Vector3.zero) { transform.rotation = Quaternion.LookRotation(dispVec); }
        }
        animator.CrossFade(deathAnimName, 0.2f); 
    }
        
    public IEnumerator PerformCounterAttack(EnemyController opponent)
    {
        InAction = true; 
        InCounter = true; 
        if (opponent != null && opponent.Fighter != null) opponent.Fighter.InCounter = true; 
        if (opponent != null) opponent.ChangeState(EnemyStates.Dead); 

        var dispVec = opponent.transform.position - transform.position; 
        dispVec.y = 0; 
        if (dispVec != Vector3.zero) 
        {
            transform.rotation = Quaternion.LookRotation(dispVec); 
            if (opponent != null) opponent.transform.rotation = Quaternion.LookRotation(-dispVec); 
        }

        var targetPos = opponent.transform.position - dispVec.normalized * 1f; 
        
        if(animator != null) animator.CrossFade("CounterAttackA", 0.2f); 
        if(opponent != null && opponent.Animator != null) opponent.Animator.CrossFade("CounterAttackVictimA", 0.2f); 
        yield return null; 

        AnimatorStateInfo animState = animator != null ? animator.GetNextAnimatorStateInfo(ACTION_ANIMATOR_LAYER) : new AnimatorStateInfo();

        float timer = 0f; 
        float animationLength = animState.length > 0.01f ? animState.length : 1f;

        while (timer <= animationLength) 
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, 5 * Time.deltaTime); 
            yield return null; 
            timer += Time.deltaTime; 
        }

        InCounter = false; 
        if (opponent != null && opponent.Fighter != null) opponent.Fighter.InCounter = false; 
        InAction = false; 
    }

    void EnableHitBox(AttackData attack) 
    {
        if (attack == null) return; // Comprobación de nulidad para attack
        switch (attack.HitboxToUse)
        {
            case AttackHitbox.LeftHand: 
                if(leftHandCollider) leftHandCollider.enabled = true; 
                break;
            case AttackHitbox.RightHand:
                if(rightHandCollider) rightHandCollider.enabled = true; 
                break;
            case AttackHitbox.LeftFoot:
                if(leftFootCollider) leftFootCollider.enabled = true; 
                break;
            case AttackHitbox.RightFoot:
                if(rightFootCollider) rightFootCollider.enabled = true; 
                break;
            case AttackHitbox.Axe:
            case AttackHitbox.Sword: 
                if(weaponCollider) weaponCollider.enabled = true; 
                break;
        }
    }

    void DisableHitboxes()
    {
        if (weaponCollider != null && weaponCollider.enabled) weaponCollider.enabled = false; 
        if (leftFootCollider != null && leftFootCollider.enabled) leftFootCollider.enabled = false; 
        if (rightFootCollider != null && rightFootCollider.enabled) rightFootCollider.enabled = false; 
        if (leftHandCollider != null && leftHandCollider.enabled) leftHandCollider.enabled = false;  
        if (rightHandCollider != null && rightHandCollider.enabled) rightHandCollider.enabled = false; 
    }

    public List<AttackData> Attacks => attacks; 
    public bool IsCounterable  => AttackStates == AttackStates.Windup && comboCount == 0 && Health > 0;
}
