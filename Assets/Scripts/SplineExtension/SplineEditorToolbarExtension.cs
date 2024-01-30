
using System.Collections.Generic;

using UnityEngine;

using UnityEditor;
using UnityEditor.Splines;



[ExecuteInEditMode]
public static class SplineToolUtility
{
    static List<SelectedSplineElementInfo> infos = new List<SelectedSplineElementInfo>();
    
    public static bool HasSelection()
    {
        return SplineSelection.HasActiveSplineSelection();
    }

    public static List<SelectedSplineElementInfo> GetSelection()
    {
        // pacage内アセンブリとして存在してinternalで設定されているのでバイパスする
        List<SelectableSplineElement> elements = SplineSelection.selection;
        
        foreach (SelectableSplineElement element in elements)
        {
            var info = new SelectedSplineElementInfo(element.target, element.targetIndex, element.knotIndex);

            if(!infos.Contains(info))
            {
                infos.Add(info);
                Debug.Log(info.ToString());
            }
        }

        return infos;
    }

    public static void ClearSelection()
    {
        infos.Clear();
    }
}

/// <summary>
/// SelectableSplineElementがinternalで設定されているので
/// 外部に渡すために新たに定義する
/// </summary>
public struct SelectedSplineElementInfo
{
    public Object target;
    public int targetIndex;
    public int knotIndex;
    public SelectedSplineElementInfo(Object Object, int Index, int knot)
    {
        target = Object;
        targetIndex = Index;
        knotIndex = knot;
    }
    public override string ToString()
    {
        return $"object = {target.name} :index = {targetIndex} : knotIndex = {knotIndex}";
    }
}




