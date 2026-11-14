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
    private HoldableOrientation grabbedOrientation;

    private PlayerInventoryController inventory;

    void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        inventory = GetComponent<PlayerInventoryController>();
        if (inventory == null)
        {
            Debug.LogError("Playerに PlayerInventoryController スクリプトがついていません！");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G) && inventory != null && inventory.GetCurrentItem() != null)
        {
            PlaceItem();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickupItem();
        }

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

            if (grabbedOrientation != null && grabbedOrientation.alignWhenHeld)
            {
                Quaternion targetRotation = grabbedOrientation.GetTargetRotation(playerCamera);
                
                Quaternion smoothRotation = Quaternion.Slerp(
                    grabbedRigidbody.rotation, 
                    targetRotation, 
                    Time.fixedDeltaTime * grabbedOrientation.alignSpeed
                );
                grabbedRigidbody.MoveRotation(smoothRotation);
            }
        }
    }

    void PlaceItem()
    {
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
            spawnPosition = hit.point + Vector3.up * 0.05f;
        }

        Quaternion spawnRotation = Quaternion.Euler(itemToPlace.defaultSpawnRotation);

        GameObject placedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);
        placedObject.name = itemToPlace.itemName;

        PickupableItem pickupScript = placedObject.GetComponent<PickupableItem>();
        if (pickupScript == null)
        {
            pickupScript = placedObject.AddComponent<PickupableItem>();
        }
        pickupScript.Setup(itemToPlace);

        Debug.Log(itemToPlace.itemName + " をリュックから取り出して設置しました。");
        
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
                if (inventory != null && inventory.AddItem(targetItem.ItemData))
                {
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
                Rigidbody rb = targetItem.GetComponentInParent<Rigidbody>();
                if (rb == null) rb = targetItem.GetComponentInChildren<Rigidbody>();

                if (rb != null)
                {
                    grabbedRigidbody = rb;
                    grabbedRigidbody.useGravity = false; 
                    grabbedRigidbody.linearDamping = 10f; 

                    grabbedOrientation = rb.GetComponent<HoldableOrientation>();
                    if (grabbedOrientation == null) grabbedOrientation = rb.GetComponentInChildren<HoldableOrientation>();
                    if (grabbedOrientation == null) grabbedOrientation = rb.GetComponentInParent<HoldableOrientation>();

                    if (grabbedOrientation != null)
                    {
                        Debug.Log($"姿勢制御ON: {grabbedOrientation.gameObject.name} を掴みました！");
                    }
                    else
                    {
                        Debug.LogWarning("掴んだオブジェクトに HoldableOrientation が見つかりません！");
                    }
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
        grabbedOrientation = null;
    }
}
