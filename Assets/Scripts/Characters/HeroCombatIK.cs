using UnityEngine;

/// Handles simple IK alignment for the hero right arm.
[RequireComponent(typeof(Animator))]
public class HeroCombatIK : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField, Range(0f, 1f)] float positionWeight = 1f;
    [SerializeField, Range(0f, 1f)] float rotationWeight = 0.5f;

    Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SetTarget(Transform ikTarget)
    {
        target = ikTarget;
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null)
            return;

        if (target == null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            return;
        }

        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, positionWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rotationWeight);
        animator.SetIKPosition(AvatarIKGoal.RightHand, target.position);
        animator.SetIKRotation(AvatarIKGoal.RightHand, target.rotation);
    }
}