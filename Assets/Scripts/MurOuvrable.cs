using UnityEngine;

// MurOuvrable.cs — Animation de descente d'un mur quand il est ouvert
// Ajouté dynamiquement par DedaleGenerator.OuvrirMur()
// + Débris de particules à l'ouverture

public class MurOuvrable : MonoBehaviour
{
    private bool enDescente = false;
    private float vitesseDescente = 3f; // 3 unités/sec (comme le Projet2)

    // Appelé par DedaleGenerator pour démarrer l'animation
    public void Descendre()
    {
        enDescente = true;

        // Particules de débris au moment de l'ouverture
        // Couleur rosée pour les murs ouvrables
        EffetsParticules.CreerDebris(transform.position, new Color(0.8f, 0.4f, 0.35f));
    }

    void Update()
    {
        if (!enDescente) return;

        // Descendre le mur
        Vector3 pos = transform.position;
        pos.y -= vitesseDescente * Time.deltaTime;
        transform.position = pos;

        // Quand le mur est sous le plancher, le détruire
        if (pos.y < -2.5f)
        {
            Destroy(gameObject);
        }
    }
}
