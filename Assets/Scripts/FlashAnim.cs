using UnityEngine;

// FlashAnim.cs — Petit script qui fait diminuer une lumière flash puis détruit l'objet
// Créé automatiquement par EffetsParticules.CreerFlash()

public class FlashAnim : MonoBehaviour
{
    public float duree = 0.3f;
    private float timer = 0f;
    private Light lumiere;
    private float intensiteInitiale;

    void Start()
    {
        lumiere = GetComponent<Light>();
        if (lumiere != null)
            intensiteInitiale = lumiere.intensity;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Diminuer l'intensité de la lumière
        if (lumiere != null)
        {
            float ratio = 1f - (timer / duree);
            lumiere.intensity = intensiteInitiale * ratio;
        }

        // Détruire quand le temps est écoulé
        if (timer >= duree)
            Destroy(gameObject);
    }
}
