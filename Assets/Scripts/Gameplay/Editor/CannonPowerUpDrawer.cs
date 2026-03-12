using System;
using System.Linq;
using Gameplay.Interfaces;
using Gameplay.PowerUps;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomPropertyDrawer(typeof(CannonPowerUp))]
public class CannonPowerUpDrawer : PropertyDrawer
{
    private const float VerticalSpacing = 4f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var castSfx = property.FindPropertyRelative("castSfx");
        var castVfx = property.FindPropertyRelative("castVfx");
        var runningVfx = property.FindPropertyRelative("runningVfx");
        var effects = property.FindPropertyRelative("effects");

        var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        rect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;

        DrawProperty(ref rect, castSfx);
        DrawProperty(ref rect, castVfx);
        DrawProperty(ref rect, runningVfx);

        var list = CreateList(effects);
        rect.height = list.GetHeight();
        list.DoList(rect);

        EditorGUI.indentLevel--;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        var castSfx = property.FindPropertyRelative("castSfx");
        var castVfx = property.FindPropertyRelative("castVfx");
        var runningVfx = property.FindPropertyRelative("runningVfx");
        var effects = property.FindPropertyRelative("effects");

        float height = EditorGUIUtility.singleLineHeight + VerticalSpacing;
        height += EditorGUI.GetPropertyHeight(castSfx, true) + VerticalSpacing;
        height += EditorGUI.GetPropertyHeight(castVfx, true) + VerticalSpacing;
        height += EditorGUI.GetPropertyHeight(runningVfx, true) + VerticalSpacing;

        var list = CreateList(effects);
        height += list.GetHeight();

        return height;
    }

    private static void DrawProperty(ref Rect rect, SerializedProperty property)
    {
        rect.height = EditorGUI.GetPropertyHeight(property, true);
        EditorGUI.PropertyField(rect, property, true);
        rect.y += rect.height + VerticalSpacing;
    }

    private static ReorderableList CreateList(SerializedProperty effectsProperty)
    {
        var list = new ReorderableList(
            effectsProperty.serializedObject,
            effectsProperty,
            true,
            true,
            true,
            true);

        list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Effects");
        };

        list.elementHeightCallback = index =>
        {
            if (index < 0 || index >= effectsProperty.arraySize)
            {
                return EditorGUIUtility.singleLineHeight + 6f;
            }

            var element = effectsProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true) + 6f;
        };

        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            if (index < 0 || index >= effectsProperty.arraySize)
            {
                return;
            }

            var element = effectsProperty.GetArrayElementAtIndex(index);

            rect.y += 2f;
            rect.height = EditorGUI.GetPropertyHeight(element, true);

            string itemLabel = GetManagedReferenceTypeName(element, index);
            EditorGUI.PropertyField(rect, element, new GUIContent(itemLabel), true);
        };

        list.onAddDropdownCallback = (buttonRect, reorderableList) =>
        {
            ShowAddMenu(buttonRect, effectsProperty);
        };

        list.onRemoveCallback = reorderableList =>
        {
            if (reorderableList.index < 0 || reorderableList.index >= effectsProperty.arraySize)
            {
                return;
            }

            effectsProperty.serializedObject.Update();
            effectsProperty.DeleteArrayElementAtIndex(reorderableList.index);
            effectsProperty.serializedObject.ApplyModifiedProperties();
        };

        return list;
    }

    private static void ShowAddMenu(Rect buttonRect, SerializedProperty effectsProperty)
    {
        var menu = new GenericMenu();

        var effectTypes = TypeCache.GetTypesDerivedFrom<IEffect<IDamageable>>()
            .Where(type => !type.IsAbstract && !type.IsInterface && type.IsSerializable)
            .OrderBy(type => type.Name)
            .ToArray();

        if (effectTypes.Length == 0)
        {
            menu.AddDisabledItem(new GUIContent("No serializable effect types found"));
            menu.DropDown(buttonRect);
            return;
        }

        foreach (var type in effectTypes)
        {
            menu.AddItem(new GUIContent(type.Name), false, () =>
            {
                effectsProperty.serializedObject.Update();

                int index = effectsProperty.arraySize;
                effectsProperty.InsertArrayElementAtIndex(index);

                var element = effectsProperty.GetArrayElementAtIndex(index);
                element.managedReferenceValue = Activator.CreateInstance(type);

                effectsProperty.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.DropDown(buttonRect);
    }

    private static string GetManagedReferenceTypeName(SerializedProperty property, int index)
    {
        if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
        {
            return $"Effect {index}";
        }

        string fullTypeName = property.managedReferenceFullTypename;
        int lastSpace = fullTypeName.LastIndexOf(' ');
        string typeName = lastSpace >= 0
            ? fullTypeName[(lastSpace + 1)..]
            : fullTypeName;

        int lastDot = typeName.LastIndexOf('.');
        if (lastDot >= 0)
        {
            typeName = typeName[(lastDot + 1)..];
        }

        return typeName;
    }
}
