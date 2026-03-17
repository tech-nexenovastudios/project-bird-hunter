using System;
using UnityEngine;
using UnityEngine.UI;

public class ManaManager : MonoBehaviour
{
    public float manaValue;
    [SerializeField] float manaFillRate;

    public static ManaManager instance;

    public static Action<float> IncreaseManaFillRate;

    [SerializeField] Image manaSlider;
    public float maxManaValue;

    public Button manaPowerUseBtn;

    [SerializeField] GameObject Player;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }


    private void OnEnable()
    {
        IncreaseManaFillRate += IncreaseManaFill;
        //GameManager.LevelEnd += ResetMana;
    }

    private void OnDisable()
    {
        IncreaseManaFillRate += IncreaseManaFill;
        //GameManager.LevelEnd -= ResetMana;


    }
    private void Start()
    {
        InitialCall();
    }
    //That function call after the 
    private void InitialCall()
    {
        manaPowerUseBtn.onClick.RemoveAllListeners();
        manaPowerUseBtn.onClick.AddListener(() =>
        {
            // AbilityManager.instance.activeAbility.UsePower(Player);

        });
        InvokeRepeating(nameof(IncreaseMana), 1f, 1f);
        IncreaseManaFillRate?.Invoke(3f);
    }

    private void IncreaseMana()
    {
        manaValue += manaFillRate;
        manaSlider.fillAmount = manaValue / maxManaValue;
    }

    private void IncreaseManaFill(float fillRate)
    {
        manaFillRate = fillRate;
    }

    public void ActiveManaPowerBtn()
    {
        // if (AbilityManager.instance.activeAbility != null)
        // {
        //     //manaPowerUseBtn.gameObject.SetActive(true);
        // }
    }

    public void ResetMana()
    {
        manaValue = 0;
    }
}
