using System;
using System.Linq;
using Gameplay.PowerUps;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomPropertyDrawer(typeof(CannonPowerUp))]
public class CannonPowerUpDrawer : PropertyDrawer
{
    private const float Spacing = 4f;

    private static readonly string[] SimpleFields = { "config", "castSfx", "castVfx", "runningVfx" };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);

        if (!property.isExpanded) { EditorGUI.EndProperty(); return; }

        EditorGUI.indentLevel++;
        rect.y += EditorGUIUtility.singleLineHeight + Spacing;

        foreach (string fieldName in SimpleFields)
        {
            var prop = property.FindPropertyRelative(fieldName);
            if (prop != null) DrawProperty(ref rect, prop);
        }

        var listProp = property.FindPropertyRelative("effects");
        if (listProp != null)
        {
            var list = CreateList(listProp);
            rect.height = list.GetHeight();
            list.DoList(rect);
            rect.y += rect.height + Spacing;
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;

        float height = EditorGUIUtility.singleLineHeight + Spacing;

        foreach (string fieldName in SimpleFields)
        {
            var prop = property.FindPropertyRelative(fieldName);
            if (prop != null)
                height += EditorGUI.GetPropertyHeight(prop, true) + Spacing;
        }

        var listProp = property.FindPropertyRelative("effects");
        if (listProp != null)
            height += CreateList(listProp).GetHeight() + Spacing;

        return height;
    }

    private static void DrawProperty(ref Rect rect, SerializedProperty property)
    {
        rect.height = EditorGUI.GetPropertyHeight(property, true);
        EditorGUI.PropertyField(rect, property, true);
        rect.y += rect.height + Spacing;
    }

    private static ReorderableList CreateList(SerializedProperty listProp)
    {
        var list = new ReorderableList(listProp.serializedObject, listProp, true, true, true, true);

        list.drawHeaderCallback = rect =>
            EditorGUI.LabelField(rect, "Effects", EditorStyles.boldLabel);

        list.elementHeightCallback = index =>
        {
            if (index < 0 || index >= listProp.arraySize)
                return EditorGUIUtility.singleLineHeight + 6f;
            return EditorGUI.GetPropertyHeight(listProp.GetArrayElementAtIndex(index), true) + 6f;
        };

        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            if (index < 0 || index >= listProp.arraySize) return;
            var element = listProp.GetArrayElementAtIndex(index);
            rect.y += 2f;
            rect.height = EditorGUI.GetPropertyHeight(element, true);

            string typeName = GetTypeName(element, index);
            string category = GetCategory(element);
            string displayLabel = category.Length > 0 ? $"[{category}] {typeName}" : typeName;

            EditorGUI.PropertyField(rect, element, new GUIContent(displayLabel), true);
        };

        list.onAddDropdownCallback = (buttonRect, _) =>
            ShowAddMenu(buttonRect, listProp);

        list.onRemoveCallback = reorderableList =>
        {
            if (reorderableList.index < 0 || reorderableList.index >= listProp.arraySize) return;
            listProp.serializedObject.Update();
            listProp.DeleteArrayElementAtIndex(reorderableList.index);
            listProp.serializedObject.ApplyModifiedProperties();
        };

        return list;
    }

    private static void ShowAddMenu(Rect buttonRect, SerializedProperty listProp)
    {
        var menu = new GenericMenu();

        var types = TypeCache.GetTypesDerivedFrom<IPowerUpEffect>()
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsSerializable)
            .OrderBy(t => GetCategoryForType(t))
            .ThenBy(t => t.Name)
            .ToArray();

        if (types.Length == 0)
        {
            menu.AddDisabledItem(new GUIContent("No effect types found"));
            menu.DropDown(buttonRect);
            return;
        }

        foreach (var type in types)
        {
            string category = GetCategoryForType(type);
            string path = $"{category}/{type.Name}";

            menu.AddItem(new GUIContent(path), false, () =>
            {
                listProp.serializedObject.Update();
                int index = listProp.arraySize;
                listProp.InsertArrayElementAtIndex(index);
                listProp.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(type);
                listProp.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.DropDown(buttonRect);
    }

    private static string GetCategoryForType(Type type)
    {
        if (typeof(Gameplay.Interfaces.IEntity).IsAssignableFrom(type)) return "Enemy";

        var interfaces = type.GetInterfaces();
        foreach (var iface in interfaces)
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEffect<>))
                return "Enemy";
        }

        if (typeof(ICannonModifier).IsAssignableFrom(type)) return "Cannon";
        if (typeof(IProjectileModifier).IsAssignableFrom(type)) return "Projectile";
        if (typeof(IReactiveEffect).IsAssignableFrom(type)) return "Reactive";
        if (typeof(ISummonEffect).IsAssignableFrom(type)) return "Summon";
        return "Other";
    }

    private static string GetCategory(SerializedProperty property)
    {
        if (string.IsNullOrEmpty(property.managedReferenceFullTypename)) return "";

        string fullName = property.managedReferenceFullTypename;
        int lastSpace = fullName.LastIndexOf(' ');
        string qualifiedName = lastSpace >= 0 ? fullName[(lastSpace + 1)..] : fullName;

        Type type = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = asm.GetType(qualifiedName);
            if (type != null) break;
        }

        return type != null ? GetCategoryForType(type) : "";
    }

    private static string GetTypeName(SerializedProperty property, int index)
    {
        if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
            return $"Element {index}";

        string full = property.managedReferenceFullTypename;
        int lastSpace = full.LastIndexOf(' ');
        string name = lastSpace >= 0 ? full[(lastSpace + 1)..] : full;
        int lastDot = name.LastIndexOf('.');
        return lastDot >= 0 ? name[(lastDot + 1)..] : name;
    }
}