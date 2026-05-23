using UnityEngine;

// EffetsParticules.cs — Crée des systèmes de particules programmatiquement
// Méthodes statiques utilisées par Tresor, Teleporteur, Recepteur, MurOuvrable, etc.

public class EffetsParticules
{
    // Trouve un shader de particules compatible URP (avec fallback)
    private static Material CreerMaterielParticule(Color couleur)
    {
        // Essayer les shaders URP d'abord, puis fallback
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.color = couleur;

        // Activer le mode additif pour un look lumineux
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 1);   // Additive

        return mat;
    }

    // -------------------------------------------------------
    // ÉTINCELLES — particules dorées qui flottent vers le haut
    // Utilisé sur le trésor
    // -------------------------------------------------------
    public static ParticleSystem CreerEtincelles(Transform parent, Color couleur)
    {
        GameObject obj = new GameObject("Etincelles");
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 1.5f;
        main.startSpeed = 0.4f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.maxParticles = 30;
        main.loop = true;
        main.startColor = couleur;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.2f; // Flottent vers le haut

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        // Fondu en sortie
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(couleur, 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(couleur, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Rétrécissent en disparaissant
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Matériau
        ParticleSystemRenderer rend = obj.GetComponent<ParticleSystemRenderer>();
        rend.material = CreerMaterielParticule(couleur);
        rend.renderMode = ParticleSystemRenderMode.Billboard;

        return ps;
    }

    // -------------------------------------------------------
    // BURST — explosion de particules (collection trésor, etc.)
    // Se détruit automatiquement après l'animation
    // -------------------------------------------------------
    public static void CreerBurst(Vector3 position, Color couleur, int nombre)
    {
        GameObject obj = new GameObject("Burst");
        obj.transform.position = position;

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.maxParticles = nombre;
        main.loop = false;
        main.startColor = couleur;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.8f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, (short)nombre)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        // Fondu
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(couleur, 0.3f),
                new GradientColorKey(couleur * 0.5f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        ParticleSystemRenderer rend = obj.GetComponent<ParticleSystemRenderer>();
        rend.material = CreerMaterielParticule(couleur);

        ps.Play();
    }

    // -------------------------------------------------------
    // VORTEX — particules qui tournent en spirale
    // Utilisé sur téléporteurs et récepteurs
    // -------------------------------------------------------
    public static ParticleSystem CreerVortex(Transform parent, Color couleur, float rayon, float vitesseOrbitale)
    {
        GameObject obj = new GameObject("Vortex");
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 2f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.maxParticles = 40;
        main.loop = true;
        main.startColor = couleur;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 18f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = rayon;

        // Mouvement orbital = spirale
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalY = vitesseOrbitale;
        vel.radial = -0.15f; // Attirées vers le centre

        // Fondu
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(couleur, 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(couleur, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.8f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Taille qui pulse
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer rend = obj.GetComponent<ParticleSystemRenderer>();
        rend.material = CreerMaterielParticule(couleur);

        return ps;
    }

    // -------------------------------------------------------
    // FLASH — éclair brillant instantané (téléportation)
    // -------------------------------------------------------
    public static void CreerFlash(Vector3 position, Color couleur)
    {
        GameObject obj = new GameObject("Flash");
        obj.transform.position = position;

        // Lumière flash
        Light flash = obj.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = couleur;
        flash.intensity = 8f;
        flash.range = 5f;

        // Le flash se détruit après 0.3 secondes
        // On utilise un petit MonoBehaviour pour l'animer
        FlashAnim anim = obj.AddComponent<FlashAnim>();
        anim.duree = 0.3f;

        // Particules de flash
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.4f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.maxParticles = 40;
        main.loop = false;
        main.startColor = couleur;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 40)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        ParticleSystemRenderer rend = obj.GetComponent<ParticleSystemRenderer>();
        rend.material = CreerMaterielParticule(couleur);

        ps.Play();
    }

    // -------------------------------------------------------
    // DEBRIS — morceaux qui tombent (ouverture de mur)
    // -------------------------------------------------------
    public static void CreerDebris(Vector3 position, Color couleur)
    {
        GameObject obj = new GameObject("Debris");
        obj.transform.position = position;

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.15f);
        main.maxParticles = 20;
        main.loop = false;
        main.startColor = couleur;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 2f;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 20)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.8f, 1.5f, 0.8f);

        // Fondu
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(couleur, 0f),
                new GradientColorKey(couleur * 0.4f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Rotation pendant la chute
        var rotOverLifetime = ps.rotationOverLifetime;
        rotOverLifetime.enabled = true;
        rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

        ParticleSystemRenderer rend = obj.GetComponent<ParticleSystemRenderer>();
        rend.material = CreerMaterielParticule(couleur);
        rend.renderMode = ParticleSystemRenderMode.Billboard;

        ps.Play();
    }

    // -------------------------------------------------------
    // POUSSIERE AMBIANTE — particules flottantes dans le labyrinthe
    // Attachées à la caméra pour suivre le joueur
    // -------------------------------------------------------
    public static ParticleSystem CreerPoussiere(Transform parent)
    {
        GameObject obj = new GameObject("PoussiereAmbiante");
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.025f);
        main.maxParticles = 100;
        main.loop = true;
        main.startColor = new Color(1f, 0.95f, 0.8f, 0.4f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.01f;

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(6f, 1.8f, 6f);

        // Mouvement lent aléatoire
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.1f;

        // Fondu doux
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0f),
                new GradientColorKey(new Color(1f, 0.95f, 0.8f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.35f, 0.3f),
                new GradientAlphaKey(0.35f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        ParticleSystemRenderer rend = obj.GetComponent<ParticleSystemRenderer>();
        rend.material = CreerMaterielParticule(new Color(1f, 0.95f, 0.8f, 0.4f));

        return ps;
    }
}
