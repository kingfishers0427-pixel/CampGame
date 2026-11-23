using UnityEngine;

public class FireStarterTool : MonoBehaviour
{
    [Header("火花の設定")]
    [Tooltip("火花を出す先端の位置（空のGameObjectを子に作って指定）")]
    public Transform sparkPoint;
    [Tooltip("火花パーティクル（あれば設定）")]
    public ParticleSystem sparkParticle;
    [Tooltip("火花が届く距離")]
    public float sparkRange = 0.6f;

    [Header("効果音（あれば設定）")]
    public AudioClip strikeSound;

    private Rigidbody rb;
    private float lastStrikeTime = -1f;
    public float strikeCooldown = 0.4f; // 連続で擦る間隔

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        if (rb == null)
        {
            rb = GetComponentInChildren<Rigidbody>();
        }
    }

    void Update()
    {
        // 左クリック長押しで「掴まれている（useGravityがfalse）」間だけ操作可能
        bool isHeld = (rb != null && !rb.useGravity);

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F))
        {
            if (!isHeld)
            {
                Debug.Log($"[FireStarter] キーは押されましたが、掴み判定(isHeld)がfalseです。rb={(rb != null ? rb.name : "null")}, useGravity={(rb != null ? rb.useGravity.ToString() : "-")}");
                return;
            }

            if (Time.time - lastStrikeTime >= strikeCooldown)
            {
                StrikeSparks();
            }
        }
    }

    void StrikeSparks()
    {
        lastStrikeTime = Time.time;
        Vector3 origin = sparkPoint != null ? sparkPoint.position : transform.position;

        // 1. 火花エフェクト再生（Play()よりも確実に毎回火花を飛ばすEmitを使用）
        if (sparkParticle != null)
        {
            sparkParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sparkParticle.Play();
            sparkParticle.Emit(25); // 強制的に25粒の火花を放出
            Debug.Log("✨ 火花を散らしました！");
        }
        else
        {
            Debug.LogWarning("[FireStarter] Inspectorの Spark Particle が設定されていません（None）！");
        }

        // 2. 効果音再生
        if (strikeSound != null && Camera.main != null)
        {
            AudioSource camAudio = Camera.main.GetComponent<AudioSource>();
            if (camAudio != null)
            {
                camAudio.PlayOneShot(strikeSound);
            }
        }

        // 3. 先端の周囲（半径 sparkRange）にある「麻ひもの火口」を探して火花を当てる
        Collider[] hits = Physics.OverlapSphere(origin, sparkRange);
        foreach (Collider hit in hits)
        {
            TinderItem tinder = hit.GetComponentInParent<TinderItem>();
            if (tinder != null)
            {
                tinder.ReceiveSpark();
            }
        }
    }

    // Sceneビューで火花が届く範囲を黄色い球で表示（調整用）
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = sparkPoint != null ? sparkPoint.position : transform.position;
        Gizmos.DrawWireSphere(origin, sparkRange);
    }
}
