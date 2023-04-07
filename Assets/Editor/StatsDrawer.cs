using Assets.Script.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Script.CustomDrawers
{
   [CustomPropertyDrawer(typeof(Stats))]
    public class StatsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            
            label = EditorGUI.BeginProperty(position, label, property);

            position.height -= 6;

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            float space = position.height / 3;

            Rect barRect = new(position.x, position.y + space + 2, position.width, space);
            Rect speedRect = new(position.x, position.y + space * 2 + 4, position.width, space);

            SerializedProperty maxHealthProp = property.FindPropertyRelative("_maxHealth");
            SerializedProperty curHealthProp = property.FindPropertyRelative("_curHealth");
            SerializedProperty speed = property.FindPropertyRelative("_speed");

            int indLvl = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            //Life slider.
            float lWidth = EditorGUIUtility.labelWidth;
            float lateral = position.width / 3;
            EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(new GUIContent("Current Health")).x;
            EditorGUI.Slider(new(position.x, position.y, lateral * 2.1f, space), curHealthProp, 0, maxHealthProp.floatValue);
            EditorGUI.LabelField(new(position.x + lateral * 2.1f, position.y, lateral * 0.3f, space), "    /  ");
            EditorGUI.PropertyField(new(position.x + lateral * 2.4f, position.y, lateral * 0.6f, space), maxHealthProp, GUIContent.none);

            EditorGUI.indentLevel = indLvl;
            EditorGUIUtility.labelWidth = lWidth;

            //Life Bar
            EditorGUI.ProgressBar(barRect, curHealthProp.floatValue / maxHealthProp.floatValue, "");

            //Speed
            EditorGUI.PropertyField(speedRect, speed, new GUIContent("Speed"));

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) * 3 + 6;
        }

    }
}
