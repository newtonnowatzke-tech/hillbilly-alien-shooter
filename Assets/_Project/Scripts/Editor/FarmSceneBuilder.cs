using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using HillbillyAlienShooter.Core;
using HillbillyAlienShooter.Data;
using HillbillyAlienShooter.UI;
using HillbillyAlienShooter.Utils;
using HillbillyAlienShooter.Waves;

namespace HillbillyAlienShooter.EditorTools
{
    /// <summary>
    /// One-click scene generator for Packet 1.1. Builds the whole playable farm —
    /// ground, barn, trees, fences, the hillbilly, a herd of cattle, the wave
    /// spawner, the game manager and the HUD — plus the ScriptableObject data
    /// assets, then wires everything together and saves it as Farm.unity.
    ///
    /// Menu: Tools ▸ Hillbilly ▸ Build Farm Scene
    /// </summary>
    public static class FarmSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Farm.unity";
        private const string DataFolder = "Assets/_Project/Data";
        private const float FarmSize = 50f;
        private const float FenceHalf = 20f;
        private const int CowCount = 6;

        [MenuItem("Tools/Hillbilly/Build Farm Scene", priority = 0)]
        public static void BuildFarmScene()
        {
            if (!EditorUtility.DisplayDialog(
                    "Build Farm Scene",
                    "This creates a fresh Farm.unity with all current gameplay wired up\n" +
                    "(core loop + horse riding + camera/pause polish).\n\n" +
                    "Any unsaved changes in the current scene will be discarded. Continue?",
                    "Build it, partner!", "Cancel"))
                return;

            BuildFarmSceneHeadless();

            EditorUtility.DisplayDialog("Done!",
                "Farm scene built at:\n" + ScenePath +
                "\n\nPress Play and start blastin'. Yee-haw!", "Nice");
        }

        /// <summary>
        /// Dialog-free scene build, callable from CI (WebGLBuilder) and the menu
        /// item alike. Generates data assets, assembles the scene, saves it, and
        /// registers it in Build Settings.
        /// </summary>
        public static void BuildFarmSceneHeadless()
        {
            // --- Data assets first, so we can wire them into components ---
            EnsureFolders();
            EnemyData enemyData = CreateOrLoad<EnemyData>($"{DataFolder}/EnemyData_LittleAlien.asset", d =>
            {
                d.displayName = "Little Alien";
            });
            WeaponData weaponData = CreateOrLoad<WeaponData>($"{DataFolder}/WeaponData_Shotgun.asset", _ => { });
            WaveData waveData = CreateOrLoad<WaveData>($"{DataFolder}/WaveData_Wave1.asset", w =>
            {
                w.waveName = "Wave 1";
                w.spawns = new System.Collections.Generic.List<WaveData.SpawnEntry>
                {
                    new WaveData.SpawnEntry { enemy = enemyData, count = 8 }
                };
            });
            // Make sure the wave references the (possibly pre-existing) enemy asset.
            if (waveData.spawns.Count > 0) waveData.spawns[0].enemy = enemyData;
            EditorUtility.SetDirty(waveData);

            HorseData horseData = CreateOrLoad<HorseData>($"{DataFolder}/HorseData_Buttercup.asset", h =>
            {
                h.displayName = "Buttercup";
            });

            // --- New empty scene ---
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupNightLighting();

            // --- Environment ---
            LowPolyFactory.BuildGround(FarmSize);
            LowPolyFactory.BuildBarn(new Vector3(0f, 0f, 15f));
            BuildPerimeterFence();
            ScatterTrees();
            BuildHills();

            // --- Player ---
            var player = LowPolyFactory.BuildPlayer(new Vector3(0f, 0f, -6f));
            AssignObjectRef(player.GetComponent<HillbillyAlienShooter.Weapons.Shotgun>(), "weaponData", weaponData);

            // --- Buttercup, waitin' by the barn ---
            var horse = LowPolyFactory.BuildHorse(horseData, new Vector3(5.5f, 0f, 11f));
            horse.transform.rotation = Quaternion.Euler(0f, -140f, 0f); // facing the pasture

            // --- Cattle herd ---
            var herd = new GameObject("Herd").transform;
            for (int i = 0; i < CowCount; i++)
            {
                Vector2 r = Random.insideUnitCircle * 7f;
                var cow = LowPolyFactory.BuildCow(new Vector3(r.x, 0f, r.y + 2f));
                cow.transform.SetParent(herd, true);
            }

            // --- Systems: GameManager, WaveSpawner, HUD ---
            new GameObject("GameManager").AddComponent<GameManager>();

            var spawnerGo = new GameObject("WaveSpawner");
            var spawner = spawnerGo.AddComponent<WaveSpawner>();
            AssignObjectRef(spawner, "wave", waveData);

            new GameObject("HUD").AddComponent<HUDController>();
            new GameObject("PauseMenu").AddComponent<PauseMenu>();

            // --- Save + register in build settings so restarts work at runtime ---
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("[Hillbilly] Farm scene built and saved to " + ScenePath);
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private static void SetupNightLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.19f, 0.32f); // cool night ambient
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.14f);
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 80f;

            var moonGo = new GameObject("Moon (Directional Light)");
            var moon = moonGo.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.7f, 0.8f, 1f);
            moon.intensity = 0.9f;
            moon.shadows = LightShadows.Soft;
            moonGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            // Warm glow by the barn for a cosy, colourful contrast.
            var barnLightGo = new GameObject("Barn Glow (Point Light)");
            var barnLight = barnLightGo.AddComponent<Light>();
            barnLight.type = LightType.Point;
            barnLight.color = new Color(1f, 0.75f, 0.4f);
            barnLight.intensity = 2.5f;
            barnLight.range = 18f;
            barnLightGo.transform.position = new Vector3(0f, 3f, 11f);
        }

        private static void BuildPerimeterFence()
        {
            var root = new GameObject("Fences").transform;
            float h = FenceHalf;
            LowPolyFactory.BuildFenceLine(new Vector3(-h, 0f, -h), new Vector3(h, 0f, -h), root);
            LowPolyFactory.BuildFenceLine(new Vector3(h, 0f, -h), new Vector3(h, 0f, h), root);
            LowPolyFactory.BuildFenceLine(new Vector3(h, 0f, h), new Vector3(-h, 0f, h), root);
            LowPolyFactory.BuildFenceLine(new Vector3(-h, 0f, h), new Vector3(-h, 0f, -h), root);
        }

        private static void BuildHills()
        {
            // Rolling terrain to prove out riding physics + AI ground snapping.
            // Kept away from the herd (centre) and the barn (0,0,15).
            var root = new GameObject("Hills").transform;
            LowPolyFactory.BuildHill(new Vector3(-13f, 0f, -11f), 8f, 2.2f).transform.SetParent(root, true);
            LowPolyFactory.BuildHill(new Vector3(14f, 0f, -13f), 7f, 1.8f).transform.SetParent(root, true);
            LowPolyFactory.BuildHill(new Vector3(-15f, 0f, 9f), 6.5f, 1.6f).transform.SetParent(root, true);
        }

        private static void ScatterTrees()
        {
            var root = new GameObject("Trees").transform;
            // A ring of trees just inside the fence line.
            int count = 10;
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
                float rad = FenceHalf - Random.Range(1.5f, 3.5f);
                var pos = new Vector3(Mathf.Cos(a) * rad, 0f, Mathf.Sin(a) * rad);
                // Keep the barn area clear.
                if (Vector3.Distance(pos, new Vector3(0f, 0f, 15f)) < 8f) continue;
                var tree = LowPolyFactory.BuildTree(pos, Random.Range(0.85f, 1.3f));
                tree.transform.SetParent(root, true);
            }
        }

        /// <summary>Sets a private [SerializeField] object reference and marks the object dirty.</summary>
        private static void AssignObjectRef(Object component, string fieldName, Object value)
        {
            if (component == null) return;
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Scenes");
            EnsureFolder("Assets/_Project/Data");
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static T CreateOrLoad<T>(string path, System.Action<T> configure) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            configure?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void RegisterInBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == scenePath)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
