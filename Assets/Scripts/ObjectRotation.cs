using DG.Tweening;
using System.Collections;
using UnityEngine;

public class ObjectRotation : MonoBehaviour
{
    [SerializeField] float speed, zRot;


    private void Start()
    {
        StartRot();
    }

    IEnumerator StartRot()
    {
        /*  transform.DOLocalRotate(new Vector3(0, 0, 360), 1f,RotateMode.FastBeyond360).SetEase(Ease.Linear).OnComplete(() =>
          {
              transform.DOLocalRotate(new Vector3(0, 0, 0), 1f, RotateMode.FastBeyond360).SetEase(Ease.Linear).OnComplete(() =>
              {
                  StartRot();
              });
          });*/

        float time = 0;
        while (time <= 1)
        {
            time += speed * Time.deltaTime;
            transform.rotation = Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(0, 0, 360), time);
        }
        yield return null;

    }
    private void Update()
    {
        /*zRot += speed*Time.deltaTime;
        transform.rotation = Quaternion.Euler(0, 0, zRot);*/
    }



}
