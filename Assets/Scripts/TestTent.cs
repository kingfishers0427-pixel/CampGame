using UnityEngine;

public class TestTent : MonoBehaviour, IInteractable
{
    public string GetInteractText() => "テントを設営する / 調べる";

    public void Interact(PlayerController player)
    {
        Debug.Log("【仮ギミック】テントが起動しました！ここで設営完了アニメーションやメニューを開く処理が入ります。");
    }
}
