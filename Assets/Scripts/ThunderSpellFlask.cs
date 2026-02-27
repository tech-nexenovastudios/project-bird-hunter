using UnityEngine;

public class ThunderSpellFlask : MonoBehaviour,IInteract
{

    public static ThunderSpellFlask instance;
    public bool alreadyExist;


    private void OnEnable()
    {
        alreadyExist = true;
    }

    private void OnDisable()
    {
        alreadyExist = false;
    }

    private void Awake()
    {
        instance = this;
        gameObject.SetActive(false);
    }
    public void Interact()
    {
        foreach(var item in EggManager.eggsList)
        {
            var health = item.GetComponent<EggHealth>();
            if (health != null)
            {
                health.TakeDamage(20);
            }
            else
            {
                break;
            }
        }
       
        this.gameObject.SetActive(false);
    }

   
}
