using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;


using UnityEngine.Splines;


[Serializable]
public struct JunctionInfo
{
    public int splineIndex;
    public int knotIndex;
    public Spline spline;
    public BezierKnot knot;

    public JunctionInfo(int splineIndex, int knotIndex, Spline spline, BezierKnot knot)
    {
        this.splineIndex = splineIndex;
        this.knotIndex = knotIndex;
        this.spline = spline;
        this.knot = knot;
    }

    public override bool Equals(object obj)
    {
        if (obj is JunctionInfo other)
        {
            // 各フィールドの等価性を確認
            return splineIndex == other.splineIndex &&
                   knotIndex == other.knotIndex &&
                   spline.Equals(other.spline) &&
                   knot.Equals(other.knot);
        }

        return false;
    }

    public override int GetHashCode()
    {
        
        return base.GetHashCode();
    }
}


[Serializable]
public class Intersection
{
    public List<JunctionInfo> junctions;
    
    public List<float> curves;
    

    public void AddJunction(int splineIndex, int knotIndex, Spline spline, BezierKnot knot)
    {
        if (junctions == null)
        {
            junctions = new List<JunctionInfo>();
            curves = new List<float>();
        }

        junctions.Add(new JunctionInfo(splineIndex, knotIndex, spline, knot));
        curves.Add(0.3f);
    }


    internal IEnumerable<JunctionInfo> GetJunctions()
    {
        return junctions;
    }


}

