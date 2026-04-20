using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdHunter.Inventory.UI
{
    public class CannonItem : MonoBehaviour
    {
        public int cannonId;
        public Button button;
        public Image cannonSprite;

        public Image cannonImgComp;
        public Image cannonBgComp;
        public Image frameComp;
        public GameObject lockImageObj;
        public GameObject unlockRequirementContainer;
        public TextMeshProUGUI unlockRequirementText;
    }
}