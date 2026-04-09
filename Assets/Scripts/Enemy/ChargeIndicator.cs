using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;
public class ChargeIndicator : MonoBehaviour
{
    private static ObjectPool<GameObject> indicatiorSectionPool = new ObjectPool<GameObject>(
            createFunc: CreateItem,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyItem,
            collectionCheck: true,   // helps catch double-release mistakes
            defaultCapacity: 10,
            maxSize: 50
        );

    [SerializeField] private List<Sprite> sprites = new List<Sprite>();
    private const float indicatorScale = 0.5f;


    List<GameObject> indicatorGraphics = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init(Vector2 start, Vector2 end)
    {
        GameObject startIndicator = indicatiorSectionPool.Get();
        startIndicator.transform.position = new Vector3(start.x, start.y, 5);
        startIndicator.transform.rotation = Quaternion.identity;

        indicatorGraphics.Add(startIndicator);

        Vector2 d = end - start;

        SpriteRenderer spriteRenderer = startIndicator.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprites[0];

        GameObject prevIndicatorSection = startIndicator;
        
        int currentSpriteIndex = 1;
        for (int i = 1; i < d.magnitude / indicatorScale; i++)
        {
            // Calculate interpolation factor (0.0 to 1.0)
            float t = (float)i / (d.magnitude - 1);
            
            Vector3 spawnPos = (Vector3)start + new Vector3((indicatorScale - 0.05f) * i, 0, 5);
            
            GameObject currentIndicatorSection = indicatiorSectionPool.Get();
            currentIndicatorSection.transform.position = spawnPos;
            currentIndicatorSection.transform.rotation = Quaternion.identity;

            spriteRenderer = currentIndicatorSection.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprites[currentSpriteIndex];

            currentIndicatorSection.transform.SetParent(prevIndicatorSection.transform);

            prevIndicatorSection = currentIndicatorSection;

            indicatorGraphics.Add(currentIndicatorSection);

            currentSpriteIndex++;
            if(currentSpriteIndex > sprites.Count - 1)
            {
                currentSpriteIndex = 0;
            }
        }

        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        startIndicator.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void ReleaseIndicator()
    {
        foreach(GameObject indicatiorSection in indicatorGraphics)
        {
            indicatiorSection.transform.SetParent(null);
        }

        foreach(GameObject indicatiorSection in indicatorGraphics)
        {
            indicatiorSectionPool.Release(indicatiorSection);
        }

        indicatorGraphics.Clear();
    }

    private static GameObject CreateItem()
    {
        GameObject section = new GameObject("IndicatorSection");
        section.transform.localScale = new Vector3(indicatorScale, indicatorScale, 1);
        section.AddComponent<SpriteRenderer>();
        DontDestroyOnLoad(section);

        section.SetActive(false);
        return section;
    }

    // Called when an item is taken from the pool.
    private static void OnGet(GameObject gameObject)
    {
        gameObject.SetActive(true);
    }

    // Called when an item is returned to the pool.
    private static void OnRelease(GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    // Called when the pool decides to destroy an item (e.g., above max size).
    private static void OnDestroyItem(GameObject gameObject)
    {
        Destroy(gameObject);
    }
}
