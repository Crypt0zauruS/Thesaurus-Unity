using UnityEngine;

// Tresor.cs — Détecte quand le joueur touche le trésor + pulsation + glow texture
// + Particules dorées flottantes + burst d'étoiles à la collection

public class Tresor : MonoBehaviour
{
    private float tempsDepart;
    private Vector3 echelleBase;
    private Material mat;

    // Lumière dorée qui pulse avec le trésor
    private Light lumiereDoree;

    void Start()
    {
        tempsDepart = Time.time;
        echelleBase = transform.localScale;

        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
            mat.EnableKeyword("_EMISSION");

            // Utiliser la texture principale comme map d'émission
            Texture tex = mat.GetTexture("_BaseMap");
            if (tex != null)
                mat.SetTexture("_EmissionMap", tex);
        }

        // Particules dorées qui flottent autour du trésor
        EffetsParticules.CreerEtincelles(transform, new Color(1f, 0.85f, 0.2f));

        // Lumière ponctuelle dorée
        GameObject lumObj = new GameObject("LumiereTresor");
        lumObj.transform.SetParent(transform, false);
        lumObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        lumiereDoree = lumObj.AddComponent<Light>();
        lumiereDoree.type = LightType.Point;
        lumiereDoree.color = new Color(1f, 0.85f, 0.3f);
        lumiereDoree.intensity = 1.5f;
        lumiereDoree.range = 3f;
    }

    void Update()
    {
        // Pulsation d'échelle
        float pulse = 1f + 0.15f * Mathf.Sin((Time.time - tempsDepart) * 3f);
        transform.localScale = echelleBase * pulse;

        // Rotation lente
        transform.Rotate(0f, 60f * Time.deltaTime, 0f);

        // Glow pulsant sur la texture (brillance comme le Projet2)
        if (mat != null)
        {
            float glow = 0.2f + 0.8f * Mathf.Abs(Mathf.Sin((Time.time - tempsDepart) * 2f));
            mat.SetColor("_EmissionColor", new Color(glow, glow * 0.85f, glow * 0.2f));
        }

        // Lumière qui pulse aussi
        if (lumiereDoree != null)
        {
            float intensite = 1f + 0.8f * Mathf.Sin((Time.time - tempsDepart) * 2f);
            lumiereDoree.intensity = intensite;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            // Burst d'étoiles dorées à la position du trésor !
            EffetsParticules.CreerBurst(transform.position, new Color(1f, 0.9f, 0.3f), 50);
            EffetsParticules.CreerBurst(
                transform.position + Vector3.up * 0.3f,
                new Color(1f, 1f, 0.8f), 30
            );

            if (GameManager.instance != null)
                GameManager.instance.PasserAuNiveauSuivant();
        }
    }
}
