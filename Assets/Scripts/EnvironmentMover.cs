using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MoverStep
{
    public List<GameObject> objectsToMove = new List<GameObject>();
    public List<Transform> targetPositions = new List<Transform>();
    public float stepDuration = 2.0f;
    public float delayBeforeStep = 0.5f;
    public bool waitForCompletion = true;
}

public class EnvironmentMover : MonoBehaviour
{
    [Header("Configuration de la scène")]
    [SerializeField] private string targetSceneName = "";
    [SerializeField] public float delayBeforeMovement = 0.5f;

    [Header("Configuration de la séquence")]
    [SerializeField] private List<MoverStep> movementSteps = new List<MoverStep>();

    [Header("Rotation")]
    public float vitesseRotation = 5f;

    private bool hasExecuted = false;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterEnvironmentMover(this);
        }
        else
        {
            Debug.LogError("GameManager non trouvé. Le EnvironmentMover ne fonctionnera pas correctement.");
        }
    }

    public bool ShouldExecuteForScene(string sceneName)
    {
        return !hasExecuted && sceneName == targetSceneName;
    }

    public IEnumerator ExecuteMovement()
    {
        if (hasExecuted) yield break;

        yield return new WaitForSeconds(delayBeforeMovement);
        yield return StartCoroutine(ExecuteSequence());

        hasExecuted = true;
        Debug.Log("Mouvements d'environnement terminés");
    }

    private IEnumerator ExecuteSequence()
    {
        Debug.Log($"Exécution d'une séquence de {movementSteps.Count} étapes");

        foreach (var step in movementSteps)
        {
            if (step.delayBeforeStep > 0)
                yield return new WaitForSeconds(step.delayBeforeStep);

            if (step.objectsToMove.Count != step.targetPositions.Count)
            {
                Debug.LogError("Mismatch objets/positions dans une étape.");
                continue;
            }

            for (int i = 0; i < step.objectsToMove.Count; i++)
            {
                if (step.objectsToMove[i] != null && step.targetPositions[i] != null)
                {
                    StartCoroutine(MoveObject(step.objectsToMove[i], step.targetPositions[i], step.stepDuration));
                }
            }

            if (step.waitForCompletion)
                yield return new WaitForSeconds(step.stepDuration);
        }
    }

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
            float t = (Time.time - startTime) / duration;

            Vector3 direction = targetPosition - obj.transform.position;
            direction.Normalize();
            Quaternion rotationSouhaitee = Quaternion.LookRotation(direction, Vector3.up);

            obj.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            obj.transform.rotation = Quaternion.Slerp(obj.transform.rotation, rotationSouhaitee, vitesseRotation * Time.deltaTime);

            yield return null;
        }

        obj.transform.position = targetPosition;
        obj.transform.rotation = targetRotation;
        if (anim != null)
        {
            anim.SetTrigger("Idle");
        }
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        for (int s = 0; s < movementSteps.Count; s++)
        {
            var step = movementSteps[s];
            Color stepColor = new Color(0.2f + (0.8f * s / (float)movementSteps.Count), 0.8f, 0.2f);

            for (int i = 0; i < step.targetPositions.Count; i++)
            {
                var target = step.targetPositions[i];
                if (target != null)
                {
                    Gizmos.color = stepColor;
                    Gizmos.DrawSphere(target.position, 0.2f);
                    Gizmos.DrawLine(target.position, target.position + target.forward);
                    UnityEditor.Handles.Label(target.position + Vector3.up * 0.3f, $"Étape {s + 1}");
                }
            }

            // Ligne entre les étapes
            if (s < movementSteps.Count - 1)
            {
                var nextStep = movementSteps[s + 1];
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
#endif
    }
}
