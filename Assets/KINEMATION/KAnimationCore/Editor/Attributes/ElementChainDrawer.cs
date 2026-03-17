// Designed by KINEMATION, 2024.

using System;
using KINEMATION.KAnimationCore.Editor.Misc;
using KINEMATION.KAnimationCore.Runtime.Rig;

using System.Collections.Generic;
using System.Linq;
using KINEMATION.KAnimationCore.Runtime.Attributes;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.KAnimationCore.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(KRigElementChain))]
    public class ElementChainDrawer : PropertyDrawer
    {
        private CustomElementChainDrawerAttribute GetCustomChainAttribute()
        {
            CustomElementChainDrawerAttribute attr = null;

            var attributes = fieldInfo.GetCustomAttributes(true);
            foreach (var customAttribute in attributes)
            {
                attr = customAttribute as CustomElementChainDrawerAttribute;
                if (attr != null) break;
            }

            return attr;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            KRig rig = RigDrawerUtility.TryGetRigAsset(fieldInfo, property);

            SerializedProperty elementChain = property.FindPropertyRelative("elementChain");
            SerializedProperty chainName = property.FindPropertyRelative("chainName");

            if (rig != null)
            {
                var rigHierarchy = rig.rigHierarchy;

                float labelWidth = EditorGUIUtility.labelWidth;
                var customChain = GetCustomChainAttribute();

                Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
                Rect buttonRect = position;

                string buttonText = $"Edit {chainName.stringValue}";

                if (customChain is {drawLabel: true})
                {
                    EditorGUI.PrefixLabel(labelRect, label);
                    labelRect.x += labelRect.width;
                    labelRect.width = (position.width - labelWidth) / 2f;

                    buttonRect.x = labelRect.x;
                    buttonRect.width = position.width - labelWidth;

                    buttonText = $"Edit {label.text}";
                }

                if (customChain is {drawTextField: true})
                {
                    chainName.stringValue = EditorGUI.TextField(labelRect, chainName.stringValue);

                    buttonRect.x = labelRect.x + labelRect.width;
                    buttonRect.width = position.width - (buttonRect.x - position.x);

                    buttonText = "Edit";
                }

                if (GUI.Button(buttonRect, buttonText))
                {
                    List<int> selectedIds = new List<int>();

                    // Get the active element indexes.
                    int arraySize = elementChain.arraySize;
                    for (int i = 0; i < arraySize; i++)
                    {
                        var indexProp
                            = elementChain.GetArrayElementAtIndex(i).FindPropertyRelative("index");
                        selectedIds.Add(indexProp.intValue + 1);
                    }

                    var elementNames = rigHierarchy.Select(element => element.name).ToList();
                    KSelectorWindow.ShowWindow(ref elementNames, ref rig.rigDepths,
                        (selectedName, selectedIndex) =>
                        {
                            // --- 核心修改：当在树中打勾/选择时立即触发 ---
                            // 这里的 selectedName 就是你勾选的骨骼名字
                            chainName.stringValue = selectedName;


                            // 立即应用属性修改，这样下方方框的 TextField 会实时刷新显示名字
                            property.serializedObject.ApplyModifiedProperties();

                            // 标记对象为脏，确保 Unity 界面重绘
                            EditorUtility.SetDirty(property.serializedObject.targetObject);
                        },
                        items =>
                        {
                            elementChain.ClearArray();

                            string firstName = "None";
                            bool firstSet = false;

                            foreach (var selection in items)
                            {
                                elementChain.arraySize++;
                                int lastIndex = elementChain.arraySize - 1;

                                var element = elementChain.GetArrayElementAtIndex(lastIndex);
                                var name = element.FindPropertyRelative("name");
                                var index = element.FindPropertyRelative("index");

                                name.stringValue = selection.Item1;
                                index.intValue = selection.Item2;

                                // 记录选中的第一个骨骼名字作为显示名
                                if (!firstSet)
                                {
                                    firstName = selection.Item1;
                                    firstSet = true;
                                }
                            }

                            // 3. 在这里统一更新文本框显示的名字
                            chainName.stringValue = firstName;

                            // 4. 核心：应用所有修改并通知系统数据已变
                            property.serializedObject.ApplyModifiedProperties();

                            // 5. 强制标记资源为脏，确保 Inspector 面板在窗口关闭后立即重绘
                            EditorUtility.SetDirty(property.serializedObject.targetObject);

                            // 6. 强制刷新所有视图，确保下方方框立即显示新名字
                            AssetDatabase.SaveAssets();
                        },
                        true, selectedIds, "Element Chain Selection"
                    );
                }
            }
            else
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = true;
            }

            EditorGUI.EndProperty();
        }
    }
}
