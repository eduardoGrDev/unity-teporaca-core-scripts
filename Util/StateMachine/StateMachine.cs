// Este script define un ScriptableObject que almacena datos sobre ataques en un sistema de combate.
// Este script es parte de un sistema de combate en Unity y se utiliza para definir los datos de un ataque.

// Esta clase representa una Máquina de Estados Finita (FSM) genérica.
// Está diseñada para controlar el comportamiento de un objeto (por ejemplo, un NPC, un enemigo o cualquier entidad).
// Es genérica gracias a <T>, lo cual permite que se pueda usar con cualquier tipo de "dueño".
public class StateMachine<T>
{
    // Propiedad pública de solo lectura que indica el estado actual en el que se encuentra la máquina.
    public State<T> CurrentState { get; private set; }

    // Referencia al "dueño" de la máquina de estados (puede ser un personaje, enemigo, etc.).
    T _ownwer;

    // Constructor que recibe el dueño de esta máquina de estados.
    // Se guarda para que pueda pasarse a los estados cuando se active uno.
    public StateMachine(T owner)
    {
        _ownwer = owner;
    }

    // Cambia el estado actual de la máquina por uno nuevo.
    // Si hay un estado activo, se llama a su método Exit() antes de cambiar.
    // Luego se cambia al nuevo estado y se llama a su método Enter(), pasándole el dueño como parámetro.
    public void ChangeState(State<T> newState)
    {
        // Salir del estado actual (si existe)
        CurrentState?.Exit();

        // Cambiar al nuevo estado
        CurrentState = newState;

        // Entrar al nuevo estado
        CurrentState.Enter(_ownwer);
    }

    // Ejecuta el estado actual, si existe.
    // Este método debe llamarse, por ejemplo, en el Update() del MonoBehaviour que maneja la lógica.
    public void Execute()
    {
        CurrentState?.Execute();
    }
}
