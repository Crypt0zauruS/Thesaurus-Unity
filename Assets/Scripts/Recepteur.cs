using UnityEngine;

// Recepteur.cs — Glow pulsant magenta par texture pour les récepteurs
// + Vortex magenta tournant

public class Recepteur : MonoBehaviour
{
    private float tempsDepart;
    private Material mat;

    void Start()
    {
        tempsDepart = Time.time;

        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
            mat.EnableKeyword("_EMISSION");

            Texture tex = mat.GetTexture("_BaseMap");
            if (tex != null)
                mat.SetTexture("_EmissionMap", tex);
        }

        // Vortex magenta (plus lent que le transporteur, sens inverse)
        EffetsParticules.CreerVortex(transform, new Color(0.95f, 0.25f, 0.95f), 0.2f, -1.5f);
    }

    void Update()
    {
        // Glow pulsant magenta sur la texture
        if (mat != null)
        {
            float glow = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin((Time.time - tempsDepart) * 2f));
            mat.SetColor("_EmissionColor", new Color(glow * 0.95f, glow * 0.25f, glow * 0.95f));
        }
    }
}
