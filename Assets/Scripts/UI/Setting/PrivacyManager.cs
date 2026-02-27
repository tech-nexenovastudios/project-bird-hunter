using UnityEngine;

public class PrivacyManager : MonoBehaviour
{
    [Header("Configuration")]
  
    [SerializeField] private string privacyPolicyUrl = "https://www.google.com";


    public void OpenPolicy()
    {
        // Basic validation to ensure the URL isn't empty
        if (string.IsNullOrEmpty(privacyPolicyUrl))
        {
            Debug.LogError("Privacy Policy URL is empty! Please set it in the PrivacyManager inspector.");
            return;
        }

        Debug.Log($"Opening external link: {privacyPolicyUrl}");
        
        // Application.OpenURL works on Android, iOS, and Desktop.
        // On mobile, this pushes the app to the background and opens the browser.
        Application.OpenURL(privacyPolicyUrl);
    }
}