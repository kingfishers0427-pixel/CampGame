using UnityEngine;

public class PlayerInventoryController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private Transform dropPoint; // アイテムを取り出した時に出現させる位置（プレイヤーの手前など）

    [Header("取り出し用プレハブ設定")]
    [SerializeField] private GameObject pickupablePrefab; // PickupableItemスクリプトがついた汎用ベースオブジェクト、または各アイテムのプレハブ

    private PickupableItem currentTargetItem; // いま目の前（トリガー内）にあるアイテム

    private void Update()
    {
        // 【拾う】目の前にアイテムがあり、Eキーが押されたらリュックに入れる
        if (Input.GetKeyDown(KeyCode.E) && currentTargetItem != null)
        {
            playerInventory.AddItem(currentTargetItem.ItemData);
            Destroy(currentTargetItem.gameObject); // 地面のオブジェクトを消す
            currentTargetItem = null;
        }

        // 【取り出す】Qキーが押されたら、リュックからアイテムを1つ取り出して地面に生成する
        if (Input.GetKeyDown(KeyCode.Q))
        {
            DropItem();
        }
    }

    private void DropItem()
    {
        if (playerInventory.IsEmpty()) return;

        // リュックからデータを1つ取得
        ItemData removedData = playerInventory.RemoveLastItem();

        if (removedData != null && pickupablePrefab != null)
        {
            // プレイヤーの目の前（dropPoint）に実体を生成
            Vector3 spawnPosition = dropPoint != null ? dropPoint.position : transform.position + transform.forward * 1.5f;
            GameObject spawnedObj = Instantiate(pickupablePrefab, spawnPosition, Quaternion.identity);

            // 生成したオブジェクトに「お前の中身はこのItemDataだよ」と覚えさせる
            PickupableItem pickupScript = spawnedObj.GetComponent<PickupableItem>();
            if (pickupScript != null)
            {
                pickupScript.Setup(removedData);
            }
        }
    }

    // エリア内に入ったアイテムをロックオンする（簡易的なトリガー判定）
    private void OnTriggerEnter(Collider other)
    {
        PickupableItem item = other.GetComponent<PickupableItem>();
        if (item != null)
        {
            currentTargetItem = item;
            Debug.Log($"「{item.ItemData.itemName}」に近づいた（Eキーで拾う）");
        }
    }

    // エリアから離れたらロックオン解除
    private void OnTriggerExit(Collider other)
    {
        PickupableItem item = other.GetComponent<PickupableItem>();
        if (item != null && currentTargetItem == item)
        {
            currentTargetItem = null;
            Debug.Log("アイテムから離れた");
        }
    }
}
