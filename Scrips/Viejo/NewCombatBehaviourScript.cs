using UnityEngine;
using UnityEngine.InputSystem;

public class NewCombatBehaviourScript : MonoBehaviour
{
    private InputActions combat;
    private Animator animator;

    private void Awake()
    {
        combat = new InputActions();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        combat.Enable();
    }

    private void OnDisable()
    {
        combat.Disable();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Attack ()
    {
        Debug.Log("Attack");
    }
}
