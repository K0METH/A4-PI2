using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SceneController : MonoBehaviour
{
    [Header("Configuration de la sc�ne")]
    [SerializeField] private string sceneName = "Ajouter nom";
    [SerializeField] private string sceneDescription = "Ajouter description";
    [SerializeField] private float sceneDuration; // Dur�e avant que l'action soit disponible
    [SerializeField] private float actionDuration; // Dur�e du timer pour l'action
    [SerializeField] private int sceneIndex = 0;  // 0 pour premi�re sc�ne, 1 pour deuxi�me, etc.

    [Header("Dialogues")]
    [SerializeField] private List<DialogueSequence> sceneDialogues = new List<DialogueSequence>(); // Dialogues pendant la sc�ne principale
    [SerializeField] private List<float> dialogueDelays = new List<float>(); // D�lais avant chaque dialogue

    [Header("Contr�le de flux")]
    [SerializeField] private bool isEndOfBranch = false; // Indique si cette sc�ne est la fin d'une branche
    [SerializeField] private string defaultNextSceneName = ""; // Sc�ne par d�faut si aucune zone n'est activ�e

    [Header("Mouvement par d�faut")]
    [SerializeField] private EnvironmentMover defaultMover; // Mover � utiliser si aucun choix n'est fait

    [Header("Indicateurs visuels")]
    [SerializeField] private GameObject actionIndicator; // Logo/indicateur visuel

    [Header("Zones de points")]
    [SerializeField] private List<PointZone> pointZones = new List<PointZone>();
    [SerializeField] private bool showZonesOnlyDuringAction = true;

    private bool isActionActive = false;
    private bool isSceneStarted = false;
    private float actionStartTime;
    private HashSet<string> zonesAlreadyCounted = new HashSet<string>();
    private bool hasTriggeredSceneChange = false; // Pour ne d�clencher qu'une seule transition
    private bool hasTriggeredDefaultMovement = false; // Pour ne d�clencher le mouvement par d�faut qu'une fois
    private Coroutine sceneDialoguesCoroutine; // R�f�rence � la coroutine des dialogues de sc�ne

    // Propri�t�s publiques en lecture seule
    public string SceneName => sceneName;
    public float SceneDuration => sceneDuration;
    public float ActionDuration => actionDuration;
    public List<PointZone> PointZones => pointZones;
    public int SceneIndex => sceneIndex;

    void Start()
    {
        // Initialisation - d�sactiv�e car maintenant g�r�e par PlayScene()
        // StartCoroutine(WaitBeforeAction());
    }

    public IEnumerator PlayScene()
    {
        Debug.Log($"D�marrage de la sc�ne: {sceneName}");

        // Informer le GameManager si cette sc�ne est la fin d'une branche
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSceneAsEndOfBranch(isEndOfBranch);
        }

        // R�initialiser l'�tat de transition
        hasTriggeredSceneChange = false;
        hasTriggeredDefaultMovement = false;

        // Cacher l'indicateur et les zones au d�but
        if (actionIndicator != null)
        {
            actionIndicator.SetActive(false);
        }

        if (showZonesOnlyDuringAction)
        {
            foreach (var zone in pointZones)
            {
                zone.HideZone();
            }
        }

        isSceneStarted = true;

        // Lancer les dialogues de la sc�ne en parall�le
        sceneDialoguesCoroutine = StartCoroutine(PlaySceneDialogues());

        // Attendre que la dur�e de la sc�ne soit �coul�e
        yield return new WaitForSeconds(sceneDuration);

        // D�marrer la phase d'action
        StartAction();

        // D�marrer le chronom�tre si GameManager est disponible
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartTimer(actionDuration);
        }

        // Attendre pendant la dur�e de l'action
        yield return new WaitForSeconds(actionDuration);

        // Terminer l'action et attribuer les points
        EndAction();

        // Attendre pour s'assurer que tous les mouvements ont �t� d�clench�s
        yield return new WaitForSeconds(0.5f);

        // Arr�ter la coroutine des dialogues de sc�ne si elle est encore en cours
        if (sceneDialoguesCoroutine != null)
        {
            StopCoroutine(sceneDialoguesCoroutine);
        }

        // Si aucune zone n'a d�clench� de changement de sc�ne et qu'il y a une sc�ne suivante par d�faut
        if (!hasTriggeredSceneChange && !string.IsNullOrEmpty(defaultNextSceneName) && GameManager.Instance != null)
        {
            GameManager.Instance.StartCoroutine(GameManager.Instance.ReplaceNextScene(defaultNextSceneName));
        }

        Debug.Log($"Fin de la sc�ne: {sceneName}");
    }

    // Coroutine pour jouer les dialogues pendant la sc�ne principale
    private IEnumerator PlaySceneDialogues()
    {
        if (sceneDialogues == null || sceneDialogues.Count == 0)
            yield break;

        // V�rifier que les d�lais sont configur�s correctement
        if (dialogueDelays.Count != sceneDialogues.Count)
        {
            Debug.LogWarning("Le nombre de d�lais ne correspond pas au nombre de dialogues. Utilisation de d�lais par d�faut.");
            dialogueDelays.Clear();
            for (int i = 0; i < sceneDialogues.Count; i++)
            {
                dialogueDelays.Add(i * 5f); // D�lai par d�faut de 5 secondes entre chaque dialogue
            }
        }

        // Jouer les dialogues avec leurs d�lais respectifs
        for (int i = 0; i < sceneDialogues.Count; i++)
        {
            if (sceneDialogues[i] != null)
            {
                yield return new WaitForSeconds(dialogueDelays[i]);

                if (DialogueSystem.Instance != null)
                {
                    yield return StartCoroutine(DialogueSystem.Instance.PlayDialogueSequence(sceneDialogues[i]));
                }
            }
        }
    }

    void Update()
    {
        if (isSceneStarted && isActionActive)
        {
            // G�rer l'attribution des points en temps r�el pendant l'action si n�cessaire
            // Ou vous pouvez d�cider de n'attribuer les points qu'� la fin de l'action
            AssignPointsToPlayer();

            // V�rifier la fin de l'action bas�e sur le temps �coul�
            if (Time.time - actionStartTime >= actionDuration)
            {
                EndAction();
            }
        }
    }

    // D�marre la phase d'action
    private void StartAction()
    {
        Debug.Log("Phase d'action commenc�e.");
        isActionActive = true;
        actionStartTime = Time.time;

        // R�initialiser les zones d�j� compt�es
        zonesAlreadyCounted.Clear();

        // Afficher l'indicateur visuel
        if (actionIndicator != null)
        {
            actionIndicator.SetActive(true);
        }

        // Activer les zones pendant l'action
        if (showZonesOnlyDuringAction)
        {
            foreach (var zone in pointZones)
            {
                zone.ShowZone();
            }
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
            Debug.LogWarning("Joueur non trouv�!");
            return;
        }

        foreach (var zone in pointZones)
        {
            // V�rifier si le joueur est dans la zone ET si cette zone n'a pas d�j� �t� compt�e
            if (zone.IsPlayerInZone(player) && !zonesAlreadyCounted.Contains(zone.ZoneName))
            {
                Debug.Log($"Le joueur est dans la zone {zone.ZoneName}, Points gagn�s: {zone.PointValue}");

                // Marquer cette zone comme d�j� compt�e
                zonesAlreadyCounted.Add(zone.ZoneName);

                // Attribuer les points via GameManager si disponible
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddPoints(zone.PointValue);
                    Debug.Log($"POINTS AJOUT�S: {zone.PointValue} - Score actuel: {GameManager.Instance.playerScore}");
                }

                // Si cette zone doit changer la prochaine sc�ne
                if (!string.IsNullOrEmpty(zone.NextSceneName) && GameManager.Instance != null && !hasTriggeredSceneChange)
                {
                    hasTriggeredSceneChange = true; // Marquer qu'une transition a �t� d�clench�e
                    GameManager.Instance.StartCoroutine(GameManager.Instance.ReplaceNextScene(zone.NextSceneName));
                    Debug.Log($"Transition d�clench�e vers la sc�ne: {zone.NextSceneName}");
                }

                break; // Sortir de la boucle une fois qu'une zone est trouv�e
            }
        }
    }

    // Fin de la phase d'action
    private void EndAction()
    {
        if (!isActionActive) return; // �viter de terminer l'action plusieurs fois

        Debug.Log("Phase d'action termin�e.");
        isActionActive = false;

        // Cacher l'indicateur visuel
        if (actionIndicator != null)
        {
            actionIndicator.SetActive(false);
        }

        // Attribuer les points finaux
        AssignPointsToPlayer();

        // Ex�cuter tous les mouvements en attente des zones activ�es
        bool anyMovementTriggered = false;
        foreach (var zone in pointZones)
        {
            if (zone.HasBeenActivated())
            {
                // Ex�cuter le mouvement en attente si pr�sent
                if (zone.HasPendingMovement())
                {
                    zone.ExecutePendingMovement();
                    anyMovementTriggered = true;
                }
            }
        }

        // Cacher les zones apr�s l'action
        if (showZonesOnlyDuringAction)
        {
            foreach (var zone in pointZones)
            {
                zone.HideZone();
            }
        }

        // Si aucun mouvement n'a �t� d�clench�, ex�cuter le mouvement par d�faut
        if (!anyMovementTriggered && defaultMover != null && !hasTriggeredDefaultMovement)
        {
            hasTriggeredDefaultMovement = true;
            Debug.Log("Aucun mouvement sp�cifique d�clench�, ex�cution du mouvement par d�faut");
            StartCoroutine(defaultMover.ExecuteMovement());
        }
    }
}