using System.Collections;
using UnityEngine;

public class AreaTornado : MonoBehaviour
{
    [SerializeField] GameObject tornado;
    [SerializeField] GameObject warningSign;
    [SerializeField] float duration = 5f, coolDownTimer = 3f, warningTimer = 3f;
    GameObject warningSignRef;
    GameObject tornadoRef;

    private void Start()
    {
        warningSignRef = Instantiate(warningSign);
        tornadoRef = Instantiate(tornado);
        tornadoRef.transform.localScale = new Vector2(ScreenBounds.maxX,(ScreenBounds.maxY*2)-2.5f);
        warningSignRef.transform.localScale = Vector3.one * ScreenBounds.maxX;
        float size = tornado.GetComponentInChildren<BoxCollider2D>().bounds.size.x;
      
        warningSignRef.SetActive(false);
        tornadoRef.SetActive(false);
        // tornado.transform.position = new Vector3(ScreenBounds.maxX - (tornado.transform.localScale.x / 2), tornado.transform.position.y+tornado.transform.localScale.y);
        StartCoroutine(Tornado());
    }

    IEnumerator Tornado()
    {
        while (true)
        {
       
            /*Debug.Log(-5.5f + (transform.lossyScale.x * 0.5f));*/
            yield return new WaitForSeconds(coolDownTimer);
            tornadoRef.SetActive(false);
            warningSignRef.SetActive(true);
            float number = Mathf.CeilToInt(Random.Range(0, 2));

            if (number == 0)
            {
                tornadoRef.transform.position = new Vector3(ScreenBounds.maxX - (tornadoRef.transform.localScale.x / 2), tornado.transform.position.y);
                Debug.Log(ScreenBounds.maxX - (tornadoRef.transform.localScale.x / 2));
            }
            else
            {
                tornadoRef.transform.position = new Vector3((ScreenBounds.minX + (tornadoRef.transform.localScale.x / 2)), tornado.transform.position.y);
                Debug.Log(ScreenBounds.minX + (tornadoRef.transform.localScale.x / 2));
            }
            warningSignRef.transform.position = tornadoRef.transform.position + transform.up * (warningSign.transform.localScale.y/2);
            yield return new WaitForSeconds(warningTimer);
            warningSignRef.SetActive(false);
            tornadoRef.SetActive(true);
            yield return new WaitForSeconds(duration);
        }

    }
}
