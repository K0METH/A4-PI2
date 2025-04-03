using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

[System.Serializable]
public class MoverStep
{
    public List<GameObject> objectsToMove = new List<GameObject>();
    public List<Transform> targetPositions = new List<Transform>();
    public float stepDuration = 2.0f;
    public float delayBeforeStep = 0.5f;
    public bool waitForCompletion = true;

    // Liste des triggers d'animation pour chaque objet
    [SerializeField]
    public List<string> animationTriggers = new List<string>();
}

public class EnvironmentMover : MonoBehaviour
{
    [Header("Configuration du mouvement")]
    [SerializeField] private List<GameObject> objectsToMove = new List<GameObject>();
    [SerializeField] private List<Transform> targetPositions = new List<Transform>();
    [SerializeField] public float movementDuration = 2.0f;
    [SerializeField] public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Configuration de la scène")]
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private bool moveBeforeSceneStarts = true;
    [SerializeField] public float delayBeforeMovement = 0.5f;

    [Header("Configuration de séquence (optionnel)")]
    [SerializeField] private bool useSequence = false;
    [SerializeField] private List<MoverStep> movementSteps = new List<MoverStep>();

    // Stocke les positions originales des objets
    private List<Vector3> originalPositions = new List<Vector3>();
    private List<Quaternion> originalRotations = new List<Quaternion>();
    private bool hasExecuted = false;

    private void Awake()
    {
        StoreOriginalPositions();
    }

    private void Start()
    {
        // S'enregistrer auprès du GameManager pour être notifié avant le démarrage d'une scène
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterEnvironmentMover(this);
        }
        else
        {
            Debug.LogError("GameManager non trouvé. Le EnvironmentMover ne fonctionnera pas correctement.");
        }
    }

    // Méthode pour configurer les objets à déplacer
    public void SetObjectsToMove(List<GameObject> objects)
    {
        objectsToMove = new List<GameObject>(objects);
        StoreOriginalPositions();
    }

    // Méthode pour configurer les positions cibles
    public void SetTargetPositions(List<Transform> positions)
    {
        targetPositions = new List<Transform>(positions);
    }

    // Méthode pour configurer la scène cible
    public void SetTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
    }

    // Stocker les positions et rotations originales
    private void StoreOriginalPositions()
    {
        originalPositions.Clear();
        originalRotations.Clear();

        if (objectsToMove.Count > 0)
        {
            foreach (GameObject obj in objectsToMove)
            {
                if (obj != null)
                {
                    originalPositions.Add(obj.transform.position);
                    originalRotations.Add(obj.transform.rotation);
                }
                else
                {
                    // Ajouter des valeurs par défaut pour maintenir les indices synchronisés
                    originalPositions.Add(Vector3.zero);
                    originalRotations.Add(Quaternion.identity);
                    Debug.LogWarning("Un objet null a été trouvé dans la liste des objets à déplacer");
                }
            }
        }
    }

    // Méthode appelée par le GameManager pour vérifier si ce mover doit être activé
    public bool ShouldExecuteForScene(string sceneName)
    {
        return !hasExecuted && sceneName == targetSceneName;
    }

    // Méthode pour exécuter le mouvement des objets
    public IEnumerator ExecuteMovement()
    {
        if (hasExecuted)
        {
            yield break;
        }

        Debug.Log($"Exécution des mouvements d'environnement");

        // Attendre le délai configuré
        yield return new WaitForSeconds(delayBeforeMovement);

        // Choisir entre mode séquence ou mode simple
        if (useSequence && movementSteps.Count > 0)
        {
            yield return StartCoroutine(ExecuteSequence());
        }
        else
        {
            // Vérifier que les listes ont la même taille
            if (objectsToMove.Count != targetPositions.Count)
            {
                Debug.LogError("Les listes d'objets et de positions cibles doivent avoir la même taille!");
                yield break;
            }

            // Déplacer chaque objet vers sa position cible
            for (int i = 0; i < objectsToMove.Count; i++)
            {
                if (objectsToMove[i] != null && targetPositions[i] != null)
                {
                    StartCoroutine(MoveObject(objectsToMove[i], targetPositions[i], movementDuration));
                }
            }

            // Attendre que tous les mouvements soient terminés
            yield return new WaitForSeconds(movementDuration);
        }

        hasExecuted = true;
        Debug.Log("Mouvements d'environnement terminés");
    }

    // Exécute une séquence de mouvements
    private IEnumerator ExecuteSequence()
    {
        Debug.Log($"Exécution d'une séquence de {movementSteps.Count} étapes");

        for (int stepIndex = 0; stepIndex < movementSteps.Count; stepIndex++)
        {
            MoverStep step = movementSteps[stepIndex];

            // Attendre le délai avant l'étape
            if (step.delayBeforeStep > 0)
            {
                yield return new WaitForSeconds(step.delayBeforeStep);
            }

            Debug.Log($"Exécution de l'étape {stepIndex + 1}");

            // Vérifier que les listes ont la même taille
            if (step.objectsToMove.Count != step.targetPositions.Count)
            {
                Debug.LogError($"Les listes d'objets et de positions cibles pour l'étape {stepIndex + 1} doivent avoir la même taille!");
                continue;
            }

            // Déclencher les animations avant le mouvement
            for (int i = 0; i < step.objectsToMove.Count; i++)
            {
                GameObject obj = step.objectsToMove[i];

                // Vérifier si un trigger d'animation existe pour cet objet
                if (i < step.animationTriggers.Count && !string.IsNullOrEmpty(step.animationTriggers[i]))
                {
                    Animator animator = obj.GetComponent<Animator>();
                    if (animator != null)
                    {
                        Debug.Log($"Déclenchement de l'animation {step.animationTriggers[i]} pour {obj.name}");
                        animator.SetTrigger(step.animationTriggers[i]);
                    }
                    else
                    {
                        Debug.LogWarning($"Pas d'Animator trouvé sur {obj.name}");
                    }
                }
            }

            // Déplacer chaque objet vers sa position cible
            List<Coroutine> stepCoroutines = new List<Coroutine>();

            for (int i = 0; i < step.objectsToMove.Count; i++)
            {
                if (step.objectsToMove[i] != null && step.targetPositions[i] != null)
                {
                    Coroutine moveCoroutine = StartCoroutine(MoveObject(step.objectsToMove[i], step.targetPositions[i], step.stepDuration));
                    stepCoroutines.Add(moveCoroutine);
                }
            }

            // Si on attend la fin de l'étape avant de passer à la suivante
            if (step.waitForCompletion)
            {
                yield return new WaitForSeconds(step.stepDuration);
            }
        }
    }

    // Coroutine pour déplacer un objet avec interpolation
    private IEnumerator MoveObject(GameObject obj, Transform target, float duration)
    {
        Vector3 startPosition = obj.transform.position;
        Quaternion startRotation = obj.transform.rotation;
        Vector3 targetPosition = target.position;
        Quaternion targetRotation = target.rotation;
        float startTime = Time.time;
        float endTime = startTime + duration;

        Animator anim = obj.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Walk");
        }

        while (Time.time < endTime)
        {
            float normalizedTime = (Time.time - startTime) / duration;
            float curveValue = movementCurve.Evaluate(normalizedTime);

            // Calcul de la direction vers la position cible
            Vector3 direction = targetPosition - obj.transform.position;

            // Normalisation de la direction
            direction.Normalize();

            // Calcul de la rotation souhaitée
            Quaternion rotationSouhaitee = Quaternion.LookRotation(direction, Vector3.up);

            // Mise à jour de la position et rotation
            obj.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
            obj.transform.rotation = Quaternion.Slerp(obj.transform.rotation, rotationSouhaitee, vitesseRotation * Time.deltaTime);

            yield return null;
        }

        // S'assurer que l'objet est exactement à la position finale
        obj.transform.position = targetPosition;
        obj.transform.rotation = targetRotation;
    }

    // Vous devez définir la vitesse de rotation quelque part dans votre script
    public float vitesseRotation = 5f;


    // Méthode pour réinitialiser les objets à leur position d'origine
    public void ResetObjects()
    {
        for (int i = 0; i < objectsToMove.Count; i++)
        {
            if (objectsToMove[i] != null && i < originalPositions.Count)
            {
                objectsToMove[i].transform.position = originalPositions[i];
                objectsToMove[i].transform.rotation = originalRotations[i];
            }
        }
        hasExecuted = false;
    }

    // Pour déboguer les positions cibles en mode éditeur
    private void OnDrawGizmos()
    {
        if (!useSequence)
        {
            // Mode standard - visualiser les positions cibles directes
            foreach (Transform target in targetPositions)
            {
                if (target != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(target.position, 0.2f);
                    Gizmos.DrawLine(target.position, target.position + target.forward);
                }
            }
        }
        else
        {
            // Mode séquence - visualiser les différentes étapes
            for (int s = 0; s < movementSteps.Count; s++)
            {
                MoverStep step = movementSteps[s];

                // Couleur différente pour chaque étape
                Color stepColor = new Color(
                    0.2f + (0.8f * s / (float)movementSteps.Count),
                    0.8f,
                    0.2f
                );

                foreach (Transform target in step.targetPositions)
                {
                    if (target != null)
                    {
                        Gizmos.color = stepColor;
                        Gizmos.DrawSphere(target.position, 0.2f);
                        Gizmos.DrawLine(target.position, target.position + target.forward);

                        // Afficher le numéro de l'étape
#if UNITY_EDITOR
                        UnityEditor.Handles.Label(target.position + Vector3.up * 0.3f, $"Étape {s + 1}");
#endif
                    }
                }

                // Connecter les positions entre les étapes
                if (s < movementSteps.Count - 1)
                {
                    MoverStep nextStep = movementSteps[s + 1];
                    for (int i = 0; i < Mathf.Min(step.targetPositions.Count, nextStep.targetPositions.Count); i++)
                    {
                        if (step.targetPositions[i] != null && nextStep.targetPositions[i] != null)
                        {
                            Gizmos.color = stepColor;
                            Gizmos.DrawLine(step.targetPositions[i].position, nextStep.targetPositions[i].position);
                        }
                    }
                }
            }
        }
    }
}
