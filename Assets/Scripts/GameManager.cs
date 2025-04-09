using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Debug = UnityEngine.Debug;

public class GameManager : MonoBehaviour
{
    // Singleton pour accéder facilement au GameManager depuis d'autres scripts
    public static GameManager Instance { get; private set; }

    [Header("Configuration du joueur")]
    public GameObject playerRig; // Référence au rig VR existant dans la scène
    public int playerScore = 0; // Score interne, non affiché au joueur

    [Header("Configuration du jeu")]
    public float transitionTime = 5f; // Temps entre les scènes
    public bool isGameActive = false;

    [Header("Contrôle de flux de scènes")]
    [SerializeField] private bool useCustomSceneFlow = true; // Active le flux personnalisé des scènes
    [SerializeField] private bool isCurrentSceneEndOfBranch = false; // Indique si la scène actuelle est la dernière de sa branche

    [Header("Interface utilisateur")]
    public TextMeshProUGUI notificationText; // Pour les notifications, pas pour le score
    public GameObject timerUI;
    public TextMeshProUGUI timerText;

    // Variables privées
    private SceneController currentScene;
    private List<GameObject> scenesList = new List<GameObject>();
    private int currentSceneIndex = 0;
    private List<EnvironmentMover> environmentMovers = new List<EnvironmentMover>(); // Liste des movers d'environnement
    private bool customSceneTransitionTriggered = false; // Pour suivre si une transition personnalisée a été déclenchée
    private string customNextSceneName = ""; // Pour stocker le nom de la prochaine scène personnalisée

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
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        // Initialisation du jeu
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Trouver le joueur (rig VR) si non assigné
        if (playerRig == null)
        {
            // Essayer de trouver l'objet XROrigin ou un objet avec le tag "Player"
            playerRig = GameObject.FindWithTag("Player");

            if (playerRig == null)
            {
                Debug.LogError("Aucun joueur VR trouvé. Veuillez assigner manuellement le playerRig dans l'inspecteur.");
                return;
            }
        }

        Debug.Log("Joueur VR trouvé: " + playerRig.name);

        // Collecter toutes les scènes dans le niveau
        SceneController[] scenes = FindObjectsOfType<SceneController>();
        foreach (SceneController scene in scenes)
        {
            scenesList.Add(scene.gameObject);
            scene.gameObject.SetActive(false);
        }

        // Trier les scènes par leur index
        scenesList.Sort((a, b) =>
            a.GetComponent<SceneController>().SceneIndex.CompareTo(
            b.GetComponent<SceneController>().SceneIndex));

        Debug.Log("Nombre de scènes trouvées: " + scenesList.Count);

        // Trouver tous les EnvironmentMovers dans la scène
        EnvironmentMover[] movers = FindObjectsOfType<EnvironmentMover>();
        environmentMovers.AddRange(movers);
        Debug.Log("Nombre de movers d'environnement trouvés: " + environmentMovers.Count);

        // Démarrer la première scène si disponible
        if (scenesList.Count > 0)
        {
            StartCoroutine(StartScene(0));
        }
        else
        {
            Debug.LogWarning("Aucune scène trouvée dans le niveau");
        }

        // Initialiser l'UI
        HideNotification();
        HideTimer();

        isGameActive = true;
        Debug.Log("Jeu initialisé avec succès");
    }

    // Méthode pour enregistrer un EnvironmentMover
    public void RegisterEnvironmentMover(EnvironmentMover mover)
    {
        if (!environmentMovers.Contains(mover))
        {
            environmentMovers.Add(mover);
            Debug.Log("EnvironmentMover enregistré: " + mover.gameObject.name);
        }
    }

    // Démarrer une scène spécifique
    public IEnumerator StartScene(int sceneIndex)
    {
        HideTimer();
        if (sceneIndex < 0 || sceneIndex >= scenesList.Count)
        {
            Debug.LogError("Index de scène invalide: " + sceneIndex);
            yield break;
        }

        // Réinitialiser les états de transition personnalisée
        customSceneTransitionTriggered = false;
        customNextSceneName = "";


        // Désactiver la scène actuelle si elle existe
        if (currentScene != null)
        {
            currentScene.gameObject.SetActive(false);
        }

        // Définir et activer la nouvelle scène
        currentSceneIndex = sceneIndex;
        GameObject sceneObject = scenesList[currentSceneIndex];
        SceneController nextScene = sceneObject.GetComponent<SceneController>();

        if (nextScene == null)
        {
            Debug.LogError("SceneController non trouvé sur l'objet de scène");
            yield break;
        }

        string nextSceneName = nextScene.SceneName;

        // Vérifier si des mouvements d'environnement doivent être exécutés avant cette scène
        bool environmentMovementExecuted = false;
        foreach (EnvironmentMover mover in environmentMovers)
        {
            if (mover != null && mover.ShouldExecuteForScene(nextSceneName))
            {
                environmentMovementExecuted = true;
                yield return StartCoroutine(mover.ExecuteMovement());
            }
        }

        // Si des mouvements ont été exécutés, attendre un court délai avant de démarrer la scène
        if (environmentMovementExecuted)
        {
            yield return new WaitForSeconds(0.5f);
            Debug.Log("Mouvements d'environnement terminés, démarrage de la scène...");
        }

        // Activer la nouvelle scène
        sceneObject.SetActive(true);
        currentScene = nextScene;

        // Attendre que la scène soit terminée
        Debug.Log("En attente de fin de scène...");
        yield return StartCoroutine(currentScene.PlayScene());

        // Vérifier si une transition personnalisée a été déclenchée
        if (useCustomSceneFlow && customSceneTransitionTriggered)
        {
            Debug.Log($"Transition personnalisée vers la scène: {customNextSceneName}");
            yield return new WaitForSeconds(transitionTime);

            // Trouver l'index de la scène par son nom
            int customSceneIndex = -1;
            for (int i = 0; i < scenesList.Count; i++)
            {
                SceneController sc = scenesList[i].GetComponent<SceneController>();
                if (sc != null && sc.SceneName == customNextSceneName)
                {
                    customSceneIndex = i;
                    break;
                }
            }

            if (customSceneIndex >= 0)
            {
                StartCoroutine(StartScene(customSceneIndex));
            }
            else
            {
                Debug.LogError($"Scène personnalisée non trouvée: {customNextSceneName}");
                EndGame();
            }
        }
        else if (isCurrentSceneEndOfBranch)
        {
            // Si c'est la fin d'une branche, terminer le jeu
            Debug.Log("Fin de branche de scène atteinte.");
            EndGame();
        }
        else
        {
            // Comportement standard: passer à la scène suivante ou terminer le jeu
            int nextSceneIndex = (currentSceneIndex + 1) % scenesList.Count;
            if (nextSceneIndex != 0) // Ne boucle pas au début
            {
                Debug.Log("Transition vers la prochaine scène dans " + transitionTime + " secondes...");
                yield return new WaitForSeconds(transitionTime);
                StartCoroutine(StartScene(nextSceneIndex));
            }
            else
            {
                EndGame();
            }
        }
    }

    // Méthode pour remplacer la prochaine scène basée sur le choix du joueur
    public IEnumerator ReplaceNextScene(string sceneName)
    {
        Debug.Log($"Demande de transition vers la scène spécifique: {sceneName}");

        // Marquer qu'une transition personnalisée a été déclenchée
        customSceneTransitionTriggered = true;
        customNextSceneName = sceneName;

        // Cette méthode ne fait plus de remplacement, elle marque simplement
        // qu'une transition spécifique doit être effectuée
        yield return null;
    }

    // Méthode pour marquer une scène comme étant la fin d'une branche
    public void SetSceneAsEndOfBranch(bool isEndOfBranch)
    {
        isCurrentSceneEndOfBranch = isEndOfBranch;
        Debug.Log($"La scène actuelle est maintenant {(isEndOfBranch ? "marquée" : "non marquée")} comme fin de branche.");
    }

    // Afficher une notification au joueur
    public void ShowNotification(string message, float duration)
    {
        if (notificationText == null)
        {
            Debug.LogWarning("notificationText non assigné dans le GameManager");
            return;
        }

        StartCoroutine(ShowNotificationCoroutine(message, duration));
    }

    private IEnumerator ShowNotificationCoroutine(string message, float duration)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        HideNotification();
    }

    public void HideNotification()
    {
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
    }

    // Démarrer le chronomètre pour une action
    public Coroutine StartTimer(float duration)
    {
        if (timerUI == null || timerText == null)
        {
            Debug.LogWarning("timerUI ou timerText non assigné dans le GameManager");
            return null;
        }

        return StartCoroutine(TimerCoroutine(duration));
    }

    private IEnumerator TimerCoroutine(float duration)
    {
        timerUI.SetActive(true);
        float timeRemaining = duration;

        Debug.Log("Timer démarré: " + duration + " secondes");

        while (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            int seconds = Mathf.CeilToInt(timeRemaining);
            timerText.text = seconds.ToString();
            yield return null;
        }

        Debug.Log("Timer terminé!");
        HideTimer();
    }

    public void HideTimer()
    {
        if (timerUI != null)
        {
            timerUI.SetActive(false);
        }

        if (timerText != null)
        {
            timerText.text = ""; // Vider le texte affiché
        }
    }

    // Ajouter des points au score du joueur (interne, non affiché)
    public void AddPoints(int points)
    {
        // Stocker le score précédent pour le log
        int previousScore = playerScore;

        // Mettre à jour le score
        playerScore += points;

        // Log détaillé de l'ajout de points (visible uniquement dans la console)
        if (points > 0)
        {
            Debug.Log("POINTS AJOUTÉS: " + points + " (Score: " + previousScore + " → " + playerScore + ")");
        }
    }

    // Terminer le jeu
    public void EndGame()
    {
        isGameActive = false;

        string endMessage;

        // Déterminer la fin en fonction du score
        if (playerScore <= 5)
        {
            endMessage = "Mauvaise fin: Vous n'avez rien fait pour empêcher le harcèlement et l'agression.";
        }
        else if (playerScore <= 8)
        {
            endMessage = "Fin mitigée: Vous avez arrêté l'agression mais pas le harcèlement.";
        }
        else
        {
            endMessage = "Bonne fin: Vous avez agi contre le harcèlement et l'agression.";
        }

        // Afficher uniquement le message final, pas le score
        ShowNotification(endMessage, 10f);
        Debug.Log("FIN DU JEU - Score final: " + playerScore);

        // Ici, vous pourriez charger un écran de fin de jeu
        // ou permettre au joueur de recommencer
    }

    // Obtenir le joueur actuel
    public GameObject GetPlayer()
    {
        // Rechercher le vrai objet qui se déplace en VR
        GameObject xrOrigin = GameObject.Find("XR Origin"); // Nom à adapter selon votre projet
        if (xrOrigin != null)
        {
            // Essayez de trouver le vrai transform qui se déplace (souvent XROrigin ou CameraOffset)
            Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
            if (cameraOffset != null)
            {
                Debug.Log($"Utilisation de Camera Offset comme joueur: {cameraOffset.position}");
                return cameraOffset.gameObject;
            }

            // Fallback sur XR Origin lui-même
            Debug.Log($"Utilisation de XR Origin comme joueur: {xrOrigin.transform.position}");
            return xrOrigin;
        }

        // Fallback sur l'approche standard
        return playerRig;
    }
}
