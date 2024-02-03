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
using UnityEditor.EditorTools;
using System;

using Cysharp.Threading.Tasks;

[ExecuteInEditMode]
[Overlay(typeof(SceneView), id: ID_OVERLAY, displayName: "Junction Builder", true)]
public class JunctionBuilderOverlay : Overlay
{
    private const string ID_OVERLAY = "JunctionBuilder-overlay";

    Label SelectionInfoLabel;
    BuildJunctionButton buildJunctionButton;
    ClearButton clearButton;

    VisualElement curveSlidersParent;

    List<SelectedSplineElementInfo> selectedInfos;
    Intersection currentIntersection;

    private event Action changeCurveValueEventHandler;

    // 各種初期化処理を行う
    public override void OnCreated()
    {
        // テキストやボタンなどパネルに表示するもの
        SelectionInfoLabel = new Label();
        buildJunctionButton = new BuildJunctionButton();
        clearButton = new ClearButton();
        curveSlidersParent = new VisualElement();

        selectedInfos = new List<SelectedSplineElementInfo>();

        // 各種初期化など
        // スプラインが選択された場合のイベントを設定
        SplineSelection.changed += OnSplineChanged;

        // ジャンクション作成ボタンが押された場合のイベント
        buildJunctionButton.clicked += OnBuildJunction;

        // カーブをスライダーで変更したときの処理
        //changeCurveValueEvent += Selection.activeGameObject.GetComponent<SplineRoad>().BuildMesh;
        InitializeChangeCurveValueEvent().Forget();

        // 選択の解除ボタンが押された場合
        clearButton.clicked += () =>
        {
            currentIntersection = null;
            curveSlidersParent.Clear();
            selectedInfos.Clear();
            ClearSelectionInfo();
        };
    }

    private async UniTaskVoid InitializeChangeCurveValueEvent()
    {
        await UniTask.WaitUntil(() => Selection.activeGameObject != null);

        await UniTask.WaitUntil(() => Selection.activeGameObject.GetComponent<SplineRoad>() != null);

        // カーブの値を変更した場合に呼び出すeventを登録
        changeCurveValueEventHandler += Selection.activeGameObject.GetComponent<SplineRoad>().OnCurveChangedByOverlayPanel;
        
    }
    

    /// <summary>
    /// シーンビューにパネルを作成して
    /// 各種機能を提供する
    /// このメソッドはパネルがON/OFFされるたびに呼ばれるので注意
    /// </summary>
    /// <returns></returns>
    public override VisualElement CreatePanelContent()
    {
        VisualElement root = new VisualElement();

        root.Add(SelectionInfoLabel);
        root.Add(buildJunctionButton);
        root.Add(clearButton);

        root.Add(curveSlidersParent);

        selectedInfos.Clear();

        

        return root;
    }

    private void ShowIntersction(Intersection intersection)
    {
        if(intersection == null) return;

        curveSlidersParent.Clear();

        SelectionInfoLabel.text = "Selected InterSection";
        
        VisibleButtons(false);

        for (int i = 0; i < intersection.curves.Count; i++)
        {
            int index = i;
            Slider slider = new Slider($"Curve {i}", 0, 1, SliderDirection.Horizontal);
            slider.labelElement.style.minWidth = 60;
            slider.labelElement.style.maxWidth = 80;
            slider.value = intersection.curves[i];
            slider.RegisterValueChangedCallback((x) =>
            {
                intersection.curves[index] = x.newValue;
                changeCurveValueEventHandler?.Invoke();
               
            });

            curveSlidersParent.Add(slider);
        }

    }

    private void VisibleButtons(bool value)
    {
        buildJunctionButton.visible = value;
        clearButton.visible = value;
    }


    /// <summary>
    /// スプラインの端をつなげ、ジャンクションを作成する
    /// </summary>
    private void OnBuildJunction()
    {
        if (selectedInfos.Count <= 1) return;

        Intersection intersection = new Intersection();
        foreach (var item in selectedInfos)
        {
            var container = (SplineContainer)item.target;
            var spline = container.Splines[item.targetIndex];
            var KnotsList = spline.Knots.ToList();
            intersection.AddJunction(item.targetIndex, item.knotIndex, spline, KnotsList[item.knotIndex]);
        }

        // 選択されているスプラインオブジェクトからSplineRoadを取り出しintersectionとして登録する
        Selection.activeGameObject.GetComponent<SplineRoad>().AddIntersection(intersection);

        selectedInfos.Clear();
        ClearSelectionInfo();
    }

    // 選択したSplineがIntersecgtionに接続されているかを探す
    // 接続されていた場合trueを返す
    // 選択したIntersectionをcurrentIntersectionとして保持する
    private bool SerchIntersection()
    {
        var road = Selection.activeGameObject?.GetComponent<SplineRoad>();
        if (road == null && !road.HasIntersection()) return false;
        var intersections = road.GetIntersections();
        
        for (int i = 0; i < intersections.Length; i++)
        {
            List<SelectedSplineElementInfo> _infos = SplineToolUtility.GetSelection();

            foreach (var element in _infos)
            {
                foreach (var junction in intersections[i].junctions)
                {
                    if (element.targetIndex == junction.splineIndex
                    && element.knotIndex == junction.knotIndex)
                    {
                        //Debug.Log($"見つけた = {element.targetIndex}: {junction.splineIndex}");
                        currentIntersection = intersections[i];
                        return true;
                    }
                }
            }

        }
        return false;
    }

    private void OnSplineChanged()
    {
        if(SerchIntersection())
        {
            ShowIntersction(currentIntersection);
        }
        else
        {
            UpdateSelectionInfo();
        }
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
        if(selectedInfos.Count < 1)
        {
            currentIntersection = null;
            curveSlidersParent.Clear();
            
            ClearSelectionInfo();
            VisibleButtons(true);
        }

        List<SelectedSplineElementInfo> _infos = SplineToolUtility.GetSelection();

        foreach (var element in _infos)
        {
            if (!selectedInfos.Contains(element))
            {
                selectedInfos.Add(element);
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

}




