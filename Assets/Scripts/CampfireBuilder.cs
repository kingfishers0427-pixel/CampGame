using System.Collections.Generic;
using UnityEngine;

public class CampfireBuilder : MonoBehaviour
{
    [Header("生成する焚き火プレハブ（CampfireSpot）")]
    public GameObject campfireSpotPrefab;

    [Header("焚き火成立の条件")]
    [Tooltip("焚き火を作るのに必要な割った薪の本数")]
    public int requiredWoodCount = 4;

    [Tooltip("薪がまとまっていると判定する半径（メートル）")]
    public float clusterRadius = 0.75f;

    [Tooltip("既存の焚き火と近すぎると判定する距離（重複生成防止）")]
    public float minCampfireDistance = 1.5f;

    private float checkInterval = 0.5f; // 0.5秒ごとに判定（負荷軽減）
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        CheckAndCreateCampfire();
    }

    void CheckAndCreateCampfire()
    {
        if (campfireSpotPrefab == null) return;

        // シーン内で現在「燃えている火口（TinderItem）」を探す（引数なし版に修正）
        TinderItem[] allTinders = FindObjectsByType<TinderItem>();
        // シーン内にすでに存在する焚き火（CampfireZone）をすべて取得（引数なし版に修正）
        CampfireZone[] existingCampfires = FindObjectsByType<CampfireZone>();

        foreach (TinderItem tinder in allTinders)
        {
            if (!tinder.IsBurning) continue;

            // ① すでに近くに焚き火（CampfireZone）が存在する場合はスキップ
            bool alreadyHasCampfire = false;
            foreach (CampfireZone zone in existingCampfires)
            {
                if (Vector3.Distance(tinder.transform.position, zone.transform.position) < minCampfireDistance)
                {
                    alreadyHasCampfire = true;
                    break;
                }
            }
            if (alreadyHasCampfire) continue;

            // ② 火口の周囲にある薪を調べる
            Collider[] hits = Physics.OverlapSphere(tinder.transform.position, clusterRadius);
            HashSet<BurnableWood> nearbyWoods = new HashSet<BurnableWood>();

            foreach (Collider hit in hits)
            {
                BurnableWood wood = hit.GetComponentInParent<BurnableWood>();
                if (wood != null)
                {
                    nearbyWoods.Add(wood);
                }
            }

            // ③ 条件達成：割った薪が4本以上まとまっている！
            if (nearbyWoods.Count >= requiredWoodCount)
            {
                // 薪たちの中心位置を計算して、そこにCampfireSpotを生成
                Vector3 centerPos = Vector3.zero;
                foreach (BurnableWood w in nearbyWoods)
                {
                    centerPos += w.transform.position;
                }
                centerPos /= nearbyWoods.Count;

                Instantiate(campfireSpotPrefab, centerPos, Quaternion.identity);
                Debug.Log($"🔥 薪が{nearbyWoods.Count}本組まれた場所に新しい焚き火が発生しました！");
                break;
            }
        }
    }
}
