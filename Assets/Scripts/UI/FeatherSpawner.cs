using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FeatherSpawnerUI : MonoBehaviour
{
    [Header("Feather Types")]
    public FeatherType[] featherTypes;

    [Header("Spawn Settings")]
    public float spawnInterval = 0.4f;
    public Vector2 sizeRange = new Vector2(40f, 70f);

    [Header("Movement (shared)")]
    public Vector2 swayAmountRange = new Vector2(30f, 80f);
    public Vector2 swayFrequencyRange = new Vector2(0.5f, 2f);
    public Vector2 rotationSpeedRange = new Vector2(-90f, 90f);

    [Header("Pooling")]
    [Tooltip("Feathers created up-front. The pool grows on demand if exhausted, but pre-warming avoids first-spawn allocations on the main menu.")]
    public int prewarmCount = 8;

    RectTransform rt;
    readonly Stack<FeatherUI> _pool = new();

    void Start()
    {
        rt = GetComponent<RectTransform>();

        for (int i = 0; i < prewarmCount; i++)
        {
            var feather = CreateFeather();
            feather.gameObject.SetActive(false);
            _pool.Push(feather);
        }

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            SpawnFeather();
            yield return wait;
        }
    }

    void SpawnFeather()
    {
        if (featherTypes == null || featherTypes.Length == 0) return;

        FeatherType type = featherTypes[Random.Range(0, featherTypes.Length)];
        if (type.sprite == null) return;

        FeatherUI f = _pool.Count > 0 ? _pool.Pop() : CreateFeather();

        float size = Random.Range(sizeRange.x, sizeRange.y);
        float halfW = rt.rect.width * 0.5f;
        float halfH = rt.rect.height * 0.5f;
        Vector2 startPos = new Vector2(Random.Range(-halfW, halfW), halfH + size);

        f.Launch(
            sprite: type.sprite,
            size: size,
            startPos: startPos,
            rotationDegrees: Random.Range(0f, 360f),
            fallSpeed: Random.Range(type.fallSpeedRange.x, type.fallSpeedRange.y),
            swayAmount: Random.Range(swayAmountRange.x, swayAmountRange.y),
            swayFrequency: Random.Range(swayFrequencyRange.x, swayFrequencyRange.y),
            rotationSpeed: Random.Range(rotationSpeedRange.x, rotationSpeedRange.y),
            despawnY: -halfH - size
        );
    }

    FeatherUI CreateFeather()
    {
        GameObject go = new GameObject("Feather", typeof(RectTransform), typeof(Image));
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(rt, false);

        Image img = go.GetComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;

        FeatherUI f = go.AddComponent<FeatherUI>();
        f.Bind(this, r, img);
        return f;
    }

    public void Recycle(FeatherUI feather)
    {
        if (feather == null) return;
        feather.gameObject.SetActive(false);
        _pool.Push(feather);
    }
}

public class FeatherUI : MonoBehaviour
{
    FeatherSpawnerUI _owner;
    RectTransform _rt;
    Image _img;

    float _fallSpeed;
    float _swayAmount;
    float _swayFrequency;
    float _rotationSpeed;
    float _despawnY;
    float _startX;
    float _seed;

    // Wired once by the spawner so Update doesn't have to GetComponent.
    public void Bind(FeatherSpawnerUI owner, RectTransform rt, Image img)
    {
        _owner = owner;
        _rt = rt;
        _img = img;
    }

    public void Launch(
        Sprite sprite,
        float size,
        Vector2 startPos,
        float rotationDegrees,
        float fallSpeed,
        float swayAmount,
        float swayFrequency,
        float rotationSpeed,
        float despawnY)
    {
        _img.sprite = sprite;
        _rt.sizeDelta = new Vector2(size, size);
        _rt.anchoredPosition = startPos;
        _rt.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);

        _fallSpeed = fallSpeed;
        _swayAmount = swayAmount;
        _swayFrequency = swayFrequency;
        _rotationSpeed = rotationSpeed;
        _despawnY = despawnY;

        _startX = startPos.x;
        _seed = Random.Range(0f, 100f);

        gameObject.SetActive(true);
    }

    void Update()
    {
        Vector2 p = _rt.anchoredPosition;
        p.y -= _fallSpeed * Time.deltaTime;
        p.x = _startX + Mathf.Sin((Time.time + _seed) * _swayFrequency) * _swayAmount;
        _rt.anchoredPosition = p;
        _rt.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime);

        if (p.y < _despawnY)
            _owner.Recycle(this);
    }
}

[System.Serializable]
public class FeatherType
{
    public Sprite sprite;
    public Vector2 fallSpeedRange = new Vector2(80f, 160f);
}
