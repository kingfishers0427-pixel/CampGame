using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("リュックの初期アイテム（インスペクターから設定可能）")]
    [SerializeField] private List<ItemData> initialItems = new List<ItemData>();

    // 実際にリュックに入っているアイテムのリスト
    private List<ItemData> mainInventory = new List<ItemData>();

    private void Start()
    {
        // ゲーム開始時に、あらかじめ設定した初期アイテムをリュックに入れる
        foreach (var item in initialItems)
        {
            AddItem(item);
        }
    }

    // アイテムをリュックに追加する
    public void AddItem(ItemData item)
    {
        if (item == null) return;
        
        mainInventory.Add(item);
        Debug.Log($"リュックに「{item.itemName}」を入れました。（合計: {mainInventory.Count}個）");
    }

    // リュックの最後のアイテムを取り出す（ポップする）
    public ItemData RemoveLastItem()
    {
        if (mainInventory.Count == 0)
        {
            Debug.Log("リュックは空っぽです！");
            return null;
        }

        int lastIndex = mainInventory.Count - 1;
        ItemData item = mainInventory[lastIndex];
        mainInventory.RemoveAt(lastIndex);
        
        Debug.Log($"リュックから「{item.itemName}」を取り出しました。（残り: {mainInventory.Count}個）");
        return item;
    }

    // リュックが空かどうかを確認する
    public bool IsEmpty()
    {
        return mainInventory.Count == 0;
    }
}
