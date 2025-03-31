using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
// Ajout de l'espace de noms pour XROrigin si disponible dans votre version
using Unity.XR.CoreUtils;

public class VRPlayerController : MonoBehaviour
{
    [Header("Déplacement")]
    [SerializeField] private ActionBasedContinuousMoveProvider moveProvider;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private bool useGravity = true;

    [Header("Composants")]
    [SerializeField] private CharacterController characterController;

    // Utiliser à la fois XROrigin et XRRig pour la compatibilité avec différentes versions
    [SerializeField] private Transform xrRigOrOrigin;

    // Variables privées
    private Camera xrCamera;
    private bool isGrounded;
    private float verticalVelocity;
    private const float GRAVITY = -9.81f;

    private void Awake()
    {
        // Récupérer les composants s'ils ne sont pas assignés
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (xrRigOrOrigin == null)
        {
            // Essayer d'abord de trouver XROrigin
            var xrOrigin = GetComponentInChildren<MonoBehaviour>();
            if (xrOrigin != null && xrOrigin.GetType().Name == "XROrigin")
            {
                xrRigOrOrigin = xrOrigin.transform;
            }
            else
            {
                // Sinon, essayer de trouver XRRig (déprécié mais pourrait être disponible)
                var xrRig = GetComponentInChildren<MonoBehaviour>();
                if (xrRig != null && xrRig.GetType().Name == "XRRig")
                {
                    xrRigOrOrigin = xrRig.transform;
                }
            }
        }

        if (moveProvider == null)
        {
            moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
        }
    }

    private void Start()
    {
        // Configurer le contrôleur de déplacement
        if (moveProvider != null)
        {
            moveProvider.moveSpeed = moveSpeed;
        }

        // Trouver la caméra principale
        xrCamera = Camera.main;
    }

    private void Update()
    {
        // Mise à jour du centre de gravité pour le Character Controller
        UpdateCharacterController();

        // Appliquer la gravité si activée
        if (useGravity)
        {
            ApplyGravity();
        }
    }

    // Mettre à jour le Character Controller en fonction de la position de la caméra VR
    private void UpdateCharacterController()
    {
        if (xrRigOrOrigin == null || characterController == null || xrCamera == null) return;

        // Calculer la différence de hauteur entre la caméra et le sol
        float heightDifference = xrCamera.transform.localPosition.y;

        // Positionner le Character Controller sous la caméra
        Vector3 capsuleCenter = Vector3.zero;
        capsuleCenter.y = heightDifference / 2f; // Moitié de la hauteur pour centrer
        characterController.center = capsuleCenter;

        // Ajuster la hauteur du Character Controller
        characterController.height = Mathf.Max(characterController.radius * 2f, heightDifference);
    }

    // Appliquer la gravité au Character Controller
    private void ApplyGravity()
    {
        if (characterController == null) return;

        // Vérifier si le joueur est au sol
        isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity < 0)
        {
            // Réinitialiser la vélocité si au sol
            verticalVelocity = -1f; // Petite valeur négative pour maintenir le contact avec le sol
        }
        else
        {
            // Appliquer la gravité
            verticalVelocity += GRAVITY * Time.deltaTime;
        }

        // Créer un vecteur de mouvement pour la gravité
        Vector3 gravityMove = new Vector3(0, verticalVelocity, 0);

        // Appliquer le mouvement
        characterController.Move(gravityMove * Time.deltaTime);
    }

    // Téléporter le joueur à une position spécifique
    public void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        if (characterController == null) return;

        // Désactiver temporairement le Character Controller pour éviter les problèmes de collision
        characterController.enabled = false;

        // Positionner le joueur
        transform.position = position;
        transform.rotation = rotation;

        // Réactiver le Character Controller
        characterController.enabled = true;
    }

    // Changer la vitesse de déplacement
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;

        if (moveProvider != null)
        {
            moveProvider.moveSpeed = speed;
        }
    }
}