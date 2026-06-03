using UnityEngine;
using UnityEditor;

public class RobotColliderToggle : EditorWindow
{
    [MenuItem("Tools/Robot Collider Toggle")]
    public static void ShowWindow()
    {
        GetWindow<RobotColliderToggle>("Robot Collider Toggle");
    }

    private GameObject robotRoot;

    void OnGUI()
    {
        GUILayout.Label("Robot Collider Manager", EditorStyles.boldLabel);

        robotRoot = (GameObject)EditorGUILayout.ObjectField("Robot Root", robotRoot, typeof(GameObject), true);

        if (robotRoot == null)
        {
            EditorGUILayout.HelpBox("Please assign the robot root object.", MessageType.Warning);
            return;
        }

        if (GUILayout.Button("Disable All Colliders"))
        {
            SetCollidersEnabled(robotRoot, false);
        }

        if (GUILayout.Button("Enable All Colliders"))
        {
            SetCollidersEnabled(robotRoot, true);
        }
    }

    private void SetCollidersEnabled(GameObject root, bool isEnabled)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        int changedCount = 0;
        foreach (Collider col in colliders)
        {
            if (col.enabled != isEnabled)
            {
                Undo.RecordObject(col, isEnabled ? "Enable Collider" : "Disable Collider");
                col.enabled = isEnabled;
                changedCount++;
            }
        }
        Debug.Log($"[RobotColliderToggle] {(isEnabled ? "Enabled" : "Disabled")} {changedCount} colliders on {root.name}.");
    }
}
