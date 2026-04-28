using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FeatherSpawnerUI : MonoBehaviour
{
    [Header("Feather Sprites")]
    public Sprite[] featherSprites;

    [Header("Spawn Settings")]
    public float spawnInterval = 0.4f;
    public Vector2 sizeRange = new Vector2(40f, 70f);

    [Header("Movement")]
    public Vector2 fallSpeedRange = new Vector2(80f, 160f);
    public Vector2 swayAmountRange = new Vector2(30f, 80f);
    public Vector2 swayFrequencyRange = new Vector2(0.5f, 2f);
    public Vector2 rotationSpeedRange = new Vector2(-90f, 90f);

    RectTransform rt;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnFeather();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnFeather()
    {
        if (featherSprites == null || featherSprites.Length == 0) return;

        GameObject go = new GameObject("Feather", typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(rt, false);

        float size = Random.Range(sizeRange.x, sizeRange.y);
        r.sizeDelta = new Vector2(size, size);

        float halfW = rt.rect.width * 0.5f;
        float halfH = rt.rect.height * 0.5f;
        r.anchoredPosition = new Vector2(Random.Range(-halfW, halfW), halfH + size);
        r.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        Image img = go.GetComponent<Image>();
        img.sprite = featherSprites[Random.Range(0, featherSprites.Length)];
        img.preserveAspect = true;
        img.raycastTarget = false;

        FeatherUI f = go.AddComponent<FeatherUI>();
        f.fallSpeed = Random.Range(fallSpeedRange.x, fallSpeedRange.y);
        f.swayAmount = Random.Range(swayAmountRange.x, swayAmountRange.y);
        f.swayFrequency = Random.Range(swayFrequencyRange.x, swayFrequencyRange.y);
        f.rotationSpeed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);
        f.despawnY = -halfH - size;
    }
}

public class FeatherUI : MonoBehaviour
{
    public float fallSpeed;
    public float swayAmount;
    public float swayFrequency;
    public float rotationSpeed;
    public float despawnY;

    RectTransform r;
    float startX;
    float seed;

    void Start()
    {
        r = GetComponent<RectTransform>();
        startX = r.anchoredPosition.x;
        seed = Random.Range(0f, 100f);
    }

    void Update()
    {
        Vector2 p = r.anchoredPosition;
        p.y -= fallSpeed * Time.deltaTime;
        p.x = startX + Mathf.Sin((Time.time + seed) * swayFrequency) * swayAmount;
        r.anchoredPosition = p;

        r.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        if (p.y < despawnY) Destroy(gameObject);
    }
}