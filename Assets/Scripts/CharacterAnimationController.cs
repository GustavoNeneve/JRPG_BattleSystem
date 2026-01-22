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

    public void SetAnimatorController(RuntimeAnimatorController controller)
    {
        Debug.Log($"[CharAnimCtrl] Setting Controller: {controller.name}");
        if (myAnim != null)
        {
            myAnim.runtimeAnimatorController = controller;
            Debug.Log($"[CharAnimCtrl] Controller assigned successfully to Animator on {myAnim.gameObject.name}");
        }
        else
        {
             Debug.LogError("[CharAnimCtrl] myAnim is NULL! Cannot assign controller.");
        }
    }
}


