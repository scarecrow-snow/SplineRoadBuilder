using System.Collections.Generic;
using UnityEditor.Splines;
using UnityEngine;

using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;

using UnityEngine.Splines;
using UnityEngine.UIElements;

using UnityEditor.Toolbars;
using UnityEditor.UIElements;

[ExecuteInEditMode]
[Overlay(typeof(SceneView), "Junction Builder", true)]
public class JunctionBuilderOverlay : Overlay
{
    Label SelectionInfoLabel;
    BuildJunctionButton buildJunctionButton;
    ClearButton clearButton;

    
    
    /// <summary>
    /// シーンビューにパネルを作成して
    /// 各種機能を提供する
    /// </summary>
    /// <returns></returns>
    public override VisualElement CreatePanelContent()
    {
        VisualElement root = new VisualElement();
        
        // テキストやボタンなどパネルに表示するもの
        SelectionInfoLabel = new Label();
        buildJunctionButton = new BuildJunctionButton();
        clearButton = new ClearButton();

        root.Add(SelectionInfoLabel);
        root.Add(buildJunctionButton);
        root.Add(clearButton);

        // 各種初期化など
        // スプラインが選択された場合のイベントを設定
        SplineSelection.changed += OnSelectionChanged;
        
        // ジャンクション作成ボタンが押された場合のイベント
        buildJunctionButton.clicked += OnBuildJunction;
        
        // 選択の解除ボタンが押された場合
        clearButton.clicked += () => {
            SplineToolUtility.ClearSelection();
            ClearSelectionInfo();
        };

        return root;
    }
    

    /// <summary>
    /// スプラインの端をつなげ、ジャンクションを作成する
    /// </summary>
    private void OnBuildJunction()
    {
        List<SelectedSplineElementInfo> selection = SplineToolUtility.GetSelection();
        // 選択したジャンクションが一つ以下の場合はそのまま抜ける
        if(selection.Count <= 1) return;

        Intersection intersection = new Intersection();
        foreach (var item in selection)
        {
            var container = (SplineContainer)item.target;
            var spline = container.Splines[item.targetIndex];
            var KnotsList = spline.Knots.ToList();
            intersection.AddJunction(item.targetIndex, item.knotIndex, spline, KnotsList[item.knotIndex]);
        }
        
        // 選択されているスプラインオブジェクトからSplineRoadを取り出し選択されたjunctionをintersectionとして登録する
        Selection.activeGameObject.GetComponent<SplineRoad>().AddJunction(intersection);
        
        SplineToolUtility.ClearSelection();
        ClearSelectionInfo();
    }

    private void OnSelectionChanged()
    {
        UpdateSelectionInfo();
    }

    private void ClearSelectionInfo()
    {
        SelectionInfoLabel.text = "";
    }

    /// <summary>
    /// ジャンクションでつなげる
    /// スプラインのパラメータをパネルに表示するテキストを更新する
    /// </summary>
    private void UpdateSelectionInfo()
    {
        ClearSelectionInfo();
        List<SelectedSplineElementInfo> infos = SplineToolUtility.GetSelection();

        foreach (var element in infos)
        {
            SelectionInfoLabel.text += $"Spline {element.targetIndex}, knot {element.knotIndex} \n";
        }
    }

}

[EditorToolbarElement(ID, typeof(SceneView))]
public class BuildJunctionButton : ToolbarButton
{
    public const string ID = "JunctionBuilder.BuildJunctionButton"; // ユニークなID

    public BuildJunctionButton()
    {
        tooltip = "スプラインの端をつなぎジャンクションを作成する";
        text = "ジャンクションを作成";
    }
  
}

[EditorToolbarElement(ID, typeof(SceneView))]
public class ClearButton : ToolbarButton
{
    public const string ID = "JunctionBuilder.ClearButton"; // ユニークなID

    public ClearButton()
    {
        tooltip = "選択したスプラインのエッジを解放する";
        text = "選択クリア";
    }
  
}
