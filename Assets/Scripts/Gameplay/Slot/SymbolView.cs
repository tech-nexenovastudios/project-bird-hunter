using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Slot
{
    public class SymbolView : MonoBehaviour
    {
        public Image iconImage;
        public Button button;
        public string symbolId;

        private void Awake()
        {
            //TryGetComponent(out button);
        }

        public SymbolView SetIcon(Sprite sprite, string id)
        {
            if (iconImage == null) iconImage = GetComponent<Image>();
            
            button.onClick.RemoveAllListeners();
            iconImage.sprite = sprite;
            symbolId = id;
            return this;
        }

        public SymbolView AddListener(Action listener)
        {
            button.onClick.AddListener(listener.Invoke);
            return this;
        }
        
        public void ResetValue()
        {
            SetIcon(null, null);
        }
    }
}