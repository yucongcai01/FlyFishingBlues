using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkScript : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }
    }

    private void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");

        transform.position += Vector3.right * moveX * moveSpeed * Time.deltaTime;
        animator.SetFloat("Speed", Mathf.Abs(moveX));

        if (moveX < -0.01f)
        {
            SetFlip(true);
        }
        else if (moveX > 0.01f)
        {
            SetFlip(false);
        }
    }

    private void SetFlip(bool isFacingLeft)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].flipX = isFacingLeft;
            }
        }
    }

}
