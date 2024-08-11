using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RecipeItem))]
public class RecipeItemEditor : Editor
{
    private int size = 150;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        RecipeItem recipeItem = (RecipeItem)target;

        // 绘制OUTPUT部分
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("OUTPUT", new GUIStyle { fontStyle = FontStyle.Bold });
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginVertical();

        Texture texture = null;
        if (recipeItem.output != null && recipeItem.output.icon != null)
        {
            texture = recipeItem.output.icon.texture;
        }
        Rect rect = GUILayoutUtility.GetRect(size, size);
        GUI.DrawTexture(rect, texture ?? Texture2D.whiteTexture, ScaleMode.ScaleToFit);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("output"), GUIContent.none, true, GUILayout.Width(size));
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 绘制RECIPE部分
        GUILayout.Label("RECIPE", new GUIStyle { fontStyle = FontStyle.Bold });

        for (int row = 0; row < 3; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < 3; col++)
            {
                DrawRecipeItem(recipeItem.GetItem(row, col), row, col);
            }
            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRecipeItem(Item item, int row, int col)
    {
        EditorGUILayout.BeginVertical();

        string propertyName = $"item_{row}{col}";
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        Texture texture = null;
        if (property.objectReferenceValue != null)
        {
            if (item.icon != null)
            {
                texture = item.icon.texture;
            }
        }
        Rect rect = GUILayoutUtility.GetRect(size, size);
        GUI.DrawTexture(rect, texture ?? Texture2D.whiteTexture, ScaleMode.ScaleToFit);
        EditorGUILayout.PropertyField(property, GUIContent.none, true, GUILayout.Width(size));
        EditorGUILayout.EndVertical();
    }
}
