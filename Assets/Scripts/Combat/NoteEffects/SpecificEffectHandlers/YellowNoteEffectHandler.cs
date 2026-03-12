using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;

public class YellowNoteEffectHandler : NoteEffectHandler
{
    // Set values used for calculations
    private float detectionRadius = 3.5f;
    private float damage = 25.0f;
    private float cooldown = 5.0f;
    private float timeBetweenTargets = 0.1f;
    private float stunTime = 5.0f;
    private string stunKey = "yellow";

    // flags and tracking variables
    private float timeElapsed = 0.0f;
    private bool onCooldown = false;
    private bool lightingEffectInProgress = false;

    // For tracking stunned enemies
    private Dictionary<EnemyBase, float> stunnedEnemies = new Dictionary<EnemyBase, float>();
    private HashSet<EnemyBase> storedEnemies = new HashSet<EnemyBase>();
    
    // For lightning chain order
    private List<EnemyBase> enemies = new List<EnemyBase>();

    // layer mask for effect
    private LayerMask enemyMask;

    // Temp Lighning effects
    private LightningVisualLogic lightningEffect;
    private ObjectPool<LightningVisualLogic> lightningChainPool;
    private List<LightningVisualLogic> currentVisibleLightningChains = new List<LightningVisualLogic>();

    void FixedUpdate() 
    {
        UpdateAffectedEnemies();

        if (!lightingEffectInProgress) 
        {
            //UpdateCooldown();

            return;
        }

        if(enemies.Count <= 0)
        {
            DisableEffect();

            return;
        }


        if(timeElapsed >= timeBetweenTargets)
        {
            ChainToNextTarget();
        }
        else
        {
            UpdateCurrentChain();
        }

        timeElapsed +=  Time.fixedDeltaTime;
    }

    public void CustomYellowInit(LightningVisualLogic lightningEffect, LayerMask enemyMask)
    {
        this.lightningEffect = lightningEffect;
        this.enemyMask = enemyMask;

        lightningChainPool = new ObjectPool<LightningVisualLogic>(
            createFunc: CreateItem,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyItem,
            collectionCheck: true,   // helps catch double-release mistakes
            defaultCapacity: 10,
            maxSize: 50
        );
    }

    public override void Init(SpellDataSO spellData, PlayerAttack playerAttack)
    {
        this.spellData = spellData;
        this.playerAttack = playerAttack;
        this.note = spellData.NoteProjectilePrefab;
    }

    // shoot projectile
    public override void Fire()
    {
        //if (onCooldown) return;

        
        note.SetOwner(this.gameObject);
        playerAttack.FireProjectile(note, this, this.gameObject);
        onCooldown = true;
    }

    // On enemy hit, enables effect and sets up initial values
    public override void HitEnemy(IDamageable damageable)
    {
        enemies.Clear();
        Health enemyHealth = (Health)damageable;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(enemyHealth.transform.position, detectionRadius, enemyMask);

        foreach(Collider2D collider in hitColliders)
        {
            if(collider.gameObject == enemyHealth.gameObject) continue;

            EnemyBase enemy = collider.gameObject.GetComponent<EnemyBase>();

            if(enemy == null) continue;

            enemies.Add(collider.gameObject.GetComponent<EnemyBase>());

        }

        enemies = enemies.OrderBy(enemy => Vector3.Distance(enemyHealth.transform.position, enemy.transform.position)).ToList();
        enemies.Insert(0, enemyHealth.gameObject.GetComponent<EnemyBase>());

        LightningVisualLogic lightningChain = lightningChainPool.Get();
        lightningChain.transform.position = enemies[0].transform.position;
        lightningChain.SetWorldPoint(enemies[0].transform.position);
        currentVisibleLightningChains.Add(lightningChain);

        StunEnemy(enemies[0]);
        enemies[0].GetComponent<Health>().TakeDamage(damage);
        enemies.RemoveAt(0);

        lightingEffectInProgress = true;
        timeElapsed = 0f;
    }

    // stuns enemies or adds to stunTime to stun effect
    private void StunEnemy(EnemyBase enemy)
    {
        if(storedEnemies.Contains(enemy))
        {
            stunnedEnemies[enemy] += stunTime;
            //enemy.gameObject.transform.localScale = new Vector3(2, 2, 2);
            enemy.AddStunEffect(stunKey);
            return;
        }

        stunnedEnemies.Add(enemy, stunTime);
        storedEnemies.Add(enemy);
        //enemy.gameObject.transform.localScale = new Vector3(2, 2, 2);
        enemy.AddStunEffect(stunKey);
    }

    // updates cooldown
    private void UpdateCooldown()
    {
        if(!onCooldown) return;

        if(timeElapsed >= cooldown)
        {
            onCooldown = false;
            timeElapsed = 0.0f;
            return;
        }

        timeElapsed += Time.fixedDeltaTime;
    }

    // updates stun time on affected enemies
    private void UpdateAffectedEnemies()
    {
        foreach(EnemyBase enemy in storedEnemies)
        {
            if(stunnedEnemies[enemy] <= 0) continue;

            stunnedEnemies[enemy] = stunnedEnemies[enemy] - Time.fixedDeltaTime;

            if(stunnedEnemies[enemy] <= 0)
            {
                enemy.RemoveStunEffect(stunKey);
                enemy.gameObject.transform.localScale = new Vector3(1, 1, 1);
            }
        }
    }

    // updates chain to next target
    private void UpdateCurrentChain()
    {
        LightningVisualLogic curLightning = currentVisibleLightningChains[currentVisibleLightningChains.Count - 1];
        Vector3 dir = enemies[0].transform.position - curLightning.transform.position;
        currentVisibleLightningChains[currentVisibleLightningChains.Count - 1].SetWorldPoint(curLightning.transform.position + timeElapsed / timeBetweenTargets * dir);
    }

    // deals damages to finished chain and sets up next chain
    private void ChainToNextTarget()
    {
        enemies[0].GetComponent<Health>().TakeDamage(damage);
            
        EnemyBase enemySource = enemies[0];

        StunEnemy(enemySource);

        enemies.RemoveAt(0);
        timeElapsed = 0;

        if(enemies.Count <= 0) return;

        LightningVisualLogic lightningChain = lightningChainPool.Get();
        lightningChain.transform.position = enemySource.transform.position;
        lightningChain.SetWorldPoint(enemySource.transform.position);
        currentVisibleLightningChains.Add(lightningChain);
        timeElapsed = 0f;
    }


    // diables and returns lightning effects
    private void DisableEffect()
    {
        lightingEffectInProgress = false;

        for (int i = 0;i < currentVisibleLightningChains.Count;i++)
        {
            lightningChainPool.Release(currentVisibleLightningChains[i]);
        }

        currentVisibleLightningChains.Clear();
    }

    // Creates a new pooled GameObject the first time (and whenever the pool needs more).
    private LightningVisualLogic CreateItem()
    {
        LightningVisualLogic lightningChain = Instantiate(lightningEffect, Vector2.zero, Quaternion.identity);
        lightningChain.gameObject.SetActive(false);

        return lightningChain;
    }

    // Called when an item is taken from the pool.
    private void OnGet(LightningVisualLogic lightningChain)
    {
        lightningChain.gameObject.SetActive(true);
    }

    // Called when an item is returned to the pool.
    private void OnRelease(LightningVisualLogic lightningChain)
    {
        lightningChain.gameObject.SetActive(false);
    }

    // Called when the pool decides to destroy an item (e.g., above max size).
    private void OnDestroyItem(LightningVisualLogic lightningChain)
    {
        Destroy(lightningChain);
    }

}
