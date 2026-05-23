using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// GameManager.cs — Cerveau du jeu : état, score, timer, transitions de niveaux
// Accès global via GameManager.instance (pas besoin de chercher avec Find)
// + Fade in/out entre les niveaux

public class GameManager : MonoBehaviour
{
    // Accès global simple
    public static GameManager instance;

    // État du jeu (visible dans l'inspecteur pour debug)
    public int niveauActuel = 1;
    public int score = 300;
    public int ouvreurs = 4;
    public float tempsRestant = 60f;
    public float dureeNiveau = 60f;
    public bool jeuEnPause = false;
    public bool enclosFerme = false;

    // Références aux autres scripts (assignées dans l'inspecteur ou trouvées au Start)
    public DedaleGenerator dedaleGenerator;
    public PlayerController playerController;
    public HUDManager hudManager;

    void Awake()
    {
        // Singleton simple : si un GameManager existe déjà, détruire le doublon
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        // Chercher les références si pas assignées dans l'inspecteur
        if (dedaleGenerator == null)
            dedaleGenerator = FindObjectOfType<DedaleGenerator>();
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
        if (hudManager == null)
            hudManager = FindObjectOfType<HUDManager>();

        // Initialiser le premier niveau
        tempsRestant = dureeNiveau;
        ouvreurs = CalculerOuvreurs(niveauActuel);

        // Jouer le son de début
        if (AudioManager.instance != null)
            AudioManager.instance.Jouer("debutNiveau");
    }

    void Update()
    {
        // Ne rien faire si le jeu est en pause
        if (jeuEnPause) return;

        // Décompte du timer
        tempsRestant -= Time.deltaTime;

        // Temps écoulé → recommencer le niveau
        if (tempsRestant <= 0)
        {
            tempsRestant = 0;
            if (AudioManager.instance != null)
                AudioManager.instance.Jouer("tempsEcoule");
            RecommencerNiveau();
        }
    }

    // Calcul du nombre d'ouvreurs selon le niveau (formule du thésaurus)
    public int CalculerOuvreurs(int niveau)
    {
        return Mathf.Max(0, 4 - (niveau - 1) / 2);
    }

    // Calcul du nombre de flèches selon le niveau
    public int CalculerFleches(int niveau)
    {
        return Mathf.Max(0, 18 - 2 * (niveau - 1));
    }

    // Calcul du nombre de transporteurs selon le niveau
    public int CalculerTransporteurs(int niveau)
    {
        return niveau / 2;
    }

    // Calcul du nombre de récepteurs selon le niveau
    public int CalculerRecepteurs(int niveau)
    {
        return niveau - 1;
    }

    // Appelé quand le joueur touche le trésor
    public void PasserAuNiveauSuivant()
    {
        jeuEnPause = true;

        // Son trésor trouvé
        if (AudioManager.instance != null)
            AudioManager.instance.Jouer("tresorTrouve");

        // Bonus de temps : +10 points par seconde restante
        int bonus = Mathf.Max(0, Mathf.FloorToInt(tempsRestant)) * 10;
        score += bonus;
        Debug.Log("Trésor trouvé ! Bonus temps : +" + bonus + " pts. Score : " + score);

        // Niveau suivant
        niveauActuel++;

        // Victoire si on dépasse le niveau 10
        if (niveauActuel > 10)
        {
            TerminerVictoire();
            return;
        }

        // Transition avec fade
        StartCoroutine(TransitionNiveau());
    }

    // Coroutine qui fait fade out → reconstruit → fade in
    private IEnumerator TransitionNiveau()
    {
        // Préparer le nouveau niveau
        ouvreurs = CalculerOuvreurs(niveauActuel);
        tempsRestant = dureeNiveau;
        enclosFerme = false;

        // Fade out (écran devient noir)
        if (hudManager != null)
            hudManager.LancerFadeOut(0.4f);

        // Attendre que le fade out soit terminé
        yield return new WaitForSeconds(0.5f);

        // Reconstruire le dédale (les anciens objets sont détruits)
        if (dedaleGenerator != null)
            dedaleGenerator.ReconstruireNiveau(niveauActuel);

        // Attendre 1 frame de plus (les Destroy sont maintenant effectifs)
        yield return null;

        // Replacer le joueur au centre de l'enclos
        if (playerController != null)
            playerController.ResetPosition();

        // Fade in (écran redevient visible)
        if (hudManager != null)
            hudManager.LancerFadeIn(0.6f);

        // Son début de niveau
        if (AudioManager.instance != null)
            AudioManager.instance.Jouer("debutNiveau");

        jeuEnPause = false;
        Debug.Log("Niveau " + niveauActuel + " — Ouvreurs : " + ouvreurs);
    }

    // Appelé quand le temps est écoulé
    public void RecommencerNiveau()
    {
        jeuEnPause = true;

        // Pénalité de -200 points
        score -= 200;
        Debug.Log("Temps écoulé ! -200 pts. Score : " + score);

        // Game over si score trop bas
        if (score < 200)
        {
            TerminerGameOver();
            return;
        }

        // Reset le niveau avec fade
        tempsRestant = dureeNiveau;
        enclosFerme = false;
        // Remettre les ouvreurs à la valeur du niveau (comme le Projet2)
        ouvreurs = CalculerOuvreurs(niveauActuel);
        StartCoroutine(TransitionRecommencer());
    }

    private IEnumerator TransitionRecommencer()
    {
        // Fade out
        if (hudManager != null)
            hudManager.LancerFadeOut(0.3f);

        yield return new WaitForSeconds(0.4f);

        if (dedaleGenerator != null)
            dedaleGenerator.ReconstruireNiveau(niveauActuel);

        yield return null;

        if (playerController != null)
            playerController.ResetPosition();

        // Fade in
        if (hudManager != null)
            hudManager.LancerFadeIn(0.5f);

        jeuEnPause = false;
    }

    // Game over : score trop bas
    public void TerminerGameOver()
    {
        jeuEnPause = true;
        Debug.Log("GAME OVER — Score final : " + score);

        if (AudioManager.instance != null)
            AudioManager.instance.Jouer("gameOver");
        if (hudManager != null)
            hudManager.AfficherMessage("GAME OVER\nScore final : " + score);

        Invoke("RechargerScene", 2f);
    }

    // Victoire : 10 niveaux complétés
    public void TerminerVictoire()
    {
        jeuEnPause = true;
        Debug.Log("VICTOIRE ! Score final : " + score);

        if (AudioManager.instance != null)
            AudioManager.instance.Jouer("finJeu");
        if (hudManager != null)
            hudManager.AfficherMessage("VICTOIRE !\nTu as franchi les 10 niveaux !\nScore final : " + score);

        Invoke("RechargerScene", 3f);
    }

    // Recharge la scène courante (reset complet)
    private void RechargerScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
