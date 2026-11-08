using UnityEngine;

public class PlayerInventoryController : MonoBehaviour
{
    [Header("リュックのスロット（最大4つ）")]
    public ItemData[] inventorySlots = new ItemData[4];

    [Header("現在選択しているスロット番号 (0～3)")]
    public int currentSlotIndex = 0;

    void Update()
    {
        // 【1～4キー】でリュックの中身を選択切り替え
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ChangeSlot(3);
    }

    // スロットを切り替える処理
    void ChangeSlot(int index)
    {
        currentSlotIndex = index;
        ItemData selectedItem = inventorySlots[currentSlotIndex];

        if (selectedItem != null)
        {
            Debug.Log($"【リュック】スロット {index + 1} : 「{selectedItem.itemName}」を選択中");
        }
        else
        {
            Debug.Log($"【リュック】スロット {index + 1} : （空っぽ）を選択中");
        }
    }

    // アイテムをリュックの空きスロットにしまう処理（成功したらtrueを返す）
    public bool AddItem(ItemData item)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null)
            {
                inventorySlots[i] = item;
                Debug.Log($"【リュック】スロット {i + 1} に「{item.itemName}」をしまいました！");
                return true;
            }
        }
        Debug.LogWarning("リュックがいっぱいでこれ以上入りません！");
        return false;
    }

    // 現在選択しているアイテムのデータを取得する
    public ItemData GetCurrentItem()
    {
        return inventorySlots[currentSlotIndex];
    }

    // 現在選択しているアイテムをリュックから消費（削除）する
    public void RemoveCurrentItem()
    {
        inventorySlots[currentSlotIndex] = null;
    }
}
