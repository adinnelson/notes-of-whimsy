using UnityEngine;

public class GenericBeam : MonoBehaviour
{
    
    private Sprite sprite0;
    private Sprite sprite1;

    private SpriteRenderer spriteRenderer;

    private float timeElapsed = 0.0f;

    bool enabled = false;

    void FixedUpdate()
    {
        if(!enabled)
        {
            return;
        }

        if(timeElapsed <= 0.15f)
        {
            timeElapsed += Time.fixedDeltaTime;
            return;
        }

        if(spriteRenderer?.sprite == sprite0)
        {
            spriteRenderer.sprite = sprite1;
        }
        else
        {
            spriteRenderer.sprite = sprite0;
        }

        timeElapsed = 0.0f;
    }

    public void Init(Sprite sprite0, Sprite sprite1)
    {
        this.sprite0 = sprite0;
        this.sprite1 = sprite1;

        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite0;

        enabled = true;
    }

    public void Disable()
    {
        enabled = false;
    }
}
