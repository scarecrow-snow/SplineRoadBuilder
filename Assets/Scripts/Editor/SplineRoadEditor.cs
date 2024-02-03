using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

[CustomEditor(typeof(SplineRoad))]
public class SplineRoadEditor : Editor
{
    SplineRoad splineRoad;

    private void OnEnable()
    {
        if (splineRoad == null)
        {
            splineRoad = target as SplineRoad;

        }

    }

    

    public override void OnInspectorGUI()
    {
        


        base.OnInspectorGUI();

        // インスペクタ上にボタンを表示する
        if (GUILayout.Button("Junction All Clear Button"))
        {
            // ダイアログを出す
            if (EditorUtility.DisplayDialog("Junction All Clear", "Are you sure you want to delete all the junctions?", "Yes", "No"))
            {
                // SplineRoad内のメソッドを呼び出す
                splineRoad.ClearIntersections();

                //SendMessageを使って実行することもできる
                //splineRoad.SendMessage ("ClearJunction", null, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

   

}
