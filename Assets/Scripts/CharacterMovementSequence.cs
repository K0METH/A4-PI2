using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ScenePositionConfig
{
    public string sceneName;         // Nom de la scène (ex: "Scene1")
    public string zoneAction;        // Nom de l'action de zone (ex: "ParlerMatthieu")
    public Transform[] characterPositions; // Tableau des positions cibles pour les personnages
}
[System.Serializable]

public class MovementStep
{
    public List<Transform> characterPositions = new List<Transform>();
    public float stepDuration = 2.0f;
    public float delayBeforeStep = 0.5f;
    [Tooltip("Si vrai, attend que les personnages atteignent leurs positions avant de passer à l'étape suivante")]
    public bool waitForCompletion = true;
}

public class CharacterMovementSequence : MonoBehaviour
{
    [SerializeField] private string sequenceName = "Sequence";
    [SerializeField] private string associatedSceneName = "";
    [SerializeField] private string associatedZoneAction = "";
    [SerializeField] private List<MovementStep> movementSteps = new List<MovementStep>();

    [Header("Visualisation")]
    [SerializeField] private Color gizmoColor = Color.magenta;
    [SerializeField] private bool drawConnections = true;

    // Propriétés publiques
    public string SequenceName => sequenceName;
    public string AssociatedSceneName => associatedSceneName;
    public string AssociatedZoneAction => associatedZoneAction;
    public List<MovementStep> MovementSteps => movementSteps;

    // Visualiser la séquence en mode éditeur
    private void OnDrawGizmos()
    {
        if (!drawConnections) return;

        Gizmos.color = gizmoColor;

        // Dessiner les connexions entre les positions des différentes étapes
        for (int i = 0; i < movementSteps.Count - 1; i++)
        {
            MovementStep currentStep = movementSteps[i];
            MovementStep nextStep = movementSteps[i + 1];

            // Connecter les positions correspondantes entre les étapes
            int minPositions = Mathf.Min(currentStep.characterPositions.Count, nextStep.characterPositions.Count);

            for (int j = 0; j < minPositions; j++)
            {
                if (currentStep.characterPositions[j] != null && nextStep.characterPositions[j] != null)
                {
                    // Dessiner une ligne entre les positions
                    Gizmos.DrawLine(
                        currentStep.characterPositions[j].position,
                        nextStep.characterPositions[j].position
                    );

                    // Dessiner une petite sphère à mi-chemin pour indiquer la direction
                    Vector3 midPoint = Vector3.Lerp(
                        currentStep.characterPositions[j].position,
                        nextStep.characterPositions[j].position,
                        0.5f
                    );
                    Gizmos.DrawSphere(midPoint, 0.1f);
                }
            }
        }

        // Afficher le nom de la séquence
#if UNITY_EDITOR
        if (movementSteps.Count > 0 && movementSteps[0].characterPositions.Count > 0)
        {
            Transform firstPos = movementSteps[0].characterPositions[0];
            if (firstPos != null)
            {
                UnityEditor.Handles.Label(firstPos.position + Vector3.up * 0.5f, 
                    $"Séquence: {sequenceName}\n{associatedSceneName}/{associatedZoneAction}");
            }
        }
#endif
    }
}