using UnityEngine;

public class OrbRotation : MonoBehaviour
{
    [SerializeField] float speed;
    public Transform parent;

    public float defaultSize = 2.5f;

/*    private void OnEnable()
    {
        CannonPower.sizeDecreaseCallBack += RunTimeScaleChange;
    }

    private void OnDisable()
    {
        CannonPower.sizeDecreaseCallBack -= RunTimeScaleChange;
    }

    public void RunTimeScaleChange(float scale)
    {
        float scalePercentage = (1 - (scale / 1));
        transform.localScale = ((Vector3.one*2.5f)-((Vector3.one*2.5f) * scalePercentage));
        transform.GlobalScale(transform.localScale);
    }
*/

    private void Update()
    {
        transform.position = parent.position;
        transform.rotation = Quaternion.Euler(transform.eulerAngles + Vector3.forward * speed * Time.deltaTime);
    }
}
