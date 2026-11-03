using UnityEngine;

public class CampfireInteractable : MonoBehaviour, IInteractable
{
    [Header("連動させる子要素のオブジェクト")]
    [SerializeField] private GameObject flameParticle; // 炎のパーティクル
    [SerializeField] private GameObject smokeParticle; // 煙のパーティクル
    [SerializeField] private GameObject sparkParticle; // 火の粉のパーティクル
    [SerializeField] private Light campfireLight;       // 揺らぐライト

    private bool isLit = false; // 現在、火がついているかどうかの状態

    private void Start()
    {
        // ゲーム開始時は、リアルに「火がついていない状態」からスタートさせる
        SetCampfireActive(false);
    }

    /// <summary>
    /// プレイヤーが視線を合わせた時に画面UIに表示する文字
    /// </summary>
    public string GetInteractText()
    {
        if (!isLit)
        {
            return "ファイアスターターで火をつける";
        }
        else
        {
            return "薪をくべる（火を長持ちさせる）";
        }
    }

    /// <summary>
    /// プレイヤーがEキーを押した時の実際の処理
    /// </summary>
    public void Interact(PlayerController player)
    {
        if (!isLit)
        {
            // 火がついていなければ、点火！
            isLit = true;
            SetCampfireActive(true);
            Debug.Log("【キャンプ飯への第一歩】シュカッ！ファイアスターターで焚き火に火をつけました！");
        }
        else
        {
            // すでに火がついている場合は、薪をくべる処理（将来的に燃焼時間を延長）
            Debug.Log("【キャンプ】薪をくべました。炎がパチパチと安定して燃え続けています。");
        }
    }

    /// <summary>
    /// パーティクルやライトの一斉ON/OFF切り替え
    /// </summary>
    private void SetCampfireActive(bool active)
    {
        if (flameParticle != null) flameParticle.SetActive(active);
        if (smokeParticle != null) smokeParticle.SetActive(active);
        if (sparkParticle != null) sparkParticle.SetActive(active);
        
        if (campfireLight != null)
        {
            campfireLight.enabled = active;
            // 揺らぎスクリプトも一緒にON/OFFする
            var flicker = campfireLight.GetComponent<CampfireFlicker>();
            if (flicker != null) flicker.enabled = active;
        }
    }
}
