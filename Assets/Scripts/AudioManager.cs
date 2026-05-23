using UnityEngine;

// AudioManager.cs — Gère les 7 sons du jeu
// Assigner les AudioClips dans l'inspecteur, appeler Jouer("nomDuSon")

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    // Les 7 sons (assigner dans l'inspecteur)
    public AudioClip debutNiveau;
    public AudioClip finJeu;
    public AudioClip gameOver;
    public AudioClip murOuvert;
    public AudioClip teleportation;
    public AudioClip tempsEcoule;
    public AudioClip tresorTrouve;

    private AudioSource audioSource;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    // Jouer un son par son nom (comme la fonction jouer() du Projet2)
    public void Jouer(string nom)
    {
        if (audioSource == null) return;

        if (nom == "debutNiveau" && debutNiveau != null)
            audioSource.PlayOneShot(debutNiveau);
        else if (nom == "finJeu" && finJeu != null)
            audioSource.PlayOneShot(finJeu);
        else if (nom == "gameOver" && gameOver != null)
            audioSource.PlayOneShot(gameOver);
        else if (nom == "murOuvert" && murOuvert != null)
            audioSource.PlayOneShot(murOuvert);
        else if (nom == "teleportation" && teleportation != null)
            audioSource.PlayOneShot(teleportation);
        else if (nom == "tempsEcoule" && tempsEcoule != null)
            audioSource.PlayOneShot(tempsEcoule);
        else if (nom == "tresorTrouve" && tresorTrouve != null)
            audioSource.PlayOneShot(tresorTrouve);
    }
}
