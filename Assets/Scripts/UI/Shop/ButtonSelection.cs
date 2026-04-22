using DG.Tweening;
using UnityEngine;

public class ButtonSelection : MonoBehaviour
{
    [SerializeField] private Transform currentSelectedBtn;


    public void btnSelection(Transform btn)
    {

        if (currentSelectedBtn != null)
        {
            currentSelectedBtn.DOScale(1f, 0.2f);
            if (currentSelectedBtn.childCount > 0)
                currentSelectedBtn.GetChild(0).gameObject.SetActive(false);
        }

        currentSelectedBtn = btn;


        currentSelectedBtn.DOScale(1.25f, 0.15f).OnComplete(() =>
        {
            currentSelectedBtn.DOScale(1.15f, 0.1f);
        });

    }
}