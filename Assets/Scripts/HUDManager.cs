using UnityEngine;
using UnityEngine.UI;

// HUDManager.cs — HUD style Atlantis
// Crée tout le UI programmatiquement au Start()
// Palette : fond bleu-sombre, texte doré, accents teal
// + Fade in/out pour les transitions de niveau

public class HUDManager : MonoBehaviour
{
    // Couleurs Atlantis
    private Color couleurFond = new Color(0.03f, 0.08f, 0.14f, 0.85f);
    private Color couleurBordure = new Color(0.55f, 0.45f, 0.15f, 0.9f);
    private Color couleurOr = new Color(1f, 0.85f, 0.35f);
    private Color couleurTeal = new Color(0.3f, 0.85f, 0.9f);

    // Références internes (créées au Start)
    private Text textNiveau;
    private Text textScore;
    private Text textTempsValeur;
    private Text textOuvreurs;

    // Panneau message central
    private GameObject panneauMessage;
    private Text textMessage;

    // Canvas
    private Canvas canvas;

    // Fade overlay
    private Image fadeOverlay;
    private float fadeTimer = 0f;
    private float fadeDuree = 0f;
    private bool fadeActif = false;
    private bool fadeIn = false; // true = apparition (noir→transparent), false = disparition (transparent→noir)

    void Start()
    {
        CreerCanvas();
        CreerBarreSuperieure();
        CreerIndicateurOuvreurs();
        CreerPanneauMessage();
        CreerFadeOverlay();
    }

    void Update()
    {
        if (GameManager.instance == null) return;

        if (textNiveau != null)
            textNiveau.text = GameManager.instance.niveauActuel + " / 10";

        if (textScore != null)
            textScore.text = Mathf.FloorToInt(GameManager.instance.score).ToString();

        if (textTempsValeur != null)
        {
            int temps = Mathf.Max(0, Mathf.CeilToInt(GameManager.instance.tempsRestant));
            textTempsValeur.text = temps + "s";

            // Rouge quand il reste peu de temps
            if (temps <= 10)
                textTempsValeur.color = new Color(1f, 0.3f, 0.2f);
            else if (temps <= 20)
                textTempsValeur.color = new Color(1f, 0.7f, 0.2f);
            else
                textTempsValeur.color = couleurOr;
        }

        if (textOuvreurs != null)
            textOuvreurs.text = GameManager.instance.ouvreurs.ToString();

        // Animation du fade
        TraiterFade();
    }

    public void AfficherMessage(string message)
    {
        if (panneauMessage != null)
            panneauMessage.SetActive(true);
        if (textMessage != null)
            textMessage.text = message;
    }

    // --- FADE IN/OUT ---

    // Lance un fade out (écran devient noir)
    public void LancerFadeOut(float duree)
    {
        fadeActif = true;
        fadeIn = false;
        fadeDuree = duree;
        fadeTimer = 0f;
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        }
    }

    // Lance un fade in (écran redevient visible)
    public void LancerFadeIn(float duree)
    {
        fadeActif = true;
        fadeIn = true;
        fadeDuree = duree;
        fadeTimer = 0f;
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.color = new Color(0f, 0f, 0f, 1f);
        }
    }

    // Retourne true si le fade est terminé
    public bool FadeTermine()
    {
        return !fadeActif;
    }

    private void TraiterFade()
    {
        if (!fadeActif || fadeOverlay == null) return;

        fadeTimer += Time.deltaTime;
        float ratio = fadeTimer / fadeDuree;

        if (ratio >= 1f)
        {
            ratio = 1f;
            fadeActif = false;

            // Si fade in terminé, cacher l'overlay
            if (fadeIn)
                fadeOverlay.gameObject.SetActive(false);
        }

        float alpha;
        if (fadeIn)
            alpha = 1f - ratio; // Noir → transparent
        else
            alpha = ratio;      // Transparent → noir

        fadeOverlay.color = new Color(0f, 0f, 0f, alpha);
    }

    private void CreerFadeOverlay()
    {
        GameObject obj = CreerPanel("FadeOverlay", canvas.transform);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        fadeOverlay = obj.GetComponent<Image>();
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.raycastTarget = false; // Ne bloque pas les clics

        // Faire un fade in au démarrage du jeu
        obj.SetActive(true);
        LancerFadeIn(1f);
    }

    // ===== CRÉATION DU UI =====

    private void CreerCanvas()
    {
        // Chercher un Canvas existant ou en créer un
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Adapter à la résolution
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Supprimer les anciens éléments UI
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            Destroy(canvas.transform.GetChild(i).gameObject);
    }

    private void CreerBarreSuperieure()
    {
        // === Barre de fond ===
        GameObject barre = CreerPanel("BarreHaut", canvas.transform);
        RectTransform rt = barre.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 85);
        barre.GetComponent<Image>().color = couleurFond;

        // Bordure dorée en bas
        CreerBordureH("BordureBas", barre.transform, false);

        // === NIVEAU (gauche) — padding 10px au-dessus de la bordure ===
        GameObject blocN = CreerBlocTransparent("BlocNiveau", barre.transform,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
            new Vector2(25, 4), new Vector2(220, -10));

        CreerTexte("LblNiv", blocN.transform, "NIVEAU", 15,
            couleurTeal, TextAnchor.UpperLeft, new Vector2(0, -6));
        textNiveau = CreerTexte("ValNiv", blocN.transform, "1 / 10", 28,
            couleurOr, TextAnchor.LowerLeft, new Vector2(0, 8));

        // === TEMPS (centre) ===
        GameObject blocT = CreerBlocTransparent("BlocTemps", barre.transform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f),
            new Vector2(0, 4), new Vector2(200, -10));

        CreerTexte("LblTps", blocT.transform, "TEMPS", 15,
            couleurTeal, TextAnchor.UpperCenter, new Vector2(0, -6));
        textTempsValeur = CreerTexte("ValTps", blocT.transform, "60s", 32,
            couleurOr, TextAnchor.LowerCenter, new Vector2(0, 8));

        // === SCORE (droite) ===
        GameObject blocS = CreerBlocTransparent("BlocScore", barre.transform,
            new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
            new Vector2(-25, 4), new Vector2(220, -10));

        CreerTexte("LblScr", blocS.transform, "SCORE", 15,
            couleurTeal, TextAnchor.UpperRight, new Vector2(0, -6));
        textScore = CreerTexte("ValScr", blocS.transform, "300", 28,
            couleurOr, TextAnchor.LowerRight, new Vector2(0, 8));
    }

    private void CreerIndicateurOuvreurs()
    {
        // Panneau en bas à gauche
        GameObject bloc = CreerPanel("BlocOuvreurs", canvas.transform);
        RectTransform rt = bloc.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(20, 20);
        rt.sizeDelta = new Vector2(175, 65);
        bloc.GetComponent<Image>().color = couleurFond;

        // Bordure dorée en haut
        CreerBordureH("BordureHaut", bloc.transform, true);

        CreerTexte("LblOuv", bloc.transform, "OUVREURS", 13,
            couleurTeal, TextAnchor.UpperLeft, new Vector2(14, -10));
        textOuvreurs = CreerTexte("ValOuv", bloc.transform, "4", 26,
            couleurOr, TextAnchor.LowerLeft, new Vector2(14, 10));
    }

    private void CreerPanneauMessage()
    {
        // Fond semi-transparent plein écran
        panneauMessage = CreerPanel("PanneauMessage", canvas.transform);
        RectTransform rt = panneauMessage.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        panneauMessage.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.1f, 0.7f);

        // Cadre central avec bordure dorée
        GameObject cadre = CreerPanel("CadreMsg", panneauMessage.transform);
        RectTransform rtC = cadre.GetComponent<RectTransform>();
        rtC.anchorMin = new Vector2(0.5f, 0.5f);
        rtC.anchorMax = new Vector2(0.5f, 0.5f);
        rtC.pivot = new Vector2(0.5f, 0.5f);
        rtC.sizeDelta = new Vector2(500, 200);
        cadre.GetComponent<Image>().color = couleurFond;

        // Bordures dorées (haut, bas, gauche, droite)
        CreerBordureH("BH", cadre.transform, true);
        CreerBordureH("BB", cadre.transform, false);
        CreerBordureV("BG", cadre.transform, true);
        CreerBordureV("BD", cadre.transform, false);

        // Texte message
        textMessage = CreerTexte("TxtMsg", cadre.transform, "", 30,
            couleurOr, TextAnchor.MiddleCenter, Vector2.zero);
        RectTransform rtMsg = textMessage.GetComponent<RectTransform>();
        rtMsg.anchorMin = Vector2.zero;
        rtMsg.anchorMax = Vector2.one;
        rtMsg.sizeDelta = new Vector2(-30, -30);
        rtMsg.anchoredPosition = Vector2.zero;

        panneauMessage.SetActive(false);
    }

    // ===== HELPERS =====

    private GameObject CreerPanel(string nom, Transform parent)
    {
        GameObject panel = new GameObject(nom);
        panel.transform.SetParent(parent, false);
        panel.AddComponent<RectTransform>();
        panel.AddComponent<Image>();
        return panel;
    }

    private GameObject CreerBlocTransparent(string nom, Transform parent,
        Vector2 ancMin, Vector2 ancMax, Vector2 pivot,
        Vector2 pos, Vector2 size)
    {
        GameObject bloc = CreerPanel(nom, parent);
        RectTransform rt = bloc.GetComponent<RectTransform>();
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        bloc.GetComponent<Image>().color = Color.clear;
        return bloc;
    }

    private Text CreerTexte(string nom, Transform parent, string contenu,
        int taille, Color couleur, TextAnchor alignement, Vector2 offset)
    {
        GameObject obj = new GameObject(nom);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = offset;

        Text txt = obj.AddComponent<Text>();
        txt.text = contenu;
        txt.fontSize = taille;
        txt.color = couleur;
        txt.alignment = alignement;
        txt.font = Font.CreateDynamicFontFromOSFont("Arial", taille);
        txt.fontStyle = FontStyle.Bold;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        return txt;
    }

    private void CreerBordureH(string nom, Transform parent, bool enHaut)
    {
        GameObject obj = CreerPanel(nom, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        float y = enHaut ? 1 : 0;
        rt.anchorMin = new Vector2(0, y);
        rt.anchorMax = new Vector2(1, y);
        rt.pivot = new Vector2(0.5f, y);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 2);
        obj.GetComponent<Image>().color = couleurBordure;
    }

    private void CreerBordureV(string nom, Transform parent, bool aGauche)
    {
        GameObject obj = CreerPanel(nom, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        float x = aGauche ? 0 : 1;
        rt.anchorMin = new Vector2(x, 0);
        rt.anchorMax = new Vector2(x, 1);
        rt.pivot = new Vector2(x, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(2, 0);
        obj.GetComponent<Image>().color = couleurBordure;
    }
}
