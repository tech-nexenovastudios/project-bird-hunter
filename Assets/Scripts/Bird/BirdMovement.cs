using DG.Tweening;
using System;
using System.Collections;
 using UnityEngine;
using Random = UnityEngine.Random;

public enum BirdMovementType
{
    LeftRightMove = 0,
    ZigZagMove = 1,
    TargetMove = 2,
    CurvePathMove = 3,
    LeftRightMoveAttack = 4,
    NormalMove = 5,
    DigonalMove = 6,
}

[RequireComponent(typeof(BoxCollider2D))]
public class BirdMovement : MonoBehaviour
{
    [Tooltip("Assign only if you want to destroy bird")]
    [SerializeField] Bird bird;

    [SerializeField] BoxCollider2D sizeCollider;
    float minX, maxX;
    float currentTarget;
    public BirdMovementType type;
    [SerializeField] float moveSpeed;
    [SerializeField] float chaseSpeed;

    [Tooltip("For Zig Zag Movement")]
    [SerializeField] Vector3[] movePoints;
    [SerializeField] bool useCameraBoundary;


    bool found;



    private void OnEnable()
    {
        StartCoroutine(Init());
    }
    IEnumerator Init()
    {
        yield return null;
        transform.PlayerBoundCalculate(sizeCollider, out Vector2 size);
        minX = ScreenBounds.minX + (size.x);
        maxX = ScreenBounds.maxX - (size.x);

        if (useCameraBoundary)
        {
            for (int i = 0; i < movePoints.Length; i++)
            {
                if (i % 2 == 0)
                {
                    movePoints[i].x = minX;
                }
                else
                {
                    movePoints[i].x = maxX;
                }
            }
        }
        currentTarget = minX;

        switch (type)
        {
            case BirdMovementType.LeftRightMove:
                StartCoroutine(LeftRightMoveCoroutine());
                //  TargetMove(new Vector2(currentTarget, transform.position.y));
                break;
            case BirdMovementType.ZigZagMove:
                StartCoroutine(ZigZagMoveCoroutine());
                //   TargetMove(new Vector2(currentTarget, transform.position.y));
                break;
            case BirdMovementType.TargetMove:
                StartCoroutine(TargetMoveCoroutine());
                break;
            case BirdMovementType.CurvePathMove:
                StartCoroutine(CurvePathMoveCoroutine());
                break;
            case BirdMovementType.LeftRightMoveAttack:
                StartCoroutine(LeftRightMoveAttackCoroutine());
                break;
            case BirdMovementType.NormalMove:
                StartCoroutine(NormalMove());
                break;
            case BirdMovementType.DigonalMove:
                StartCoroutine(DigonalMove());
                break;
        }

        /* if (type == BirdMovementType.LeftRightMove)
         {
             StartCoroutine(LeftRightMoveCoroutine());
             TargetMove(new Vector2(currentTarget, transform.position.y));
         }
         else if (type == BirdMovementType.ZigZagMove)
         {
             StartCoroutine(ZigZagMoveCoroutine());
         }
         else if (type == BirdMovementType.TargetMove)
         {
             StartCoroutine(TargetMoveCoroutine());
         }*/

    }
    private void Update()
    {



    }
    IEnumerator NormalMove()
    {
        while (true)
        {
            transform.position += Vector3.right * moveSpeed * Time.deltaTime;
            yield return null;
        }
    }
    IEnumerator LeftRightMoveAttackCoroutine()
    {
        found = false;
        StartCoroutine(LeftRightMoveCoroutine());
        while (true)
        {
            var ray = Physics2D.Raycast(transform.position, Vector2.down, float.MaxValue, LayerManager.PlayerMask);
            if (ray.collider != null)
            {

                var groundRay = Physics2D.Raycast(transform.position, Vector2.down, float.MaxValue, LayerManager.GroundMask);
                if (!found)
                {
                    found = true;
                    DOTween.Kill(transform);
                    var attack = GetComponent<IAttack>();
                    if (attack != null)
                    {
                        attack.Attack();
                    }
                    yield return new WaitForSeconds(2f);
                    //  this.GetComponent<SpriteRenderer>().color = Color.red;
                  
                    transform.DOMove(new Vector2(groundRay.point.x, groundRay.point.y), chaseSpeed).SetSpeedBased().SetEase(Ease.InSine).OnComplete(() =>
                    {
                        StartCoroutine(GoBackToCurrentHeight(6, () =>
                        {
                            if (attack != null)
                            {
                                attack.ResetAttack();
                            }
                            this.GetComponent<BoxCollider2D>().enabled = true;
                            //  this.GetComponent<SpriteRenderer>().color = Color.white;
                            StartCoroutine(LeftRightMoveAttackCoroutine());

                        }, 20));
                    });


                }
            }
            yield return null;
        }

    }
    IEnumerator LeftRightMoveCoroutine()
    {
        if (transform.position.x - minX <= 0.2f)
        {
            currentTarget = maxX;
            TargetMove(new Vector2(currentTarget, transform.position.y), () =>
            {
                StartCoroutine(LeftRightMoveCoroutine());
            });
        }
        else if (transform.position.x - maxX <= 0.2f)
        {
            currentTarget = minX;
            TargetMove(new Vector2(currentTarget, transform.position.y), () =>
            {
                StartCoroutine(LeftRightMoveCoroutine());
            });
        }
        yield return null;
    }
    int movePoint = 0;
    IEnumerator ZigZagMoveCoroutine()
    {
        TargetMove(movePoints[movePoint], () =>
        {
            movePoint++;
            movePoint %= movePoints.Length;
            StartCoroutine(ZigZagMoveCoroutine());
        });
        yield return null;
    }
    IEnumerator TargetMoveCoroutine()
    {
        if (CannonSpawner.cannon != null)
        {
            transform.DOMoveX(minX + 2f, moveSpeed / 4).SetSpeedBased().SetEase(Ease.Linear).OnComplete(() =>
            {
                TargetMove(CannonSpawner.cannon.transform.position, () =>
                {
                    Destroy(gameObject);
                });
            });
        }
        else
        {
            Debug.LogError("No Cannon Found");
        }
        yield return null;
    }

    IEnumerator CurvePathMoveCoroutine()
    {
        movePoints[0].x = ScreenBounds.minX;
        movePoints[movePoints.Length - 1].x = ScreenBounds.maxX;
        transform.DOPath(movePoints, 2f * movePoints.Length, PathType.CatmullRom)
          .SetEase(Ease.Linear)
          .SetOptions(true).OnComplete(() =>
          {
              StartCoroutine(CurvePathMoveCoroutine());
          }); // false = don't close path

        yield return null;
    }


    public void TargetMove(Vector2 Move, Action MoveCompleted = null)
    {
        DOTween.Kill(transform);  //to prevent error of multiple dotween ref
        transform.DOMove(Move, moveSpeed).SetSpeedBased().SetEase(Ease.Linear).OnComplete(() =>
        {
            transform.position = Move;
            MoveCompleted?.Invoke();
            bird?.DestroyBird();
        });
    }

    IEnumerator GoBackToCurrentHeight(float height, Action moveComplete = null, float speed = 5f)
    {
        yield return new WaitForSeconds(2f);
        transform.DOMoveY(height, chaseSpeed).SetSpeedBased().OnComplete(() =>
        {
            moveComplete?.Invoke();
        });
    }

    IEnumerator DigonalMove()
    {
        var randomNum = 3;//Mathf.RoundToInt(Random.Range(0, 2));

        float xMove = 0;
        float yMove = 0;

        if (randomNum == 1)
        {
            transform.position = new Vector2(ScreenBounds.minX - 2, ScreenBounds.maxY + 2);

            xMove = ScreenBounds.maxX;
            yMove = ScreenBounds.minY - 3;


        }
        else if (randomNum == 2)
        {
            transform.position = new Vector2(ScreenBounds.maxX + 2, ScreenBounds.maxY + 2);
            xMove = ScreenBounds.minX;
            yMove = ScreenBounds.minY - 3;
        }
        else
        {
            transform.position = new Vector2(0, ScreenBounds.maxY + 2);
            xMove = 0;
            yMove = ScreenBounds.minY - 2;
        }
        TargetMove(new Vector2(xMove, yMove));
        yield return null;
    }
    private void OnDrawGizmos()
    {
        foreach (var movepoint in movePoints)
        {
            Gizmos.DrawSphere(movepoint, 0.2f);
            Gizmos.color = Color.yellow;
        }
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 10f);
    }

    private void OnDisable()
    {
        DOTween.Kill(transform);
    }
    private void OnDestroy()
    {
        DOTween.Kill(transform);
    }

}
