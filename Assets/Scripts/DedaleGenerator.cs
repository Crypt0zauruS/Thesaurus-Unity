using UnityEngine;
using System.Collections.Generic;

// DedaleGenerator.cs — Instancie les murs, plancher, plafond et objets du dédale
// Lit DedaleData.TabDedale et crée les GameObjects correspondants.

public class DedaleGenerator : MonoBehaviour
{
    [Header("Dimensions")]
    public float hauteurMur = 2f;
    public float tailleCellule = 1f;

    [Header("Matériaux (assigner dans l'inspecteur)")]
    public Material matMurX;
    public Material matMurO;
    public Material matPlancher;
    public Material matPlancherEnclos;
    public Material matPlafond;

    [Header("Matériaux objets")]
    public Material matTresor;
    public Material matFleche;         // Pyramide orange (Unlit)
    public Material matFlecheTexture;  // Quad transparent avec arrow.png
    public Material matTransporteur;
    public Material matRecepteur;

    // Grille des murs pour manipulation (ouverture)
    private GameObject[,] grilleMurs;

    // Conteneurs parents
    private Transform parentMurs;
    private Transform parentPlancher;
    private Transform parentObjets;

    // Copie modifiable du dédale (murs ouverts, enclos fermé)
    private char[,] dedaleEffectif;

    // Référence au plafond (pour la vue aérienne)
    public GameObject plafond;

    // Références aux objets du niveau actuel
    private GameObject tresorActuel;
    private List<GameObject> flechesActuelles = new List<GameObject>();
    private List<GameObject> transporteursActuels = new List<GameObject>();
    private List<GameObject> recepteursActuels = new List<GameObject>();

    // Positions sauvegardées par niveau (pour garder les mêmes positions au restart)
    private int[,] positionsSauvees;   // [niveau, index] → ligne*31+col
    private bool[] niveauDejaGenere;

    void Start()
    {
        grilleMurs = new GameObject[DedaleData.TAILLE, DedaleData.TAILLE];
        positionsSauvees = new int[11, 50]; // 10 niveaux, max 50 objets
        niveauDejaGenere = new bool[11];

        InitDedaleEffectif();
        GenererDedale();

        // Placer les objets du niveau 1
        if (GameManager.instance != null)
            PlacerObjetsNiveau(GameManager.instance.niveauActuel);
        else
            PlacerObjetsNiveau(1);
    }

    // Copie de la matrice originale dans un tableau 2D modifiable
    private void InitDedaleEffectif()
    {
        dedaleEffectif = new char[DedaleData.TAILLE, DedaleData.TAILLE];
        for (int l = 0; l < DedaleData.TAILLE; l++)
        {
            for (int c = 0; c < DedaleData.TAILLE; c++)
            {
                dedaleEffectif[l, c] = DedaleData.GetCell(l, c);
            }
        }
    }

    public void GenererDedale()
    {
        parentMurs = new GameObject("Murs").transform;
        parentMurs.SetParent(transform);

        parentPlancher = new GameObject("Plancher").transform;
        parentPlancher.SetParent(transform);

        parentObjets = new GameObject("Objets").transform;
        parentObjets.SetParent(transform);

        GenererMurs();
        GenererPlancher();
        GenererPlafond();
    }

    // Détruit tout et reconstruit pour un nouveau niveau
    public void ReconstruireNiveau(int niveau)
    {
        // Désactiver les colliders AVANT la destruction pour éviter que
        // le CharacterController soit poussé par des fantômes de l'ancien niveau
        if (parentMurs != null)
        {
            foreach (Collider c in parentMurs.GetComponentsInChildren<Collider>())
                c.enabled = false;
            Destroy(parentMurs.gameObject);
        }
        if (parentPlancher != null) Destroy(parentPlancher.gameObject);
        if (parentObjets != null)
        {
            foreach (Collider c in parentObjets.GetComponentsInChildren<Collider>())
                c.enabled = false;
            Destroy(parentObjets.gameObject);
        }
        if (tresorActuel != null) Destroy(tresorActuel);

        // Détruire l'ancien plafond (sinon il s'accumule à chaque niveau)
        if (plafond != null) Destroy(plafond);

        // Reset la grille des murs
        grilleMurs = new GameObject[DedaleData.TAILLE, DedaleData.TAILLE];

        // Reset le dédale effectif (matrice propre)
        InitDedaleEffectif();

        // Reconstruire
        GenererDedale();
        PlacerObjetsNiveau(niveau);
    }

    private void GenererMurs()
    {
        for (int ligne = 0; ligne < DedaleData.TAILLE; ligne++)
        {
            for (int col = 0; col < DedaleData.TAILLE; col++)
            {
                char cellule = dedaleEffectif[ligne, col];

                if (cellule == 'X' || cellule == 'O')
                {
                    CreerMur(ligne, col, cellule);
                }
            }
        }
        Debug.Log("DedaleGenerator : " + parentMurs.childCount + " murs instanciés.");
    }

    private GameObject CreerMur(int ligne, int col, char type)
    {
        GameObject mur = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mur.name = "Mur_" + ligne + "_" + col;
        mur.transform.SetParent(parentMurs);

        float posX = col * tailleCellule + tailleCellule / 2f;
        float posY = hauteurMur / 2f;
        float posZ = ligne * tailleCellule + tailleCellule / 2f;
        mur.transform.position = new Vector3(posX, posY, posZ);
        mur.transform.localScale = new Vector3(tailleCellule, hauteurMur, tailleCellule);

        Renderer rend = mur.GetComponent<Renderer>();
        if (type == 'X')
        {
            if (matMurX != null) rend.material = matMurX;
            mur.tag = "MurFixe";
        }
        else
        {
            if (matMurO != null) rend.material = matMurO;
            mur.tag = "MurOuvrable";
        }

        grilleMurs[ligne, col] = mur;
        return mur;
    }

    private void GenererPlancher()
    {
        float taille = DedaleData.TAILLE * tailleCellule;

        GameObject plancher = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plancher.name = "PlancherPrincipal";
        plancher.transform.SetParent(parentPlancher);
        plancher.transform.position = new Vector3(taille / 2f, 0f, taille / 2f);
        plancher.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        plancher.transform.localScale = new Vector3(taille, taille, 1f);

        if (matPlancher != null)
        {
            Renderer rend = plancher.GetComponent<Renderer>();
            rend.material = matPlancher;
            rend.material.SetTextureScale("_BaseMap", new Vector2(DedaleData.TAILLE, DedaleData.TAILLE));
        }

        // Plancher enclos (3x3 au centre, légèrement au-dessus)
        GameObject plancherEnclos = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plancherEnclos.name = "PlancherEnclos";
        plancherEnclos.transform.SetParent(parentPlancher);
        plancherEnclos.transform.position = new Vector3(15.5f, 0.01f, 15.5f);
        plancherEnclos.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        plancherEnclos.transform.localScale = new Vector3(3f * tailleCellule, 3f * tailleCellule, 1f);

        if (matPlancherEnclos != null)
        {
            Renderer rend = plancherEnclos.GetComponent<Renderer>();
            rend.material = matPlancherEnclos;
            rend.material.SetTextureScale("_BaseMap", new Vector2(3f, 3f));
        }
    }

    private void GenererPlafond()
    {
        float taille = DedaleData.TAILLE * tailleCellule;

        plafond = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plafond.name = "Plafond";
        plafond.transform.SetParent(transform);
        plafond.transform.position = new Vector3(taille / 2f, hauteurMur, taille / 2f);
        plafond.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        plafond.transform.localScale = new Vector3(taille, taille, 1f);

        Renderer rend = plafond.GetComponent<Renderer>();
        if (matPlafond != null)
        {
            rend.material = matPlafond;
            rend.material.SetTextureScale("_BaseMap", new Vector2(DedaleData.TAILLE, DedaleData.TAILLE));
        }
        else
        {
            rend.material.color = new Color(0.2f, 0.2f, 0.25f);
        }

        Collider col = plafond.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    // --- OUVERTURE DE MURS ---

    public void OuvrirMur(int ligne, int col)
    {
        if (ligne < 0 || ligne >= DedaleData.TAILLE) return;
        if (col < 0 || col >= DedaleData.TAILLE) return;
        if (dedaleEffectif[ligne, col] != 'O') return;

        // Marquer comme couloir dans le dédale effectif
        dedaleEffectif[ligne, col] = ' ';

        GameObject mur = grilleMurs[ligne, col];
        if (mur != null)
        {
            // Désactiver le collider immédiatement (le joueur peut passer)
            Collider murCol = mur.GetComponent<Collider>();
            if (murCol != null) murCol.enabled = false;

            // Ajouter le script d'animation de descente
            MurOuvrable anim = mur.AddComponent<MurOuvrable>();
            anim.Descendre();

            grilleMurs[ligne, col] = null;
        }
    }

    // Ferme l'enclos en créant un mur X à la position (13, 15)
    public void FermerEnclos()
    {
        dedaleEffectif[13, 15] = 'X';
        CreerMur(13, 15, 'X');
        Debug.Log("Enclos fermé : mur créé à (13, 15)");
    }

    // --- PLACEMENT DES OBJETS DU NIVEAU ---

    public void PlacerObjetsNiveau(int niveau)
    {
        // Nettoyer les objets du niveau précédent
        NettoyerObjets();

        // Trouver toutes les cellules vides (couloirs seulement, pas l'enclos ni le spawn)
        List<Vector2Int> cellulesVides = new List<Vector2Int>();
        for (int l = 0; l < DedaleData.TAILLE; l++)
        {
            for (int c = 0; c < DedaleData.TAILLE; c++)
            {
                if (dedaleEffectif[l, c] == ' ')
                {
                    // Exclure la cellule de spawn (15, 15) et les cellules autour de l'enclos
                    if (l == 15 && c == 15) continue;
                    if (l >= 13 && l <= 17 && c >= 13 && c <= 17) continue;
                    cellulesVides.Add(new Vector2Int(l, c));
                }
            }
        }

        if (cellulesVides.Count == 0)
        {
            Debug.LogError("Aucune cellule vide trouvée !");
            return;
        }

        // Mélanger les cellules pour un placement aléatoire
        MelangerListe(cellulesVides);

        int index = 0;

        // 1. Placer le trésor
        if (index < cellulesVides.Count)
        {
            Vector2Int pos = cellulesVides[index];
            index++;
            tresorActuel = CreerTresor(pos.x, pos.y);
        }

        // 2. Placer les flèches (pointent vers le trésor)
        int nbFleches = GameManager.instance != null
            ? GameManager.instance.CalculerFleches(niveau) : 18;
        for (int i = 0; i < nbFleches && index < cellulesVides.Count; i++)
        {
            Vector2Int pos = cellulesVides[index];
            index++;
            GameObject fleche = CreerFleche(pos.x, pos.y);
            flechesActuelles.Add(fleche);
        }

        // 3. Placer les transporteurs
        int nbTrans = GameManager.instance != null
            ? GameManager.instance.CalculerTransporteurs(niveau) : 0;
        for (int i = 0; i < nbTrans && index < cellulesVides.Count; i++)
        {
            Vector2Int pos = cellulesVides[index];
            index++;
            GameObject trans = CreerTransporteur(pos.x, pos.y);
            transporteursActuels.Add(trans);
        }

        // 4. Placer les récepteurs
        int nbRecept = GameManager.instance != null
            ? GameManager.instance.CalculerRecepteurs(niveau) : 0;
        for (int i = 0; i < nbRecept && index < cellulesVides.Count; i++)
        {
            Vector2Int pos = cellulesVides[index];
            index++;
            GameObject recept = CreerRecepteur(pos.x, pos.y);
            recepteursActuels.Add(recept);
        }

        Debug.Log("Niveau " + niveau + " : 1 trésor, " + nbFleches + " flèches, "
                  + nbTrans + " transporteurs, " + nbRecept + " récepteurs placés.");
    }

    private void NettoyerObjets()
    {
        if (tresorActuel != null) Destroy(tresorActuel);
        foreach (GameObject go in flechesActuelles) { if (go != null) Destroy(go); }
        foreach (GameObject go in transporteursActuels) { if (go != null) Destroy(go); }
        foreach (GameObject go in recepteursActuels) { if (go != null) Destroy(go); }
        flechesActuelles.Clear();
        transporteursActuels.Clear();
        recepteursActuels.Clear();
    }

    // Mélange Fisher-Yates simple
    private void MelangerListe(List<Vector2Int> liste)
    {
        for (int i = liste.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2Int temp = liste[i];
            liste[i] = liste[j];
            liste[j] = temp;
        }
    }

    // Position monde du centre d'une cellule
    private Vector3 PositionCellule(int ligne, int col, float posY)
    {
        float x = col * tailleCellule + tailleCellule / 2f;
        float z = ligne * tailleCellule + tailleCellule / 2f;
        return new Vector3(x, posY, z);
    }

    // --- CRÉATION DES OBJETS ---

    private GameObject CreerTresor(int ligne, int col)
    {
        GameObject tresor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tresor.name = "Tresor";
        tresor.transform.SetParent(parentObjets);
        tresor.transform.position = PositionCellule(ligne, col, 0.25f);
        tresor.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);

        // Couleur dorée
        Renderer rend = tresor.GetComponent<Renderer>();
        if (matTresor != null)
            rend.material = matTresor;
        else
            rend.material.color = new Color(1f, 0.88f, 0.55f);

        // Collider en trigger pour détecter le joueur
        BoxCollider box = tresor.GetComponent<BoxCollider>();
        box.isTrigger = true;

        // Tag + script
        tresor.tag = "Tresor";
        tresor.AddComponent<Tresor>();

        return tresor;
    }

    private GameObject CreerFleche(int ligne, int col)
    {
        // Pyramide 3D à 5 sommets (comme le Projet2 WebGL)
        // Pointe = orange vif brillant, base = brun sombre mat
        // Le contraste rend la direction évidente même vu d'en bas
        GameObject fleche = new GameObject("Fleche_" + ligne + "_" + col);
        fleche.transform.SetParent(parentObjets);
        fleche.transform.position = PositionCellule(ligne, col, hauteurMur * 0.8f);

        MeshFilter mf = fleche.AddComponent<MeshFilter>();
        MeshRenderer mr = fleche.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();

        // Pyramide plus allongée pour mieux voir la direction
        Vector3 pointe = new Vector3(0.5f, 0f, 0f);
        Vector3 a = new Vector3(-0.2f, 0.16f, 0.16f);
        Vector3 b = new Vector3(-0.2f, 0.16f, -0.16f);
        Vector3 c = new Vector3(-0.2f, -0.16f, -0.16f);
        Vector3 d = new Vector3(-0.2f, -0.16f, 0.16f);

        Vector3[] vertices = new Vector3[] {
            pointe, a, b,       // face haut (0-2)
            pointe, b, c,       // face droite (3-5)
            pointe, c, d,       // face bas (6-8)
            pointe, d, a,       // face gauche (9-11)
            a, c, b, d          // base (12-15)
        };

        int[] triangles = new int[] {
            0, 1, 2,
            3, 4, 5,
            6, 7, 8,
            9, 10, 11,
            12, 13, 14,
            12, 15, 13
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        // Matériau métallique doré
        if (matFleche != null)
            mr.material = matFleche;
        else
            mr.material.color = new Color(1f, 0.55f, 0f);

        // Orienter la pointe vers le trésor + inclinaison de 35° vers le bas
        if (tresorActuel != null)
        {
            Vector3 dir = tresorActuel.transform.position - fleche.transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                float angleY = Mathf.Atan2(-dir.z, dir.x) * Mathf.Rad2Deg;
                fleche.transform.rotation = Quaternion.Euler(0f, angleY, -35f);
            }
        }

        return fleche;
    }

    private GameObject CreerTransporteur(int ligne, int col)
    {
        GameObject trans = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trans.name = "Transporteur_" + ligne + "_" + col;
        trans.transform.SetParent(parentObjets);
        trans.transform.position = PositionCellule(ligne, col, 0.3f);
        trans.transform.localScale = new Vector3(0.7f, 0.3f, 0.7f); // Large et court

        // Couleur cyan
        Renderer rend = trans.GetComponent<Renderer>();
        if (matTransporteur != null)
            rend.material = matTransporteur;
        else
            rend.material.color = new Color(0f, 0.85f, 1f);

        // Collider en trigger
        CapsuleCollider cap = trans.GetComponent<CapsuleCollider>();
        if (cap != null) cap.isTrigger = true;

        // Ajouter le script Teleporteur
        trans.AddComponent<Teleporteur>();
        trans.tag = "Transporteur";

        return trans;
    }

    private GameObject CreerRecepteur(int ligne, int col)
    {
        GameObject recept = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        recept.name = "Recepteur_" + ligne + "_" + col;
        recept.transform.SetParent(parentObjets);
        recept.transform.position = PositionCellule(ligne, col, 0.5f);
        recept.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); // Étroit et haut

        // Couleur magenta
        Renderer rend = recept.GetComponent<Renderer>();
        if (matRecepteur != null)
            rend.material = matRecepteur;
        else
            rend.material.color = new Color(0.95f, 0.25f, 0.95f);

        // Pas de trigger sur les récepteurs (c'est une destination, pas un déclencheur)
        CapsuleCollider cap = recept.GetComponent<CapsuleCollider>();
        if (cap != null) Destroy(cap);

        recept.tag = "Recepteur";
        recept.AddComponent<Recepteur>();

        return recept;
    }

    // Retourne la liste des récepteurs (utilisé par Teleporteur.cs)
    public List<GameObject> GetRecepteurs()
    {
        return recepteursActuels;
    }
}
