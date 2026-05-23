using UnityEngine;

// CameraController.cs — Gère le switch entre vue FPS et vue aérienne
// Page Up : activer vue aérienne (coûte 10 pts/sec)
// Page Down : retour en vue FPS
// Ctrl+Shift+Espace : mode tricheur (montre les objets en vue aérienne)
// Bug 2 : marqueur joueur (triangle blanc) visible en vue aérienne
// Bug 3 : objets émissifs en mode tricheur

public class CameraController : MonoBehaviour
{
    public Camera cameraFPS;
    public Camera cameraAerienne;

    public bool vueAerienne = false;
    public bool modeTricheur = false;

    public float penaliteParSeconde = 10f;

    private DedaleGenerator dedaleGenerator;
    private PlayerController joueur;
    private GameObject marqueurJoueur;

    void Start()
    {
        if (cameraAerienne != null)
            cameraAerienne.enabled = false;
        if (cameraFPS != null)
            cameraFPS.enabled = true;

        dedaleGenerator = FindObjectOfType<DedaleGenerator>();
        joueur = FindObjectOfType<PlayerController>();

        CreerMarqueurJoueur();
    }

    void Update()
    {
        if (GameManager.instance != null && GameManager.instance.jeuEnPause) return;

        if (Input.GetKeyDown(KeyCode.PageUp) && !vueAerienne)
            EntrerVueAerienne();

        if (Input.GetKeyDown(KeyCode.PageDown) && vueAerienne)
            SortirVueAerienne();

        // Ctrl+Shift+Espace en vue aérienne : toggle tricheur
        if (vueAerienne && Input.GetKeyDown(KeyCode.Space)
            && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            modeTricheur = !modeTricheur;
            AjusterVisibiliteObjets();
            Debug.Log("Mode tricheur : " + (modeTricheur ? "ON" : "OFF"));
        }

        // Pénalité de score en vue aérienne
        if (vueAerienne && GameManager.instance != null)
        {
            GameManager.instance.score -= Mathf.RoundToInt(penaliteParSeconde * Time.deltaTime);
            if (GameManager.instance.score < 0) GameManager.instance.score = 0;
        }

        // Mettre à jour le marqueur joueur en vue aérienne
        if (vueAerienne && marqueurJoueur != null && joueur != null)
        {
            MajMarqueurJoueur();
        }
    }

    // --- MARQUEUR JOUEUR (Bug 2) ---

    private void CreerMarqueurJoueur()
    {
        marqueurJoueur = new GameObject("MarqueurJoueur");

        MeshFilter mf = marqueurJoueur.AddComponent<MeshFilter>();
        MeshRenderer mr = marqueurJoueur.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();

        // Hauteur : juste sous le plafond
        float hauteur = 2f;
        if (dedaleGenerator != null) hauteur = dedaleGenerator.hauteurMur;
        float y = hauteur - 0.05f;

        // Triangle pointant en +X local (comme le Projet2)
        Vector3[] vertices = new Vector3[] {
            new Vector3(0.60f, y, 0f),       // pointe avant
            new Vector3(-0.35f, y, -0.40f),  // base droite
            new Vector3(-0.35f, y, 0.40f)    // base gauche
        };

        // Double face (visible du dessus et du dessous)
        int[] triangles = new int[] {
            0, 2, 1,    // face du dessus
            0, 1, 2     // face du dessous
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        // Matériau blanc uni
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.SetColor("_BaseColor", Color.white);
            mat.color = Color.white;
            mr.material = mat;
        }
        else
        {
            mr.material.color = Color.white;
        }

        // Caché par défaut (visible seulement en vue aérienne)
        marqueurJoueur.SetActive(false);
    }

    private void MajMarqueurJoueur()
    {
        // Position XZ = position du joueur (Y=0 car le Y est dans les vertices)
        Vector3 pos = joueur.transform.position;
        marqueurJoueur.transform.position = new Vector3(pos.x, 0f, pos.z);

        // Rotation = direction du regard du joueur (formule du Projet2)
        Vector3 fwd = joueur.transform.forward;
        float angle = Mathf.Atan2(-fwd.z, fwd.x) * Mathf.Rad2Deg;
        marqueurJoueur.transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }

    // --- VUE AÉRIENNE ---

    private void EntrerVueAerienne()
    {
        if (GameManager.instance == null) return;
        if (GameManager.instance.score < 10)
        {
            Debug.Log("Score trop bas pour la vue aérienne !");
            return;
        }

        vueAerienne = true;

        if (cameraFPS != null) cameraFPS.enabled = false;
        if (cameraAerienne != null) cameraAerienne.enabled = true;

        // Cacher le plafond
        if (dedaleGenerator != null && dedaleGenerator.plafond != null)
            dedaleGenerator.plafond.SetActive(false);

        // Afficher le marqueur joueur
        if (marqueurJoueur != null)
            marqueurJoueur.SetActive(true);

        // En vue aérienne, cacher les objets sauf en mode tricheur
        AjusterVisibiliteObjets();
    }

    private void SortirVueAerienne()
    {
        vueAerienne = false;
        modeTricheur = false;

        if (cameraFPS != null) cameraFPS.enabled = true;
        if (cameraAerienne != null) cameraAerienne.enabled = false;

        // Remettre le plafond
        if (dedaleGenerator != null && dedaleGenerator.plafond != null)
            dedaleGenerator.plafond.SetActive(true);

        // Cacher le marqueur joueur
        if (marqueurJoueur != null)
            marqueurJoueur.SetActive(false);

        // Tout rendre visible en FPS (sans émission)
        SetVisibiliteObjetsNiveau(true);
        SetEmissionObjets(false);
    }

    // --- VISIBILITÉ ET ÉMISSION (Bug 3) ---

    private void AjusterVisibiliteObjets()
    {
        bool visible = modeTricheur;
        SetVisibiliteObjetsNiveau(visible);

        // En mode tricheur : rendre les objets émissifs pour les voir d'en haut
        SetEmissionObjets(modeTricheur);
    }

    private void SetVisibiliteObjetsNiveau(bool visible)
    {
        if (dedaleGenerator == null) return;
        Transform parentObjets = dedaleGenerator.transform.Find("Objets");
        if (parentObjets == null) return;

        for (int i = 0; i < parentObjets.childCount; i++)
        {
            parentObjets.GetChild(i).gameObject.SetActive(visible);
        }
    }

    private void SetEmissionObjets(bool emissif)
    {
        if (dedaleGenerator == null) return;
        Transform parentObjets = dedaleGenerator.transform.Find("Objets");
        if (parentObjets == null) return;

        for (int i = 0; i < parentObjets.childCount; i++)
        {
            GameObject obj = parentObjets.GetChild(i).gameObject;
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend == null) continue;

            if (emissif)
            {
                // Couleur émissive selon le type d'objet
                Color emission = Color.black;

                if (obj.name.StartsWith("Tresor"))
                    emission = new Color(1f, 0.85f, 0.2f) * 2f;  // doré brillant
                else if (obj.name.StartsWith("Transporteur"))
                    emission = new Color(0f, 0.85f, 1f) * 2f;    // cyan brillant
                else if (obj.name.StartsWith("Recepteur"))
                    emission = new Color(0.95f, 0.25f, 0.95f) * 2f; // magenta brillant

                if (emission != Color.black)
                {
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", emission);
                }
            }
            else
            {
                // Retirer l'émission
                rend.material.DisableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}
