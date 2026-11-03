using UnityEngine;

/// <summary>
/// キャンプゲームに登場するすべてのアイテムデータの共通設計図（ScriptableObject）
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "CampGame/Item")]
public class ItemData : ScriptableObject
{
    [Header("アイテムの基本情報")]
    public string itemName;            // アイテムのゲーム内表示名
    public Sprite itemIcon;            // インベントリ用のアイコン画像（仮オブジェクト時は空でOK）
    
    [Multiline(3)]
    public string itemDescription;     // アイテムの簡単な説明文

    [Header("サバイバル・生態系システム連動パラメーター")]
    public float odorRadius = 0f;      // 【超重要】クマやカラスを引き寄せる「ニオイの強さ・範囲」
    public bool isEdible = false;      // プレイヤーが食べられる食材かどうか
    public bool isFuel = false;        // 焚き火の燃料（薪など）としてくべられるか
}
