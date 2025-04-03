using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        // Récupérer le composant Animator
        animator = GetComponent<Animator>();


        if (animator == null)
        {
            Debug.LogError("Pas d'Animator trouvé sur ce personnage!");
        }
        animator.SetTrigger("Idle");
    }

    // Méthode pour déclencher l'animation Walk
    public void TriggerWalkAnimation()
    {
        if (animator != null)
        {
            // Réinitialiser tous les autres triggers
            animator.ResetTrigger("Idle");
            animator.ResetTrigger("Move");

            // Déclencher l'animation Walk
            animator.SetTrigger("Walk");
        }
    }

    // Méthode pour déclencher l'animation Idle
    public void TriggerIdleAnimation()
    {
        if (animator != null)
        {
            // Réinitialiser tous les autres triggers
            animator.ResetTrigger("Walk");
            animator.ResetTrigger("Move");

            // Déclencher l'animation Run
            animator.SetTrigger("Idle");
        }
    }
}