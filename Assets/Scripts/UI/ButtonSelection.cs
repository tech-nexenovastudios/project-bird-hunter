using DG.Tweening;
using UnityEngine;

public class ButtonSelection : MonoBehaviour
{
   // Transform prevBtnSelection;
    [Tooltip("Put Default selected button here")]
    [SerializeField]Transform currentSelectedBtn;

    public void btnSelection(Transform btn)
    {
        if (currentSelectedBtn != null)
        {
            currentSelectedBtn.DOScale(1f, 0.2f);
            currentSelectedBtn.transform.GetChild(0).gameObject.SetActive(false);
        }

        currentSelectedBtn = btn;
        currentSelectedBtn.transform.DOScale(1.3f, 0.2f);
        currentSelectedBtn.transform.GetChild(0).gameObject.SetActive(true);

    }
}
