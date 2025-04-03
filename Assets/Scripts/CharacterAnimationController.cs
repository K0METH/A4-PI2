using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        // R�cup�rer le composant Animator
        animator = GetComponent<Animator>();


        if (animator == null)
        {
            Debug.LogError("Pas d'Animator trouv� sur ce personnage!");
        }
        animator.SetTrigger("Idle");
    }

    // M�thode pour d�clencher l'animation Walk
    public void TriggerWalkAnimation()
    {
        if (animator != null)
        {
            // R�initialiser tous les autres triggers
            animator.ResetTrigger("Idle");
            animator.ResetTrigger("Move");

            // D�clencher l'animation Walk
            animator.SetTrigger("Walk");
        }
    }

    // M�thode pour d�clencher l'animation Idle
    public void TriggerIdleAnimation()
    {
        if (animator != null)
        {
            // R�initialiser tous les autres triggers
            animator.ResetTrigger("Walk");
            animator.ResetTrigger("Move");

            // D�clencher l'animation Run
            animator.SetTrigger("Idle");
        }
    }
}