using UnityEngine;

// Esta clase define un "estado" genérico dentro de una máquina de estados.
// La clase es genérica gracias al parámetro <T>, lo cual permite que pueda adaptarse a cualquier tipo de "dueño" o entidad (por ejemplo: un enemigo, un NPC, el jugador, etc.)
public class State<T> : MonoBehaviour
{
    // Método que se llama una vez al entrar a este estado.
    // 'owner' representa el objeto que posee esta máquina de estados (por ejemplo, el enemigo o jugador que está cambiando de estado).
    public virtual void Enter(T owner) { }

    // Método que se llama en cada frame mientras este estado esté activo.
    // Aquí iría la lógica principal del comportamiento que define este estado (por ejemplo: patrullar, atacar, huir, etc.).
    public virtual void Execute() { }

    // Método que se llama una vez al salir de este estado.
    // Aquí se puede limpiar o resetear valores antes de cambiar al siguiente estado.
    public virtual void Exit() { }
}

