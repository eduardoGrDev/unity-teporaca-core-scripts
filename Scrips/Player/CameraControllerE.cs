using UnityEngine;
using UnityEngine.InputSystem; // Importar el nuevo Input System

public class CameraControllerE : MonoBehaviour
{
    [SerializeField] Transform followTarget; // Referencia al objeto que la cámara seguirá
    [SerializeField] float rotationSpeed = 2f; // Velocidad de rotación de la cámara  
    [SerializeField] float distance = 5f; // Distancia entre la cámara y el objetivo
    [SerializeField] float minVerticalAngle = -45; // Ángulo mínimo de rotación en el eje X (vertical)
    [SerializeField] float maxVerticalAngle = 45; // Ángulo máximo de rotación en el eje X
    [SerializeField] Vector2 framingOffset; // Desplazamiento de la cámara respecto al objetivo
    [SerializeField] bool invertX; // Invertir el eje horizontal
    [SerializeField] bool invertY; // Invertir el eje vertical

    private float rotationX;
    private float rotationY;
    private Vector2 lookInput; // Variable para almacenar la entrada de movimiento de la cámara

    [HideInInspector]
    protected InputActions _inputActions;
    public InputActions inputActions
    {
        get
        {
            return _inputActions;
        }
    }

    [HideInInspector]
    protected InputActions.PlayerActions _playerInput;
    public InputActions.PlayerActions playerInput
    {
        get
        {
            return _playerInput;
        }
    }

    private void Awake()
    {
        _inputActions = new InputActions();
        _playerInput = inputActions.Player;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Start()
    {
        Cursor.visible = false; // Oculta el cursor
        Cursor.lockState = CursorLockMode.Locked; // Bloquea el cursor en el centro de la pantalla


        Vector3 offset = transform.position - followTarget.position;
        Vector3 planarOffset = new Vector3(offset.x, 0, offset.z); // Ignorar altura para obtener rotación Y
        rotationY = Quaternion.LookRotation(planarOffset).eulerAngles.y;

        // Opcional: si quieres también ajustar verticalmente
        rotationX = Quaternion.LookRotation(offset).eulerAngles.x;
    }

    private void Update()
    {
        // Aplica la inversión de controles si está activada
        float invertXVal = (invertX) ? -1 : 1;
        float invertYVal = (invertY) ? -1 : 1;

        // Modifica los valores de rotación basados en la entrada del usuario

        lookInput = _playerInput.Look.ReadValue<Vector2>();
        rotationX += lookInput.y * invertYVal * rotationSpeed;
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle); // Limita la rotación vertical
        rotationY += lookInput.x * invertXVal * rotationSpeed;

        // Calcula la nueva rotación de la cámara
        Quaternion targetRotation = Quaternion.Euler(rotationX, rotationY, 0);

        // Calcula la posición de la cámara en función del objetivo y la distancia
        Vector3 focusPosition = followTarget.position + new Vector3(framingOffset.x, framingOffset.y);

        // Aplica la nueva posición y rotación
        transform.position = focusPosition - targetRotation * new Vector3(0, 0, distance);
        transform.rotation = targetRotation;
    }

    public Quaternion PlanarRotation => Quaternion.Euler(0, rotationY, 0);
}
