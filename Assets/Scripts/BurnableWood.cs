using UnityEngine;

public class BurnableWood : MonoBehaviour
{
    public enum LengthAxis
    {
        Auto, // 起動時のサイズから一番長い軸を自動判定（おすすめ）
        X,
        Y,
        Z
    }

    [Header("燃焼と炭化の設定")]
    [Tooltip("薪が完全に燃え尽きるまでの時間（秒）※180秒＝3分")]
    public float totalBurnTime = 180f;

    [Tooltip("炭になった時の最終的な色")]
    public Color charredColor = new Color(0.08f, 0.08f, 0.08f);

    [Tooltip("燃えている最中に赤く熾火（おきび）のように光る色")]
    public Color emberEmissionColor = new Color(0.6f, 0.15f, 0.02f);

    [Header("縮小（細くなり方）の設定")]
    [Tooltip("薪の長さ方向（Autoならモデルの見た目から自動判定）")]
    public LengthAxis woodLengthAxis = LengthAxis.Auto;

    [Tooltip("「太さ」が細くなり始める進行度（0.15＝15%燃えた頃から細くなり始める）")]
    [Range(0f, 0.8f)]
    public float thicknessShrinkStart = 0.15f;

    [Tooltip("「長さ」が縮み始める進行度（0.75＝75%まで長さを保ち、最後の25%で短くなって消える）")]
    [Range(0.3f, 0.95f)]
    public float lengthShrinkStart = 0.75f;

    [Header("燃焼中の安定化設定（重力は残したまま暴れを防ぐ）")]
    [Tooltip("燃えている最中の移動抵抗（高くすると転がりにくくなる）")]
    public float burningLinearDamping = 3.0f;
    [Tooltip("燃えている最中の回転抵抗")]
    public float burningAngularDamping = 3.0f;

    public bool IsBurning { get; private set; } = false;
    private float burnedTime = 0f;

    public float RemainingFuelRatio => Mathf.Clamp01(1f - (burnedTime / totalBurnTime));

    private Renderer[] woodRenderers;
    private Material[] woodMaterials;
    private Color[] initialColors;
    private Vector3 initialScale;
    private LengthAxis detectedAxis;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialScale = transform.localScale;
        woodRenderers = GetComponentsInChildren<Renderer>();
        woodMaterials = new Material[woodRenderers.Length];
        initialColors = new Color[woodRenderers.Length];

        for (int i = 0; i < woodRenderers.Length; i++)
        {
            woodMaterials[i] = woodRenderers[i].material;
            if (woodMaterials[i].HasProperty("_BaseColor"))
            {
                initialColors[i] = woodMaterials[i].GetColor("_BaseColor");
            }
            else if (woodMaterials[i].HasProperty("_Color"))
            {
                initialColors[i] = woodMaterials[i].color;
            }
        }

        // 長手方向の軸を決定
        if (woodLengthAxis == LengthAxis.Auto)
        {
            detectedAxis = DetectLongestAxis();
        }
        else
        {
            detectedAxis = woodLengthAxis;
        }
    }

    // メッシュまたはコライダーの見た目サイズから一番長い軸を自動判定する
    LengthAxis DetectLongestAxis()
    {
        MeshFilter mf = GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Vector3 size = mf.sharedMesh.bounds.size;
            if (size.x >= size.y && size.x >= size.z) return LengthAxis.X;
            if (size.y >= size.x && size.y >= size.z) return LengthAxis.Y;
            return LengthAxis.Z;
        }
        return LengthAxis.Y; // 判定できない場合はUnity標準の円柱と同じY軸
    }

    // 火が燃え移った時に呼ばれる
    public void IgniteWood()
    {
        if (IsBurning) return;
        IsBurning = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearDamping = burningLinearDamping;
            rb.angularDamping = burningAngularDamping;
        }

        foreach (Material mat in woodMaterials)
        {
            if (mat != null)
            {
                mat.EnableKeyword("_EMISSION");
            }
        }
    }

    void Update()
    {
        if (!IsBurning) return;

        burnedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(burnedTime / totalBurnTime); // 0.0（生木）〜 1.0（完全燃焼）

        // 1. 徐々に黒く炭化させ、中盤は赤く熾火（おきび）のように発光させる
        float emberGlow = Mathf.Sin(progress * Mathf.PI);
        if (progress > 0.85f)
        {
            emberGlow *= Mathf.InverseLerp(1.0f, 0.85f, progress);
        }

        for (int i = 0; i < woodMaterials.Length; i++)
        {
            Material mat = woodMaterials[i];
            if (mat == null) continue;

            Color currentColor = Color.Lerp(initialColors[i], charredColor, progress);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", currentColor);
            if (mat.HasProperty("_Color")) mat.color = currentColor;

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emberEmissionColor * emberGlow * 1.5f);
            }
        }

        // 2. 「太さ」と「長さ」を別々のタイミングで0まで滑らかに縮小させる
        float thicknessRatio = 1.0f;
        if (progress > thicknessShrinkStart)
        {
            float t = Mathf.InverseLerp(thicknessShrinkStart, 1.0f, progress);
            thicknessRatio = Mathf.Lerp(1.0f, 0.0f, t); // 15%〜100%にかけて太さが1.0→0.0へ細くなる
        }

        float lengthRatio = 1.0f;
        if (progress > lengthShrinkStart)
        {
            float l = Mathf.InverseLerp(lengthShrinkStart, 1.0f, progress);
            lengthRatio = Mathf.Lerp(1.0f, 0.0f, l * l); // 75%までは長さを保ち、最後に1.0→0.0へ短くなる
        }

        Vector3 newScale = initialScale;
        switch (detectedAxis)
        {
            case LengthAxis.X:
                newScale = new Vector3(initialScale.x * lengthRatio, initialScale.y * thicknessRatio, initialScale.z * thicknessRatio);
                break;
            case LengthAxis.Y:
                newScale = new Vector3(initialScale.x * thicknessRatio, initialScale.y * lengthRatio, initialScale.z * thicknessRatio);
                break;
            case LengthAxis.Z:
                newScale = new Vector3(initialScale.x * thicknessRatio, initialScale.y * thicknessRatio, initialScale.z * lengthRatio);
                break;
        }
        transform.localScale = newScale;

        // 3. 完全に燃え尽きた（太さも長さも0になった）ら消滅
        if (burnedTime >= totalBurnTime)
        {
            Destroy(gameObject);
        }
    }
}
