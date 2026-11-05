using UnityEngine;

public class PickupableItem : MonoBehaviour
{
    [Header("このオブジェクトが持っているアイテムデータ")]
    [SerializeField] private ItemData itemData;

    // 外部からアイテムデータを取得するためのプロパティ
    public ItemData ItemData => itemData;

    // インスペクターでデータを直接セットするための初期化メソッド（取り出し時に使用）
    public void Setup(ItemData data)
    {
        itemData = data;
    }
}
