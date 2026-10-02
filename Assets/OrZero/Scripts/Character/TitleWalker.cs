using System.Collections;
using UnityEngine;

/// <summary>
/// タイトル画面で数秒待機後、右端から左端へ歩いて横切るキャラクター。
/// 歩きアニメーションは Animator 側に設定済みの前提。
/// </summary>
public class TitleWalker : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("開始位置（ワールド座標）。画面の右外側に置く")]
    [SerializeField] private Vector3 startPosition = new Vector3(8f, 0f, 0f);

    [Tooltip("終了位置（ワールド座標）。画面の左外側に置く")]
    [SerializeField] private Vector3 endPosition = new Vector3(-8f, 0f, 0f);

    [Tooltip("歩く速度（ユニット/秒）")]
    [SerializeField] private float walkSpeed = 1.5f;

    [Header("タイミング設定")]
    [Tooltip("タイトル画面表示から歩き始めるまでの待機時間（秒）")]
    [SerializeField] private float startDelay = 3f;

    [Tooltip("ONにすると、歩き終わったあと繰り返し横切る")]
    [SerializeField] private bool loop = false;

    [Tooltip("ループ時、次に歩き始めるまでの待機時間（秒）")]
    [SerializeField] private float loopInterval = 5f;

    [Header("向き設定")]
    [Tooltip("ONにすると、進行方向を向く")]
    [SerializeField] private bool faceMoveDirection = true;

    [Tooltip("モデルの正面がワールドの前方(+Z)からズレている場合の補正角度（Y軸）")]
    [SerializeField] private float modelYawOffset = 0f;

    [Header("アニメーション設定（任意）")]
    [SerializeField] private Animator animator;

    [Tooltip("歩行のON/OFFに使うAnimatorのBoolパラメータ名。空欄なら何もしない")]
    [SerializeField] private string walkBoolName = "";

    [Tooltip("歩いていないときにモデルを非表示にする")]
    [SerializeField] private bool hideWhileWaiting = true;

    private Renderer[] renderers;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        ResetToStart();
        StartCoroutine(WalkRoutine());
    }

    private IEnumerator WalkRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        do
        {
            ResetToStart();
            SetVisible(true);
            SetWalking(true);

            // 進行方向を向く
            Vector3 dir = endPosition - startPosition;
            dir.y = 0f;
            if (faceMoveDirection && dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0f, modelYawOffset, 0f);
            }

            // 終点まで移動
            while ((transform.position - endPosition).sqrMagnitude > 0.0001f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, endPosition, walkSpeed * Time.deltaTime);
                yield return null;
            }

            SetWalking(false);
            SetVisible(!hideWhileWaiting);

            if (loop) yield return new WaitForSeconds(loopInterval);

        } while (loop);
    }

    private void ResetToStart()
    {
        transform.position = startPosition;
        SetWalking(false);
        SetVisible(!hideWhileWaiting);
    }

    private void SetWalking(bool value)
    {
        if (animator != null && !string.IsNullOrEmpty(walkBoolName))
        {
            animator.SetBool(walkBoolName, value);
        }
    }

    private void SetVisible(bool visible)
    {
        if (renderers == null) return;
        foreach (var r in renderers)
        {
            if (r != null) r.enabled = visible;
        }
    }

    // シーンビューで開始・終了位置を確認できるようにする
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPosition, 0.3f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(endPosition, 0.3f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPosition, endPosition);
    }
}
