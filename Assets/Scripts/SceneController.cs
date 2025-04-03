using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SceneController : MonoBehaviour
{
    [Header("Configuration de la scène")]
    [SerializeField] private string sceneName = "Ajouter nom";
    [SerializeField] private string sceneDescription = "Ajouter description";
    [SerializeField] private float sceneDuration; // Durée avant que l'action soit disponible
    [SerializeField] private float actionDuration; // Durée du timer pour l'action
    [SerializeField] private int sceneIndex = 0;  // 0 pour première scène, 1 pour deuxième, etc.

    [Header("Dialogues")]
    [SerializeField] private List<DialogueSequence> sceneDialogues = new List<DialogueSequence>(); // Dialogues pendant la scène principale
    [SerializeField] private List<float> dialogueDelays = new List<float>(); // Délais avant chaque dialogue

    [Header("Contrôle de flux")]
    [SerializeField] private bool isEndOfBranch = false; // Indique si cette scène est la fin d'une branche
    [SerializeField] private string defaultNextSceneName = ""; // Scène par défaut si aucune zone n'est activée

    [Header("Mouvement par défaut")]
    [SerializeField] private EnvironmentMover defaultMover; // Mover à utiliser si aucun choix n'est fait

    [Header("Indicateurs visuels")]
    [SerializeField] private GameObject actionIndicator; // Logo/indicateur visuel

    [Header("Zones de points")]
    [SerializeField] private List<PointZone> pointZones = new List<PointZone>();

    private bool isActionActive = false;
    private bool isSceneStarted = false;
    private float actionStartTime;
    private HashSet<string> zonesAlreadyCounted = new HashSet<string>();
    private bool hasTriggeredSceneChange = false; // Pour ne déclencher qu'une seule transition
    private bool hasTriggeredDefaultMovement = false; // Pour ne déclencher le mouvement par défaut qu'une fois
    private Coroutine sceneDialoguesCoroutine; // Référence à la coroutine des dialogues de scène

    // Propriétés publiques en lecture seule
    public string SceneName => sceneName;
    public float SceneDuration => sceneDuration;
    public float ActionDuration => actionDuration;
    public List<PointZone> PointZones => pointZones;
    public int SceneIndex => sceneIndex;

    void Start()
    {
        // Initialisation - désactivée car maintenant gérée par PlayScene()
        // StartCoroutine(WaitBeforeAction());
    }

    public IEnumerator PlayScene()
    {
        Debug.Log($"Démarrage de la scène: {sceneName}");

        // Informer le GameManager si cette scène est la fin d'une branche
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSceneAsEndOfBranch(isEndOfBranch);
        }

        // Réinitialiser l'état de transition
        hasTriggeredSceneChange = false;
        hasTriggeredDefaultMovement = false;

        // Cacher l'indicateur au début
        if (actionIndicator != null)
        {
            actionIndicator.SetActive(false);
        }

        // Cacher toutes les zones au début de la scène
        foreach (var zone in pointZones)
        {
            zone.HideZone(); // Réinitialiser l'état de la zone
        }

        isSceneStarted = true;

        // Lancer les dialogues de la scène en parallèle
        sceneDialoguesCoroutine = StartCoroutine(PlaySceneDialogues());

        // Attendre que la durée de la scène soit écoulée
        yield return new WaitForSeconds(sceneDuration);

        // Démarrer la phase d'action
        StartAction();

        // Démarrer le chronomètre si GameManager est disponible
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartTimer(actionDuration);
        }

        // Attendre pendant la durée de l'action
        yield return new WaitForSeconds(actionDuration);

        // Terminer l'action et attribuer les points
        EndAction();

        // Attendre pour s'assurer que tous les mouvements ont été déclenchés
        yield return new WaitForSeconds(0.5f);

        // Arrêter la coroutine des dialogues de scène si elle est encore en cours
        if (sceneDialoguesCoroutine != null)
        {
            StopCoroutine(sceneDialoguesCoroutine);
        }

        // Si aucune zone n'a déclenché de changement de scène et qu'il y a une scène suivante par défaut
        if (!hasTriggeredSceneChange && !string.IsNullOrEmpty(defaultNextSceneName) && GameManager.Instance != null)
        {
            GameManager.Instance.StartCoroutine(GameManager.Instance.ReplaceNextScene(defaultNextSceneName));
        }

        Debug.Log($"Fin de la scène: {sceneName}");
    }

    // Coroutine pour jouer les dialogues pendant la scène principale
    private IEnumerator PlaySceneDialogues()
    {
        // Filtrer les éléments nuls de la liste avant de commencer
        List<DialogueSequence> validDialogues = new List<DialogueSequence>();
        List<float> validDelays = new List<float>();

        for (int i = 0; i < sceneDialogues.Count; i++)
        {
            if (sceneDialogues[i] != null)
            {
                validDialogues.Add(sceneDialogues[i]);
                // S'assurer que nous avons assez de délais
                if (i < dialogueDelays.Count)
                {
                    validDelays.Add(dialogueDelays[i]);
                }
                else
                {
                    validDelays.Add(0f); // Délai par défaut si manquant
                }
            }
        }

        // Si aucun dialogue valide, sortir
        if (validDialogues.Count == 0)
            yield break;

        // Vérifier que les délais sont configurés correctement
        if (validDelays.Count != validDialogues.Count)
        {
            Debug.LogWarning("Le nombre de délais ne correspond pas au nombre de dialogues. Utilisation de délais par défaut.");
            validDelays.Clear();
            for (int i = 0; i < validDialogues.Count; i++)
            {
                validDelays.Add(i * 5f); // Délai par défaut de 5 secondes entre chaque dialogue
            }
        }

        // Jouer les dialogues valides avec leurs délais respectifs
        for (int i = 0; i < validDialogues.Count; i++)
        {
            yield return new WaitForSeconds(validDelays[i]);

            if (DialogueSystem.Instance != null)
            {
                // Afficher des infos de débogage pour comprendre ce qui est joué
                Debug.Log($"Lancement de la séquence de dialogue: {validDialogues[i].sequenceName}");
                Debug.Log($"Nombre de lignes dans cette séquence: {validDialogues[i].dialogueLines.Length}");

                yield return StartCoroutine(DialogueSystem.Instance.PlayDialogueSequence(validDialogues[i]));
            }
        }
    }

    void Update()
    {
        if (isSceneStarted && isActionActive)
        {
            // Gérer l'attribution des points en temps réel pendant l'action si nécessaire
            // Ou vous pouvez décider de n'attribuer les points qu'à la fin de l'action
            AssignPointsToPlayer();

            // Vérifier la fin de l'action basée sur le temps écoulé
            if (Time.time - actionStartTime >= actionDuration)
            {
                EndAction();
            }
        }
    }

    // Démarre la phase d'action
    private void StartAction()
    {
        Debug.Log("Phase d'action commencée.");
        isActionActive = true;
        actionStartTime = Time.time;

        // Réinitialiser les zones déjà comptées
        zonesAlreadyCounted.Clear();

        // Afficher l'indicateur visuel
        if (actionIndicator != null)
        {
            actionIndicator.SetActive(true);
        }

        // Activer toutes les zones pendant l'action
        foreach (var zone in pointZones)
        {
            zone.ShowZone();
        }
    }

    // Attribue des points en fonction de la position du joueur dans les zones
    private void AssignPointsToPlayer()
    {
        GameObject player = null;

        // Obtenir le joueur via GameManager si disponible
        if (GameManager.Instance != null)
        {
            player = GameManager.Instance.GetPlayer();
        }
        else
        {
            // Fallback pour obtenir le joueur si GameManager n'est pas disponible
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player == null)
        {
            Debug.LogWarning("Joueur non trouvé!");
            return;
        }

        foreach (var zone in pointZones)
        {
            // Vérifier si le joueur est dans la zone ET si cette zone n'a pas déjà été comptée
            if (zone.IsPlayerInZone(player) && !zonesAlreadyCounted.Contains(zone.ZoneName))
            {
                Debug.Log($"Le joueur est dans la zone {zone.ZoneName}, Points gagnés: {zone.PointValue}");

                // Marquer cette zone comme déjà comptée
                zonesAlreadyCounted.Add(zone.ZoneName);

                // Attribuer les points via GameManager si disponible
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddPoints(zone.PointValue);
                    Debug.Log($"POINTS AJOUTÉS: {zone.PointValue} - Score actuel: {GameManager.Instance.playerScore}");
                }

                // Si cette zone doit changer la prochaine scène
                if (!string.IsNullOrEmpty(zone.NextSceneName) && GameManager.Instance != null && !hasTriggeredSceneChange)
                {
                    hasTriggeredSceneChange = true; // Marquer qu'une transition a été déclenchée
                    GameManager.Instance.StartCoroutine(GameManager.Instance.ReplaceNextScene(zone.NextSceneName));
                    Debug.Log($"Transition déclenchée vers la scène: {zone.NextSceneName}");
                }

                break; // Sortir de la boucle une fois qu'une zone est trouvée
            }
        }
    }

    // Fin de la phase d'action
    private void EndAction()
    {
        if (!isActionActive) return; // Éviter de terminer l'action plusieurs fois

        Debug.Log("Phase d'action terminée.");
        isActionActive = false;

        // Cacher l'indicateur visuel
        if (actionIndicator != null)
        {
            actionIndicator.SetActive(false);
        }

        // Attribuer les points finaux
        AssignPointsToPlayer();

        // Exécuter tous les mouvements en attente des zones activées
        bool anyMovementTriggered = false;
        foreach (var zone in pointZones)
        {
            if (zone.HasBeenActivated())
            {
                // Exécuter le mouvement en attente si présent
                if (zone.HasPendingMovement())
                {
                    zone.ExecutePendingMovement();
                    anyMovementTriggered = true;
                }
            }
        }

        // Cacher toutes les zones après l'action
        foreach (var zone in pointZones)
        {
            zone.HideZone();
        }

        // Si aucun mouvement n'a été déclenché, exécuter le mouvement par défaut
        if (!anyMovementTriggered && defaultMover != null && !hasTriggeredDefaultMovement)
        {
            hasTriggeredDefaultMovement = true;
            Debug.Log("Aucun mouvement spécifique déclenché, exécution du mouvement par défaut");
            StartCoroutine(defaultMover.ExecuteMovement());
        }
    }
}
