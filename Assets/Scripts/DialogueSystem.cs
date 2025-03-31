using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Classe pour représenter une ligne de dialogue
[System.Serializable]
public class DialogueLine
{
    public string speakerName; // Nom du personnage qui parle
    public string dialogueText; // Texte du dialogue
    public float displayDuration = 4f; // Durée d'affichage en secondes
    public bool waitForInput = false; // Si true, attend une interaction du joueur pour passer au dialogue suivant
}

// Classe pour représenter une séquence de dialogue
[System.Serializable]
public class DialogueSequence
{
    public string sequenceName; // Nom de la séquence pour référence
    public DialogueLine[] dialogueLines; // Lignes de dialogue dans cette séquence
    public float delayBeforeStart = 0f; // Délai avant de commencer cette séquence
}

// Classe pour gérer les dialogues
public class DialogueSystem : MonoBehaviour
{
    // Singleton pour accéder facilement au système de dialogue
    public static DialogueSystem Instance { get; private set; }

    [Header("Configuration UI")]
    [SerializeField] private GameObject dialoguePanel; // Panel contenant l'UI du dialogue
    [SerializeField] private TMPro.TextMeshProUGUI speakerNameText; // Texte pour le nom du locuteur
    [SerializeField] private TMPro.TextMeshProUGUI dialogueText; // Texte pour le dialogue

    private bool isDisplayingDialogue = false;
    private DialogueSequence currentSequence;
    private int currentLineIndex = 0;
    private Coroutine dialogueCoroutine;

    private void Awake()
    {
        // Configuration du singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // S'assurer que le panel est masqué au démarrage
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    // Démarrer une séquence de dialogue
    public IEnumerator PlayDialogueSequence(DialogueSequence sequence)
    {
        if (sequence == null || sequence.dialogueLines == null || sequence.dialogueLines.Length == 0)
        {
            Debug.LogWarning("Tentative de jouer une séquence de dialogue vide ou nulle.");
            yield break;
        }

        Debug.Log($"Début de la séquence de dialogue: {sequence.sequenceName}");

        // Attendre le délai initial si nécessaire
        if (sequence.delayBeforeStart > 0)
        {
            yield return new WaitForSeconds(sequence.delayBeforeStart);
        }

        currentSequence = sequence;
        currentLineIndex = 0;
        isDisplayingDialogue = true;

        // Jouer chaque ligne de dialogue
        while (currentLineIndex < sequence.dialogueLines.Length)
        {
            // Afficher la ligne actuelle
            DialogueLine currentLine = sequence.dialogueLines[currentLineIndex];

            // Configurer l'UI
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }

            if (speakerNameText != null)
            {
                speakerNameText.text = currentLine.speakerName;
            }

            if (dialogueText != null)
            {
                dialogueText.text = currentLine.dialogueText;
            }

            // Si on attend une entrée utilisateur, attendre jusqu'à ce qu'il appuie sur un bouton
            if (currentLine.waitForInput)
            {
                Debug.Log($"Dialogue en attente d'interaction: {currentLine.dialogueText}");

                // Attendre que le joueur appuie sur un bouton (à adapter selon vos contrôles VR)
                bool inputReceived = false;

                while (!inputReceived)
                {
                    // Exemple: attendre un clic souris (à remplacer par votre détection d'entrée VR)
                    if (Input.GetMouseButtonDown(0))
                    {
                        inputReceived = true;
                    }

                    yield return null;
                }
            }
            else
            {
                // Sinon, simplement attendre la durée configurée
                Debug.Log($"Dialogue affiché: {currentLine.dialogueText}");
                yield return new WaitForSeconds(currentLine.displayDuration);
            }

            // Passer à la ligne suivante
            currentLineIndex++;
        }

        // Cacher l'UI de dialogue à la fin
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        isDisplayingDialogue = false;
        Debug.Log($"Fin de la séquence de dialogue: {sequence.sequenceName}");
    }

    // Pour sauter le dialogue en cours (optionnel)
    public void SkipCurrentDialogue()
    {
        if (isDisplayingDialogue && dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);

            // Cacher l'UI de dialogue
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            isDisplayingDialogue = false;
            Debug.Log("Dialogue ignoré");
        }
    }

    // Pour vérifier si un dialogue est en cours
    public bool IsDialogueActive()
    {
        return isDisplayingDialogue;
    }
}