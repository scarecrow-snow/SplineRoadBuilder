using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
[ExecuteInEditMode]
public class SplineSampler : MonoBehaviour
{
    [SerializeField] SplineContainer m_splineContainer;

    // プロパティを通じてSplinesの数を外部から取得できるように
    public int NumSplines => m_splineContainer.Splines.Count;

    // 指定したスプラインの幅をサンプリングするメソッド
    public void SampleSplineWidth(int index, float t, float width, out Vector3 _p1, out Vector3 _p2)
    {
        // 指定した時刻における座標と向きを取得
        var time = t;
        if (time == 0)
        {
            time = 0.00001f; // ゼロの場合、微小な値に補正
        }
        m_splineContainer.Evaluate(index, time, out float3 position, out float3 forward, out float3 upVector);

        // スプラインの幅を考慮して_p1と_p2を計算
        float3 right = Vector3.Cross(forward, upVector).normalized;
        _p1 = position + (right * width);
        _p2 = position + (-right * width);
    }
}

