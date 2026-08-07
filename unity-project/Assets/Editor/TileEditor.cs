using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileData))]
public class TileEditor : Editor
{
    SerializedProperty category,
        suit,
        rank,
        honor,
        flower,
        texture;

    private void OnEnable()
    {
        category = serializedObject.FindProperty("category");
        suit = serializedObject.FindProperty("suit");
        rank = serializedObject.FindProperty("rank");
        honor = serializedObject.FindProperty("honor");
        flower = serializedObject.FindProperty("flower");
        texture = serializedObject.FindProperty("texture");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(category);

        TileCategory categoryValue = (TileCategory)category.enumValueIndex;
        switch (categoryValue)
        {
            case TileCategory.Suit:
                EditorGUILayout.PropertyField(suit);
                EditorGUILayout.PropertyField(rank);
                break;
            case TileCategory.Honor:
                EditorGUILayout.PropertyField(honor);
                break;
            case TileCategory.Flower:
                EditorGUILayout.PropertyField(flower);
                break;
        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(texture);

        serializedObject.ApplyModifiedProperties();
    }
}
