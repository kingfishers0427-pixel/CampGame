using UnityEngine;

public class EnvironmentVisualizer : MonoBehaviour
{
    private Light directionalLight;

    private void Awake()
    {
        // 自身のアタッチされているLightコンポーネントを取得
        directionalLight = GetComponent<Light>();
    }

    private void OnEnable()
    {
        // 時間帯が変わったという放送を聴くように登録
        EnvironmentManager.OnTimeChanged += ApplyTimeVisuals;
    }

    private void OnDisable()
    {
        // 登録解除
        EnvironmentManager.OnTimeChanged -= ApplyTimeVisuals;
    }

    /// <summary>
    /// 時間帯の放送が届いたら、太陽の光の色と強さを変える
    /// </summary>
    private void ApplyTimeVisuals(TimeOfDay newTime)
    {
        if (directionalLight == null) return;

        switch (newTime)
        {
            case TimeOfDay.Dawn: // 早朝：薄暗いオレンジ
                directionalLight.color = new Color(1f, 0.6f, 0.4f);
                directionalLight.intensity = 0.5f;
                break;

            case TimeOfDay.Noon: // 昼：明るい白・黄色
                directionalLight.color = new Color(1f, 1f, 0.9f);
                directionalLight.intensity = 1.3f;
                break;

            case TimeOfDay.Evening: // 夕方：深い赤・茜色
                directionalLight.color = new Color(0.9f, 0.4f, 0.1f);
                directionalLight.intensity = 0.6f;
                break;

            case TimeOfDay.Night: // 夜：暗い青紫（ほぼ真っ暗）
                directionalLight.color = new Color(0.1f, 0.1f, 0.3f);
                directionalLight.intensity = 0.05f;
                break;
        }
    }
}
