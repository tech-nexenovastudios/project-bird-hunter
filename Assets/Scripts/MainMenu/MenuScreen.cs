using System;
using System.Collections.Generic;
using Constants;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game
{
    public class MenuScreen : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI chapterTheme;
        [SerializeField] private Text chapterText;
        [SerializeField] private Button playButton;

        private async void Awake()
        
        {
            
            var result = await CloudSaveManager.Instance.LoadAsync(
                new HashSet<string>()
                {
                    GameConstants.PLAYER_GOLD_COIN_KEY,
                    GameConstants.PLAYER_GEM_COIN_KEY,
                    GameConstants.PLAYER_POWER_COIN_KEY,
                    GameConstants.PLAYER_HIGH_SCORE_KEY,
                    GameConstants.PLAYER_TOTAL_SCORE_KEY,
                    
                    GameConstants.PLAYER_CURRENT_CHAPTER_KEY,
                    GameConstants.PLAYER_CURRENT_LEVEL_KEY,
                });
        }

        public void SetChapter(string chapterName, string theme)
        {
            
            chapterTheme.text = theme;
            chapterText.text = chapterName;
            playButton.gameObject.SetActive(true);
        }
        
    }    
}
