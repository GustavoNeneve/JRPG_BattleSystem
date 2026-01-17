using UnityEngine;
using NewBark.Support;
using DG.Tweening;

public class DustStormTransition : MonoBehaviour
{
    private ParticleSystem particles;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        UniquePersistent uniquePersistent = gameObject.AddComponent<UniquePersistent>();
        uniquePersistent.uniqueID = "dust_storm_transition";
        particles = GetComponent<ParticleSystem>();
    }

    public void PlayParticles()
    {
        particles.Play();
        particles.transform.DOMoveX(0, 7f).SetEase(Ease.OutQuint);
    }
}
