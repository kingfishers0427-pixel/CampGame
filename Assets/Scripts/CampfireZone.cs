using System.Collections.Generic;
using UnityEngine;

public class CampfireZone : MonoBehaviour
{
    [Header("焚き火の見た目・光")]
    public GameObject bigFireEffect;

    [Header("煙パーティクル（あれば設定）")]
    public ParticleSystem smokeParticle;

    [Header("判定の広さ（半径）")]
    [Tooltip("すでに燃えている薪を燃料としてカウントする範囲")]
    public float checkRadius = 0.6f;

    [Tooltip("新しい薪に火が燃え移る範囲（炎のすぐそばまで近づける必要あり）")]
    public float igniteRadius = 0.35f;

    [Tooltip("炎の近くに入れてから実際に着火するまでの時間（秒）")]
    public float igniteDelay = 1.2f;

    [Tooltip("薪がすべて燃え尽きてから炎が完全に消えるまでの猶予（秒）")]
    public float extinguishGraceTime = 3.0f;

    [Header("燃料（薪の量）に応じた炎の大きさ設定")]
    [Tooltip("基準となる薪の本数（この本数の時に炎が1.0倍になります）")]
    public float baseWoodCount = 4.0f;

    [Tooltip("炎の最小スケール（完全に炭になる直前の大きさ割合）")]
    [Range(0.05f, 0.5f)]
    public float minFireScaleRatio = 0.15f;

    [Tooltip("薪をたくさんくべた時の炎の最大倍率（例：2.2 ＝ 標準の2.2倍まで大きくなる）")]
    public float maxFireScaleMultiplier = 2.2f;

    [Header("煙の変化・追加時のブワッ演出")]
    [Tooltip("薪を多くくべた時に煙の量を何倍まで増やすか")]
    public float maxSmokeRateMultiplier = 3.5f;

    [Tooltip("新しい薪をくべて着火した瞬間に一気に噴き出す白煙の粒数")]
    public int addWoodSmokeBurstCount = 20;

    private float emptyTimer = 0f;
    private float aliveTime = 0f; // 生成直後の初期4本の着火と区別するための経過時間
    private Vector3 initialFireScale = Vector3.one;

    // 薪ごとの「炎に当たっている累計時間」を記録する辞書
    private Dictionary<BurnableWood, float> heatingTimers = new Dictionary<BurnableWood, float>();

    // 炎パーティクル自体の勢いも連動させる用
    private ParticleSystem fireParticle;
    private float initialStartSize;
    private float initialStartSpeed;

    // 煙パーティクルの発生量・勢い・大きさ連動用
    private float initialSmokeRate;
    private float initialSmokeSpeed;
    private float initialSmokeSize;

    // 周囲の明かり（PointLight）も炎の大きさに連動させる用
    private FireLightFlicker lightFlicker;
    private float initialMinIntensity;
    private float initialMaxIntensity;

    void Start()
    {
        if (bigFireEffect != null)
        {
            bigFireEffect.SetActive(true);
            initialFireScale = bigFireEffect.transform.localScale;

            fireParticle = bigFireEffect.GetComponentInChildren<ParticleSystem>();
            if (fireParticle != null)
            {
                var main = fireParticle.main;
                initialStartSize = main.startSize.constant;
                initialStartSpeed = main.startSpeed.constant;
            }

            lightFlicker = bigFireEffect.GetComponentInChildren<FireLightFlicker>();
            if (lightFlicker != null)
            {
                initialMinIntensity = lightFlicker.minIntensity;
                initialMaxIntensity = lightFlicker.maxIntensity;
            }
        }

        if (smokeParticle != null)
        {
            initialSmokeRate = smokeParticle.emission.rateOverTime.constant;
            var smokeMain = smokeParticle.main;
            initialSmokeSpeed = smokeMain.startSpeed.constant;
            initialSmokeSize = smokeMain.startSize.constant;
        }
    }

    void Update()
    {
        aliveTime += Time.deltaTime;

        // ① 着火判定（狭い範囲 igniteRadius の中にある薪だけ温める）
        Collider[] igniteHits = Physics.OverlapSphere(transform.position, igniteRadius);
        HashSet<BurnableWood> currentlyHeatingWoods = new HashSet<BurnableWood>();

        foreach (Collider hit in igniteHits)
        {
            BurnableWood wood = hit.GetComponentInParent<BurnableWood>();
            if (wood != null && !wood.IsBurning)
            {
                currentlyHeatingWoods.Add(wood);

                if (!heatingTimers.ContainsKey(wood))
                {
                    heatingTimers[wood] = 0f;
                }

                heatingTimers[wood] += Time.deltaTime;

                // 一定時間（igniteDelay）以上炎の近くにあり続けたら着火！
                if (heatingTimers[wood] >= igniteDelay)
                {
                    wood.IgniteWood();
                    heatingTimers.Remove(wood);

                    // 焚き火設立後（初期着火のあと）に新しい薪が燃え始めたら、白煙を一気にブワッと噴き出す！
                    if (aliveTime > igniteDelay + 0.5f && smokeParticle != null)
                    {
                        EmitAddWoodSmokeBurst(wood.transform.position);
                    }
                }
            }
        }

        // 炎の近くから離れた薪のタイマーはリセットする
        List<BurnableWood> keysToRemove = new List<BurnableWood>();
        foreach (var kvp in heatingTimers)
        {
            if (kvp.Key == null || kvp.Key.IsBurning || !currentlyHeatingWoods.Contains(kvp.Key))
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            heatingTimers.Remove(key);
        }

        // ② 燃料カウント判定（checkRadius 内にある「すでに燃えている薪」だけを合計）
        Collider[] fuelHits = Physics.OverlapSphere(transform.position, checkRadius);
        HashSet<BurnableWood> burningWoodsInZone = new HashSet<BurnableWood>();
        float totalRemainingFuel = 0f;

        foreach (Collider hit in fuelHits)
        {
            BurnableWood wood = hit.GetComponentInParent<BurnableWood>();
            if (wood != null && wood.IsBurning && burningWoodsInZone.Add(wood))
            {
                totalRemainingFuel += wood.RemainingFuelRatio;
            }
        }

        if (burningWoodsInZone.Count > 0)
        {
            emptyTimer = 0f;

            float fuelRatio = totalRemainingFuel / baseWoodCount;

            float targetScaleMultiplier;
            if (fuelRatio <= 1.0f)
            {
                targetScaleMultiplier = Mathf.Lerp(minFireScaleRatio, 1.0f, fuelRatio);
            }
            else
            {
                float extraRatio = (fuelRatio - 1.0f) * 0.6f;
                targetScaleMultiplier = Mathf.Clamp(1.0f + extraRatio, 1.0f, maxFireScaleMultiplier);
            }

            ApplyFireScale(targetScaleMultiplier);
        }
        else
        {
            emptyTimer += Time.deltaTime;
            float fade = Mathf.Clamp01(1f - (emptyTimer / extinguishGraceTime));

            ApplyFireScale(minFireScaleRatio * fade);

            if (emptyTimer >= extinguishGraceTime)
            {
                Debug.Log("薪がすべて燃え尽き、焚き火が鎮火しました。");
                Destroy(gameObject);
            }
        }
    }

    // 新しい薪をくべた瞬間に、その薪の周辺から白い煙をブワッと噴き出す演出
    void EmitAddWoodSmokeBurst(Vector3 woodPos)
    {
        for (int i = 0; i < addWoodSmokeBurstCount; i++)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            // くべた薪の周囲からランダムに発生させる
            Vector3 randomOffset = Random.insideUnitSphere * 0.25f;
            randomOffset.y = Mathf.Abs(randomOffset.y);
            emitParams.position = woodPos + randomOffset;

            // 外側へフワッと広がりながら上昇する初速を与える
            Vector3 outward = (randomOffset.normalized + Vector3.up * 1.5f).normalized;
            emitParams.velocity = outward * Random.Range(0.8f, 1.6f);

            // 通常より少し濃くて白い煙にする
            emitParams.startColor = new Color(0.95f, 0.95f, 0.95f, 0.35f);
            emitParams.startSize = initialSmokeSize * Random.Range(1.1f, 1.6f);

            smokeParticle.Emit(emitParams, 1);
        }
    }

    void ApplyFireScale(float multiplier)
    {
        if (bigFireEffect != null)
        {
            bigFireEffect.transform.localScale = Vector3.Lerp(
                bigFireEffect.transform.localScale,
                initialFireScale * multiplier,
                Time.deltaTime * 2f
            );
        }

        if (fireParticle != null)
        {
            var main = fireParticle.main;
            float sizeMod = Mathf.Clamp(multiplier, 0.3f, maxFireScaleMultiplier);
            main.startSize = Mathf.Lerp(main.startSize.constant, initialStartSize * sizeMod, Time.deltaTime * 2f);
            main.startSpeed = Mathf.Lerp(main.startSpeed.constant, initialStartSpeed * Mathf.Sqrt(sizeMod), Time.deltaTime * 2f);
        }

        if (lightFlicker != null)
        {
            lightFlicker.minIntensity = initialMinIntensity * multiplier;
            lightFlicker.maxIntensity = initialMaxIntensity * multiplier;
        }

        if (smokeParticle != null)
        {
            var emission = smokeParticle.emission;
            float rateRatio = (multiplier <= 1.0f)
                ? Mathf.Clamp(multiplier, 0.2f, 1.0f)
                : Mathf.Lerp(1.0f, maxSmokeRateMultiplier, (multiplier - 1.0f) / (maxFireScaleMultiplier - 1.0f));
            emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, initialSmokeRate * rateRatio, Time.deltaTime * 2f);

            var smokeMain = smokeParticle.main;
            float sizeSpeedMod = Mathf.Clamp(multiplier, 0.5f, maxFireScaleMultiplier);
            smokeMain.startSpeed = Mathf.Lerp(smokeMain.startSpeed.constant, initialSmokeSpeed * sizeSpeedMod, Time.deltaTime * 2f);
            smokeMain.startSize = Mathf.Lerp(smokeMain.startSize.constant, initialSmokeSize * Mathf.Sqrt(sizeSpeedMod), Time.deltaTime * 2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, checkRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, igniteRadius);
    }
}
