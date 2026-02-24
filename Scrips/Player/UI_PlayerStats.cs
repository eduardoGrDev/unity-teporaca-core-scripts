using UnityEngine;
using UnityEngine.UI; // Necesario para trabajar con elementos UI como Slider e Image

public class UI_PlayerStats : MonoBehaviour
{
    [Header("Player References")]
    [Tooltip("Arrastra aquí el GameObject del Jugador que tiene el script MeeleFighter.")]
    public MeeleFighter playerFighter; // Asigna esto en el Inspector

    [Header("Health UI")]
    [Tooltip("Arrastra aquí el Slider de la barra de vida del jugador.")]
    public Slider healthSlider; 
    // public Image healthImage; // Alternativa: Descomenta y usa esto si prefieres una Imagen con Fill Amount

    [Header("Stamina UI")]
    [Tooltip("Arrastra aquí el Slider de la barra de estamina del jugador.")]
    public Slider staminaSlider; 
    // public Image staminaImage; // Alternativa: Descomenta y usa esto si prefieres una Imagen con Fill Amount


    void Start()
    {
        if (playerFighter == null)
        {
            Debug.LogError("UI_PlayerStats: No se ha asignado el PlayerFighter en el Inspector. Intentando encontrarlo automáticamente...");
            PlayerControllerE playerInstance = PlayerControllerE.Instance; // Asume que PlayerControllerE es un Singleton
            if (playerInstance != null)
            {
                playerFighter = playerInstance.GetComponent<MeeleFighter>();
            }
            
            if (playerFighter == null)
            {
                Debug.LogError("UI_PlayerStats: PlayerFighter no encontrado. El script se desactivará.");
                enabled = false; // Desactiva el script si no hay jugador
                return;
            }
        }

        // Suscribirse a los eventos del jugador
        if (healthSlider != null) // Solo suscribirse si hay UI de vida asignada
        {
            playerFighter.OnHealthChanged += UpdateHealthUI;
        }
        else
        {
            Debug.LogWarning("UI_PlayerStats: HealthSlider no asignado. La barra de vida no se actualizará.");
        }

        if (staminaSlider != null) // Solo suscribirse si hay UI de estamina asignada
        {
            playerFighter.OnStaminaChanged += UpdateStaminaUI;
        }
        else
        {
            Debug.LogWarning("UI_PlayerStats: StaminaSlider no asignado. La barra de estamina no se actualizará.");
        }

        // Actualizar las barras de UI con los valores iniciales
        InitializeHealthUI();
        InitializeStaminaUI();
    }

    void InitializeHealthUI()
    {
        if (healthSlider == null || playerFighter == null) return;

        if (playerFighter.maxHealth > 0) 
        {
            UpdateHealthUI(playerFighter.Health, playerFighter.maxHealth);
        }
        else
        {
            Debug.LogWarning("UI_PlayerStats: playerFighter.maxHealth es 0 o negativo. La barra de vida podría no mostrarse correctamente.");
            UpdateHealthUI(0, 100); // Ejemplo: barra vacía con un máximo de 100 por defecto
        }
    }

    void InitializeStaminaUI()
    {
        if (staminaSlider == null || playerFighter == null) return;
        
        if (playerFighter.MaxStamina > 0)
        {
             UpdateStaminaUI(playerFighter.CurrentStamina, playerFighter.MaxStamina);
        }
        else
        {
            Debug.LogWarning("UI_PlayerStats: playerFighter.MaxStamina es 0 o negativo. La barra de estamina podría no mostrarse correctamente.");
            UpdateStaminaUI(0,100); // Ejemplo: barra vacía con un máximo de 100 por defecto
        }
    }

    void OnDestroy()
    {
        // Es MUY importante desuscribirse de los eventos cuando el objeto se destruye
        if (playerFighter != null)
        {
            if (healthSlider != null)
            {
                playerFighter.OnHealthChanged -= UpdateHealthUI;
            }
            if (staminaSlider != null)
            {
                playerFighter.OnStaminaChanged -= UpdateStaminaUI;
            }
        }
    }

    void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthSlider == null) return; 

        if (maxHealth <= 0) 
        {
            healthSlider.gameObject.SetActive(false); 
            return;
        }
        
        healthSlider.gameObject.SetActive(true); 
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
        
        /* // Alternativa para Image:
        if (healthImage != null)
        {
            if (maxHealth <= 0) { healthImage.fillAmount = 0; return; }
            healthImage.gameObject.SetActive(true);
            healthImage.fillAmount = currentHealth / maxHealth;
        }
        */
    }

    void UpdateStaminaUI(float currentStamina, float maxStamina)
    {
        if (staminaSlider == null) return;

        if (maxStamina <= 0)
        {
            staminaSlider.gameObject.SetActive(false);
            return;
        }

        staminaSlider.gameObject.SetActive(true); 
        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = currentStamina;

        /* // Alternativa para Image:
        if (staminaImage != null)
        {
            if (maxStamina <= 0) { staminaImage.fillAmount = 0; return; }
            staminaImage.gameObject.SetActive(true);
            staminaImage.fillAmount = currentStamina / maxStamina;
        }
        */
    }
}
