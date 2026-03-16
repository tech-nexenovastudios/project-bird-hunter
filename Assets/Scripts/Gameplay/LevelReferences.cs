using System;
using UnityEngine;
using UnityEngine.UI;
using UnityUtils;
using GameManager = Gameplay.Managers.GameManager;

public class LevelReferences : Singleton<LevelReferences>
{
    [SerializeField] private Image chapterBG;
    [SerializeField] private SpriteRenderer groundSprite;
    
    [SerializeField] private Transform cannonSpawnPoint;
    
    public Transform CannonSpawnPoint => cannonSpawnPoint;

    public void SetChapterData(ChapterData data)
    {
        chapterBG.sprite = data.chapterBackground;
        groundSprite.sprite = data.platform;
    }
}
