using UnityEngine;

/// <summary>
/// 焚き火やランタンの光のチラチラとしたリアルな揺らぎを表現するスクリプト
/// </summary>
[RequireComponent(typeof(Light))]
public class CampfireFlicker : MonoBehaviour
{
    [Header("明るさの揺らぎ設定")]
    [SerializeField] private float minIntensity = 1.0f; // 最小の明るさ
    [SerializeField] private float maxIntensity = 2.5f; // 最大の明るさ

    [Header("揺らぎのスピード")]
    [SerializeField] private float flickerSpeed = 5.0f; // 数値が大きいほどチラチラが速くなる

    private Light targetLight;
    private float noiseToken;

    private void Awake()
    {
        // 自身のアタッチされているLightコンポーネントを自動取得
        targetLight = GetComponent<Light>();
        
        // 毎回同じ揺らぎパターンにならないように、開始時にランダムなシード値を設定
        noiseToken = Random.Range(0f, 65535f);
    }

    private void Update()
    {
        if (targetLight == null) return;

        // 時間の経過に合わせてノイズのサンプリング位置をずらしていく
        noiseToken += Time.deltaTime * flickerSpeed;

        // パーリンノイズを使って、0.0 〜 1.0 の間の滑らかで不規則な数値を計算
        float rawNoise = Mathf.PerlinNoise(noiseToken, 0f);

        // 計算したノイズ（0〜1）を、設定した最小・最大の明るさの幅（min〜max）に変換
        targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, rawNoise);
    }
}
