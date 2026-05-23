using UnityEngine;
using System.Collections.Generic;

// Teleporteur.cs — Téléporte le joueur vers un récepteur aléatoire + glow texture
// + Vortex cyan tournant + flash de téléportation

public class Teleporteur : MonoBehaviour
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

        // Vortex cyan tournant autour du transporteur
        EffetsParticules.CreerVortex(transform, new Color(0f, 0.9f, 1f), 0.3f, 2f);
    }

    void Update()
    {
        // Glow pulsant cyan sur la texture
        if (mat != null)
        {
            float glow = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin((Time.time - tempsDepart) * 2.5f));
            mat.SetColor("_EmissionColor", new Color(0f, glow * 0.85f, glow));
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController joueur = other.GetComponent<PlayerController>();
        if (joueur == null) return;

        DedaleGenerator generateur = FindObjectOfType<DedaleGenerator>();
        if (generateur == null) return;

        List<GameObject> recepteurs = generateur.GetRecepteurs();
        if (recepteurs.Count == 0) return;

        int index = Random.Range(0, recepteurs.Count);
        GameObject recepteur = recepteurs[index];
        if (recepteur == null) return;

        Vector3 direction = joueur.transform.forward;

        // Flash cyan au départ !
        EffetsParticules.CreerFlash(transform.position, new Color(0f, 0.9f, 1f));

        CharacterController controller = joueur.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        Vector3 destination = recepteur.transform.position;
        destination.y = joueur.transform.position.y;
        joueur.transform.position = destination;
        joueur.transform.forward = direction;

        if (controller != null) controller.enabled = true;

        // Flash cyan à l'arrivée !
        EffetsParticules.CreerFlash(destination, new Color(0.5f, 0.2f, 1f));

        if (AudioManager.instance != null)
            AudioManager.instance.Jouer("teleportation");

        Debug.Log("Téléporté vers récepteur à " + destination);
    }
}
