using UnityEngine;

// Este atributo permite crear una nueva instancia de este ScriptableObject desde el menú de Unity.
// Se crea un nuevo menú en: Assets > Create > Combat System > Create a new attack
[CreateAssetMenu(menuName = "Combat System/Create a new attack")]
public class AttackData : ScriptableObject
{
    // Este campo representa el nombre de la animación del ataque.
    // Por ejemplo, puede coincidir con el nombre de una animación en el Animator.
    [field: SerializeField] 
    public string AnimName { get; private set; }

    // Este campo indica qué parte del cuerpo (o arma) se usará como "hitbox" para detectar el impacto.
    // Usa un enumerador llamado AttackHitbox definido más abajo.
    [field: SerializeField] 
    public AttackHitbox HitboxToUse { get; private set; }

    // Tiempo (en segundos) desde que inicia la animación hasta que el impacto del ataque comienza a ser efectivo.
    // Se usa para sincronizar la lógica del daño con la animación.
    [field: SerializeField] 
    public float ImpactStartTime { get; private set; }

    // Tiempo (en segundos) hasta el cual el impacto sigue siendo válido.
    // Después de este tiempo, el ataque ya no tiene efecto aunque la animación continúe.
    [field: SerializeField] 
    public float ImpactEndTime { get; private set; } 

    [field: Header("Movimiento al objetivo")] 

    [field: SerializeField]
    public bool MoveToTarget { get; private set; } // Indica si el personaje se moverá hacia el objetivo al atacar

    [field: SerializeField]
    public float DistanceFromTarget { get; private set; } = 1f;// Distancia a la que el personaje se detendrá al atacar

    [field: SerializeField]
    public float MaxMoveDistance { get; private set; } = 3f; // Distancia máxima que el personaje puede moverse al atacar

    [field: SerializeField]
    public float MoveStartTime { get; private set; } = 0f; // Tiempo que tarda en comenzar a moverse hacia el objetivo

    [field: SerializeField]
    public float MoveEndTime { get; private set; } = 1f; // Tiempo que tarda en llegar al objetivo
}

// Enumerador que define las posibles zonas de impacto o armas que se pueden usar en un ataque.
// Esto ayuda a identificar qué collider o parte del cuerpo debe activarse para detectar colisiones.
public enum AttackHitbox 
{
    LeftHand,   // Mano izquierda
    RightHand,  // Mano derecha
    LeftFoot,   // Pie izquierdo
    RightFoot,  // Pie derecho
    Axe,        // Hacha
    Arrow,      // Flecha
    Sword,      // Espada 
    Shield      // Escudo

}
