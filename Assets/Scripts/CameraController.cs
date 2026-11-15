using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("ターゲット（Player）の設定")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform fpsCameraAnchor;

    [Header("見下ろしモード（TopDown）のカメラ設定")]
    [SerializeField] private Vector3 topDownOffset = new Vector3(0f, 10f, -7f); // プレイヤーからの距離
    [SerializeField] private Vector3 topDownRotation = new Vector3(55f, 0f, 0f); // 見下ろす角度

    [Header("カメラの移動スムーズさ")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 5f;

    // 現在目指すべきカメラの「目標座標」と「目標角度」
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    public bool isCameraLocked = false;

    private void OnEnable()
    {
        // GameManagerのモード切り替えイベント（放送）を聴くように登録
        GameManager.OnModeChanged += HandleModeChanged;
    }

    private void OnDisable()
    {
        // オブジェクトが消えるときは登録を解除（エラー防止）
        GameManager.OnModeChanged -= HandleModeChanged;
    }

    private void Start()
    {
        // 自動でシーン内のPlayerを探してセット（セットし忘れた時用の安全策）
        if (playerTransform == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                // 子要素からFPS_CameraAnchorを探す
                fpsCameraAnchor = player.transform.Find("FPS_CameraAnchor");
            }
        }

        // 初期位置を現在のモードに合わせるために一度手動で呼び出す
        HandleModeChanged(GameManager.Instance.CurrentMode);
        
        // 開始時はパッと初期位置にワープさせる
        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

    private void LateUpdate()
    {
        if (isCameraLocked) return;
        if (playerTransform == null) return;

        // モードごとに、毎フレーム「最新の目標座標」を更新する
        UpdateTargetTransform();

        // 【修正】FPSモードの時は遅れずにピタッと追従させ、見下ろしモードの時だけ滑らかに動かす
        if (GameManager.Instance.CurrentMode == CameraMode.FPS)
        {
            // ラグを一切無くすため、補間（Lerp）を使わずに座標と角度を完全に一致させる
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
        else
        {
            // 見下ろし（TopDown）モードの時は、今まで通りウィンと滑らかに追いかける
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotateSpeed);
        }
    }

    /// <summary>
    /// GameManagerからモード切り替えの放送が届いた時に実行される処理
    /// </summary>
    private void HandleModeChanged(CameraMode newMode)
    {
        UpdateTargetTransform();

        // モード切り替え時のカーソル制御（設計書より）
        if (newMode == CameraMode.FPS)
        {
            Cursor.lockState = CursorLockMode.Locked; // 画面中央に固定
            Cursor.visible = false;                   // マウスカーソルを非表示
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;   // 固定を解除
            Cursor.visible = true;                    // マウスカーソルを表示
        }
    }

    /// <summary>
    /// 現在のモードに応じた「目標座標・角度」を計算する
    /// </summary>
    private void UpdateTargetTransform()
    {
        if (GameManager.Instance.CurrentMode == CameraMode.FPS)
        {
            if (fpsCameraAnchor != null)
            {
                targetPosition = fpsCameraAnchor.position;
                
                // 【修正】プレイヤーの左右の向き（Y軸回転）と、PlayerControllerが計算した上下の首振りを合体させる
                float verticalRot = playerTransform.GetComponent<PlayerController>().VerticalRotation;
                targetRotation = Quaternion.Euler(verticalRot, playerTransform.eulerAngles.y, 0f);
            }
        }
        else // TopDown（見下ろしモード）
        {
            // プレイヤーの位置から、指定したオフセット分だけ離れた上空
            targetPosition = playerTransform.position + topDownOffset;
            targetRotation = Quaternion.Euler(topDownRotation); // 見下ろし時は常に固定の角度（回らない！）
        }
    }
}
