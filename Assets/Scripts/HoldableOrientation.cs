using UnityEngine;

public class HoldableOrientation : MonoBehaviour
{
    [Header("掴んでいる時の姿勢補正")]
    public bool alignWhenHeld = true;

    [Tooltip("ONにすると、視線を上下に動かしてもアイテムが上下に傾かず水平を保ちます")]
    public bool ignoreVerticalPitch = true;

    [Tooltip("基準方向からの角度の微調整（X, Y, Z）")]
    public Vector3 holdRotationOffset = Vector3.zero;

    [Tooltip("目標の角度に揃うスピード")]
    public float alignSpeed = 15f;

    // 目標となる回転角度を計算して返す汎用メソッド
    public Quaternion GetTargetRotation(Transform cameraTransform)
    {
        Vector3 forwardDir = cameraTransform.forward;

        // 視線の上下の傾き（Y軸成分）を無視して、常に地面と水平な前方向を作る
        if (ignoreVerticalPitch)
        {
            forwardDir.y = 0f;
            if (forwardDir.sqrMagnitude > 0.001f)
            {
                forwardDir.Normalize();
            }
            else
            {
                forwardDir = Vector3.forward; // 真上・真下を向いた時の安全対策
            }
        }

        Quaternion baseRotation = Quaternion.LookRotation(forwardDir, Vector3.up);
        return baseRotation * Quaternion.Euler(holdRotationOffset);
    }
}
