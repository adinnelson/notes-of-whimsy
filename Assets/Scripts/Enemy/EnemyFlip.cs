using UnityEngine;

public class EnemyFlip : MonoBehaviour
{
    private GameObject player;
    private GameObject Enemy;
    
    private SpriteRenderer enemySpriteRenderer;

    Vector3 playerPos;
    Vector3 enemyPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Enemy = this.gameObject;
        player = GameObject.FindGameObjectWithTag("Player");
        playerPos = player.transform.position;
        enemyPos = Enemy.transform.position;
        enemySpriteRenderer = Enemy.GetComponent<SpriteRenderer>();
       
    }

    ///When telegraphing or attacking, the enemy should face the player. This is called in the animation events of the attack and telegraph animations.
    public void FlipEnemy()
    {
        playerPos = player.transform.position;
        enemyPos = Enemy.transform.position;

        if (playerPos.x > enemyPos.x)
        {
            enemySpriteRenderer.flipX = true;
        }
        else
        {
            enemySpriteRenderer.flipX = false;
        }
    }
}
