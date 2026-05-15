using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EggLoadingBar : MonoBehaviour
{
    [Header("References")]
    public Image eggFillImage;        // the cracked yellow egg

    [Header("Settings")]
    [Range(0.1f, 5f)]
    public float fillSpeed = 1f;      // how fast it fills 0→1
    public bool loop = true;          // keep filling repeatedly until told to stop

    private Coroutine fillRoutine;
    private bool isLoading = false;

    void Awake()
    {
        if (eggFillImage != null)
        {
            eggFillImage.type = Image.Type.Filled;
            eggFillImage.fillMethod = Image.FillMethod.Vertical;
            eggFillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
            eggFillImage.fillAmount = 0f;
        }
    }

    void OnEnable()
    {
        if (eggFillImage != null) eggFillImage.fillAmount = 0f;
        StartLoading();
    }

    void OnDisable()
    {
        CompleteLoading();
    }

    /// <summary>
    /// Start the looping crack/fill animation (use while loading is in progress).
    /// </summary>
    public void StartLoading()
    {
        if (isLoading) return;
        isLoading = true;
        fillRoutine = StartCoroutine(FillLoop());
    }

    /// <summary>
    /// Stop the animation and snap the egg to fully filled.
    /// Call this once your real loading is done.
    /// </summary>
    public void CompleteLoading()
    {
        isLoading = false;
        if (fillRoutine != null) StopCoroutine(fillRoutine);
        if (eggFillImage != null) eggFillImage.fillAmount = 1f;
    }

    /// <summary>
    /// Optional: drive the fill directly from a real progress value (0–1).
    /// Use this instead of StartLoading if you have actual load progress.
    /// </summary>
    public void SetProgress(float progress01)
    {
        if (eggFillImage != null)
            eggFillImage.fillAmount = Mathf.Clamp01(progress01);
    }

    private IEnumerator FillLoop()
    {
        while (isLoading)
        {
            // Fill from 0 to 1
            float t = 0f;
            while (t < 1f && isLoading)
            {
                t += Time.deltaTime * fillSpeed;
                eggFillImage.fillAmount = t;
                yield return null;
            }

            if (!loop) break;

            // Small pause at full, then reset and loop
            yield return new WaitForSeconds(0.2f);
            eggFillImage.fillAmount = 0f;
        }
    }
}