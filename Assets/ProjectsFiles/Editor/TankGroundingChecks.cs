using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TankGroundingChecks
{
    private const string PlayerPath = "Assets/ProjectsFiles/Prefabes/Player.prefab";

    [MenuItem("Tools/TankProject/Check tank slope physics")]
    public static void CheckPhysics()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Exit Play Mode first.");
        var report = new StringBuilder("Isolated Unity PhysicsScene, Edit Mode. Actual prefab/motor, no NGO sessions or collision callbacks.\n");
        int failures = 0;
        void Check(bool pass, string label) { report.AppendLine((pass ? "PASS " : "FAIL ") + label); if (!pass) failures++; }
        foreach (float angle in new[] { 0f, 20f, 35f, 55f })
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var physics = scene.GetPhysicsScene();
                if (!physics.IsValid() || physics == Physics.defaultPhysicsScene)
                    throw new System.InvalidOperationException("Checks require a separate physics scene; refusing to simulate the user's scene.");
                var ground = new GameObject("Test ground");
                SceneManager.MoveGameObjectToScene(ground, scene);
                var rotation = Quaternion.Euler(-angle, 0f, 0f);
                ground.transform.SetPositionAndRotation(rotation * new Vector3(0f, -0.1f, 0f), rotation);
                ground.AddComponent<BoxCollider>().size = new Vector3(30f, 0.2f, 50f);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
                var tank = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var rb = tank.GetComponent<Rigidbody>();
                var movement = tank.GetComponent<PlayerMovement>();
                var grounding = tank.GetComponent<TankGrounding>();
                rb.isKinematic = false;
                rb.interpolation = RigidbodyInterpolation.None; // Deterministic manual steps; asset unchanged.
                rb.position = rotation * new Vector3(0f, 0.15f, 0f);
                rb.rotation = rotation;
                Invoke(movement, "Awake");
                Invoke(grounding, "Awake");
                void Step(Vector2 input, int count)
                {
                    for (int i = 0; i < count; i++) { Invoke(movement, "StepMotor", input, 0.02f); physics.Simulate(0.02f); }
                }
                Step(Vector2.zero, 20);
                grounding.Sample();
                Check(grounding.Grounded == (angle <= 40f), angle + " degrees support=" + grounding.Grounded);
                if (angle > 40f) continue;
                Vector3 start = rb.position;
                Step(Vector2.up, 100);
                Check(rb.position.z > start.z + 5f, angle + " ascent/forward distance=" + (rb.position.z - start.z).ToString("F3"));
                Check(Vector3.Angle(rb.rotation * Vector3.up, rotation * Vector3.up) < 2f, angle + " alignment");
                Step(Vector2.zero, 40);
                Check(rb.linearVelocity.magnitude < 0.35f, angle + " stop speed=" + rb.linearVelocity.magnitude.ToString("F3"));
                start = rb.position;
                Step(Vector2.down, 100);
                Check(rb.position.z < start.z - 5f, angle + " reverse/descent distance=" + (rb.position.z - start.z).ToString("F3"));
                Step(Vector2.zero, 40);
                Step(Vector2.right, 50);
                Check(Vector3.Angle(rb.rotation * Vector3.up, rotation * Vector3.up) < 2f, angle + " turn maintains support normal");
                Check(Vector3.Angle(rb.rotation * Vector3.forward, rotation * Vector3.forward) > 35f, angle + " turn responds");
                // Same pending force path as ConfirmHitRpc, without needing a server.
                Vector3 normal = grounding.Normal;
                typeof(PlayerMovement).GetField("pendingImpulse", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, normal * 50f);
                Step(Vector2.zero, 1);
                Check(Vector3.Dot(rb.linearVelocity, normal) > 4f, angle + " impulse is not overwritten");
                grounding.Sample();
                Check(!grounding.Grounded, angle + " separating impulse releases support");
                float before = Vector3.Dot(rb.linearVelocity, normal);
                Step(Vector2.up, 1);
                Check(Mathf.Abs(Vector3.Dot(rb.linearVelocity, normal) - before - Vector3.Dot(Physics.gravity, normal) * 0.02f) < 0.05f,
                    angle + " airborne gravity preserved, no adhesion");
                // Move away from all support. No transform snapping to an old sample.
                rb.position = new Vector3(100f, 1f, 0f);
                rb.linearVelocity = Vector3.zero;
                grounding.ResetSupport();
                Step(Vector2.up, 30);
                Check(!grounding.Grounded && rb.position.y < 0f, angle + " unsupported tank falls");
                Check(Mathf.Abs(rb.position.x - 100f) < 0.001f && Mathf.Abs(rb.position.z) < 0.001f, angle + " no air drive across gaps");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        Directory.CreateDirectory("Tests/Grounding");
        File.WriteAllText("Tests/Grounding/physics-results.txt", report.ToString());
        Debug.Log("[TankGroundingChecks] " + failures + " failures.\n" + report);
    }

    private static void Invoke(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

    [MenuItem("Tools/TankProject/Save tank slope setup")]
    public static void SaveSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Exit Play Mode first.");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.scene.isDirty) throw new System.InvalidOperationException("Save the open prefab edits first.");
        var root = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            var body = root.GetComponent<Rigidbody>();
            var hull = root.GetComponent<BoxCollider>();
            var ground = root.GetComponent<TankGrounding>();
            if (ground == null) ground = root.AddComponent<TankGrounding>();
            const string materialPath = "Assets/ProjectsFiles/Prefabes/TankContact.physicMaterial";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(materialPath);
            if (material == null)
            {
                material = new PhysicsMaterial("TankContact") { staticFriction = 0f, dynamicFriction = 0f,
                    bounciness = 0f, frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            // Match the measured Track bounds, without touching the animated model
            // or root scale. The previous root box remains the upper hull collider.
            hull.size = new Vector3(0.023f, 0.0187f, 0.036f);
            hull.center = new Vector3(hull.center.x, 0.00435f, hull.center.z);
            hull.sharedMaterial = material;
            hull.contactOffset = 0.003f;
            var container = root.transform.Find("Collision");
            if (container == null)
            {
                container = new GameObject("Collision").transform;
                container.SetParent(root.transform, false);
            }
            container.gameObject.layer = root.layer;
            for (int side = 0; side < 2; side++)
            {
                string name = side == 0 ? "LeftTrack" : "RightTrack";
                var child = container.Find(name);
                if (child == null) { child = new GameObject(name).transform; child.SetParent(container, false); }
                child.gameObject.layer = root.layer;
                var capsule = child.GetComponent<CapsuleCollider>();
                if (capsule == null) capsule = child.gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = 0.0055f;
                capsule.height = 0.052744f;
                capsule.center = new Vector3(side == 0 ? -0.0085f : 0.0085f, -0.0088f, 0.005446f);
                capsule.sharedMaterial = material;
                capsule.contactOffset = 0.003f;
            }
            // All rotation is controlled by the motor, collision impulses remain
            // linear. Freeze prevents contact torque fighting MoveRotation.
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var settings = new SerializedObject(ground);
            settings.FindProperty("body").objectReferenceValue = body;
            var samples = settings.FindProperty("samples");
            samples.arraySize = 6;
            for (int i = 0; i < 6; i++) samples.GetArrayElementAtIndex(i).vector3Value =
                new Vector3(i < 3 ? -0.0085f : 0.0085f, -0.0143f, 0.005446f + (i % 3 - 1) * 0.020872f);
            settings.FindProperty("probeLift").floatValue = 0.22f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var movement = new SerializedObject(root.GetComponent<PlayerMovement>());
            movement.FindProperty("grounding").objectReferenceValue = ground;
            movement.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPath, out bool saved);
            if (!saved) throw new System.InvalidOperationException("Player prefab save failed.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Inspect(); // Reopen saved asset, not the authoring instance.
        Debug.Log("[TankGroundingChecks] Slope setup saved and reopened.");
    }

    [MenuItem("Tools/TankProject/Inspect tank collision geometry")]
    public static void Inspect()
    {
        var root = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            var report = new StringBuilder();
            report.AppendLine("Root scale: " + root.transform.localScale.ToString("F6"));
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var bounds = new Bounds();
                bool first = true;
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    Vector3 point = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                    else bounds.Encapsulate(point);
                }
                report.AppendLine(filter.name + " root-local center=" + bounds.center.ToString("F6") + " size=" + bounds.size.ToString("F6"));
            }
            foreach (var body in root.GetComponentsInChildren<Rigidbody>(true))
                report.AppendLine("Rigidbody " + body.name + " constraints=" + body.constraints);
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                report.AppendLine("Collider " + collider.name + " " + collider.GetType().Name);
            Directory.CreateDirectory("Tests/Grounding");
            File.WriteAllText("Tests/Grounding/geometry.txt", report.ToString());
            Debug.Log("[TankGroundingChecks] " + report);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
