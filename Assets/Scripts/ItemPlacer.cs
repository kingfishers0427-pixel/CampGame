using UnityEngine;

public class ItemPlacer : MonoBehaviour
{
    [Header("プレイヤーのカメラ（レティクルの中心用）")]
    public Transform playerCamera; 

    [Header("距離の設定")]
    public float placeDistance = 3.0f;
    public float pickupDistance = 3.0f;

    [Header("追加：アイテム固有のプレハブが無い場合に使う汎用プレハブ")]
    public GameObject defaultItemPrefab; 

    [Header("物理ドラッグ操作の設定")]
    public float grabMoveSpeed = 12f;     
    public float grabHoldDistance = 1.8f; 
    private Rigidbody grabbedRigidbody;   

    // ★追加：連動させるリュックスクリプトの参照
    private PlayerInventoryController inventory;

    void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        // 同じPlayerオブジェクトからリュックのコンポーネントを取得
        inventory = GetComponent<PlayerInventoryController>();
        if (inventory == null)
        {
            Debug.LogError("Playerに PlayerInventoryController スクリプトがついていません！");
        }
    }

    void Update()
    {
        // -------------------------------------------------------------
        // 1. インベントリ操作システム（Gキー / Eキー連動版）
        // -------------------------------------------------------------
        // 【Gキー】リュックの選択中のスロットにアイテムがあれば設置
        if (Input.GetKeyDown(KeyCode.G) && inventory != null && inventory.GetCurrentItem() != null)
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
        if (Input.GetMouseButtonDown(0))
        {
            if (grabbedRigidbody == null) TryGrabItemPhysics();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (grabbedRigidbody != null) ReleaseGrabbedItem();
        }
    }

    void FixedUpdate()
    {
        if (grabbedRigidbody != null)
        {
            Vector3 targetPosition = playerCamera.position + (playerCamera.forward * grabHoldDistance);
            Vector3 newVelocity = (targetPosition - grabbedRigidbody.position) * grabMoveSpeed;
            
            grabbedRigidbody.linearVelocity = newVelocity;
            grabbedRigidbody.angularVelocity = Vector3.zero; 
        }
    }

    void PlaceItem()
    {
        // ★修正：手持ちデータではなく、リュックで現在選んでいるアイテムを呼び出す
        ItemData itemToPlace = inventory.GetCurrentItem();
        if (itemToPlace == null) return;

        GameObject prefabToSpawn = itemToPlace.itemPrefab != null ? itemToPlace.itemPrefab : defaultItemPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"{itemToPlace.itemName} の配置用プレハブが設定されていません！");
            return;
        }

        Vector3 spawnPosition = playerCamera.position + (playerCamera.forward * placeDistance);
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, placeDistance))
        {
            spawnPosition = hit.point + Vector3.up * 0.05f; // 床抜け防止
        }

        // ★最新修正：アイテムデータに登録された固有の回転角度（defaultSpawnRotation）を適用する
        Quaternion spawnRotation = Quaternion.Euler(itemToPlace.defaultSpawnRotation);

        // Quaternion.identity の代わりに spawnRotation を渡して生成
        GameObject placedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);
        placedObject.name = itemToPlace.itemName;

        PickupableItem pickupScript = placedObject.GetComponent<PickupableItem>();
        if (pickupScript == null)
        {
            pickupScript = placedObject.AddComponent<PickupableItem>();
        }
        pickupScript.Setup(itemToPlace);

        Debug.Log(itemToPlace.itemName + " をリュックから取り出して設置しました。");
        
        // ★修正：設置が成功したので、リュックのそのスロットを空にする
        inventory.RemoveCurrentItem();
    }

    void TryPickupItem()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            PickupableItem targetItem = hit.collider.gameObject.GetComponentInParent<PickupableItem>();

            if (targetItem != null)
            {
                // ★修正：拾ったデータを「リュックの空きスロット」にしまおうとする
                if (inventory != null && inventory.AddItem(targetItem.ItemData))
                {
                    // リュックに無事入ったら、ドラッグを解除してフィールドから消す
                    if (grabbedRigidbody != null && grabbedRigidbody.gameObject == targetItem.gameObject)
                    {
                        ReleaseGrabbedItem();
                    }
                    Destroy(targetItem.gameObject);
                }
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
                    grabbedRigidbody.linearDamping = 10f; 
                }
            }
        }
    }

    void ReleaseGrabbedItem()
    {
        grabbedRigidbody.useGravity = true;  
        grabbedRigidbody.linearDamping = 0f;  
        grabbedRigidbody.linearVelocity = Vector3.zero; 
        grabbedRigidbody.angularVelocity = Vector3.zero;
        grabbedRigidbody = null;
    }
}
