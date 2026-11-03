using UnityEngine;

public class ItemPlacer : MonoBehaviour
{
    [Header("設置したいアイテムのデータ")]
    public ItemData itemToPlace; // ここにItem_Lanternを入れます

    [Header("プレイヤーのカメラ（目の前の計算用）")]
    public Transform playerCamera; 

    [Header("プレイヤーからどれくらい離れた場所に置くか")]
    public float placeDistance = 2.0f;

    void Update()
    {
        // キーボードの「Gキー」を押したら設置する（キーはお好みで変更できます）
        if (Input.GetKeyDown(KeyCode.G))
        {
            PlaceItem();
        }
    }

    void PlaceItem()
    {
        // データのチェック
        if (itemToPlace == null)
        {
            Debug.LogWarning("設置するアイテムデータがセットされていません！");
            return;
        }
        if (itemToPlace.itemPrefab == null)
        {
            Debug.LogWarning("アイテムデータに3Dモデル（Prefab）が登録されていません！");
            return;
        }

        // プレイヤーのカメラの正面方向から、設置する位置（座標）を計算する
        Vector3 spawnPosition = playerCamera.position + (playerCamera.forward * placeDistance);
        
        // 地面に置くため、高さ（Y軸）をプレイヤーの足元に合わせる調整（簡易版）
        spawnPosition.y = transform.position.y;

        // ★データアセットに登録されている3Dモデルをゲーム世界に出現させる！
        GameObject placedObject = Instantiate(itemToPlace.itemPrefab, spawnPosition, Quaternion.identity);

        // 生成された物体の名前をアイテム名にする
        placedObject.name = itemToPlace.itemName;

        Debug.Log(itemToPlace.itemName + " を設置しました！");
    }
}
