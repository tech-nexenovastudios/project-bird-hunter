using DG.Tweening;
using System.Collections;
using UnityEngine;

public class RootAttack : MonoBehaviour
{
    [SerializeField] GameObject warningSign;
    public GameObject root;
    BoxCollider2D col;

    [SerializeField] float delayTime = 3f, duration= 5f, coolDown=5f, animationDuration= 0.2f;

    GameObject warningSignRef, rootRef;
    private void Start()
    {
        col = GetComponentInChildren<BoxCollider2D>();
        root.SetActive(true);
        if(warningSignRef == null)
        {
            warningSignRef = Instantiate(warningSign);
        }
        if(rootRef == null)
        {
            rootRef = Instantiate(root);
        }
        StartCoroutine(DecideRandomPosToSpawn());
    }

    IEnumerator DecideRandomPosToSpawn()
    {
        rootRef.GetComponentInChildren<Collider2D>().enabled = true;
        float pos = Random.Range(ScreenBounds.minX+col.size.x,ScreenBounds.maxX-col.size.x);
        warningSignRef.transform.position = new Vector2(pos, warningSign.transform.position.y);
        warningSignRef.SetActive(true);
        yield return new WaitForSeconds(delayTime);
        warningSignRef.SetActive(false);
        rootRef.transform.position = new Vector3(pos,root.transform.position.y);
        rootRef.transform.DOScale(Vector3.one * 0.85f, animationDuration).SetEase(Ease.Linear);
        yield return new WaitForSeconds(duration);
        rootRef.transform.DOScaleY(0, animationDuration).SetEase(Ease.Linear);  
        yield return new WaitForSeconds(duration);
        StartCoroutine(DecideRandomPosToSpawn()); 
    }
}
