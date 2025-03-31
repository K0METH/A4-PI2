using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetPosition : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string positionId = "Position_1"; // Identifiant unique pour cette position
    [SerializeField] private Color gizmoColor = Color.cyan; // Couleur pour visualiser dans l'éditeur
    [SerializeField] private float gizmoSize = 0.3f; // Taille du gizmo

    // Propriété publique pour accéder à l'ID
    public string PositionId => positionId;

    // Visualiser la position cible en mode éditeur
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        // Dessiner une sphère pour représenter la position
        Gizmos.DrawSphere(transform.position, gizmoSize);

        // Dessiner une flèche pour indiquer la direction (forward)
        Gizmos.DrawRay(transform.position, transform.forward * gizmoSize * 2);

        // Afficher le nom au-dessus de la position
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * gizmoSize * 1.5f, positionId);
#endif
    }
}