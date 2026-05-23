using UnityEngine;

// PlayerController.cs — Contrôle FPS du joueur dans le dédale
// Flèches haut/bas : avancer/reculer, gauche/droite : tourner
// Espace : ouvrir le mur en face (coûte 1 ouvreur et 50 pts)
// + Headbob quand on marche + torche chaleureuse + poussière ambiante

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Mouvement")]
    public float vitesseDeplacement = 4f;
    public float vitesseRotation = 143f;    // degrés/sec (~2.5 rad/s)

    [Header("Position de départ")]
    public int ligneDepart = 15;
    public int colDepart = 15;
    public float hauteurYeux = 0.7f;

    [Header("Ouvreur de murs")]
    public float porteeOuvreur = 1.5f;      // portée du raycast
    public float cooldownOuvreur = 0.4f;    // 400ms entre deux ouvertures

    private CharacterController controller;
    private DedaleGenerator dedaleGenerator;
    private float dernierOuvrageTime = 0f;

    // Headbob
    private float headbobTimer = 0f;
    private float headbobAmplitude = 0.04f;   // subtil (4cm)
    private float headbobFrequence = 10f;     // pas rapides
    private Camera cameraFPS;

    // Torche du joueur (lumière chaude qui tremble)
    private Light torche;
    private float torcheIntensiteBase = 1.2f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        dedaleGenerator = FindObjectOfType<DedaleGenerator>();

        // Trouver la caméra FPS pour le headbob
        CameraController camCtrl = FindObjectOfType<CameraController>();
        if (camCtrl != null && camCtrl.cameraFPS != null)
            cameraFPS = camCtrl.cameraFPS;

        // Créer la torche du joueur
        CreerTorche();

        // Poussière ambiante autour du joueur
        EffetsParticules.CreerPoussiere(transform);

        ResetPosition();
    }

    private void CreerTorche()
    {
        GameObject torchObj = new GameObject("TorcheJoueur");
        torchObj.transform.SetParent(transform, false);
        torchObj.transform.localPosition = new Vector3(0.2f, 0f, 0.1f);

        torche = torchObj.AddComponent<Light>();
        torche.type = LightType.Point;
        torche.color = new Color(1f, 0.85f, 0.6f); // Lumière chaude orangée
        torche.intensity = torcheIntensiteBase;
        torche.range = 6f;
        torche.shadows = LightShadows.Soft;
    }

    void Update()
    {
        // Ne rien faire si le jeu est en pause
        if (GameManager.instance != null && GameManager.instance.jeuEnPause) return;

        TraiterRotation();
        bool enMouvement = TraiterDeplacement();
        TraiterActions();
        VerifierFermetureEnclos();

        // Headbob seulement quand on marche
        TraiterHeadbob(enMouvement);

        // Tremblement de la torche
        TraiterTorche();
    }

    private void TraiterRotation()
    {
        float rotation = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))
            rotation -= vitesseRotation * Time.deltaTime;
        if (Input.GetKey(KeyCode.RightArrow))
            rotation += vitesseRotation * Time.deltaTime;

        if (rotation != 0f)
            transform.Rotate(0f, rotation, 0f);
    }

    // Retourne true si le joueur bouge
    private bool TraiterDeplacement()
    {
        Vector3 direction = Vector3.zero;

        if (Input.GetKey(KeyCode.UpArrow))
            direction += transform.forward;
        if (Input.GetKey(KeyCode.DownArrow))
            direction -= transform.forward;

        if (direction != Vector3.zero)
        {
            direction.y = 0f;
            direction.Normalize();

            Vector3 deplacement = direction * vitesseDeplacement * Time.deltaTime;
            controller.Move(deplacement);

            // Forcer Y constant (pas de gravité)
            Vector3 pos = transform.position;
            pos.y = hauteurYeux;
            transform.position = pos;

            return true;
        }

        return false;
    }

    private void TraiterActions()
    {
        // Espace seul (sans Ctrl/Shift qui est réservé au mode tricheur)
        if (Input.GetKeyDown(KeyCode.Space)
            && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)
            && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
        {
            TenterOuvrirMur();
        }
    }

    private void TraiterHeadbob(bool enMouvement)
    {
        if (cameraFPS == null) return;

        // Vérifier qu'on n'est pas en vue aérienne
        CameraController camCtrl = FindObjectOfType<CameraController>();
        if (camCtrl != null && camCtrl.vueAerienne) return;

        if (enMouvement)
        {
            headbobTimer += Time.deltaTime * headbobFrequence;

            // Oscillation douce en Y + légère en X pour un mouvement naturel
            float bobY = Mathf.Sin(headbobTimer) * headbobAmplitude;
            float bobX = Mathf.Cos(headbobTimer * 0.5f) * headbobAmplitude * 0.5f;

            cameraFPS.transform.localPosition = new Vector3(bobX, bobY, 0f);
        }
        else
        {
            // Revenir doucement au centre quand on s'arrête
            headbobTimer = 0f;
            Vector3 localPos = cameraFPS.transform.localPosition;
            cameraFPS.transform.localPosition = Vector3.Lerp(localPos, Vector3.zero, Time.deltaTime * 8f);
        }
    }

    private void TraiterTorche()
    {
        if (torche == null) return;

        // Tremblement subtil de l'intensité (effet torche/flamme)
        float bruit = Mathf.PerlinNoise(Time.time * 4f, 0f);
        torche.intensity = torcheIntensiteBase + (bruit - 0.5f) * 0.6f;

        // Léger mouvement aléatoire de la position
        float bruit2 = Mathf.PerlinNoise(Time.time * 3f, 10f);
        float bruit3 = Mathf.PerlinNoise(Time.time * 3f, 20f);
        torche.transform.localPosition = new Vector3(
            0.2f + (bruit2 - 0.5f) * 0.1f,
            (bruit3 - 0.5f) * 0.05f,
            0.1f
        );
    }

    private void TenterOuvrirMur()
    {
        if (dedaleGenerator == null) return;
        if (GameManager.instance == null) return;

        // Cooldown
        if (Time.time - dernierOuvrageTime < cooldownOuvreur) return;

        // Vérifier qu'on a des ouvreurs et assez de score
        if (GameManager.instance.ouvreurs <= 0) return;
        if (GameManager.instance.score < 50) return;

        // Raycast vers l'avant pour toucher le mur en face
        RaycastHit hit;
        if (!Physics.Raycast(transform.position, transform.forward, out hit, porteeOuvreur)) return;

        // Vérifier que c'est un mur ouvrable
        if (hit.collider.CompareTag("MurOuvrable"))
        {
            // Trouver la cellule du mur touché
            Vector3 murPos = hit.collider.transform.position;
            int ligne = Mathf.FloorToInt(murPos.z);
            int col = Mathf.FloorToInt(murPos.x);

            dedaleGenerator.OuvrirMur(ligne, col);

            // Coût : -1 ouvreur, -50 points
            GameManager.instance.ouvreurs--;
            GameManager.instance.score -= 50;

            dernierOuvrageTime = Time.time;

            if (AudioManager.instance != null)
                AudioManager.instance.Jouer("murOuvert");

            Debug.Log("Mur ouvert ! Ouvreurs restants : " + GameManager.instance.ouvreurs);
        }
    }

    // Vérifie si le joueur a quitté l'enclos pour le fermer
    private void VerifierFermetureEnclos()
    {
        if (GameManager.instance == null) return;
        if (GameManager.instance.enclosFerme) return;

        // L'ouverture de l'enclos est à (13, 15)
        // Quand le joueur passe au nord de Z=13.0, on ferme
        if (transform.position.z < 13.0f)
        {
            GameManager.instance.enclosFerme = true;

            // Créer un mur à la position de l'ouverture
            if (dedaleGenerator != null)
            {
                dedaleGenerator.FermerEnclos();
            }

            Debug.Log("Enclos fermé !");
        }
    }

    // Remet le joueur au centre de l'enclos, regardant vers le nord
    public void ResetPosition()
    {
        // Désactiver le CharacterController pour pouvoir déplacer le transform
        if (controller == null)
            controller = GetComponent<CharacterController>();

        controller.enabled = false;

        float posX = colDepart + 0.5f;
        float posZ = ligneDepart + 0.5f;
        transform.position = new Vector3(posX, hauteurYeux, posZ);
        transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        controller.enabled = true;
    }

    // Retourne la cellule (ligne, col) où se trouve le joueur
    public Vector2Int GetCelluleJoueur()
    {
        int col = Mathf.FloorToInt(transform.position.x);
        int ligne = Mathf.FloorToInt(transform.position.z);
        return new Vector2Int(ligne, col);
    }
}
