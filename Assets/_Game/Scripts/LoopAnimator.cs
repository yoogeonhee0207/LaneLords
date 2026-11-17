using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>
    /// Keeps a one-shot Tiny Swords animation (e.g. a tree swaying) looping,
    /// starting at a random point so neighbouring objects don't move in sync.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class LoopAnimator : MonoBehaviour
    {
        Animator anim;
        bool started;

        void Awake()
        {
            anim = GetComponent<Animator>();
        }

        void Update()
        {
            if (anim == null || anim.runtimeAnimatorController == null) return;
            var state = anim.GetCurrentAnimatorStateInfo(0);
            if (!started)
            {
                started = true;
                anim.Play(state.fullPathHash, 0, Random.value);
                return;
            }
            if (state.normalizedTime >= 1f) anim.Play(state.fullPathHash, 0, 0f);
        }
    }
}
