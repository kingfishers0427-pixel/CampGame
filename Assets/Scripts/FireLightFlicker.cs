using UnityEngine;

[RequireComponent(typeof(Light))]
public class FireLightFlicker : MonoBehaviour
{
    [Header("明るさ（Intensity）の揺らぎ設定")]
    [Tooltip("最小の明るさ")]
    public float minIntensity = 2.5f;
    [Tooltip("最大の明るさ")]
    public float maxIntensity = 4.5f;

    [Header("揺らぎのスピード・細かさ")]
    [Tooltip("光の明滅スピード（大きいほど激しくチカチカします）")]
    public float flickerSpeed = 6.0f;

    [Header("炎の揺れによる位置の微振動（立体感アップ）")]
    [Tooltip("光源がわずかに動く幅（0にすると位置は固定されます）")]
    public float positionWobble = 0.04f;

    private Light fireLight;
    private Vector3 initialLocalPosition;
    private float randomSeed;

    void Start()
    {
        fireLight = GetComponent<Light>();
        initialLocalPosition = transform.localPosition;
        // 複数の焚き火やランタンがあっても同じタイミングで光らないようランダム値を設定
        randomSeed = Random.Range(0f, 100f);
    }

    void Update()
    {
        // パーリンノイズを使って、不規則で自然な0.0〜1.0の揺らぎ値を計算
        float time = Time.time * flickerSpeed;
        float noise = Mathf.PerlinNoise(randomSeed, time);

        // 1. 明るさ（Intensity）を滑らかに変化させる
        fireLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

        // 2. 光源の位置をわずかに揺らして、薪や周囲に落ちる影もユラユラ動かす
        if (positionWobble > 0f)
        {
            float offsetX = (Mathf.PerlinNoise(randomSeed + 10f, time) - 0.5f) * 2f * positionWobble;
            float offsetY = (Mathf.PerlinNoise(randomSeed + 20f, time) - 0.5f) * 2f * positionWobble;
            float offsetZ = (Mathf.PerlinNoise(randomSeed + 30f, time) - 0.5f) * 2f * positionWobble;

            transform.localPosition = initialLocalPosition + new Vector3(offsetX, offsetY, offsetZ);
        }
    }
}
