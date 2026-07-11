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

            // Little Alien: cattle rustler with an "annoying path" weave. The
            // weave/drop fields are (re)applied even to pre-existing assets since
            // they were introduced in Packet 2.1.
            EnemyData littleAlien = CreateOrLoad<EnemyData>($"{DataFolder}/EnemyData_LittleAlien.asset", d =>
            {
                d.displayName = "Little Alien";
            });
            littleAlien.role = EnemyData.EnemyRole.Rustler;
            littleAlien.weaveAmplitude = 1.2f;
            littleAlien.weaveFrequency = 0.7f;
            littleAlien.techDropChance = 0.25f;
            littleAlien.techAmount = 1;
            EditorUtility.SetDirty(littleAlien);

            // Medium Alien: faster hunter that flanks the hillbilly.
            EnemyData mediumAlien = CreateOrLoad<EnemyData>($"{DataFolder}/EnemyData_MediumAlien.asset", d =>
            {
                d.displayName = "Medium Alien";
                d.role = EnemyData.EnemyRole.Hunter;
                d.bodyTint = new Color(0.8f, 0.4f, 1f); // menacing violet
                d.bodyScale = 1.3f;
                d.maxHealth = 45f;
                d.moveSpeed = 4.6f;
                d.weaveAmplitude = 0.5f;   // slight jink even while flanking
                d.weaveFrequency = 1.1f;
                d.meleeRange = 1.8f;
                d.meleeDamage = 6f;        // light...
                d.meleeCooldown = 0.8f;    // ...but quick
                d.flankOffset = 5f;
                d.flankCloseRange = 4.5f;
                d.scoreValue = 250;
                d.techDropChance = 0.45f;
                d.techAmount = 1;
            });

            // Scout Saucer: hovers above the herd and beams cows from the air.
            EnemyData scoutSaucer = CreateOrLoad<EnemyData>($"{DataFolder}/EnemyData_ScoutSaucer.asset", d =>
            {
                d.displayName = "Scout Saucer";
                d.role = EnemyData.EnemyRole.Saucer;
                d.bodyTint = new Color(0.4f, 1f, 0.75f); // glowing canopy green
                d.maxHealth = 120f;
                d.moveSpeed = 4f;
                d.hoverHeight = 9f;
                d.hoverBobAmplitude = 0.4f;
                d.beamLockRadius = 1.6f;
                d.abductRatePerSecond = 0.6f; // faster than ground rustlers — priority target!
                d.scoreValue = 500;
                d.techDropChance = 1f;        // shooting down a saucer always pays
                d.techAmount = 3;
            });
            // Weak point field arrived in Packet 2.2 — apply to pre-existing assets too.
            scoutSaucer.weakPointMultiplier = 2.5f;
            EditorUtility.SetDirty(scoutSaucer);

            // Large Alien: tougher hunter with medium-damage swipes. Pure data —
            // no new code, which is exactly what the role system is for.
            EnemyData largeAlien = CreateOrLoad<EnemyData>($"{DataFolder}/EnemyData_LargeAlien.asset", d =>
            {
                d.displayName = "Large Alien";
                d.role = EnemyData.EnemyRole.Hunter;
                d.bodyTint = new Color(1f, 0.45f, 0.25f); // hot ember orange
                d.bodyScale = 1.7f;
                d.maxHealth = 90f;
                d.moveSpeed = 3.4f;
                d.weaveAmplitude = 0.3f;
                d.weaveFrequency = 0.6f;
                d.meleeRange = 2.2f;
                d.meleeDamage = 14f;      // medium damage...
                d.meleeCooldown = 1.4f;   // ...at a deliberate pace
                d.flankOffset = 3f;       // flanks less, bullies more
                d.flankCloseRange = 5f;
                d.scoreValue = 400;
                d.techDropChance = 0.6f;
                d.techAmount = 2;
            });

            // Brute: slow walking wall with a telegraphed AoE ground slam.
            EnemyData brute = CreateOrLoad<EnemyData>($"{DataFolder}/EnemyData_Brute.asset", d =>
            {
                d.displayName = "Brute";
                d.role = EnemyData.EnemyRole.Hunter;
                d.attackStyle = EnemyData.AttackStyle.Smash;
                d.bodyTint = new Color(0.55f, 0.6f, 0.35f); // sickly moss
                d.bodyScale = 2.2f;
                d.maxHealth = 220f;
                d.moveSpeed = 2.2f;
                d.weaveAmplitude = 0f;    // walks dead straight at you
                d.flankOffset = 0f;       // no flanking — pure inevitability
                d.flankCloseRange = 4f;
                d.meleeRange = 2.6f;
                d.meleeDamage = 30f;      // the smash
                d.meleeCooldown = 2.6f;
                d.smashRadius = 3.4f;
                d.smashWindup = 0.8f;     // the dodge window
                d.scoreValue = 800;
                d.techDropChance = 1f;
                d.techAmount = 3;
            });

            // War Saucer: the scout's meaner cousin — support fire + abduction.
            EnemyData warSaucer = CreateOrLoad<EnemyData>($"{DataFolder}/EnemyData_WarSaucer.asset", d =>
            {
                d.displayName = "War Saucer";
                d.role = EnemyData.EnemyRole.Saucer;
                d.bodyTint = new Color(1f, 0.45f, 0.4f); // hostile red canopy
                d.maxHealth = 200f;
                d.moveSpeed = 4.5f;
                d.hoverHeight = 9f;
                d.hoverBobAmplitude = 0.4f;
                d.beamLockRadius = 1.6f;
                d.abductRatePerSecond = 0.5f;
                d.projectileDamage = 8f;      // support fire enabled
                d.projectileSpeed = 10f;
                d.projectileInterval = 2f;
                d.projectileRange = 22f;
                d.weakPointMultiplier = 2.5f;
                d.scoreValue = 800;
                d.techDropChance = 1f;
                d.techAmount = 4;
            });

            WeaponData weaponData = CreateOrLoad<WeaponData>($"{DataFolder}/WeaponData_Shotgun.asset", _ => { });

            // --- The wild upgrade pool (Packet 2.3) ---
            var upgradePool = new UpgradeData[]
            {
                CreateOrLoad<UpgradeData>($"{DataFolder}/UpgradeData_ExtraShells.asset", u =>
                {
                    u.displayName = "Extra Shells";
                    u.flavor = "Found a box o' shells in the truck!";
                    u.type = UpgradeData.UpgradeType.ExtraAmmo;
                    u.amount = 12f;
                    u.duration = 0f;
                    u.weight = 1.2f; // ammo is the bread-and-butter roll
                }),
                CreateOrLoad<UpgradeData>($"{DataFolder}/UpgradeData_GreasedLightnin.asset", u =>
                {
                    u.displayName = "Greased Lightnin'";
                    u.flavor = "Slicker'n a greased pig!";
                    u.type = UpgradeData.UpgradeType.FastReload;
                    u.amount = 0.45f;   // reload time multiplier
                    u.duration = 20f;
                }),
                CreateOrLoad<UpgradeData>($"{DataFolder}/UpgradeData_BoomstickRounds.asset", u =>
                {
                    u.displayName = "Boomstick Rounds";
                    u.flavor = "Now THAT'S a boomstick!";
                    u.type = UpgradeData.UpgradeType.ExplosiveShells;
                    u.amount = 2.2f;    // blast radius (m), +25%/stack
                    u.explosionDamage = 10f;
                    u.duration = 15f;
                }),
                CreateOrLoad<UpgradeData>($"{DataFolder}/UpgradeData_HairTrigger.asset", u =>
                {
                    u.displayName = "Hair Trigger";
                    u.flavor = "Faster'n gossip at church!";
                    u.type = UpgradeData.UpgradeType.RapidFire;
                    u.amount = 0.45f;   // fire cooldown multiplier
                    u.duration = 15f;
                }),
                CreateOrLoad<UpgradeData>($"{DataFolder}/UpgradeData_MoonshineTimer.asset", u =>
                {
                    u.displayName = "Moonshine Timer";
                    u.flavor = "Time moves slower after moonshine.";
                    u.type = UpgradeData.UpgradeType.DurationExtender;
                    u.amount = 8f;      // seconds added to every running upgrade
                    u.duration = 0f;
                    u.weight = 0.7f;    // the gamble roll
                }),
            };

            // --- The five-wave farm campaign (Packet 3.1) -------------------
            // Compositions are authored here and refreshed on every rebuild so
            // escalation tuning ships with the code. Tweak in the Inspector
            // between rebuilds if you want to experiment.
            WaveData MakeWave(string file, string title, float startDelay, float interval,
                params (EnemyData enemy, int count)[] entries)
            {
                var w = CreateOrLoad<WaveData>($"{DataFolder}/{file}.asset", x => { });
                w.waveName = title;
                w.startDelay = startDelay;
                w.spawnInterval = interval;
                w.spawns = new System.Collections.Generic.List<WaveData.SpawnEntry>();
                foreach (var (enemy, count) in entries)
                    w.spawns.Add(new WaveData.SpawnEntry { enemy = enemy, count = count });
                EditorUtility.SetDirty(w);
                return w;
            }

            var campaign = new Object[]
            {
                // Gentle intro: weaving scouts only — learn the save-the-cow loop.
                MakeWave("WaveData_Wave1", "First Contact", 3f, 1.3f,
                    (littleAlien, 6)),
                // Hunters join: now you're a target too.
                MakeWave("WaveData_Wave2", "Rustle Up", 0f, 1.15f,
                    (littleAlien, 8), (mediumAlien, 3)),
                // First air support + heavies.
                MakeWave("WaveData_Wave3", "Saucer Season", 0f, 1f,
                    (littleAlien, 8), (mediumAlien, 4), (largeAlien, 2), (scoutSaucer, 1)),
                // The wall arrives.
                MakeWave("WaveData_Wave4", "Heavy Metal", 0f, 0.95f,
                    (littleAlien, 6), (mediumAlien, 4), (largeAlien, 3), (brute, 1), (scoutSaucer, 1)),
                // Everything they've got.
                MakeWave("WaveData_Wave5", "The Whole Dang Armada", 0f, 0.8f,
                    (littleAlien, 8), (mediumAlien, 5), (largeAlien, 3), (brute, 2), (warSaucer, 1), (scoutSaucer, 1)),
            };

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
            AssignObjectList(player.GetComponent<HillbillyAlienShooter.Weapons.WeaponUpgradeController>(),
                "upgradePool", upgradePool);

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
            AssignObjectList(spawner, "waves", campaign);

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

        /// <summary>Fills a private [SerializeField] List/array of object references.</summary>
        private static void AssignObjectList(Object component, string fieldName, Object[] values)
        {
            if (component == null || values == null) return;
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop == null || !prop.isArray) return;

            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
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
