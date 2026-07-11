using UnityEngine;
using HillbillyAlienShooter.Combat;
using HillbillyAlienShooter.Data;
using HillbillyAlienShooter.Enemies;
using HillbillyAlienShooter.Player;
using HillbillyAlienShooter.Weapons;

namespace HillbillyAlienShooter.Utils
{
    /// <summary>
    /// Builds fully-configured low-poly placeholder objects from Unity primitives.
    /// This is the single source of truth for "what a cow / alien / hillbilly is",
    /// shared by the runtime <c>WaveSpawner</c> and the editor <c>FarmSceneBuilder</c>
    /// so they can never drift apart. Swap these out for real low-poly art in
    /// Packet 4.3 — every spawner keeps working unchanged.
    /// </summary>
    public static class LowPolyFactory
    {
        // A cheerful, saturated night-farm palette.
        public static readonly Color Grass = new Color(0.30f, 0.62f, 0.24f);
        public static readonly Color BarnRed = new Color(0.72f, 0.16f, 0.14f);
        public static readonly Color BarnRoof = new Color(0.28f, 0.24f, 0.30f);
        public static readonly Color Wood = new Color(0.52f, 0.36f, 0.22f);
        public static readonly Color TrunkBrown = new Color(0.40f, 0.27f, 0.16f);
        public static readonly Color Leaf = new Color(0.20f, 0.52f, 0.28f);
        public static readonly Color CowWhite = new Color(0.93f, 0.90f, 0.85f);
        public static readonly Color CowSpot = new Color(0.20f, 0.16f, 0.14f);
        public static readonly Color Denim = new Color(0.22f, 0.34f, 0.55f);
        public static readonly Color AlienGreen = new Color(0.45f, 1f, 0.35f);
        public static readonly Color AlienEye = new Color(0.05f, 0.05f, 0.08f);

        // -----------------------------------------------------------------
        // Materials
        // -----------------------------------------------------------------

        /// <summary>Creates a flat-shaded coloured material that works in URP or Built-in.</summary>
        public static Material MakeMaterial(Color color, float smoothness = 0.1f)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");

            var m = new Material(sh) { name = "LP_" + ColorUtility.ToHtmlStringRGB(color) };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            return m;
        }

        // -----------------------------------------------------------------
        // Primitive helpers
        // -----------------------------------------------------------------

        private static GameObject Prim(PrimitiveType type, Transform parent, string name,
            Vector3 localPos, Vector3 localScale, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!collider) SafeDestroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying) { Object.DestroyImmediate(obj); return; }
#endif
            Object.Destroy(obj);
        }

        // -----------------------------------------------------------------
        // Environment
        // -----------------------------------------------------------------

        public static GameObject BuildGround(float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane); // 10x10 units at scale 1
            go.name = "Ground";
            go.layer = GameLayers.Ground;
            go.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);
            go.GetComponent<Renderer>().sharedMaterial = MakeMaterial(Grass);
            return go;
        }

        /// <summary>
        /// A gentle grassy mound: a squashed sphere buried past its equator so
        /// only the walkable dome pokes out. Gives the flat farm some rolling
        /// terrain to prove out horse riding + ground snapping.
        /// </summary>
        public static GameObject BuildHill(Vector3 pos, float radius, float height)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Hill";
            go.layer = GameLayers.Ground;
            // Semi-axes (radius, height, radius); centre sunk so ~60% of the dome shows.
            go.transform.position = new Vector3(pos.x, -height * 0.4f, pos.z);
            go.transform.localScale = new Vector3(radius * 2f, height * 2f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = MakeMaterial(
                new Color(Grass.r * 1.12f, Grass.g * 1.12f, Grass.b * 1.05f)); // slightly sunnier green
            return go;
        }

        public static GameObject BuildBarn(Vector3 pos)
        {
            var root = new GameObject("Barn");
            root.transform.position = pos;
            var body = MakeMaterial(BarnRed);
            var roof = MakeMaterial(BarnRoof);
            var trim = MakeMaterial(CowWhite);

            Prim(PrimitiveType.Cube, root.transform, "Body", new Vector3(0f, 2f, 0f), new Vector3(8f, 4f, 6f), body);
            // Prism-ish roof: two tilted slabs.
            var r1 = Prim(PrimitiveType.Cube, root.transform, "Roof_L", new Vector3(0f, 4.6f, -1.6f), new Vector3(8.4f, 0.4f, 3.8f), roof);
            r1.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            var r2 = Prim(PrimitiveType.Cube, root.transform, "Roof_R", new Vector3(0f, 4.6f, 1.6f), new Vector3(8.4f, 0.4f, 3.8f), roof);
            r2.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            Prim(PrimitiveType.Cube, root.transform, "Door", new Vector3(0f, 1.4f, 3.01f), new Vector3(2.4f, 2.8f, 0.2f), trim);
            return root;
        }

        public static GameObject BuildTree(Vector3 pos, float scale = 1f)
        {
            var root = new GameObject("Tree");
            root.transform.position = pos;
            root.transform.localScale = Vector3.one * scale;
            Prim(PrimitiveType.Cylinder, root.transform, "Trunk", new Vector3(0f, 1.1f, 0f), new Vector3(0.35f, 1.1f, 0.35f), MakeMaterial(TrunkBrown));
            // Two stacked spheres for a chunky low-poly canopy.
            var leaf = MakeMaterial(Leaf);
            Prim(PrimitiveType.Sphere, root.transform, "Canopy_A", new Vector3(0f, 2.6f, 0f), new Vector3(2.2f, 2.0f, 2.2f), leaf, collider: false);
            Prim(PrimitiveType.Sphere, root.transform, "Canopy_B", new Vector3(0.3f, 3.6f, 0.2f), new Vector3(1.5f, 1.4f, 1.5f), leaf, collider: false);
            return root;
        }

        /// <summary>A run of fence between two points (posts + two rails).</summary>
        public static GameObject BuildFenceLine(Vector3 from, Vector3 to, Transform parent = null)
        {
            var root = new GameObject("Fence");
            if (parent != null) root.transform.SetParent(parent, false);
            var wood = MakeMaterial(Wood);

            Vector3 dir = to - from;
            float length = dir.magnitude;
            Vector3 mid = (from + to) * 0.5f;
            root.transform.position = mid;
            root.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            // Posts every ~2m.
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 2f));
            for (int i = 0; i <= posts; i++)
            {
                float z = -length / 2f + (length * i / posts);
                Prim(PrimitiveType.Cube, root.transform, "Post", new Vector3(0f, 0.6f, z), new Vector3(0.15f, 1.2f, 0.15f), wood);
            }
            // Two rails.
            Prim(PrimitiveType.Cube, root.transform, "Rail_Top", new Vector3(0f, 0.95f, 0f), new Vector3(0.1f, 0.12f, length), wood);
            Prim(PrimitiveType.Cube, root.transform, "Rail_Bottom", new Vector3(0f, 0.45f, 0f), new Vector3(0.1f, 0.12f, length), wood);
            return root;
        }

        // -----------------------------------------------------------------
        // Cattle
        // -----------------------------------------------------------------

        public static GameObject BuildCow(Vector3 pos)
        {
            // Root origin sits at the cow's feet.
            var root = new GameObject("Cow");
            root.transform.position = pos;

            var white = MakeMaterial(CowWhite);
            var spot = MakeMaterial(CowSpot);

            Prim(PrimitiveType.Cube, root.transform, "Body", new Vector3(0f, 0.85f, 0f), new Vector3(0.9f, 0.8f, 1.5f), white, collider: false);
            Prim(PrimitiveType.Cube, root.transform, "Head", new Vector3(0f, 1.05f, 1.0f), new Vector3(0.6f, 0.6f, 0.6f), white, collider: false);
            Prim(PrimitiveType.Cube, root.transform, "Spot", new Vector3(0.25f, 1.1f, -0.1f), new Vector3(0.5f, 0.4f, 0.6f), spot, collider: false);
            // Four stubby legs.
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -0.3f : 0.3f;
                float z = (i < 2) ? 0.5f : -0.5f;
                Prim(PrimitiveType.Cube, root.transform, "Leg", new Vector3(x, 0.3f, z), new Vector3(0.18f, 0.6f, 0.18f), white, collider: false);
            }

            // Trigger collider so shotgun pellets pass THROUGH cows (never friendly fire),
            // while still giving them a footprint for future systems.
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.8f, 0f);
            col.size = new Vector3(1.1f, 1.2f, 1.9f);
            col.isTrigger = true;

            root.AddComponent<HillbillyAlienShooter.Livestock.Cattle>();
            return root;
        }

        // -----------------------------------------------------------------
        // Aliens
        // -----------------------------------------------------------------

        /// <summary>
        /// Role-based dispatch: Saucers get a hovering dish, everything else a
        /// ground alien. Spawners call this and never care about enemy classes.
        /// </summary>
        public static GameObject BuildEnemy(EnemyData data, Vector3 groundPos)
        {
            if (data != null && data.role == EnemyData.EnemyRole.Saucer)
                return BuildUfo(data, groundPos);
            return BuildAlien(data, groundPos);
        }

        public static GameObject BuildAlien(EnemyData data, Vector3 pos)
        {
            if (data == null) data = EnemyData.CreateDefault();

            // Root origin at feet. Scale applied BEFORE components are added so
            // AlienEnemy's Awake captures the final size as its squash baseline.
            var root = new GameObject("Alien_" + data.displayName);
            root.transform.position = pos;
            root.transform.localScale = Vector3.one * Mathf.Max(0.3f, data.bodyScale);

            var bodyMat = MakeMaterial(data.bodyTint, 0.35f);
            var eyeMat = MakeMaterial(AlienEye);

            // Chubby body + big head.
            Prim(PrimitiveType.Capsule, root.transform, "Body", new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 0.42f, 0.5f), bodyMat, collider: false);
            Prim(PrimitiveType.Sphere, root.transform, "Head", new Vector3(0f, 1.0f, 0f), new Vector3(0.6f, 0.55f, 0.6f), bodyMat, collider: false);
            // Two bug eyes.
            Prim(PrimitiveType.Sphere, root.transform, "Eye_L", new Vector3(-0.16f, 1.05f, 0.24f), new Vector3(0.2f, 0.28f, 0.2f), eyeMat, collider: false);
            Prim(PrimitiveType.Sphere, root.transform, "Eye_R", new Vector3(0.16f, 1.05f, 0.24f), new Vector3(0.2f, 0.28f, 0.2f), eyeMat, collider: false);
            // Antenna.
            Prim(PrimitiveType.Cylinder, root.transform, "Antenna", new Vector3(0f, 1.45f, 0f), new Vector3(0.04f, 0.2f, 0.04f), bodyMat, collider: false);

            // One capsule collider on the root = the shootable hitbox.
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.75f, 0f);
            col.height = 1.5f;
            col.radius = 0.4f;

            root.AddComponent<Health>();
            var alien = root.AddComponent<AlienEnemy>();
            alien.Configure(data);
            root.AddComponent<GroundSnap>(); // hug the hills while shambling
            root.AddComponent<HitFlash>();

            var bar = root.AddComponent<HillbillyAlienShooter.UI.EnemyHealthBar>();
            bar.Configure(1.9f * data.bodyScale, 0.7f + 0.35f * data.bodyScale);
            return root;
        }

        /// <summary>Dish-shaped scout saucer, spawned at hover altitude above <paramref name="groundPos"/>.</summary>
        public static GameObject BuildUfo(EnemyData data, Vector3 groundPos)
        {
            if (data == null) data = EnemyData.CreateDefault();

            var root = new GameObject("UFO_" + data.displayName);
            root.transform.position = new Vector3(groundPos.x, data.hoverHeight, groundPos.z);

            var hull = MakeMaterial(new Color(0.5f, 0.53f, 0.6f), 0.65f);   // brushed saucer metal
            var domeMat = MakeMaterial(data.bodyTint, 0.5f);                 // glowing canopy
            var darkMat = MakeMaterial(new Color(0.2f, 0.2f, 0.25f), 0.3f);

            // Dish + canopy + belly. The dome gets its OWN collider (weak point);
            // the dish hitbox below is a flat box so it doesn't swallow the dome.
            Prim(PrimitiveType.Sphere, root.transform, "Dish", Vector3.zero, new Vector3(3.2f, 0.7f, 3.2f), hull, collider: false);
            var dome = Prim(PrimitiveType.Sphere, root.transform, "Dome", new Vector3(0f, 0.45f, 0f), new Vector3(1.3f, 1.05f, 1.3f), domeMat, collider: true);
            Prim(PrimitiveType.Sphere, root.transform, "Belly", new Vector3(0f, -0.3f, 0f), new Vector3(1.0f, 0.4f, 1.0f), darkMat, collider: false);

            // Ring of rim lights for that classic saucer look.
            var rimA = MakeMaterial(new Color(1f, 0.9f, 0.4f), 0.8f);
            var rimB = MakeMaterial(new Color(0.4f, 1f, 0.9f), 0.8f);
            const int rimCount = 6;
            for (int i = 0; i < rimCount; i++)
            {
                float a = (i / (float)rimCount) * Mathf.PI * 2f;
                var lightPos = new Vector3(Mathf.Cos(a) * 1.45f, 0.02f, Mathf.Sin(a) * 1.45f);
                Prim(PrimitiveType.Sphere, root.transform, "RimLight",
                    lightPos, Vector3.one * 0.22f, i % 2 == 0 ? rimA : rimB, collider: false);
            }

            // Soft glow pooling on the pasture beneath it.
            var glowGo = new GameObject("BellyGlow");
            glowGo.transform.SetParent(root.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            var glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(0.45f, 1f, 0.85f);
            glow.intensity = 2.2f;
            glow.range = 12f;

            // Flat box hitbox for the dish: covers the saucer body while leaving
            // the dome exposed above it so weak-point shots land on the dome.
            var col = root.AddComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = new Vector3(3.2f, 0.75f, 3.2f);

            var health = root.AddComponent<Health>();

            // Weak point: hits on the glowing dome hurt a lot more.
            var weak = dome.AddComponent<WeakPoint>();
            weak.Configure(health, data.weakPointMultiplier);

            var ufo = root.AddComponent<UfoEnemy>();
            ufo.Configure(data);
            root.AddComponent<HitFlash>();

            var bar = root.AddComponent<HillbillyAlienShooter.UI.EnemyHealthBar>();
            bar.Configure(2.3f, 2.4f); // wide bar floating above the dome
            return root;
        }

        /// <summary>Slow, dodgeable plasma bolt fired by war saucers.</summary>
        public static GameObject BuildPlasmaBolt(Vector3 pos, Vector3 dir, EnemyData data, GameObject playerTarget)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PlasmaBolt";
            SafeDestroy(go.GetComponent<Collider>()); // distance-checked, never physical
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.38f;
            go.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(1f, 0.35f, 0.55f), 0.8f);

            // Hot pink glow so bolts read at night.
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.4f, 0.6f);
            light.intensity = 1.8f;
            light.range = 3f;

            var bolt = go.AddComponent<PlasmaBolt>();
            bolt.Configure(dir, data.projectileSpeed, data.projectileDamage, playerTarget);
            return go;
        }

        /// <summary>Expanding slam ring marking a Brute's AoE. Self-destructs.</summary>
        public static GameObject BuildShockwave(Vector3 pos, float radius, float duration)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Shockwave";
            SafeDestroy(go.GetComponent<Collider>());
            go.transform.position = new Vector3(pos.x, pos.y + 0.06f, pos.z);
            go.transform.localScale = new Vector3(0.6f, 0.08f, 0.6f);

            var color = new Color(1f, 0.7f, 0.25f, 0.65f); // hot dust ring
            var mat = new Material(Shader.Find("Sprites/Default")) { color = color };
            go.GetComponent<Renderer>().material = mat;

            var fx = go.AddComponent<HillbillyAlienShooter.Effects.ShockwaveFx>();
            fx.Configure(radius, duration, mat, color);
            return go;
        }

        /// <summary>
        /// The MOTHERSHIP (Packet 3.1 progression gate): a colossal set-piece
        /// saucer that descends over the farm when enough cattle are saved.
        /// No collider, no health — Packet 3.2 boards it.
        /// </summary>
        public static GameObject BuildMothership(Vector3 groundCenter)
        {
            var root = new GameObject("Mothership");
            root.transform.position = new Vector3(groundCenter.x, 60f, groundCenter.z);

            var hull = MakeMaterial(new Color(0.25f, 0.26f, 0.33f), 0.7f);   // ominous gunmetal
            var domeMat = MakeMaterial(new Color(0.5f, 1f, 0.6f), 0.55f);
            var vent = MakeMaterial(new Color(0.12f, 0.12f, 0.16f), 0.4f);

            Prim(PrimitiveType.Sphere, root.transform, "Hull", Vector3.zero, new Vector3(18f, 3.6f, 18f), hull, collider: false);
            Prim(PrimitiveType.Sphere, root.transform, "Dome", new Vector3(0f, 2.2f, 0f), new Vector3(6.5f, 4.5f, 6.5f), domeMat, collider: false);
            Prim(PrimitiveType.Sphere, root.transform, "Underbelly", new Vector3(0f, -1.6f, 0f), new Vector3(7f, 2.2f, 7f), vent, collider: false);

            // Double ring of running lights.
            var lightA = MakeMaterial(new Color(1f, 0.85f, 0.35f), 0.9f);
            var lightB = MakeMaterial(new Color(0.4f, 1f, 0.9f), 0.9f);
            for (int ring = 0; ring < 2; ring++)
            {
                int count = ring == 0 ? 12 : 8;
                float radius = ring == 0 ? 8.4f : 5.2f;
                float y = ring == 0 ? 0.4f : 1.4f;
                for (int i = 0; i < count; i++)
                {
                    float a = (i / (float)count) * Mathf.PI * 2f;
                    Prim(PrimitiveType.Sphere, root.transform, "RunningLight",
                        new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius),
                        Vector3.one * 0.7f, i % 2 == 0 ? lightA : lightB, collider: false);
                }
            }

            // A vast sickly glow pooling over the whole pasture.
            var glowGo = new GameObject("AbyssalGlow");
            glowGo.transform.SetParent(root.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, -2.5f, 0f);
            var glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(0.5f, 1f, 0.7f);
            glow.intensity = 4f;
            glow.range = 45f;

            root.AddComponent<HillbillyAlienShooter.Effects.MothershipFx>();
            return root;
        }

        // -----------------------------------------------------------------
        // Pickups
        // -----------------------------------------------------------------

        /// <summary>A glowing shard of alien tech (dropped by dead invaders).</summary>
        public static GameObject BuildTechPickup(Vector3 pos, int amount)
        {
            var root = new GameObject("TechPickup");
            root.transform.position = pos;

            var glowMat = MakeMaterial(new Color(0.35f, 1f, 1f), 0.8f); // electric cyan

            // Two interlocked rotated cubes read as a crystal at low poly counts.
            var a = Prim(PrimitiveType.Cube, root.transform, "Shard_A", Vector3.zero, Vector3.one * 0.34f, glowMat, collider: false);
            a.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            var b = Prim(PrimitiveType.Cube, root.transform, "Shard_B", Vector3.zero, Vector3.one * 0.34f, glowMat, collider: false);
            b.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);

            // Night-time farm = pickup glow does real work guiding the player.
            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(root.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.4f, 1f, 1f);
            light.intensity = 2.4f;
            light.range = 4f;

            var pickup = root.AddComponent<HillbillyAlienShooter.Pickups.TechPickup>();
            pickup.Configure(amount);
            return root;
        }

        // -----------------------------------------------------------------
        // Horse
        // -----------------------------------------------------------------

        public static GameObject BuildHorse(HillbillyAlienShooter.Data.HorseData data, Vector3 pos)
        {
            if (data == null) data = HillbillyAlienShooter.Data.HorseData.CreateDefault();

            // Root origin at hooves.
            var root = new GameObject("Horse_" + data.displayName);
            root.transform.position = pos;

            var coat = MakeMaterial(data.bodyColor, 0.15f);
            var mane = MakeMaterial(data.maneColor, 0.1f);
            var saddle = MakeMaterial(data.saddleColor, 0.25f);
            var blaze = MakeMaterial(CowWhite);

            // Body & hindquarters.
            Prim(PrimitiveType.Cube, root.transform, "Body", new Vector3(0f, 1.15f, 0f), new Vector3(0.75f, 0.75f, 1.9f), coat, collider: false);
            // Neck leaning forward, head on top.
            var neck = Prim(PrimitiveType.Cube, root.transform, "Neck", new Vector3(0f, 1.75f, 0.85f), new Vector3(0.35f, 0.95f, 0.35f), coat, collider: false);
            neck.transform.localRotation = Quaternion.Euler(-32f, 0f, 0f);
            Prim(PrimitiveType.Cube, root.transform, "Head", new Vector3(0f, 2.25f, 1.28f), new Vector3(0.32f, 0.42f, 0.65f), coat, collider: false);
            Prim(PrimitiveType.Cube, root.transform, "Blaze", new Vector3(0f, 2.28f, 1.58f), new Vector3(0.12f, 0.24f, 0.1f), blaze, collider: false);
            // Ears.
            Prim(PrimitiveType.Cube, root.transform, "Ear_L", new Vector3(-0.11f, 2.53f, 1.1f), new Vector3(0.08f, 0.18f, 0.06f), mane, collider: false);
            Prim(PrimitiveType.Cube, root.transform, "Ear_R", new Vector3(0.11f, 2.53f, 1.1f), new Vector3(0.08f, 0.18f, 0.06f), mane, collider: false);
            // Mane along the back of the neck + tail.
            var maneGo = Prim(PrimitiveType.Cube, root.transform, "Mane", new Vector3(0f, 1.85f, 0.62f), new Vector3(0.14f, 0.9f, 0.2f), mane, collider: false);
            maneGo.transform.localRotation = Quaternion.Euler(-32f, 0f, 0f);
            var tail = Prim(PrimitiveType.Cube, root.transform, "Tail", new Vector3(0f, 1.15f, -1.1f), new Vector3(0.15f, 0.75f, 0.15f), mane, collider: false);
            tail.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
            // Legs + dark hooves.
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -0.27f : 0.27f;
                float z = (i < 2) ? 0.68f : -0.68f;
                Prim(PrimitiveType.Cube, root.transform, "Leg", new Vector3(x, 0.42f, z), new Vector3(0.17f, 0.85f, 0.17f), coat, collider: false);
                Prim(PrimitiveType.Cube, root.transform, "Hoof", new Vector3(x, 0.06f, z), new Vector3(0.19f, 0.12f, 0.19f), mane, collider: false);
            }
            // Saddle + the seat the player snaps to.
            Prim(PrimitiveType.Cube, root.transform, "Saddle", new Vector3(0f, 1.58f, -0.15f), new Vector3(0.55f, 0.16f, 0.75f), saddle, collider: false);

            var seat = new GameObject("MountPoint");
            seat.transform.SetParent(root.transform, false);
            seat.transform.localPosition = new Vector3(0f, 1.85f, -0.15f);

            // The mover + the interactable hitbox in one: a CharacterController.
            var cc = root.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 1.1f, 0f);
            cc.height = 2.0f;
            cc.radius = 0.6f;
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.45f;

            var horse = root.AddComponent<HillbillyAlienShooter.Horse.HorseController>();
            horse.Configure(data, seat.transform);
            return root;
        }

        // -----------------------------------------------------------------
        // Player (the hillbilly)
        // -----------------------------------------------------------------

        public static GameObject BuildPlayer(Vector3 pos)
        {
            // Capsule body (first-person, so you won't see much of it).
            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "Hillbilly (Player)";
            root.transform.position = pos + Vector3.up * 1f; // capsule centre 1m up so feet touch ground
            root.GetComponent<Renderer>().sharedMaterial = MakeMaterial(Denim);

            // The CharacterController is the mover; drop the auto CapsuleCollider so
            // we don't have two overlapping colliders fighting each other.
            SafeDestroy(root.GetComponent<CapsuleCollider>());

            var cc = root.AddComponent<CharacterController>();
            cc.center = Vector3.zero;
            cc.height = 2f;
            cc.radius = 0.4f;

            root.AddComponent<PlayerInputHandler>();
            root.AddComponent<PlayerController>();
            root.AddComponent<PlayerInteraction>();
            root.AddComponent<CameraRig>(); // third/first person framing (V toggles)
            root.AddComponent<Health>();
            root.AddComponent<PlayerHealth>();

            // Camera rig (PlayerController will also adopt a "CameraPivot" named child).
            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 0.7f, 0f);

            var camGo = new GameObject("PlayerCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.14f); // night sky
            camGo.AddComponent<AudioListener>();

            // Shotgun + the jury-rig upgrade bench live on the player root; the
            // shotgun resolves the camera pivot as its aim source at Start.
            root.AddComponent<WeaponUpgradeController>();
            root.AddComponent<Shotgun>();

            return root;
        }
    }
}
