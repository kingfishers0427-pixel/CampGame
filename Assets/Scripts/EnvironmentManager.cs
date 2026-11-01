using UnityEngine;

/// <summary>
/// ゲーム内の時間帯を表す列挙型
/// </summary>
public enum TimeOfDay
{
    Dawn,      // 早朝
    Noon,      // 昼
    Evening,   // 夕方
    Night      // 夜
}

/// <summary>
/// ゲーム内の天候を表す列挙型
/// </summary>
public enum WeatherType
{
    Clear,     // 晴れ
    Cloudy,    // 曇り
    Rain,      // 雨
    Storm,     // 雷雨
    HeavyWind  // 強風
}

public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    // 時間帯や天候が変わったことを周囲に一斉放送（通知）するためのイベント
    public static event System.Action<TimeOfDay> OnTimeChanged;
    public static event System.Action<WeatherType> OnWeatherChanged;

    [Header("現在の環境状態")]
    [SerializeField] private TimeOfDay currentTime = TimeOfDay.Noon;
    [SerializeField] private WeatherType currentWeather = WeatherType.Clear;

    [Header("デバッグ用切り替えキー設定")]
    [SerializeField] private KeyCode nextTimeKey = KeyCode.T;     // Tキーで時間を進める
    [SerializeField] private KeyCode nextWeatherKey = KeyCode.Y;  // Yキーで天候を変える

    public TimeOfDay CurrentTime => currentTime;
    public WeatherType CurrentWeather => currentWeather;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // ゲーム開始時の初期状態を一斉放送
        OnTimeChanged?.Invoke(currentTime);
        OnWeatherChanged?.Invoke(currentWeather);
    }

    private void Update()
    {
        // デバッグ用：Tキーが押されたら時間を次の時間帯に進める
        if (Input.GetKeyDown(nextTimeKey))
        {
            AdvanceTime();
        }

        // デバッグ用：Yキーが押されたら天候を次の天候に切り替える
        if (Input.GetKeyDown(nextWeatherKey))
        {
            AdvanceWeather();
        }
    }

    /// <summary>
    /// 時間帯を次の段階へ進める
    /// </summary>
    private void AdvanceTime()
    {
        // 列挙型の値を順番に進める（Nightの次はDawnに戻る）
        currentTime = (TimeOfDay)(((int)currentTime + 1) % System.Enum.GetValues(typeof(TimeOfDay)).Length);
        
        // 周囲に一斉放送
        OnTimeChanged?.Invoke(currentTime);
        Debug.Log($"【環境変化】時間帯が切り替わりました: {currentTime}");
    }

    /// <summary>
    /// 天候を次の段階へ切り替える
    /// </summary>
    private void AdvanceWeather()
    {
        // 列挙型の値を順番に進める
        currentWeather = (WeatherType)(((int)currentWeather + 1) % System.Enum.GetValues(typeof(WeatherType)).Length);
        
        // 周囲に一斉放送
        OnWeatherChanged?.Invoke(currentWeather);
        Debug.Log($"【環境変化】天候が切り替わりました: {currentWeather}");
    }
}
