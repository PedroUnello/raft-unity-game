using Assets.Script.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.Script.CustomDrawers
{
    [CustomPropertyDrawer(typeof(NormalizedVector3), true)]
    public class NormalizedVector3Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            position.height -= 2;

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            Rect pointRect = new(position.x, position.y, (position.width - 8) / 3, position.height);

            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(new GUIContent("-")).x;

            //X
            EditorGUI.PropertyField(pointRect, property.FindPropertyRelative("_x"), new GUIContent("X"));
            pointRect.x += pointRect.width + 4;
            //Y
            EditorGUI.PropertyField(pointRect, property.FindPropertyRelative("_y"), new GUIContent("Y"));
            pointRect.x += pointRect.width + 4;
            //Z
            EditorGUI.PropertyField(pointRect, property.FindPropertyRelative("_z"), new GUIContent("Z"));
            pointRect.x += pointRect.width + 4;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) + 2;
        }
    }
}

