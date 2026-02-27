using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


//Scene Change use for change scenes
public class SceneChanger : MonoBehaviour
{
    //Pass Scene Name as a parameter
    public void SceneChangeCallBack(string sceneName)
    {
        StartCoroutine(SceneCoroutine(sceneName));


    }


    
    IEnumerator SceneCoroutine(string sceneName)
    {
        var sceneChange = SceneManager.LoadSceneAsync(sceneName);
        sceneChange.allowSceneActivation = false;
        while (sceneChange.progress >= 0.9f)
        {

        }

        sceneChange.allowSceneActivation = true;
        yield return null;
    }
}
