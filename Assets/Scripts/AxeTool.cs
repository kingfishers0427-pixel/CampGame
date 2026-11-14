using UnityEngine;

public class AxeTool : MonoBehaviour
{
    [Header("斧の攻撃力")]
    public float chopPower = 1.0f;

    [Header("刃先の方向設定（赤い線の向き）")]
    public Vector3 bladeLocalDirection = Vector3.down;

    public Vector3 GetWorldBladeDirection()
    {
        return transform.TransformDirection(bladeLocalDirection).normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 start = transform.position;
        Vector3 end = start + GetWorldBladeDirection() * 0.8f;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.05f);
    }
}
