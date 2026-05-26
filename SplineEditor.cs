#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

// ── Custom Tool Context ──────────────────────────────────────────────────────
[EditorToolContext("Spline Editor", typeof(SplineContainer))]
public class SplineEditorContext : EditorToolContext
{
    public override void OnActivated()
    {
        base.OnActivated();
    }

    public override void OnWillBeDeactivated()
    {
        base.OnWillBeDeactivated();
    }

    protected override Type GetEditorToolType(Tool tool)
    {
        return base.GetEditorToolType(tool);
    }
}

// ── Custom Inspector + Scene GUI ─────────────────────────────────────────────
[CustomEditor(typeof(SplineContainer))]
public class SimpleSplineEditor : Editor
{
    int    _resolution = 50;
    float  _height     = 100f;
    string _meshName   = "SplineMesh";

    // ── Scene GUI ────────────────────────────────────────────────────────────
    void OnSceneGUI()
    {
        // Only draw knot handles while our context is active.
        if (!ToolManager.activeContextType.IsAssignableFrom(typeof(SplineEditorContext)))
            return;

        var container = (SplineContainer)target;
        var spline    = container.Spline;

        for (int i = 0; i < spline.Count; i++)
        {
            var knot     = spline[i];
            var worldPos = container.transform.TransformPoint(
                new Vector3(knot.Position.x, knot.Position.y, knot.Position.z));

            EditorGUI.BeginChangeCheck();
            var newPos = Handles.PositionHandle(worldPos, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(container, "Move Spline Knot");
                var localPos = container.transform.InverseTransformPoint(newPos);
                knot.Position = new Unity.Mathematics.float3(localPos.x, localPos.y, localPos.z);
                spline[i]     = knot;
                EditorUtility.SetDirty(container);
            }
        }
    }

    // ── Inspector GUI ─────────────────────────────────────────────────────────
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var container = (SplineContainer)target;

        GUILayout.Space(10);

        // ── Context switcher ─────────────────────────────────────────────────
        GUILayout.Label("Scene Tool Context", EditorStyles.boldLabel);

        bool contextActive = ToolManager.activeContextType == typeof(SplineEditorContext);

        EditorGUILayout.BeginHorizontal();

        // Activate button
        using (new EditorGUI.DisabledScope(contextActive))
        {
            if (GUILayout.Button("Activate Spline Context"))
            {
                Selection.activeGameObject = ((SplineContainer)target).gameObject;
                ToolManager.SetActiveContext<SplineEditorContext>();
            }
        }

        // Restore default button
        using (new EditorGUI.DisabledScope(!contextActive))
        {
            if (GUILayout.Button("Restore Default Context"))
                ToolManager.SetActiveContext<GameObjectToolContext>();
        }

        EditorGUILayout.EndHorizontal();

        // Status label
        GUILayout.Label(
            contextActive ? "● Spline context is ACTIVE" : "○ Spline context is inactive",
            contextActive
                ? new GUIStyle(EditorStyles.helpBox) { normal = { textColor = Color.green } }
                : EditorStyles.helpBox);

        // ── Mesh generation ──────────────────────────────────────────────────
        GUILayout.Space(10);
        GUILayout.Label("Mesh Generation", EditorStyles.boldLabel);

        _resolution = EditorGUILayout.IntSlider("Resolution", _resolution, 4, 200);
        _height     = EditorGUILayout.FloatField("Height", _height);
        _meshName   = EditorGUILayout.TextField("Mesh Name", _meshName);

        if (GUILayout.Button("Generate Mesh"))
            GenerateMesh(container);
    }

    // ── Mesh Generation ───────────────────────────────────────────────────────
    void GenerateMesh(SplineContainer container)
    {
        var spline         = container.Spline;
        var perimeterVerts = new List<Vector3>();

        for (int i = 0; i < _resolution; i++)
        {
            float t     = i / (float)_resolution;
            var   point = spline.EvaluatePosition(t);
            perimeterVerts.Add(new Vector3(point.x, point.y, point.z));
        }

        var verts = new List<Vector3>();
        var tris  = new List<int>();

        foreach (var v in perimeterVerts) verts.Add(v);                        // bottom ring
        foreach (var v in perimeterVerts) verts.Add(v + Vector3.up * _height); // top ring

        int count = perimeterVerts.Count;
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            int b0 = i,          b1 = next;
            int t0 = i + count,  t1 = next + count;

            tris.Add(b0); tris.Add(t0); tris.Add(b1);
            tris.Add(b1); tris.Add(t0); tris.Add(t1);
        }

        var mesh = new Mesh
        {
            name      = _meshName,
            vertices  = verts.ToArray(),
            triangles = tris.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var meshFilter = container.GetComponent<MeshFilter>()
                      ?? Undo.AddComponent<MeshFilter>(container.gameObject);

        Undo.RecordObject(meshFilter, "Assign Spline Mesh");
        meshFilter.sharedMesh = mesh;

        EditorUtility.SetDirty(meshFilter);
        EditorUtility.SetDirty(container.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        Debug.Log($"Generated mesh: {count * 2} verts, height {_height}");
    }
}
#endif