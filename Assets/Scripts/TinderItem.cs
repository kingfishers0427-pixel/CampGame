using UnityEngine;

public class TinderItem : MonoBehaviour
{
    [Header("着火の設定")]
    public int sparksNeeded = 3;

    [Header("燃焼寿命（秒）")]
    [Tooltip("種火がついてから燃え尽きて消えるまでの秒数")]
    public float burnDuration = 10.0f;

    [Header("種火の見た目（エフェクト）")]
    public GameObject smallFireEffect;

    private int currentSparks = 0;
    private float burnTimer = 0f;
    public bool IsBurning { get; private set; } = false;

    void Start()
    {
        if (smallFireEffect != null)
        {
            smallFireEffect.SetActive(false);
        }
    }

    void Update()
    {
        if (!IsBurning) return;

        burnTimer += Time.deltaTime;

        // 残り2秒になったら少しずつ炎を小さくする
        if (smallFireEffect != null && burnDuration - burnTimer < 2.0f)
        {
            float ratio = Mathf.Clamp01((burnDuration - burnTimer) / 2.0f);
            smallFireEffect.transform.localScale = Vector3.one * ratio;
        }

        // 寿命が尽きたら燃え尽きて消滅
        if (burnTimer >= burnDuration)
        {
            Debug.Log("麻ひもの火口が燃え尽きました。");
            Destroy(gameObject);
        }
    }

    public void ReceiveSpark()
    {
        if (IsBurning) return;

        currentSparks++;
        if (currentSparks >= sparksNeeded)
        {
            Ignite();
        }
    }

    public void Ignite()
    {
        IsBurning = true;
        burnTimer = 0f;
        Debug.Log("麻ひもの火口に火がついた！（種火状態）");

        if (smallFireEffect != null)
        {
            smallFireEffect.SetActive(true);
        }
    }
}
