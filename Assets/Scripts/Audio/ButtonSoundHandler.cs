using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class ButtonSoundHelper : MonoBehaviour, IPointerEnterHandler
{
    public enum ClickSoundType
    {
        ButtonClick,
        PanelOpen,
        PanelClose,
        PowerUp,
        CoinCollect,
        LevelComplete,
        UpgradeSound,
        None
    }

    public enum HoverSoundType
    {
        ButtonHover,
        None
    }

    [SerializeField] private ClickSoundType clickSound = ClickSoundType.ButtonClick;
    [SerializeField] private HoverSoundType hoverSound = HoverSoundType.ButtonHover;

    [Header("Click Feedback")]
    [SerializeField] private bool scalePunchOnClick = true;
    [SerializeField] private float punchScale = 0.92f;
    [SerializeField] private float punchDuration = 0.14f;

    private Vector3 _restScale;

    private void Awake()
    {
        _restScale = transform.localScale;
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (scalePunchOnClick) DoScalePunch();
        PlayClickSound();
    }

    private void DoScalePunch()
    {
        transform.DOKill();
        transform.localScale = _restScale * punchScale;
        transform.DOScale(_restScale, punchDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void PlayClickSound()
    {
        if (AudioManager.Instance == null) return;

        switch (clickSound)
        {
            case ClickSoundType.ButtonClick: AudioManager.Instance.PlayButtonClick(); break;
            case ClickSoundType.PanelOpen: AudioManager.Instance.PlayPanelOpen(); break;
            case ClickSoundType.PanelClose: AudioManager.Instance.PlayPanelClose(); break;
            case ClickSoundType.PowerUp: AudioManager.Instance.PlayPowerUp(); break;
            case ClickSoundType.CoinCollect: AudioManager.Instance.PlayCoinCollect(); break;
            case ClickSoundType.LevelComplete: AudioManager.Instance.PlayLevelComplete(); break;
            case ClickSoundType.UpgradeSound: AudioManager.Instance.PlayUpgradeSound(); break;
            case ClickSoundType.None: break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSound == HoverSoundType.ButtonHover)
            AudioManager.Instance?.PlayButtonHover();
    }
}