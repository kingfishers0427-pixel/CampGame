using UnityEngine;

/// <summary>
/// カメラのモード（視点）を表す列挙型
/// </summary>
public enum CameraMode
{
    FPS,       // 一人称視点モード
    TopDown    // 見下ろし型モード
}

public class GameManager : MonoBehaviour
{
    // シングルトン（どこからでもアクセスできるようにする仕組み）
    public static GameManager Instance { get; private set; }

    // モードが切り替わったことを周囲に一斉放送（通知）するためのイベント
    public static event System.Action<CameraMode> OnModeChanged;

    [Header("現在のモード設定")]
    [SerializeField] private CameraMode currentMode = CameraMode.FPS;

    [Header("切り替え用キー設定")]
    [SerializeField] private KeyCode toggleKey = KeyCode.V; // Vキーで切り替え

    public CameraMode CurrentMode => currentMode;

    private void Awake()
    {
        // シングルトンの初期化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンを切り替えても消えないようにする
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // ゲーム開始時の初期モードを一斉放送
        NotifyModeChange();
    }

    private void Update()
    {
        // 設定されたキー（Vキー）が押されたらモードを切り替える
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleCameraMode();
        }
    }

    /// <summary>
    /// モードを交互に切り替える
    /// </summary>
    private void ToggleCameraMode()
    {
        if (currentMode == CameraMode.FPS)
        {
            currentMode = CameraMode.TopDown;
        }
        else
        {
            currentMode = CameraMode.FPS;
        }

        NotifyModeChange();
        Debug.Log($"カメラモードが切り替わりました: {currentMode}");
    }

    /// <summary>
    /// 周囲のスクリプトに現在のモードを通知する
    /// </summary>
    private void NotifyModeChange()
    {
        // 登録されている（聴いている）すべてのスクリプトに通知を一斉送信
        OnModeChanged?.Invoke(currentMode);
    }
}
