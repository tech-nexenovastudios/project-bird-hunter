using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityUtils;

public class PersistantData : Singleton<PersistantData>
{
    private ChaptersConfig _chaptersConfig;
    public ChaptersConfig ChaptersConfig => _chaptersConfig;
    public int ChapterIndex { get; private set;}
    public int CannonIndex { get; private set;}
    public ChapterData ChapterData => ChaptersConfig.GetChapterData(ChapterIndex);
    public ChapterData NextChapterData => ChaptersConfig.GetNextChapterData(ChapterIndex);

    public void SetChapterIndex(int index) => ChapterIndex = index;
    public void SetCannonIndex(int index) => CannonIndex = index;
    public void UpdateChaptersConfig(ChaptersConfig config) => _chaptersConfig = config;
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }
    
}
