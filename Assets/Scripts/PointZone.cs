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
    [SerializeField] private string zoneDescription = ""; // Description de cette zone pour documentation dans l'�diteur

    [Header("Transition de sc�ne")]
    [SerializeField] private string nextSceneName = ""; // Pour la prochaine sc�ne

    [Header("Mouvements des personnages")]
    [SerializeField] private EnvironmentMover associatedMover; // Mover sp�cifique � cette zone

    [Header("Aspects visuels")]
    [SerializeField] private Renderer zoneRenderer; // R�f�rence au renderer de la zone
    [SerializeField] private Material activeMaterial; // Mat�riau quand la zone est active
    [SerializeField] private Material inactiveMaterial; // Mat�riau quand la zone est inactive
    [SerializeField] private float detectionRadius = 10.0f;

    private Collider zoneCollider;
    private bool isActive = false;
    private bool hasTriggeredMovement = false; // Pour s'assurer qu'on ne d�clenche le mouvement qu'une fois
    private bool hasBeenActivated = false; // Pour savoir si la zone a �t� activ�e, m�me sans mover
    private bool isMovementPending = false; // Pour diff�rer l'ex�cution du mouvement jusqu'� la fin du timer

    // Propri�t�s publiques en lecture seule
    public string ZoneName => zoneName;
    public int PointValue => pointValue;
    public string NextSceneName => nextSceneName;
    public bool IsActive => isActive;

    // M�thodes pour v�rifier l'�tat d'activation et de d�clenchement de mouvement
    public bool HasBeenActivated() => hasBeenActivated;
    public bool HasTriggeredMovement() => hasTriggeredMovement;
    public bool HasPendingMovement() => isMovementPending;

    // Ajout d'un getter pour le rayon de d�tection
    public float GetDetectionRadius() => detectionRadius;

    void Awake()
    {
        // Obtenir le collider attach�
        zoneCollider = GetComponent<Collider>();

        if (zoneCollider == null)
        {
            Debug.LogError($"La zone {zoneName} n'a pas de collider! Veuillez en ajouter un.");
        }

        // Si le renderer n'est pas assign�, essayer de le trouver
        if (zoneRenderer == null)
        {
            zoneRenderer = GetComponent<Renderer>();
        }

        // Toujours cacher la zone au d�marrage
        HideZone();
    }

    // Active et rend visible la zone
    public void ShowZone()
    {
        isActive = true;

        // Activer le gameObject s'il �tait d�sactiv�
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // Changer le mat�riau si sp�cifi�
        if (zoneRenderer != null && activeMaterial != null)
        {
            zoneRenderer.material = activeMaterial;
        }

        // Activer le collider si disponible
        if (zoneCollider != null)
        {
            zoneCollider.enabled = true;
        }

        // V�rifier imm�diatement si le joueur est d�j� dans la zone
        if (GameManager.Instance != null)
        {
            GameObject player = GameManager.Instance.GetPlayer();
            if (player != null)
            {
                IsPlayerInZone(player);
            }
        }
    }

    // D�sactive et cache la zone
    public void HideZone()
    {
        isActive = false;
        hasTriggeredMovement = false;
        hasBeenActivated = false;
        isMovementPending = false;

        // Changer le mat�riau si sp�cifi�
        if (zoneRenderer != null && inactiveMaterial != null)
        {
            zoneRenderer.material = inactiveMaterial;
        }
        else if (zoneRenderer != null)
        {
            // Rendre la zone transparente si pas de mat�riau inactif sp�cifi�
            Color transparent = zoneRenderer.material.color;
            transparent.a = 0;
            zoneRenderer.material.color = transparent;
        }
    }

    // V�rifie si le joueur est dans la zone
    public bool IsPlayerInZone(GameObject player)
    {
        if (!isActive)
        {
            return false;
        }

        // Utiliser directement la cam�ra principale comme r�f�rence
        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 zonePosition = transform.position;

        // Distance entre la cam�ra et le centre de la zone
        float distance = Vector3.Distance(zonePosition, cameraPosition);

        // Si le joueur est dans la zone
        if (distance <= detectionRadius)
        {
            // Marquer que cette zone a �t� activ�e
            hasBeenActivated = true;

            // Marquer le mouvement comme "en attente" au lieu de l'ex�cuter imm�diatement
            if (associatedMover != null && !hasTriggeredMovement && !isMovementPending)
            {
                isMovementPending = true;
                Debug.Log($"Zone {zoneName} activ�e, mouvement en attente de la fin du timer");
            }
            return true;
        }

        return false;
    }

    // Ex�cute le mouvement qui a �t� mis en attente
    public bool ExecutePendingMovement()
    {
        if (isMovementPending && associatedMover != null && !hasTriggeredMovement)
        {
            hasTriggeredMovement = true;
            isMovementPending = false;
            Debug.Log($"Ex�cution du mouvement associ� � la zone {zoneName} � la fin du timer");
            StartCoroutine(associatedMover.ExecuteMovement());
            return true;
        }
        return false;
    }

    // Visualiser la zone en mode �diteur
    private void OnDrawGizmos()
    {
        // Dessiner une sph�re wireframe pour repr�senter le rayon de d�tection
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Optionnel: dessiner une sph�re semi-transparente quand l'objet est s�lectionn�
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            Gizmos.color = new Color(1, 1, 0, 0.2f); // Jaune semi-transparent
            Gizmos.DrawSphere(transform.position, detectionRadius);

            // Afficher aussi la description dans la sc�ne si l'objet est s�lectionn�
            if (!string.IsNullOrEmpty(zoneDescription))
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * (detectionRadius + 0.5f),
                    $"{zoneName} ({pointValue} pts)\n{zoneDescription}");
            }
        }
    }
}