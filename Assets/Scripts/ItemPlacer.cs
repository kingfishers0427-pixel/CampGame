using UnityEngine;

public class ItemPlacer : MonoBehaviour
{
    [Header("現在の手持ちアイテムデータ（空でもOK）")]
    public ItemData holdingItem; 

    [Header("プレイヤーのカメラ（レティクルの中心用）")]
    public Transform playerCamera; 

    [Header("距離の設定")]
    public float placeDistance = 3.0f;
    public float pickupDistance = 3.0f;

    [Header("追加：アイテム固有のプレハブが無い場合に使う汎用プレハブ")]
    public GameObject defaultItemPrefab; // ★ここに先ほど作ったTest_PickupableItemなどを入れます

    void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        // 【Gキー】手持ちがあり、レティクルが有効な位置にあれば設置
        if (Input.GetKeyDown(KeyCode.G) && holdingItem != null)
        {
            PlaceItem();
        }

        // 【Eキー】目の前のものを拾おうとする
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickupItem();
        }
    }

    void PlaceItem()
    {
        if (holdingItem == null) return;

        // ★修正点：アイテム固有のプレハブがあればそれを使い、無ければデフォルトのプレハブを使う
        GameObject prefabToSpawn = holdingItem.itemPrefab != null ? holdingItem.itemPrefab : defaultItemPrefab;

        // どちらのプレハブも存在しない場合は設置できないので処理を抜ける
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"{holdingItem.itemName} の配置用プレハブ（およびデフォルトプレハブ）が設定されていません！");
            return;
        }

        Vector3 spawnPosition = playerCamera.position + (playerCamera.forward * placeDistance);
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, placeDistance))
        {
            spawnPosition = hit.point;
        }

        // 物体を出現させる
        GameObject placedObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        placedObject.name = holdingItem.itemName;

        // 出現させた物体に「拾える目印」がまだ付いていなければ自動でくっつけ、自身のデータを記憶させる
        PickupableItem pickupScript = placedObject.GetComponent<PickupableItem>();
        if (pickupScript == null)
        {
            pickupScript = placedObject.AddComponent<PickupableItem>();
        }
        pickupScript.Setup(holdingItem);

        Debug.Log(holdingItem.itemName + " を設置しました。手持ちが空になりました。");
        holdingItem = null; // 設置したので手持ちを空にする
    }

    void TryPickupItem()
    {
        if (holdingItem != null) return; // 手持ちがすでに埋まってたら拾えない

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            PickupableItem targetItem = hit.collider.gameObject.GetComponentInParent<PickupableItem>();

            if (targetItem != null)
            {
                holdingItem = targetItem.ItemData;
                Destroy(targetItem.gameObject);
                
                Debug.Log(holdingItem.itemName + " を拾って手持ちに戻しました！もう一度Gキーで設置できます。");
            }
        }
    }
}
