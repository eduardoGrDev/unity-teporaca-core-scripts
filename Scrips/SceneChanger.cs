using UnityEngine;

public class SceneChanger : MonoBehaviour
{
    [SerializeField] private string sceneNameToLoad = "Narrative3"; // Nombre de la escena a cargar
    [SerializeField] private float delayInSeconds = 30f; // Tiempo de espera antes de cambiar de escena

    public SceneTransitionManager manager;

    void Start()
    {
        Invoke("ChangeScene", delayInSeconds);
    }

    void ChangeScene()
    {
        manager.LoadScene(sceneNameToLoad);
    }
}
