using UnityEngine;

/// <summary>
/// プレイヤーがインタラクト（調べる・触る）できるオブジェクトの共通インターフェース
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// 画面に表示する説明テキスト（例：「Eキーで薪を割る」「クリックでテントを調べる」など）
    /// </summary>
    string GetInteractText();

    /// <summary>
    /// 実際にオブジェクトに触った時の具体的な処理
    /// </summary>
    /// <param name="player">触ったプレイヤーのコンポーネント</param>
    void Interact(PlayerController player);
}
