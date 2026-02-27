using UnityEngine;

public class AccountsManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   [SerializeField] private string contactUsUrl = "https://nexenovastudios.com/contact";


    public void OpenContactUs()
    {
        // Basic validation to ensure the URL isn't empty
        if (string.IsNullOrEmpty(contactUsUrl))
        {
            Debug.LogError("Terms URL is empty! Please set it in the TermsManager inspector.");
            return;
        }

        Debug.Log($"Opening Terms & Conditions: {contactUsUrl}");
        
        // Opens the URL in the external browser (outside the Unity app)
        Application.OpenURL(contactUsUrl);
    }
}
