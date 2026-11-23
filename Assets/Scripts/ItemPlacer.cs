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
    public float minHoldDistance = 0.8f;  // 手前に寄せられる限界距離
    public float maxHoldDistance = 3.5f;  // 奥へ離せる限界距離
    private float currentHoldDistance;
    private Rigidbody grabbedRigidbody;
    private HoldableOrientation grabbedOrientation;

    [Header("掴み中のアイテム操作設定（R:回転 / Ctrl:上下左右 / ホイール:奥前）")]
    public float itemRotateSpeed = 5.0f;
    public float itemSlideSpeed = 0.05f;
    public float itemScrollSpeed = 0.5f;
    public CameraController cameraController;

    private PlayerController playerController;
    private bool isManipulatingItem = false; // 回転またはスライド操作中か
    private Quaternion manualTargetRotation;
    private Vector3 manualPositionOffset = Vector3.zero; // 上下左右の微調整オフセット

    private PlayerInventoryController inventory;

    void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        inventory = GetComponent<PlayerInventoryController>();
        playerController = GetComponent<PlayerController>();

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

        HandleItemManipulationInput();
    }

    void HandleItemManipulationInput()
    {
        if (grabbedRigidbody == null)
        {
            if (isManipulatingItem) SetManipulationMode(false);
            return;
        }

        // 1. マウスホイールで「奥・手前」の距離を調整（常に有効）
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            currentHoldDistance = Mathf.Clamp(currentHoldDistance + scroll * itemScrollSpeed, minHoldDistance, maxHoldDistance);
        }

        bool isRotateKey = Input.GetKey(KeyCode.R);
        bool isSlideKey = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        // 2. Rキーで「回転」、Ctrlキーで「上下・左右スライド」
        if (isRotateKey || isSlideKey)
        {
            if (!isManipulatingItem)
            {
                SetManipulationMode(true);
            }

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            if (isRotateKey)
            {
                // 角度の回転
                Quaternion yaw = Quaternion.AngleAxis(-mouseX * itemRotateSpeed, Vector3.up);
                Quaternion pitch = Quaternion.AngleAxis(mouseY * itemRotateSpeed, playerCamera.right);
                manualTargetRotation = yaw * pitch * manualTargetRotation;
            }
            else if (isSlideKey)
            {
                // 上下・左右への位置スライド（カメラの右方向・上方向を基準にずらす）
                Vector3 slideDelta = (playerCamera.right * mouseX + playerCamera.up * mouseY) * itemSlideSpeed;
                manualPositionOffset += slideDelta;

                // 画面外へ行き過ぎないように移動範囲を制限（±1.0m以内）
                manualPositionOffset = Vector3.ClampMagnitude(manualPositionOffset, 1.0f);
            }
        }
        else if (isManipulatingItem)
        {
            SetManipulationMode(false);
        }
    }

    void SetManipulationMode(bool active)
    {
        isManipulatingItem = active;

        if (cameraController != null)
        {
            cameraController.isCameraLocked = active;
        }
        if (playerController != null)
        {
            playerController.isLookLocked = active;
        }
    }

    void FixedUpdate()
    {
        if (grabbedRigidbody != null)
        {
            // 基本位置 ＋ 奥前距離 ＋ 上下左右の微調整オフセット
            if (!grabbedRigidbody.isKinematic) // isKinematic（固定状態）でない場合のみ速度と回転速度を操作する
            {
                Vector3 targetPosition = playerCamera.position + (playerCamera.forward * currentHoldDistance) + manualPositionOffset;
                Vector3 newVelocity = (targetPosition - grabbedRigidbody.position) * grabMoveSpeed;
                
                grabbedRigidbody.linearVelocity = newVelocity;
                grabbedRigidbody.angularVelocity = Vector3.zero;
            }

            if (grabbedOrientation != null && grabbedOrientation.alignWhenHeld && !Input.GetKey(KeyCode.R))
            {
                Quaternion targetRotation = grabbedOrientation.GetTargetRotation(playerCamera);
                
                Quaternion smoothRotation = Quaternion.Slerp(
                    grabbedRigidbody.rotation, 
                    targetRotation, 
                    Time.fixedDeltaTime * grabbedOrientation.alignSpeed
                );
                grabbedRigidbody.MoveRotation(smoothRotation);
                manualTargetRotation = smoothRotation;
            }
            else
            {
                Quaternion smoothRotation = Quaternion.Slerp(
                    grabbedRigidbody.rotation, 
                    manualTargetRotation, 
                    Time.fixedDeltaTime * 20f
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

        inventory.RemoveCurrentItem();
    }

    void TryPickupItem()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            // ★追加：火が付いている薪は熱くてリュックにしまえない
            BurnableWood burningWood = hit.collider.gameObject.GetComponentInParent<BurnableWood>();
            if (burningWood != null && burningWood.IsBurning)
            {
                Debug.Log("燃えている薪は熱くて拾えません！");
                return;
            }

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
            // 火が付いている薪は掴んで移動できない
            BurnableWood burningWood = hit.collider.gameObject.GetComponentInParent<BurnableWood>();
            if (burningWood != null && burningWood.IsBurning)
            {
                Debug.Log("燃えている薪は熱くて掴めません！");
                return;
            }

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

                    // 掴んだ瞬間の距離と角度を初期化
                    currentHoldDistance = Vector3.Distance(playerCamera.position, rb.position);
                    currentHoldDistance = Mathf.Clamp(currentHoldDistance, minHoldDistance, maxHoldDistance);
                    manualPositionOffset = Vector3.zero;
                    manualTargetRotation = grabbedRigidbody.rotation;

                    grabbedOrientation = rb.GetComponent<HoldableOrientation>();
                    if (grabbedOrientation == null) grabbedOrientation = rb.GetComponentInChildren<HoldableOrientation>();
                    if (grabbedOrientation == null) grabbedOrientation = rb.GetComponentInParent<HoldableOrientation>();
                }
            }
        }
    }

    void ReleaseGrabbedItem()
    {
        if (isManipulatingItem) SetManipulationMode(false);

        // isKinematicでない場合のみ重力を戻し、速度をリセットする
        if (!grabbedRigidbody.isKinematic)
        {
            grabbedRigidbody.useGravity = true;  
            grabbedRigidbody.linearDamping = 0f;  
            grabbedRigidbody.linearVelocity = Vector3.zero; 
            grabbedRigidbody.angularVelocity = Vector3.zero;
        }

        grabbedRigidbody = null;
        grabbedOrientation = null;
        manualPositionOffset = Vector3.zero;
    }
}
