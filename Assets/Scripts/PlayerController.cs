using UnityEngine;
using UnityEngine.UI; // UnityのUI機能を使うための宣言

public class PlayerController : MonoBehaviour
{
    [Header("インタラクト設定")]
    [SerializeField] private float interactRange = 3f; // 触れる距離
    [SerializeField] private LayerMask interactLayer;   // 反応するレイヤー（後で設定）

    [Header("UI設定")]
    [SerializeField] private Text interactTextUI; // 画面に表示するTextコンポーネント

    [Header("移動速度設定")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("FPSモード用の設定")]
    [SerializeField] private float mouseSensitivity = 2f; // マウスの感度
    private float verticalRotation = 0f;                  // 上下の首振り角度を記録
    public float VerticalRotation => verticalRotation;    // 外部（カメラ）から角度を読み取るための窓口

    private Camera mainCamera;

    private void OnEnable()
    {
        // GameManagerのモード切り替えイベント（放送）を聴くように登録
        GameManager.OnModeChanged += HandleModeChanged;
    }

    private void OnDisable()
    {
        // 登録解除
        GameManager.OnModeChanged -= HandleModeChanged;
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // 今がどっちのモードかによって、Updateでの処理（操作の受付）を分ける
        if (GameManager.Instance.CurrentMode == CameraMode.FPS)
        {
            HandleFPSRotation(); // FPS時はマウスで首振り
            HandleFPSInteract(); // 毎フレーム正面に触れるものがないか監視
        }
        else if (GameManager.Instance.CurrentMode == CameraMode.TopDown)
        {
            HandleTopDownClickMove(); // 見下ろし時はマウスクリックで移動
        }
    }

    private void FixedUpdate()
    {
        // 物理挙動や移動系はFixedUpdateで行うのがUnityのセオリー
        if (GameManager.Instance.CurrentMode == CameraMode.FPS)
        {
            HandleFPSMovement(); // FPS時はWASD移動
        }
    }

    #region FPSモードの処理

    /// <summary>
    /// FPSモード時のWASD移動
    /// </summary>
    private void HandleFPSMovement()
    {
        float moveX = Input.GetAxis("Horizontal"); // A, Dキー
        float moveZ = Input.GetAxis("Vertical");   // W, Sキー

        // プレイヤーが向いている「前」と「右」を基準に移動方向を計算
        Vector3 moveDirection = (transform.forward * moveZ) + (transform.right * moveX);
        moveDirection.Normalize();

        // 位置を移動させる
        transform.position += moveDirection * moveSpeed * Time.fixedDeltaTime;
    }

    /// <summary>
    /// FPSモード時のマウスによる首振り（視点移動）
    /// </summary>
    private void HandleFPSRotation()
    {
        // マウスの移動量を取得
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 1. 左右の回転：プレイヤーの体そのものを横に回す
        transform.Rotate(Vector3.up * mouseX);

        // 2. 上下の回転：カメラだけを上下に傾ける（体は傾けない）
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f); // 真上や真後ろを向けないように制限

        // if (mainCamera != null)
        // {
        //     // カメラのローカル角度を書き換える
        //     mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        // }
    }

    #endregion

    #region 見下ろしモードの処理

    /// <summary>
    /// 見下ろしモード時のマウスクリック移動
    /// </summary>
    private void HandleTopDownClickMove()
    {
        // マウスの左クリックが押された瞬間
        if (Input.GetMouseButtonDown(0))
        {
            // 画面のクリック位置から、3D世界に向けて目に見えない光線（Ray）を発射する
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 光線が何かに当たったら（今回は地面）
            if (Physics.Raycast(ray, out hit))
            {
                // 当たった場所（hit.point）に向かって進む処理
                // ※本来はNavMeshという機能を使いますが、まずは一番シンプルな「直進」で実装します
                StopAllCoroutines();
                StartCoroutine(MoveToTarget(hit.point));
            }
        }
    }

    /// <summary>
    /// クリックした目標地点まで滑らかに移動する簡易システム
    /// </summary>
    private System.Collections.IEnumerator MoveToTarget(Vector3 targetPos)
    {
        // キャラクターの高さ（Y軸）が変わらないように固定
        targetPos.y = transform.position.y;

        // 目標地点に向きを変える
        Vector3 direction = (targetPos - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // 目標地点に十分近づくまで歩く
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    #endregion

    /// <summary>
    /// モードが切り替わった瞬間に一度だけ呼ばれる処理
    /// </summary>
    private void HandleModeChanged(CameraMode newMode)
    {
        // 見下ろしモードに切り替わったら、移動のコルーチン（途中の歩き）を強制停止する
        StopAllCoroutines();

        // FPSモードに戻った時は、カメラの上下傾きをリセットして真っ直ぐ前を向かせる
        if (newMode == CameraMode.FPS)
        {
            verticalRotation = 0f;
        }
    }

    /// <summary>
    /// FPSモード時の視線によるインタラクト処理（UI連動版）
    /// </summary>
    private void HandleFPSInteract()
    {
        // 画面中央（カメラの正面）から前方に光線を飛ばす
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        // レイキャストが何かに当たったか、かつ当たったものがIInteractableを持っているか判定するフラグ
        bool isLookingAtInteractable = false;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            // 当たったオブジェクトが「IInteractable」の窓口を持っているか確認
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                isLookingAtInteractable = true;

                // 【強化】UIテキストに「[E] テントを設営する」のように綺麗に表示させる
                if (interactTextUI != null)
                {
                    interactTextUI.text = $"[E] {interactable.GetInteractText()}";
                }

                // Eキーが押されたら実行
                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact(this);
                }
            }
        }

        // 何も見ていない、または触れないオブジェクトを見ている時はテキストを消す
        if (!isLookingAtInteractable && interactTextUI != null)
        {
            interactTextUI.text = ""; // 空文字にして非表示にする
        }
    }

}

