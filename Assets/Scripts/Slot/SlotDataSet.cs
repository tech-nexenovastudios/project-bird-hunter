using System.Collections;
using Gameplay.Managers;
 using UnityEngine;
using UnityEngine.UI;

public class SlotDataSet : MonoBehaviour
{

    //[HideInInspector] public SlotPowerHolder_SO powerUp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    


    public void HighlightResultWithAnimation(int index)
    {
        // if (index >= slotElements.Length) return;
        var itemContainer = transform;

        // Get the result button
        Transform resultTransform = transform.GetChild(index);
        Debug.Log(resultTransform.name);
        Button resultButton = resultTransform.GetComponent<Button>();

        var power = GameProgressManager.Instance.allPowerups;
        for (int i = 0; i < itemContainer.childCount; i++)
        {
            if (i != index)
            {

                var objicon = itemContainer.GetChild(i).gameObject.AddComponent<SlotIcon>();
                
                objicon.SetIcon(power[Random.Range(0, power.Length)]);
            }
        }

        var icon = resultTransform.GetComponent<SlotIcon>();
        icon.SetIcon(power[index]);

        // Enable button interaction
        if (resultButton != null)
        {
            resultButton.interactable = true;
            resultButton.onClick.RemoveAllListeners();
            resultButton.onClick.AddListener(() =>
            {
                icon.ApplyPowerUp();
            });
            resultButton.onClick.AddListener(() =>
            {
                OnIconSelected();
            });
        }

        // Show descriptions with animations
        //  StartCoroutine(ShowDescriptionsWithAnimation(index));
    }

    private void OnIconSelected()
    {
        // Trigger selection effect
        //  StartCoroutine(SelectionEffect());

        // Notify controller
        FindAnyObjectByType<SlotMachineController>().OnUserSelectsIcon();
    }

/*    private IEnumerator SelectionEffect()
    {
        // Scale up selected button
        Transform selectedButton = transform.GetChild(resultIndex);
        Vector3 originalScale = selectedButton.localScale;

        for (float t = 0; t <= 1; t += Time.deltaTime * 4f)
        {
            selectedButton.localScale = Vector3.Lerp(originalScale, originalScale * 1.3f, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        for (float t = 0; t <= 1; t += Time.deltaTime * 4f)
        {
            selectedButton.localScale = Vector3.Lerp(originalScale * 1.3f, originalScale, t);
            yield return null;
        }
    }*/
}
