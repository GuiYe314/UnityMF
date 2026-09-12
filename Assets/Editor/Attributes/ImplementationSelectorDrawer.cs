#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using AOT.HotUpdate.Framework.Serialization;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ImplementationSelectorAttribute))]
public sealed class ImplementationSelectorDrawer : PropertyDrawer
{
    private const float RemoveButtonWidth = 24f;
    private const float TypeButtonMinWidth = 120f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (IsManagedReferenceList(property))
        {
            DrawList(position, property, label);
        }
        else if (property.propertyType == SerializedPropertyType.ManagedReference)
        {
            DrawReference(position, property, label);
        }
        else
        {
            EditorGUI.HelpBox(
                position,
                "ImplementationSelector 只能用于带 SerializeReference 的接口/基类字段或集合。",
                MessageType.Error);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (IsManagedReferenceList(property))
            return GetListHeight(property);

        if (property.propertyType == SerializedPropertyType.ManagedReference)
            return GetReferenceHeight(property);

        return EditorGUIUtility.singleLineHeight * 2f;
    }

    private void DrawList(Rect position, SerializedProperty list, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        Rect header = new Rect(position.x, position.y, position.width, line);
        Rect addRect = new Rect(header.xMax - RemoveButtonWidth, header.y, RemoveButtonWidth, line);
        Rect foldoutRect = new Rect(header.x, header.y, header.width - RemoveButtonWidth - 2f, line);

        list.isExpanded = EditorGUI.Foldout(
            foldoutRect,
            list.isExpanded,
            new GUIContent($"{label.text} ({list.arraySize})", label.tooltip),
            true);

        using (new EditorGUI.DisabledScope(list.serializedObject.isEditingMultipleObjects))
        {
            if (GUI.Button(addRect, "+"))
                ShowTypeMenu(list, -1, GetDeclaredType());
        }

        if (!list.isExpanded)
            return;

        float y = header.yMax + spacing;
        EditorGUI.indentLevel++;

        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            float height = GetReferenceHeight(element);
            Rect elementRect = new Rect(position.x, y, position.width, height);

            if (DrawListElement(elementRect, list, element, i))
                break;

            y += height + spacing;
        }

        EditorGUI.indentLevel--;
    }

    private bool DrawListElement(
        Rect position,
        SerializedProperty list,
        SerializedProperty element,
        int index)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        Rect indented = EditorGUI.IndentedRect(
            new Rect(position.x, position.y, position.width, line));
        Rect removeRect = new Rect(
            indented.xMax - RemoveButtonWidth,
            indented.y,
            RemoveButtonWidth,
            line);

        float typeWidth = Math.Max(
            TypeButtonMinWidth,
            Math.Min(indented.width * 0.55f, indented.width - RemoveButtonWidth - 70f));

        Rect typeRect = new Rect(
            removeRect.x - typeWidth - 2f,
            indented.y,
            typeWidth,
            line);
        Rect foldoutRect = new Rect(
            indented.x,
            indented.y,
            Math.Max(40f, typeRect.x - indented.x - 2f),
            line);

        element.isExpanded = EditorGUI.Foldout(
            foldoutRect,
            element.isExpanded,
            $"Element {index}",
            true);

        if (GUI.Button(typeRect, GetTypeLabel(element), EditorStyles.popup))
            ShowTypeMenu(list, index, GetDeclaredType());

        if (GUI.Button(removeRect, "-"))
        {
            Undo.RecordObjects(list.serializedObject.targetObjects, "Remove implementation");
            element.managedReferenceValue = null;
            list.DeleteArrayElementAtIndex(index);
            list.serializedObject.ApplyModifiedProperties();
            return true;
        }

        if (element.isExpanded && !string.IsNullOrEmpty(element.managedReferenceFullTypename))
        {
            Rect childrenRect = new Rect(
                position.x,
                position.y + line + spacing,
                position.width,
                position.height - line - spacing);
            DrawChildren(childrenRect, element);
        }

        return false;
    }

    private void DrawReference(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        Rect header = new Rect(position.x, position.y, position.width, line);
        Rect indented = EditorGUI.IndentedRect(header);
        float typeWidth = Math.Max(TypeButtonMinWidth, indented.width * 0.55f);
        Rect typeRect = new Rect(indented.xMax - typeWidth, indented.y, typeWidth, line);
        Rect foldoutRect = new Rect(
            indented.x,
            indented.y,
            Math.Max(40f, typeRect.x - indented.x - 2f),
            line);

        property.isExpanded = EditorGUI.Foldout(
            foldoutRect,
            property.isExpanded,
            label,
            true);

        if (GUI.Button(typeRect, GetTypeLabel(property), EditorStyles.popup))
            ShowTypeMenu(property, -1, GetDeclaredType());

        if (property.isExpanded &&
            !string.IsNullOrEmpty(property.managedReferenceFullTypename))
        {
            Rect childrenRect = new Rect(
                position.x,
                position.y + line + spacing,
                position.width,
                position.height - line - spacing);
            DrawChildren(childrenRect, property);
        }
    }

    private static void DrawChildren(Rect position, SerializedProperty reference)
    {
        // 先保存路径，再开始绘制。字段编辑可能使 Unity 2022.3 的活动迭代器失效。
        List<string> paths = GetDirectChildPaths(reference);
        float y = position.y;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.indentLevel++;
        foreach (string path in paths)
        {
            SerializedProperty child = reference.serializedObject.FindProperty(path);
            if (child == null)
                continue;

            float height = EditorGUI.GetPropertyHeight(child, true);
            Rect childRect = new Rect(position.x, y, position.width, height);
            EditorGUI.PropertyField(childRect, child, true);
            y += height + spacing;
        }
        EditorGUI.indentLevel--;
    }

    private void ShowTypeMenu(
        SerializedProperty property,
        int listIndex,
        Type declaredType)
    {
        GenericMenu menu = new GenericMenu();
        UnityEngine.Object owner = property.serializedObject.targetObject;
        string path = property.propertyPath;

        menu.AddItem(
            new GUIContent("None"),
            IsNullReference(property, listIndex),
            () => SetReference(owner, path, listIndex, null));

        List<Type> types = GetImplementationTypes(declaredType);
        if (types.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No serializable implementations found"));
        }
        else
        {
            foreach (Type type in types)
            {
                Type captured = type;
                menu.AddItem(
                    new GUIContent(GetMenuPath(captured, declaredType)),
                    IsCurrentType(property, listIndex, captured),
                    () => SetReference(owner, path, listIndex, captured));
            }
        }

        menu.ShowAsContext();
    }

    private static void SetReference(
        UnityEngine.Object owner,
        string propertyPath,
        int listIndex,
        Type implementationType)
    {
        Undo.RecordObject(owner, "Change implementation");

        SerializedObject serializedObject = new SerializedObject(owner);
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        SerializedProperty reference;

        if (listIndex < 0 && IsManagedReferenceList(property))
        {
            int index = property.arraySize;
            property.InsertArrayElementAtIndex(index);
            reference = property.GetArrayElementAtIndex(index);
        }
        else if (listIndex >= 0)
        {
            if (listIndex >= property.arraySize)
                return;
            reference = property.GetArrayElementAtIndex(listIndex);
        }
        else
        {
            reference = property;
        }

        reference.managedReferenceValue =
            implementationType == null ? null : Activator.CreateInstance(implementationType);
        reference.isExpanded = true;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(owner);
    }

    private Type GetDeclaredType()
    {
        Type type = fieldInfo.FieldType;
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType)
        {
            Type[] arguments = type.GetGenericArguments();
            if (arguments.Length == 1)
                return arguments[0];
        }

        return type;
    }

    private static List<Type> GetImplementationTypes(Type declaredType)
    {
        if (declaredType == null)
            return new List<Type>();

        return TypeCache.GetTypesDerivedFrom(declaredType)
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                !type.IsGenericTypeDefinition &&
                type.IsSerializable &&
                type.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(type => GetInheritanceDepth(type, declaredType))
            .ThenBy(type => type.FullName)
            .ToList();
    }

    private static string GetMenuPath(Type type, Type declaredType)
    {
        string group = string.IsNullOrEmpty(type.Namespace)
            ? "Implementations"
            : type.Namespace.Replace('.', '/');

        return group + "/" + BuildInheritancePath(type, declaredType);
    }

    /// <summary>
    /// 计算具体实现相对声明类型的类继承深度。
    /// 接口字段以第一个仍实现该接口的基类为继承链起点。
    /// </summary>
    private static int GetInheritanceDepth(Type type, Type declaredType)
    {
        int depth = 0;
        Type current = type;

        while (current != null && current != declaredType)
        {
            Type baseType = current.BaseType;
            if (baseType == null ||
                baseType == typeof(object) ||
                !declaredType.IsAssignableFrom(baseType))
            {
                break;
            }

            depth++;
            current = baseType;
        }

        return depth;
    }

    /// <summary>
    /// 将继承链转换成 GenericMenu 路径。
    /// 例如 IAbility -> AbilityBase -> MoveAbility -> FlyAbility
    /// 会显示为 Ability Base/Move Ability/Fly Ability。
    /// </summary>
    private static string BuildInheritancePath(Type type, Type declaredType)
    {
        var names = new Stack<string>();
        Type current = type;
        names.Push(ObjectNames.NicifyVariableName(current.Name));

        while (current != null && current != declaredType)
        {
            Type baseType = current.BaseType;
            if (baseType == null ||
                baseType == typeof(object) ||
                baseType == declaredType ||
                !declaredType.IsAssignableFrom(baseType))
            {
                break;
            }

            names.Push(ObjectNames.NicifyVariableName(baseType.Name));
            current = baseType;
        }

        return string.Join("/", names.ToArray());
    }

    private static string GetTypeLabel(SerializedProperty property)
    {
        string fullName = property.managedReferenceFullTypename;
        if (string.IsNullOrEmpty(fullName))
            return "None";

        int space = fullName.IndexOf(' ');
        string name = space >= 0 ? fullName.Substring(space + 1) : fullName;
        int dot = name.LastIndexOf('.');
        if (dot >= 0)
            name = name.Substring(dot + 1);

        return ObjectNames.NicifyVariableName(name);
    }

    private static bool IsManagedReferenceList(SerializedProperty property)
    {
        if (!property.isArray || property.propertyType == SerializedPropertyType.String)
            return false;

        return property.arraySize == 0 ||
               property.GetArrayElementAtIndex(0).propertyType ==
               SerializedPropertyType.ManagedReference;
    }

    private static bool IsNullReference(SerializedProperty property, int listIndex)
    {
        SerializedProperty reference = GetReferenceProperty(property, listIndex);
        return reference == null ||
               string.IsNullOrEmpty(reference.managedReferenceFullTypename);
    }

    private static bool IsCurrentType(
        SerializedProperty property,
        int listIndex,
        Type type)
    {
        SerializedProperty reference = GetReferenceProperty(property, listIndex);
        if (reference == null)
            return false;

        string fullName = reference.managedReferenceFullTypename;
        return fullName.EndsWith(" " + type.FullName, StringComparison.Ordinal);
    }

    private static SerializedProperty GetReferenceProperty(
        SerializedProperty property,
        int listIndex)
    {
        if (listIndex >= 0 && listIndex < property.arraySize)
            return property.GetArrayElementAtIndex(listIndex);

        return IsManagedReferenceList(property) ? null : property;
    }

    private static float GetListHeight(SerializedProperty list)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!list.isExpanded)
            return height;

        float spacing = EditorGUIUtility.standardVerticalSpacing;
        for (int i = 0; i < list.arraySize; i++)
        {
            height += spacing +
                      GetReferenceHeight(list.GetArrayElementAtIndex(i));
        }

        return height;
    }

    private static float GetReferenceHeight(SerializedProperty reference)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!reference.isExpanded ||
            string.IsNullOrEmpty(reference.managedReferenceFullTypename))
            return height;

        float spacing = EditorGUIUtility.standardVerticalSpacing;
        foreach (string path in GetDirectChildPaths(reference))
        {
            SerializedProperty child = reference.serializedObject.FindProperty(path);
            if (child != null)
                height += spacing + EditorGUI.GetPropertyHeight(child, true);
        }

        return height;
    }

    private static List<string> GetDirectChildPaths(SerializedProperty reference)
    {
        var paths = new List<string>();
        SerializedProperty iterator = reference.Copy();
        SerializedProperty end = iterator.GetEndProperty();
        bool next = iterator.NextVisible(true);

        while (next && !SerializedProperty.EqualContents(iterator, end))
        {
            if (iterator.depth == reference.depth + 1)
                paths.Add(iterator.propertyPath);

            next = iterator.NextVisible(false);
        }

        return paths;
    }
}
#endif
