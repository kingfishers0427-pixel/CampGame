using UnityEngine;

public class TestWood : MonoBehaviour, IInteractable
{
    public string GetInteractText() => "斧で薪を割る";

    public void Interact(PlayerController player)
    {
        Debug.Log("【仮ギミック】パカーン！薪を割りました！ここでオブジェクトがバラバラになる処理が入ります。");
    }
}
