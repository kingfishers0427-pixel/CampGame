using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    private Light targetLight;

    [Header("明るさの最小・最大値")]
    public float minIntensity = 1.0f;
    public float maxIntensity = 1.5f;

    [Header("揺らぎの速さ（大きいほど細かく揺れる）")]
    public float flickerSpeed = 10.0f;

    void Start()
    {
        // 自身についているLightコンポーネントを取得
        targetLight = GetComponent<Light>();
    }

    void Update()
    {
        if (targetLight == null) return;

        // ノイズ関数（Mathf.PerlinNoise）を使って、自然な炎の揺らぎを計算
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.0f);
        
        // 計算した揺らぎをライトの明るさに反映
        targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}
