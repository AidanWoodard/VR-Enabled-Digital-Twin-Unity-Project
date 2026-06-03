using UnityEngine;
using UnityEditor;

public class RobotDriveManager : EditorWindow
{
    [MenuItem("Tools/Robot Drive Manager")]
    public static void ShowWindow()
    {
        GetWindow<RobotDriveManager>("Robot Drive Manager");
    }

    private GameObject robotRoot;
    private float bulkStiffness = 10000f;
    private float bulkDamping = 100f;
    private float bulkForceLimit = 1000f;

    private bool applyStiffness = true;
    private bool applyDamping = true;
    private bool applyForceLimit = false;

    private Vector2 scrollPosition;

    void OnGUI()
    {
        GUILayout.Label("Robot Articulation Drive Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. Root Object Assignment
        robotRoot = (GameObject)EditorGUILayout.ObjectField("Robot Root", robotRoot, typeof(GameObject), true);

        if (robotRoot == null)
        {
            EditorGUILayout.HelpBox("Please assign the robot root object (e.g. sgr532 or Robot).", MessageType.Warning);
            return;
        }

        ArticulationBody[] articulationBodies = robotRoot.GetComponentsInChildren<ArticulationBody>(true);

        if (articulationBodies.Length == 0)
        {
            EditorGUILayout.HelpBox("No ArticulationBody components found under the selected root.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Found {articulationBodies.Length} Articulation Bodies", EditorStyles.miniBoldLabel);

        // 2. Bulk Modifications Section
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Bulk Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        applyStiffness = EditorGUILayout.ToggleLeft("Stiffness", applyStiffness, GUILayout.Width(80));
        if (applyStiffness)
        {
            bulkStiffness = EditorGUILayout.FloatField(bulkStiffness);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        applyDamping = EditorGUILayout.ToggleLeft("Damping", applyDamping, GUILayout.Width(80));
        if (applyDamping)
        {
            bulkDamping = EditorGUILayout.FloatField(bulkDamping);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        applyForceLimit = EditorGUILayout.ToggleLeft("Force Limit", applyForceLimit, GUILayout.Width(80));
        if (applyForceLimit)
        {
            bulkForceLimit = EditorGUILayout.FloatField(bulkForceLimit);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        if (GUILayout.Button("Apply Bulk Settings to All Drives"))
        {
            ApplyBulkSettings(articulationBodies);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 3. Individual Joint Parameter Adjustments
        EditorGUILayout.LabelField("Individual Joint Drives", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        foreach (ArticulationBody ab in articulationBodies)
        {
            if (ab == null) continue;

            EditorGUILayout.BeginVertical("helpBox");
            
            // Display path relative to root or name
            string displayName = ab.gameObject.name;
            EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);

            var drive = ab.xDrive;
            
            EditorGUI.BeginChangeCheck();
            
            float newStiffness = EditorGUILayout.FloatField("Stiffness (P)", drive.stiffness);
            float newDamping = EditorGUILayout.FloatField("Damping (D)", drive.damping);
            float newForceLimit = EditorGUILayout.FloatField("Force Limit", drive.forceLimit);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(ab, "Modify Articulation Drive");
                drive.stiffness = newStiffness;
                drive.damping = newDamping;
                drive.forceLimit = newForceLimit;
                ab.xDrive = drive;
                EditorUtility.SetDirty(ab);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    private void ApplyBulkSettings(ArticulationBody[] bodies)
    {
        Undo.RecordObjects(bodies, "Apply Bulk Articulation Drive Settings");
        
        int count = 0;
        foreach (ArticulationBody ab in bodies)
        {
            if (ab == null) continue;
            
            var drive = ab.xDrive;
            bool changed = false;

            if (applyStiffness)
            {
                drive.stiffness = bulkStiffness;
                changed = true;
            }

            if (applyDamping)
            {
                drive.damping = bulkDamping;
                changed = true;
            }

            if (applyForceLimit)
            {
                drive.forceLimit = bulkForceLimit;
                changed = true;
            }

            if (changed)
            {
                ab.xDrive = drive;
                EditorUtility.SetDirty(ab);
                count++;
            }
        }
        
        Debug.Log($"[RobotDriveManager] Successfully updated xDrive settings on {count} ArticulationBodies.");
    }
}
