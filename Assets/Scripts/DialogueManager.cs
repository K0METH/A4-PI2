using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class Dialogue
{
    public string speaker; // Le nom du personnage qui parle
    public string text; // Le texte du dialogue
}

public class DialogueManager : MonoBehaviour
{
    public TextMeshProUGUI dialogueText; // Le TextMesh Pro qui affichera les dialogues
    public TextMeshProUGUI speakerText; // Le TextMesh Pro qui affichera le nom du personnage qui parle
    public List<Dialogue> dialogues = new List<Dialogue>(); // La liste de dialogues
    public List<Dialogue> dialogues2 = new List<Dialogue>(); // La liste de dialogues 2
    public float delay; // Le délai avant d'afficher les dialogues 2

    private int currentDialogueIndex = 0; // L'index du dialogue en cours

    void Start()
    {
        // Ajouter les dialogues à la liste
        dialogues.Add(new Dialogue { speaker = "Clara", text = "Ahah, et là, il me sort : \"Mais pourquoi est-ce que tu as mis un citron dans ton café ?\" J’ai cru que j’allais m’étouffer !" });
        dialogues.Add(new Dialogue { speaker = "Matthieu", text = "En boissons, je suis l’expert. Vous saviez que j’ai un palais très exigeant ?" });
        dialogues.Add(new Dialogue { speaker = "Clara", text = "Ah bon ? Un connaisseur parmi nous !" });
        dialogues.Add(new Dialogue { speaker = "Matthieu", text = "Absolument, mais passons. Alors Laure, comment avance le projet ?" });
        dialogues.Add(new Dialogue { speaker = "Laure", text = "Plutôt bien, quelques ajustements encore." });
        dialogues.Add(new Dialogue { speaker = "Matthieu", text = "Sérieusement, Laure, tu devrais te mettre plus en avant. On manque de talents comme toi." });
        dialogues.Add(new Dialogue { speaker = "Laure", text = "Je préfère laisser mon travail parler." });
        dialogues.Add(new Dialogue { speaker = "Clara", text = "On a une équipe en or, c’est clair !" });
        dialogues.Add(new Dialogue { speaker = "Matthieu", text = "Toi aussi, Clara. Mais Laure, on devrait parler de certains points. Pourquoi pas maintenant ?" });
        dialogues.Add(new Dialogue { speaker = "Laure", text = "Plus tard, j’allais justement voir Nausicaa et Clémence." });
        dialogues.Add(new Dialogue { speaker = "Matthieu", text = "Je n’ai pas terminé. Laure, tu peux m’éclairer sur un point ?" });
        dialogues.Add(new Dialogue { speaker = "Laure", text = "Plus tard, si tu permets…" });

        // Ajouter les dialogues 2 à la liste
        dialogues2.Add(new Dialogue { speaker = "Matthieu", text = "Vous savez, c’est impressionnant de voir à quel point certains se dévouent à leur travail." });
        dialogues2.Add(new Dialogue { speaker = "Collègue 2", text = "Peut-être que c’est aussi important de prendre des pauses" });
        dialogues2.Add(new Dialogue { speaker = "Matthieu", text = "Ah, mais le travail, c’est toute la vie, non ? Enfin, pour certains..." });
        dialogues2.Add(new Dialogue { speaker = "Laure", text = "Matthieu, je pense qu’on doit en discuter plus tard." });
        dialogues2.Add(new Dialogue { speaker = "Matthieu", text = "Bien sûr Mais n’oublie pas, Laure... certaines choses ne se disent pas toujours en réunion." });


        // Lancer la coroutine pour afficher les dialogues
        StartCoroutine(DisplayDialogues());
    }

    IEnumerator DisplayDialogues()
    {
        while (currentDialogueIndex < dialogues.Count)
        {
            // Afficher le nom du personnage qui parle
            speakerText.text = dialogues[currentDialogueIndex].speaker;
            // Afficher le dialogue en cours
            dialogueText.text = dialogues[currentDialogueIndex].text;

            // Attendre le délai avant de passer au dialogue suivant
            yield return new WaitForSeconds(2f);

            // Passer au dialogue suivant
            currentDialogueIndex++;
        }

        /*// Attendre le délai avant d'afficher les dialogues 2
        yield return new WaitForSeconds(delay);
        Debug.Log("Dialogues 2");

        // Réinitialiser l'index du dialogue en cours
        currentDialogueIndex = 0;

        while (currentDialogueIndex < dialogues2.Count)
        {
            // Afficher le nom du personnage qui parle
            speakerText.text = dialogues2[currentDialogueIndex].speaker;
            // Afficher le dialogue en cours
            dialogueText.text = dialogues2[currentDialogueIndex].text;

            // Attendre le délai avant de passer au dialogue suivant
            yield return new WaitForSeconds(2f);

            // Passer au dialogue suivant
            currentDialogueIndex++;
        }*/ 
    }
}
