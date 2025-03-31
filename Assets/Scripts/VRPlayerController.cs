using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
// Ajout de l'espace de noms pour XROrigin si disponible dans votre version
using Unity.XR.CoreUtils;

public class VRPlayerController : MonoBehaviour
{
    [Header("D�placement")]
    [SerializeField] private ActionBasedContinuousMoveProvider moveProvider;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private bool useGravity = true;

    [Header("Composants")]
    [SerializeField] private CharacterController characterController;

    // Utiliser � la fois XROrigin et XRRig pour la compatibilit� avec diff�rentes versions
    [SerializeField] private Transform xrRigOrOrigin;

    // Variables priv�es
    private Camera xrCamera;
    private bool isGrounded;
    private float verticalVelocity;
    private const float GRAVITY = -9.81f;

    private void Awake()
    {
        // R�cup�rer les composants s'ils ne sont pas assign�s
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
                // Sinon, essayer de trouver XRRig (d�pr�ci� mais pourrait �tre disponible)
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
        // Configurer le contr�leur de d�placement
        if (moveProvider != null)
        {
            moveProvider.moveSpeed = moveSpeed;
        }

        // Trouver la cam�ra principale
        xrCamera = Camera.main;
    }

    private void Update()
    {
        // Mise � jour du centre de gravit� pour le Character Controller
        UpdateCharacterController();

        // Appliquer la gravit� si activ�e
        if (useGravity)
        {
            ApplyGravity();
        }
    }

    // Mettre � jour le Character Controller en fonction de la position de la cam�ra VR
    private void UpdateCharacterController()
    {
        if (xrRigOrOrigin == null || characterController == null || xrCamera == null) return;

        // Calculer la diff�rence de hauteur entre la cam�ra et le sol
        float heightDifference = xrCamera.transform.localPosition.y;

        // Positionner le Character Controller sous la cam�ra
        Vector3 capsuleCenter = Vector3.zero;
        capsuleCenter.y = heightDifference / 2f; // Moiti� de la hauteur pour centrer
        characterController.center = capsuleCenter;

        // Ajuster la hauteur du Character Controller
        characterController.height = Mathf.Max(characterController.radius * 2f, heightDifference);
    }

    // Appliquer la gravit� au Character Controller
    private void ApplyGravity()
    {
        if (characterController == null) return;

        // V�rifier si le joueur est au sol
        isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity < 0)
        {
            // R�initialiser la v�locit� si au sol
            verticalVelocity = -1f; // Petite valeur n�gative pour maintenir le contact avec le sol
        }
        else
        {
            // Appliquer la gravit�
            verticalVelocity += GRAVITY * Time.deltaTime;
        }

        // Cr�er un vecteur de mouvement pour la gravit�
        Vector3 gravityMove = new Vector3(0, verticalVelocity, 0);

        // Appliquer le mouvement
        characterController.Move(gravityMove * Time.deltaTime);
    }

    // T�l�porter le joueur � une position sp�cifique
    public void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        if (characterController == null) return;

        // D�sactiver temporairement le Character Controller pour �viter les probl�mes de collision
        characterController.enabled = false;

        // Positionner le joueur
        transform.position = position;
        transform.rotation = rotation;

        // R�activer le Character Controller
        characterController.enabled = true;
    }

    // Changer la vitesse de d�placement
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;

        if (moveProvider != null)
        {
            moveProvider.moveSpeed = speed;
        }
    }
}