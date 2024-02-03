using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
using UnityEngine.Splines;


[RequireComponent(typeof(SplineSampler))]
[ExecuteInEditMode]
public class SplineRoad : MonoBehaviour
{
    [SerializeField] bool m_onDrawGizumo;
    [SerializeField] private SplineSampler m_splineSampler;

    private List<Vector3> m_vertsP1 = new List<Vector3>();
    private List<Vector3> m_vertsP2 = new List<Vector3>();
    [SerializeField] private int resolution;
    [SerializeField] private MeshFilter m_meshFilter;

    [SerializeField] private float m_width;
    [SerializeField, Range(0.01f, 0.99f)] private float m_curveStep = 0.1f;


    [Tooltip("交差点、選択した端を結合する。")]
    [SerializeField] private List<Intersection> intersections = new List<Intersection>();


    void OnEnable()
    {
        Spline.Changed += OnSplineChanged;
        GetVerts();
        BuildMesh();
    }

    private void OnDisable()
    {
        Spline.Changed -= OnSplineChanged;
    }

    private void OnSplineChanged(Spline arg1, int arg2, SplineModification arg3)
    {
        GetVerts();
        BuildMesh();
    }

    public void OnCurveChangedByOverlayPanel()
    {
        BuildMesh();

        // overlayから変更した場合に値が保存されるように設定
        EditorUtility.SetDirty(gameObject);
    }


    private void GetVerts()
    {
        m_vertsP1.Clear();
        m_vertsP2.Clear();

        float step = 1f / (float)resolution;

        Vector3 p1;
        Vector3 p2;
        for (int j = 0; j < m_splineSampler.NumSplines; j++)
        {
            for (int i = 0; i < resolution; i++)
            {
                float t = step * i;
                m_splineSampler.SampleSplineWidth(j, t, m_width, out p1, out p2);

                m_vertsP1.Add(p1);
                m_vertsP2.Add(p2);
            }

            m_splineSampler.SampleSplineWidth(j, 1f, m_width, out p1, out p2);
            m_vertsP1.Add(p1);
            m_vertsP2.Add(p2);

        }

    }
    public bool HasIntersection()
    {
        return intersections.Count > 0;
    }
    
    public Intersection[] GetIntersections()
    {
        return intersections.ToArray();
    }

   

    public void AddIntersection(Intersection intersection)
    {
        if (!intersections.Contains(intersection))
        {
            intersections.Add(intersection);
            
            BuildMesh();
            return;
        }
        
    }


    public void ClearIntersections()
    {
        intersections.Clear();
        BuildMesh();
    }

    private void OnDrawGizmos()
    {
        if (!m_onDrawGizumo) return;

        Handles.color = Color.white;

        for (int i = 0; i < m_vertsP1.Count; i++)
        {
            Handles.matrix = transform.localToWorldMatrix;
            Handles.SphereHandleCap(0, m_vertsP1[i], Quaternion.identity, 1f, EventType.Repaint);
            Handles.SphereHandleCap(0, m_vertsP2[i], Quaternion.identity, 1f, EventType.Repaint);

            Handles.DrawLine(m_vertsP1[i], m_vertsP2[i]);
        }


        // ジャンクションの位置を取得して描画
        if (intersections.Count <= 0) return;


        for (int i = 0; i < intersections.Count; i++)
        {
            Vector3 addPos = new Vector3();
            foreach (JunctionInfo info in intersections[i].GetJunctions())
            {
                m_splineSampler.SampleSplineWidth(info.splineIndex, info.knotIndex == 0 ? 0f : 1f, m_width, out Vector3 p1, out Vector3 p2);
                Handles.color = Color.blue;
                Handles.SphereHandleCap(0, p1, Quaternion.identity, 1f, EventType.Repaint);
                Handles.color = Color.red;
                Handles.SphereHandleCap(0, p2, Quaternion.identity, 1f, EventType.Repaint);

                addPos += p1;
                addPos += p2;
            }

            Handles.color = Color.white;
            Vector3 centerPos = addPos / (intersections[i].junctions.Count * 2);
            Handles.DrawWireDisc(centerPos, Vector3.up, 0.5f);
        }

    }

    public void BuildMesh()
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        AddRoadGeometry(ref verts, ref tris);

        AddIntersectionGeometry(ref verts, ref tris);

        // meshに流し込む
        Mesh m = new Mesh();
        m.vertices = verts.ToArray();
        m.triangles = tris.ToArray();
        m_meshFilter.mesh = m;

    }


    private void AddRoadGeometry(ref List<Vector3> verts, ref List<int> tris)
    {
        // 道路のメッシュ
        for (int currentSplineIndex = 0; currentSplineIndex < m_splineSampler.NumSplines; currentSplineIndex++)
        {
            int splineOffset = resolution * currentSplineIndex;
            splineOffset += currentSplineIndex;
            for (int currentSplinePoint = 1; currentSplinePoint < resolution + 1; currentSplinePoint++)
            {
                int vertoffset = splineOffset + currentSplinePoint;

                Vector3 p1 = m_vertsP1[vertoffset - 1];
                Vector3 p2 = m_vertsP2[vertoffset - 1];
                Vector3 p3 = m_vertsP1[vertoffset];
                Vector3 p4 = m_vertsP2[vertoffset];

                int offset = 4 * resolution * currentSplineIndex;
                offset += 4 * (currentSplinePoint - 1);

                int t1 = offset + 0;
                int t2 = offset + 2;
                int t3 = offset + 3;

                int t4 = offset + 3;
                int t5 = offset + 1;
                int t6 = offset + 0;

                verts.AddRange(new List<Vector3> { p1, p2, p3, p4 });
                tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });
            }
        }
    }

    private void AddIntersectionGeometry(ref List<Vector3> verts, ref List<int> tris)
    {
        // 交差点のメッシュ作成用の頂点を算出する
        for (int i = 0; i < intersections.Count; i++)
        {
            Intersection intersection = intersections[i];

            List<JunctionEdge> junctionEdges = new List<JunctionEdge>();

            Vector3 center = new Vector3();

            foreach (JunctionInfo junction in intersection.GetJunctions())
            {
                int splineIndex = junction.splineIndex;
                float t = junction.knotIndex == 0 ? 0f : 1f;
                m_splineSampler.SampleSplineWidth(splineIndex, t, m_width, out Vector3 p1, out Vector3 p2);

                //junctionのインデックスが0の場合、splineはjunctionに背を向けている
                //junctionのインデックスが0より大きければ、splineはjunctionに面している。
                if (junction.knotIndex == 0)
                {
                    junctionEdges.Add(new JunctionEdge(p1, p2));

                }
                else
                {
                    junctionEdges.Add(new JunctionEdge(p2, p1));
                }

                center += p1;
                center += p2;
            }

            center = center / (junctionEdges.Count * 2);

            // ソート
            junctionEdges.Sort((x, y) =>
            {
                Vector3 xDir = x.Center - center;
                Vector3 yDir = y.Center - center;

                float angleA = Vector3.SignedAngle(center.normalized, xDir.normalized, Vector3.up);
                float angleB = Vector3.SignedAngle(center.normalized, yDir.normalized, Vector3.up);

                if (angleA > angleB)
                {
                    return -1;
                }

                if (angleA < angleB)
                {
                    return 1;
                }

                return 0;
            });



            // エッジを滑らかにするため、ベジェ曲線で補完する
            List<Vector3> curvePoints = new List<Vector3>();

            for (int j = 1; j <= junctionEdges.Count; j++)
            {
                // 各種ポイントを指定
                Vector3 startPoint = junctionEdges[j - 1].left;
                Vector3 endPoint = (j < junctionEdges.Count) ? junctionEdges[j].right : junctionEdges[0].right;
                Vector3 mid = Vector3.Lerp(startPoint, endPoint, 0.5f);
                Vector3 dir = center - mid;
                mid = mid - dir;
                Vector3 c = Vector3.Lerp(mid, center, intersection.curves[j - 1]);

                // ベジェ曲線で補完する
                BezierCurve curve = new BezierCurve(startPoint, c, endPoint);

                // 始点から終点までベジェ曲線で補完して座標をListに追加していく
                curvePoints.Add(startPoint);
                for (float t = 0f; t < 1f; t += m_curveStep)
                {
                    Vector3 pos = CurveUtility.EvaluatePosition(curve, t);
                    curvePoints.Add(pos);
                }

                curvePoints.Add(endPoint);
            }

            curvePoints.Reverse();


            int pointOffset = verts.Count;

            for (int j = 1; j <= curvePoints.Count; j++)
            {
                verts.Add(center);
                verts.Add(curvePoints[j - 1]);
                if (j == curvePoints.Count)
                {
                    verts.Add(curvePoints[0]);
                }
                else
                {
                    verts.Add(curvePoints[j]);
                }

                tris.Add(pointOffset + ((j - 1) * 3) + 0);
                tris.Add(pointOffset + ((j - 1) * 3) + 1);
                tris.Add(pointOffset + ((j - 1) * 3) + 2);
            }
        }
    }




    struct JunctionEdge
    {
        public Vector3 left;
        public Vector3 right;
        public Vector3 Center => (left + right) / 2;

        public JunctionEdge(Vector3 p1, Vector3 p2)
        {
            this.left = p1;
            this.right = p2;
        }
    }



}
