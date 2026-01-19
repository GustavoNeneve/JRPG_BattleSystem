using UnityEngine;

[System.Serializable]
public struct AnimationCycle
{
    public string name;
    public float cycleTime;
}

public class CharacterAnimationController : MonoBehaviour
{
    [Header("ANIMATION PARAMETERS")]
    [SerializeField] Animator myAnim;
    [SerializeField] protected float secondsToReachTarget = .75f;
    public float SecondsToReachTarget => secondsToReachTarget;
    [SerializeField] protected float secondsToGoBack = .45f;
    public float SecondsToGoBack => secondsToGoBack;
    [SerializeField] protected string idleAnimation;
    public string IdleAnimationName => idleAnimation;
    [SerializeField] string deadAnimation;
    public string DeadAnimationName => deadAnimation;
    [SerializeField] Transform projectileSpawnPoint;
    public Transform ProjectileSpawnPoint => projectileSpawnPoint;
    [SerializeField] ParticleSystem healingEffect;

    private void Start()
    {
        EnableAnimator();
    }


    public void PlayAnimation(string animName)
    {
        if (myAnim != null && myAnim.gameObject.activeInHierarchy) myAnim.Play(animName);
    }

    public void EnableAnimator()
    {
        if (myAnim != null) myAnim.enabled = true;
    }

    public void DisableAnimator()
    {
        if (myAnim != null) myAnim.enabled = false;
    }

    public void PlayHealingEffect() => healingEffect.Play();
}


