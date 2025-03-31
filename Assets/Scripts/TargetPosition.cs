using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetPosition : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string positionId = "Position_1"; // Identifiant unique pour cette position
    [SerializeField] private Color gizmoColor = Color.cyan; // Couleur pour visualiser dans l'�diteur
    [SerializeField] private float gizmoSize = 0.3f; // Taille du gizmo

    // Propri�t� publique pour acc�der � l'ID
    public string PositionId => positionId;

    // Visualiser la position cible en mode �diteur
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        // Dessiner une sph�re pour repr�senter la position
        Gizmos.DrawSphere(transform.position, gizmoSize);

        // Dessiner une fl�che pour indiquer la direction (forward)
        Gizmos.DrawRay(transform.position, transform.forward * gizmoSize * 2);

        // Afficher le nom au-dessus de la position
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * gizmoSize * 1.5f, positionId);
#endif
    }
}