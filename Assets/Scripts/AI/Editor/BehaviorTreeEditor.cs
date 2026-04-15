using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Callbacks;

namespace AI.BehaviorTree.Editor
{
    public class BehaviorTreeEditor : EditorWindow
    {
        BehaviorTreeGraphView treeView;
        UnityEditor.Editor objEditor;
        VisualElement inspectorContainer;

        [MenuItem("Tools/Behavior Tree Editor")]
        public static void OpenWindow()
        {
            BehaviorTreeEditor wnd = GetWindow<BehaviorTreeEditor>();
            wnd.titleContent = new GUIContent("Behavior Tree");
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (Selection.activeObject is BehaviorTreeAsset)
            {
                OpenWindow();
                return true;
            }
            return false;
        }

        public void CreateGUI()
        {
            // Split View layout
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            // Left Pane (Inspector)
            var leftPane = new VisualElement();
            var label = new Label("Inspector");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 10;
            leftPane.Add(label);
            inspectorContainer = new ScrollView();
            leftPane.Add(inspectorContainer);

            // Right Pane (Graph)
            treeView = new BehaviorTreeGraphView();
            treeView.style.flexGrow = 1;
            treeView.OnNodeSelected = OnNodeSelectionChanged;

            splitView.Add(leftPane);
            splitView.Add(treeView);

            OnSelectionChange();
        }

        private void OnSelectionChange()
        {
            BehaviorTreeAsset tree = Selection.activeObject as BehaviorTreeAsset;
            
            // Allow debugging by selecting GameObjects with the runner during play mode
            if (Application.isPlaying)
            {
                if (Selection.activeGameObject)
                {
                    BehaviorTreeRunner runner = Selection.activeGameObject.GetComponent<BehaviorTreeRunner>();
                    if (runner != null && runner.treeAsset != null)
                    {
                        tree = runner.treeAsset; // Note: We actually want to debug `_runtimeTree` via reflection, but showing base tree works initially
                    }
                }
            }

            if (tree != null)
            {
                treeView?.PopulateView(tree);
            }
            else
            {
                inspectorContainer.Clear();
            }
        }

        void OnNodeSelectionChanged(NodeView nodeView)
        {
            inspectorContainer.Clear();
            UnityEngine.Object.DestroyImmediate(objEditor);
            objEditor = UnityEditor.Editor.CreateEditor(nodeView.node);
            IMGUIContainer container = new IMGUIContainer(() => {
                if (objEditor && objEditor.target)
                {
                    objEditor.OnInspectorGUI();
                }
            });
            inspectorContainer.Add(container);
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying && treeView != null)
            {
                treeView.UpdateNodeStates();
            }
        }
    }
}
