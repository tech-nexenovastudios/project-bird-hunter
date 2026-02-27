using UnityEngine;

public class TermsManager : MonoBehaviour
{

    [SerializeField] private string termsUrl = "https://www.example.com/terms";


    public void OpenTerms()
    {
        // Basic validation to ensure the URL isn't empty
        if (string.IsNullOrEmpty(termsUrl))
        {
            Debug.LogError("Terms URL is empty! Please set it in the TermsManager inspector.");
            return;
        }

        Debug.Log($"Opening Terms & Conditions: {termsUrl}");
        
        // Opens the URL in the external browser (outside the Unity app)
        Application.OpenURL(termsUrl);
    }
}