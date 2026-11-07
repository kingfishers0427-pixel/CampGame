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
    public GameObject defaultItemPrefab; 

    [Header("★新機能：物理ドラッグ操作の設定")]
    public float grabMoveSpeed = 12f;      // アイテムが手元に移動してくる追従スピード
    public float grabHoldDistance = 1.8f;  // 掴んでいる間、カメラからどれくらい離すか
    private Rigidbody grabbedRigidbody;    // 現在左クリックで掴んでいるアイテムのRigidbody

    void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        // -------------------------------------------------------------
        // 1. インベントリ操作システム（Gキー / Eキー）
        // -------------------------------------------------------------
        // 【Gキー】手持ちがあり、レティクルが有効な位置にあれば設置
        if (Input.GetKeyDown(KeyCode.G) && holdingItem != null)
        {
            PlaceItem();
        }

        // 【Eキー】目の前のものをインベントリに拾おうとする
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickupItem();
        }

        // -------------------------------------------------------------
        // 2. 物理ドラッグシステム（左クリック長押し）
        // -------------------------------------------------------------
        // クリックした瞬間：正面の物理オブジェクトを掴む
        if (Input.GetMouseButtonDown(0))
        {
            if (grabbedRigidbody == null)
            {
                TryGrabItemPhysics();
            }
        }

        // クリックを離した瞬間：掴んでいたオブジェクトを落とす
        if (Input.GetMouseButtonUp(0))
        {
            if (grabbedRigidbody != null)
            {
                ReleaseGrabbedItem();
            }
        }
    }

    void FixedUpdate()
    {
        // 掴んでいる間、カメラの正面にスムーズかつ物理的に追従させる
        if (grabbedRigidbody != null)
        {
            Vector3 targetPosition = playerCamera.position + (playerCamera.forward * grabHoldDistance);
            Vector3 newVelocity = (targetPosition - grabbedRigidbody.position) * grabMoveSpeed;
            
            // 最新のUnity仕様（linearVelocity / angularVelocity）
            grabbedRigidbody.linearVelocity = newVelocity;
            grabbedRigidbody.angularVelocity = Vector3.zero; // 持っている間は勝手に回転させない
        }
    }

    void PlaceItem()
    {
        if (holdingItem == null) return;

        GameObject prefabToSpawn = holdingItem.itemPrefab != null ? holdingItem.itemPrefab : defaultItemPrefab;

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
            spawnPosition = hit.point + Vector3.up * 0.05f;
        }

        GameObject placedObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        placedObject.name = holdingItem.itemName;

        PickupableItem pickupScript = placedObject.GetComponent<PickupableItem>();
        if (pickupScript == null)
        {
            pickupScript = placedObject.AddComponent<PickupableItem>();
        }
        pickupScript.Setup(holdingItem);

        Debug.Log(holdingItem.itemName + " を設置しました。手持ちが空になりました。");
        holdingItem = null; 
    }

    void TryPickupItem()
    {
        if (holdingItem != null) return; 

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            PickupableItem targetItem = hit.collider.gameObject.GetComponentInParent<PickupableItem>();

            if (targetItem != null)
            {
                if (grabbedRigidbody != null && grabbedRigidbody.gameObject == targetItem.gameObject)
                {
                    ReleaseGrabbedItem();
                }

                holdingItem = targetItem.ItemData;
                Destroy(targetItem.gameObject);
                
                Debug.Log(holdingItem.itemName + " を拾って手持ちに戻しました！もう一度Gキーで設置できます。");
            }
        }
    }

    void TryGrabItemPhysics()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            PickupableItem targetItem = hit.collider.gameObject.GetComponentInParent<PickupableItem>();

            if (targetItem != null)
            {
                Rigidbody rb = targetItem.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    grabbedRigidbody = rb;
                    grabbedRigidbody.useGravity = false; 
                    grabbedRigidbody.linearDamping = 10f; // ★drag を linearDamping に修正（空気抵抗）
                }
            }
        }
    }

    void ReleaseGrabbedItem()
    {
        grabbedRigidbody.useGravity = true;  
        grabbedRigidbody.linearDamping = 0f;  // ★ここも linearDamping に修正
        grabbedRigidbody.linearVelocity = Vector3.zero; 
        grabbedRigidbody.angularVelocity = Vector3.zero;
        grabbedRigidbody = null;
    }
}
