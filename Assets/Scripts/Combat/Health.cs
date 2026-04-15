using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using FMODUnity;

public class Health : MonoBehaviour, IDamageable
{
    public event Action OnDeath;

    [SerializeField] private float enemyMaxHealth = 100.0f;
    [SerializeField] private GameObject healthPrefab; // HealthDisplay prefab
    [SerializeField] private HealthBarUpdater healthBarUpdater; // for the player health bar, fixed in UI
    [SerializeField] private HealthText healthText; // assign this in the HealthDisplay prefab
    [SerializeField] private float healthBarVerticalOffset = 1.0f; //how far above player/enemies should a health bar be

    [Header("Hit Flash")]
    [SerializeField] private Material whiteMaterial;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private float flashTime = 0.1f;

    private float currentHealth;
    private HealthBarUpdater floatingHealthBar;
    private bool isPlayer = false;
    private bool invincible = false;
    private PlayerStats stats;

    private float hitFlashTimer;
    private bool inHitFlash = false;
    private SpriteRenderer sprite;

    public float MaxHealth => stats != null ? stats.MaxHealth : enemyMaxHealth;


    [SerializeField] private EventReference damageSound;


    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.OnMaxHealthIncreased += OnMaxHealthIncreased;
        }

        currentHealth = MaxHealth;
        SpawnHealthUI();

        isPlayer = GetComponent<PlayerAttack>() != null;
    }

    void Start()
    {
        if (isPlayer)
        {
            ReconnectLargeUIHPBar();
        }
    }

    public void ReconnectLargeUIHPBar()
    {
        if (!isPlayer)
        {
            return;
        }

        HPBarLargeInUI[] bars = FindObjectsOfType<HPBarLargeInUI>(true);
        foreach (var bar in bars)
        {
            if (!bar.isBossBar)
            {
                healthBarUpdater = bar.healthBarUpdater;
                healthText = bar.healthText;
                break;
            }
        }

        if (healthBarUpdater != null)
        {
            healthBarUpdater.UpdateHealthBar(currentHealth, MaxHealth);
        }

        if (healthText != null)
        {
            healthText.UpdateHPText(currentHealth, MaxHealth);
        }
    }

    public void ReconnectBossUIHPBar()
    {
        HPBarLargeInUI[] bars = FindObjectsOfType<HPBarLargeInUI>(true);
        foreach (var bar in bars)
        {
            if (bar.isBossBar)
            {
                healthBarUpdater = bar.healthBarUpdater;
                healthText = bar.healthText;
                bar.gameObject.SetActive(true);
                break;
            }
        }

        if (healthBarUpdater != null)
        {
            healthBarUpdater.UpdateHealthBar(currentHealth, MaxHealth);
        }

        if (healthText != null)
        {
            healthText.UpdateHPText(currentHealth, MaxHealth);
        }
    }

    public bool HasLargeUIBarReference()
    {
        return healthBarUpdater != null && healthText != null;
    }

    void SpawnHealthUI()
    {
        if (healthPrefab != null)
        {
            GameObject objectToDisplayHealth = Instantiate(healthPrefab, transform);
            objectToDisplayHealth.transform.localPosition = new Vector3(0, healthBarVerticalOffset, 0);

            floatingHealthBar = objectToDisplayHealth.GetComponentInChildren<HealthBarUpdater>();

            if (floatingHealthBar != null)
            {
                floatingHealthBar.UpdateHealthBar(currentHealth, MaxHealth);
            }
        }
    }

    private void FixedUpdate()
    {
        UpdateHitFlash();
    }

    private void UpdateHitFlash()
    {
        hitFlashTimer -= Time.fixedDeltaTime;

        if (hitFlashTimer <= 0 && inHitFlash)
        {
            sprite.material = defaultMaterial;
            inHitFlash = false;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (invincible) return;
        Debug.Log($"{gameObject.name} took {damageAmount} damage.");
        currentHealth -= damageAmount;

        //Play taking damage sound effect
        DamageSoundEffect();

        hitFlashTimer = flashTime;
        inHitFlash = true;
        sprite.material = whiteMaterial;

        floatingHealthBar?.UpdateHealthBar(currentHealth, MaxHealth);
        if (healthBarUpdater != null)
        {
            healthBarUpdater.UpdateHealthBar(currentHealth, MaxHealth);
        }

        if (healthText != null)
        {
            healthText.UpdateHPText(currentHealth, MaxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (isPlayer)
        {
            invincible = true;
            SimpleTimer timer = new SimpleTimer();
            timer.StartTimer(0.25f, onFinish: () => invincible = false);
        }
    }

    void Die()
    {
        if (floatingHealthBar != null && !isPlayer)
        {
            Destroy(floatingHealthBar.transform.parent.gameObject);
        }

        OnDeath?.Invoke();

        if (isPlayer)
        {
            GoldManager.Instance.ResetGold();
            PlayerStats.Instance.ResetStats();
            GetComponent<PlayerActiveSpellsHandler>()?.RemoveAllSpells();

            // TODO: load death scene
            SceneManager.LoadScene("DeathScreen", LoadSceneMode.Single);

            currentHealth = MaxHealth;
            if (healthBarUpdater != null)
            {
                healthBarUpdater.UpdateHealthBar(currentHealth, MaxHealth);
            }
            if (healthText != null)
            {
                healthText.UpdateHPText(currentHealth, MaxHealth);
            }
        }
    }

    private void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnMaxHealthIncreased -= OnMaxHealthIncreased;
        }
    }

    private void OnMaxHealthIncreased(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, MaxHealth);
        floatingHealthBar?.UpdateHealthBar(currentHealth, MaxHealth);

        if (healthBarUpdater != null)
        {
            healthBarUpdater.UpdateHealthBar(currentHealth, MaxHealth);
        }
        if (healthText != null)
        {
            healthText.UpdateHPText(currentHealth, MaxHealth);
        }
    }

    public float CurrentHealth
    {
        get => currentHealth;
        set
        {
            currentHealth = Mathf.Clamp(value, 0.0f, MaxHealth);
            floatingHealthBar?.UpdateHealthBar(currentHealth, MaxHealth);
            if (healthBarUpdater != null)
            {
                healthBarUpdater.UpdateHealthBar(currentHealth, MaxHealth);
            }
            if (healthText != null)
            {
                healthText.UpdateHPText(currentHealth, MaxHealth);
            }
        }
    }



    private void DamageSoundEffect()
    {
        if (!damageSound.IsNull)
        {
            RuntimeManager.PlayOneShot(damageSound);
        }
    }
}
