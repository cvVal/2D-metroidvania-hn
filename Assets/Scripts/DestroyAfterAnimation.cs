using UnityEngine;

public class DestroyAfterAnimation : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Animator animator = GetComponent<Animator>();
        Destroy(gameObject, animator.GetCurrentAnimatorStateInfo(0).length);
    }
}
