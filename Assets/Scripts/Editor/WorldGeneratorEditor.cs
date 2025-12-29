
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var t = (WorldGenerator)target;

        if (GUILayout.Button("Generate"))
        {
            t.SendMessage("Generate", SendMessageOptions.DontRequireReceiver);
        }

        if (GUILayout.Button("Clear"))
        {
            t.SendMessage("Clear", SendMessageOptions.DontRequireReceiver);
        }
    }
}
#endif