using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PointZone : MonoBehaviour
{
    [Header("Configuration de base")]
    [SerializeField] private string zoneName = "Zone";
    [SerializeField] private int pointValue = 0;
    [TextArea(3, 5)] // Pour permettre plusieurs lignes de texte dans l'inspecteur
    [Tooltip("Description pour documentation interne uniquement")]
    [SerializeField] private string zoneDescription = ""; // Description de cette zone pour documentation dans l'éditeur

    [Header("Transition de scène")]
    [SerializeField] private string nextSceneName = ""; // Pour la prochaine scène

    [Header("Mouvements des personnages")]
    [SerializeField] private EnvironmentMover associatedMover; // Mover spécifique à cette zone

    [Header("Aspects visuels")]
    [SerializeField] private Renderer zoneRenderer; // Référence au renderer de la zone
    [SerializeField] private Material activeMaterial; // Matériau quand la zone est active
    [SerializeField] private Material inactiveMaterial; // Matériau quand la zone est inactive
    [SerializeField] private float detectionRadius = 10.0f;

    private Collider zoneCollider;
    private bool isActive = false;
    private bool hasTriggeredMovement = false; // Pour s'assurer qu'on ne déclenche le mouvement qu'une fois
    private bool hasBeenActivated = false; // Pour savoir si la zone a été activée, même sans mover
    private bool isMovementPending = false; // Pour différer l'exécution du mouvement jusqu'à la fin du timer

    // Propriétés publiques en lecture seule
    public string ZoneName => zoneName;
    public int PointValue => pointValue;
    public string NextSceneName => nextSceneName;
    public bool IsActive => isActive;

    // Méthodes pour vérifier l'état d'activation et de déclenchement de mouvement
    public bool HasBeenActivated() => hasBeenActivated;
    public bool HasTriggeredMovement() => hasTriggeredMovement;
    public bool HasPendingMovement() => isMovementPending;

    // Ajout d'un getter pour le rayon de détection
    public float GetDetectionRadius() => detectionRadius;

    void Awake()
    {
        // Obtenir le collider attaché
        zoneCollider = GetComponent<Collider>();

        if (zoneCollider == null)
        {
            Debug.LogError($"La zone {zoneName} n'a pas de collider! Veuillez en ajouter un.");
        }

        // Si le renderer n'est pas assigné, essayer de le trouver
        if (zoneRenderer == null)
        {
            zoneRenderer = GetComponent<Renderer>();
        }

        // Toujours cacher la zone au démarrage
        HideZone();
    }

    // Active et rend visible la zone
    public void ShowZone()
    {
        isActive = true;

        // Activer le gameObject s'il était désactivé
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // Changer le matériau si spécifié
        if (zoneRenderer != null && activeMaterial != null)
        {
            zoneRenderer.material = activeMaterial;
        }

        // Activer le collider si disponible
        if (zoneCollider != null)
        {
            zoneCollider.enabled = true;
        }

        // Vérifier immédiatement si le joueur est déjà dans la zone
        if (GameManager.Instance != null)
        {
            GameObject player = GameManager.Instance.GetPlayer();
            if (player != null)
            {
                IsPlayerInZone(player);
            }
        }
    }

    // Désactive et cache la zone
    public void HideZone()
    {
        isActive = false;
        hasTriggeredMovement = false;
        hasBeenActivated = false;
        isMovementPending = false;

        // Changer le matériau si spécifié
        if (zoneRenderer != null && inactiveMaterial != null)
        {
            zoneRenderer.material = inactiveMaterial;
        }
        else if (zoneRenderer != null)
        {
            // Rendre la zone transparente si pas de matériau inactif spécifié
            Color transparent = zoneRenderer.material.color;
            transparent.a = 0;
            zoneRenderer.material.color = transparent;
        }
    }

    // Vérifie si le joueur est dans la zone
    public bool IsPlayerInZone(GameObject player)
    {
        if (!isActive)
        {
            return false;
        }

        // Utiliser directement la caméra principale comme référence
        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 zonePosition = transform.position;

        // Distance entre la caméra et le centre de la zone
        float distance = Vector3.Distance(zonePosition, cameraPosition);

        // Si le joueur est dans la zone
        if (distance <= detectionRadius)
        {
            // Marquer que cette zone a été activée
            hasBeenActivated = true;

            // Marquer le mouvement comme "en attente" au lieu de l'exécuter immédiatement
            if (associatedMover != null && !hasTriggeredMovement && !isMovementPending)
            {
                isMovementPending = true;
                Debug.Log($"Zone {zoneName} activée, mouvement en attente de la fin du timer");
            }
            return true;
        }

        return false;
    }

    // Exécute le mouvement qui a été mis en attente
    public bool ExecutePendingMovement()
    {
        if (isMovementPending && associatedMover != null && !hasTriggeredMovement)
        {
            hasTriggeredMovement = true;
            isMovementPending = false;
            Debug.Log($"Exécution du mouvement associé à la zone {zoneName} à la fin du timer");
            StartCoroutine(associatedMover.ExecuteMovement());
            return true;
        }
        return false;
    }

    // Visualiser la zone en mode éditeur
    private void OnDrawGizmos()
    {
        // Dessiner une sphère wireframe pour représenter le rayon de détection
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Optionnel: dessiner une sphère semi-transparente quand l'objet est sélectionné
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            Gizmos.color = new Color(1, 1, 0, 0.2f); // Jaune semi-transparent
            Gizmos.DrawSphere(transform.position, detectionRadius);

            // Afficher aussi la description dans la scène si l'objet est sélectionné
            if (!string.IsNullOrEmpty(zoneDescription))
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * (detectionRadius + 0.5f),
                    $"{zoneName} ({pointValue} pts)\n{zoneDescription}");
            }
        }
    }
}