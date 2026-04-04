using UnityEngine;
using System;
using UnityEngine.SceneManagement;

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
    //private HealthUI UI;
    private HealthBarUpdater floatingHealthBar;
    private bool isPlayer = false;
    private bool invincible = false;
    private PlayerStats stats;

    private float hitFlashTimer;
    private bool inHitFlash = false;
    private SpriteRenderer sprite;

    public float MaxHealth => stats != null ? stats.MaxHealth : enemyMaxHealth;

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
        if (healthText != null)
        {
            healthText.UpdateHPText(currentHealth, MaxHealth);
        }
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
            /*UI = objectToDisplayHealth.GetComponent<HealthUI>();
            if (UI != null)
            {
                UI.Initialize(this);
            }*/
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
            //sprite.material = defaultMaterial;
            inHitFlash = false;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (invincible) return;
        currentHealth -= damageAmount;

        hitFlashTimer = flashTime;
        //sprite.material = whiteMaterial;
        inHitFlash = true;

        floatingHealthBar?.UpdateHealthBar(currentHealth, MaxHealth);
        if (healthBarUpdater != null)
        {
            healthBarUpdater.UpdateHealthBar(currentHealth, MaxHealth);
        }

        if (healthText != null)
        {
            healthText.UpdateHPText(currentHealth, MaxHealth);
        }

        /*if (UI != null )
        {
            UI.UpdateText();
        }*/

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if(isPlayer)
        {
            invincible = true;

            SimpleTimer timer = new SimpleTimer();
            timer.StartTimer(0.25f, onFinish: () => invincible = false);
        }
    }

    void Die()
    {
        /*if (UI != null && !isPlayer)
        {
            Destroy(UI.gameObject);
        }*/
        if (floatingHealthBar != null && !isPlayer)
        {
            Destroy(floatingHealthBar.transform.parent.gameObject);
        }

        OnDeath?.Invoke();

        if(isPlayer)
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
            //UI.UpdateText();
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
        //UI?.UpdateText();
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
            //UI?.UpdateText();
            if (healthBarUpdater != null)
            {
                healthBarUpdater.UpdateHealthBar(currentHealth, MaxHealth);
            }

        }
    }
}
