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

        public static GameObject BuildAlien(EnemyData data, Vector3 pos)
        {
            if (data == null) data = EnemyData.CreateDefault();

            // Root origin at feet.
            var root = new GameObject("Alien_" + data.displayName);
            root.transform.position = pos;

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

            // Shotgun lives on the player root; it resolves the camera as its aim source.
            root.AddComponent<Shotgun>();

            return root;
        }
    }
}
