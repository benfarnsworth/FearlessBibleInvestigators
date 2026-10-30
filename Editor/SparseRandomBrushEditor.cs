
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(SparseRandomBrush))]
public class SparseRandomBrushEditor : RandomBrushEditor
{
    public override GameObject[] validTargets
    {
        get
        {
            return Object.FindObjectsByType<Tilemap>(
                    FindObjectsSortMode.None)
                .Where(tilemap => tilemap.gameObject.activeInHierarchy)
                .Select(tilemap => tilemap.gameObject)
                .Distinct()
                .ToArray();
        }
    }

    public override void OnPaintInspectorGUI()
    {
        base.OnPaintInspectorGUI();

        serializedObject.Update();

        SerializedProperty chance =
            serializedObject.FindProperty("placementChance");

        if (chance != null)
        {
            EditorGUILayout.Slider(
                chance, 0f, 1f,
                new GUIContent("Placement Chance"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
