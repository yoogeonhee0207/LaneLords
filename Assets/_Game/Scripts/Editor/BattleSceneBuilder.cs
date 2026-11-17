using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace TinySwordsBattle.EditorTools
{
    /// <summary>
    /// Menu: Tools > Lane Lords > Build Battle Scene
    /// Creates animator controllers, unit/stage data and a playable Battle scene from the Tiny Swords assets.
    /// Unit and stage data that already exist keep their values, so your balance tweaks are safe.
    /// </summary>
    public static class BattleSceneBuilder
    {
        const string TS = "Assets/Tiny Swords";
        const string Root = "Assets/_Game";
        const string TSE = "Assets/Tiny Swords Enemy";
        const string ScenePath = Root + "/Scenes/Battle.unity";
        const string StageSelectScenePath = Root + "/Scenes/StageSelect.unity";
        const string TitleScenePath = Root + "/Scenes/Title.unity";
        const string TitleArt = Root + "/Art/Title/";
        const string StoryArt = Root + "/Art/Story/";
        const float LaneY = -1.7f;

        static Font uiFont;
        static Sprite uiSprite;
        static UnitData[] codexUnits = new UnitData[0];
        static StageCatalog builtCatalog;

        [MenuItem("Tools/Lane Lords/Play From Title")]
        public static void PlayFromTitle()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath) == null)
            {
                EditorUtility.DisplayDialog("Lane Lords", "Title 씬이 없어요. 먼저 Build Battle Scene 을 실행해 주세요.", "OK");
                return;
            }
            EditorSceneManager.OpenScene(TitleScenePath);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/Lane Lords/Play From Stage Select")]
        public static void PlayFromStageSelect()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(StageSelectScenePath) == null)
            {
                EditorUtility.DisplayDialog("Lane Lords", "StageSelect 씬이 없어요. 먼저 Build Battle Scene 을 실행해 주세요.", "OK");
                return;
            }
            EditorSceneManager.OpenScene(StageSelectScenePath);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/Lane Lords/Test: Infinite Gems")]
        public static void ToggleInfiniteGems()
        {
            bool on = PlayerPrefs.GetInt("test_infinite_gems", 0) == 0;
            PlayerPrefs.SetInt("test_infinite_gems", on ? 1 : 0);
            PlayerPrefs.Save();
        }

        [MenuItem("Tools/Lane Lords/Test: Infinite Gems", true)]
        public static bool ToggleInfiniteGemsCheck()
        {
            Menu.SetChecked("Tools/Lane Lords/Test: Infinite Gems", PlayerPrefs.GetInt("test_infinite_gems", 0) == 1);
            return true;
        }

        [MenuItem("Tools/Lane Lords/Reset Story + Tutorial (show again)")]
        public static void ResetStory()
        {
            for (int c = 1; c <= 9; c++)
                for (int i = 1; i <= 9; i++)
                {
                    PlayerPrefs.DeleteKey($"story_in_{c}-{i}");
                    PlayerPrefs.DeleteKey($"story_out_{c}-{i}");
                    PlayerPrefs.DeleteKey($"story_boss_{c}-{i}");
                }
            PlayerPrefs.DeleteKey("story_ending");
            PlayerPrefs.DeleteKey(TutorialUI.DoneKey);
            PlayerPrefs.DeleteKey(ShopGuideUI.DoneKey);
            foreach (var t in UnitProgress.ShopUnits) PlayerPrefs.DeleteKey("unit_tip_" + t);
            PlayerPrefs.Save();
            EditorUtility.DisplayDialog("Lane Lords", "스토리 대사와 튜토리얼(1-1, 상점 안내, 새 유닛 안내)을 처음 상태로 돌렸어요.", "OK");
        }

        [MenuItem("Tools/Lane Lords/Build Battle Scene")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(TS))
            {
                EditorUtility.DisplayDialog("Lane Lords", "Assets/Tiny Swords 폴더를 찾을 수 없어요. 에셋을 먼저 Import 해주세요.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(Root + "/Data");
            EnsureFolder(Root + "/Generated");
            EnsureFolder(Root + "/Scenes");

            // ---------- Unit data ----------
            var pawn = MakeUnit(UnitType.Pawn, "Pawn", "Interact Axe", cost: 25, hp: 30, atk: 0, range: 0, interval: 1f, speed: 1.5f, cd: 3f);
            // Roles: Warrior = melee damage, Lancer = tank that blocks, Archer = ranged, Monk = healer. No counter bonuses.
            var warrior = MakeUnit(UnitType.Warrior, "Warrior", "Attack1", cost: 30, hp: 90, atk: 15, range: 0.9f, interval: 1f, speed: 1.2f, cd: 2f);
            var archer = MakeUnit(UnitType.Archer, "Archer", "Shoot", cost: 45, hp: 55, atk: 14, range: 5f, interval: 1.4f, speed: 1.1f, cd: 3f, ranged: true);
            var lancer = MakeUnit(UnitType.Lancer, "Lancer", "Right_Attack", cost: 50, hp: 260, atk: 8, range: 1.3f, interval: 1.3f, speed: 0.9f, cd: 4f);
            var monk = MakeUnit(UnitType.Monk, "Monk", "Heal", cost: 70, hp: 60, atk: 14, range: 3.5f, interval: 2.2f, speed: 0.9f, cd: 6f, heal: 14f);

            var m = MakeMonsters();
            var catalog = MakeStages(m);
            var stage = catalog.stages[0];
            builtCatalog = catalog;

            // Codex order: our units, then monsters chapter by chapter (bosses last in each chapter)
            var codex = new List<UnitData> { pawn, warrior, archer, lancer, monk };
            foreach (var n in new[] { "Torch Goblin", "Spear Goblin", "Snake", "Spider", "Hex Shaman",
                                      "Gnome", "Slingshot Gnome", "Bumblebee", "Bear", "Panda",
                                      "Paddle Shark", "Harpoon Shark", "Bomb Fish", "Lizard", "Turtle", "Minotaur",
                                      "Skull", "Giant Bat", "Imp", "Gnoll", "Thief", "Troll" })
                if (m.TryGetValue(n, out var md) && md != null) codex.Add(md);
            codexUnits = codex.ToArray();

            AssetDatabase.SaveAssets();

            uiFont = LoadUIFont();
            uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            BuildTitleScene();
            BuildStageSelectScene(catalog);

            // ---------- Scene ----------
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            uiFont = LoadUIFont();
            uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // Camera + light
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0.6f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(71, 171, 169, 255); // water
            camGo.AddComponent<AudioListener>();
            var pan = camGo.AddComponent<CameraPan>();
            pan.fieldMinX = -13f;
            pan.fieldMaxX = 13f;

            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<Light2D>();
            var lightSo = new SerializedObject(light);
            var lightTypeProp = lightSo.FindProperty("m_LightType");
            if (lightTypeProp != null) lightTypeProp.intValue = (int)Light2D.LightType.Global;
            var intensityProp = lightSo.FindProperty("m_Intensity");
            if (intensityProp != null) intensityProp.floatValue = 1f;
            lightSo.ApplyModifiedPropertiesWithoutUndo();

            // Castles and gold mine
            var playerCastle = MakeTieredCastle("Player Base", TS + "/Buildings/Blue Buildings/", -9.5f, Team.Player);
            var enemyCastle = MakeTieredCastle("Enemy Base", TS + "/Buildings/Black Buildings/", 9.5f, Team.Enemy);

            // Forest: workers chop wood here (this is where their income comes from).
            var mine = new GameObject("Forest");
            mine.transform.position = new Vector3(-5.8f, -0.5f, 0f); // back pines sit right on the shore (land top y = 0.6)
            int[] treeNumbers = { 1, 2, 1, 2, 3, 4, 3 };
            Vector3[] treeOffsets =
            {
                new Vector3(-1.6f, 0.95f), new Vector3(-0.5f, 1.0f), new Vector3(0.6f, 0.95f), new Vector3(1.7f, 1.0f), // back: tall pines
                new Vector3(-1.05f, 0.3f), new Vector3(0.05f, 0.25f), new Vector3(1.15f, 0.3f)                         // front: round trees
            };
            // Distance from the tree's centre down to its trunk base (pixels / 64), per tree type.
            float[] trunkBelowCentre = { 0f, 113f / 64f, 121f / 64f, 74f / 64f, 72f / 64f };
            const float treeScale = 0.75f;
            for (int i = 0; i < treeNumbers.Length; i++)
            {
                int n = treeNumbers[i];
                string treeDir = TS + "/Pawn and Resources/Wood/Trees/";
                var tree = MakeSpriteObject("Tree " + (i + 1), LoadSprite(treeDir + $"Tree{n}.png"), mine.transform.position + treeOffsets[i]);
                tree.transform.SetParent(mine.transform, true);
                tree.transform.localScale = Vector3.one * treeScale;

                var controller = AssetDatabase.FindAssets("t:AnimatorController", new[] { treeDir + $"Tree {n} Animation" })
                    .Select(g => AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(g)))
                    .FirstOrDefault(c => c != null);
                if (controller != null)
                {
                    tree.AddComponent<Animator>().runtimeAnimatorController = controller;
                    tree.AddComponent<LoopAnimator>();
                }

                float baseY = tree.transform.position.y - trunkBelowCentre[n] * treeScale;
                var trunk = new GameObject("Base");
                trunk.transform.SetParent(tree.transform, true);
                trunk.transform.position = new Vector3(tree.transform.position.x, baseY, 0f);
                tree.GetComponent<SpriteRenderer>().sortingOrder = Mathf.RoundToInt(-baseY * 100f);
            }

            BuildTerrain();
            Decorate();
            BuildChapterThemes(mine.transform, camGo);

            // Managers
            var battleGo = new GameObject("Battle");
            var gold = battleGo.AddComponent<GoldManager>();
            var waves = battleGo.AddComponent<EnemyWaveController>();
            var spawner = battleGo.AddComponent<Spawner>();
            spawner.playerUnits = new[] { pawn, warrior, archer, lancer, monk };
            spawner.laneY = LaneY;
            var bm = battleGo.AddComponent<BattleManager>();
            bm.stage = stage;
            bm.catalog = catalog;
            bm.playerCastle = playerCastle;
            bm.enemyCastle = enemyCastle;
            bm.goldMine = mine.transform;
            bm.gold = gold;
            bm.waves = waves;
            bm.dustEffect = Load<RuntimeAnimatorController>(TS + "/Particle FX/Dust 1 Animation/Dust 1.controller");
            bm.explosionEffect = Load<RuntimeAnimatorController>(TS + "/Particle FX/Explosion 1 Animation/Explosion 1.controller");
            bm.fireEffect = Load<RuntimeAnimatorController>(TS + "/Particle FX/Fire 1 Animation/Fire 1.controller");
            bm.healEffect = Load<RuntimeAnimatorController>(TS + "/Units/Extra/Heal Effect/Heal.controller");
            bm.popupFont = LoadUIFont();
            bm.arrowSprite = LoadSprite(TS + "/Units/Extra/Arrow/Arrow.png");
            bm.bigBarFrame = LoadSprite(TS + "/UI Elements/Bars/BigBar_Base.png");
            bm.bigBarFill = LoadSprite(TS + "/UI Elements/Bars/BigBar_Fill.png");
            bm.smallBarFrame = LoadSprite(TS + "/UI Elements/Bars/SmallBar_Base.png");
            bm.smallBarFill = LoadSprite(TS + "/UI Elements/Bars/SmallBar_Fill.png");

            BuildUI(bm, spawner);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(TitleScenePath, true), // first scene of the game
                new EditorBuildSettingsScene(StageSelectScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();

            Debug.Log("[Lane Lords] Scenes built: " + StageSelectScenePath + ", " + ScenePath);
            EditorUtility.DisplayDialog("Lane Lords",
                "Title / StageSelect / Battle 씬을 만들었어요.\n지금은 Battle 씬이 열려 있어요. 처음부터 하려면 Tools > Lane Lords > Play From Title 을 누르세요.", "OK");
        }

        // ======================= Data =======================

        static UnitData MakeUnit(UnitType type, string name, string attackKey, int cost, float hp, float atk, float range, float interval,
            float speed, float cd, bool counter = false, UnitType strong = UnitType.Pawn, bool ranged = false, float heal = 0f)
        {
            string path = $"{Root}/Data/Unit_{name}.asset";
            var data = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<UnitData>();
                data.type = type;
                data.displayName = name;
                data.cost = cost;
                data.maxHp = hp;
                data.attack = atk;
                data.attackRange = range;
                data.attackInterval = interval;
                data.moveSpeed = speed;
                data.spawnCooldown = cd;
                data.hasCounter = counter;
                data.strongAgainst = strong;
                data.isRanged = ranged;
                data.healAmount = heal;
                AssetDatabase.CreateAsset(data, path);
            }
            else if (type == UnitType.Warrior && data.attack >= 18f)
            {
                // Balance patch v2: the Warrior was the best deal by far.
                data.maxHp = hp;
                data.attack = atk;
            }
            else if (type == UnitType.Monk && data.attack <= 0f)
            {
                // Migrate the old single-target healer to the holy-burst Monk.
                data.attack = atk;
                data.attackRange = range;
                data.attackInterval = interval;
                data.healAmount = heal;
                data.skillRadius = 1.8f;
            }

            string playerFolder = type == UnitType.Pawn ? $"{TS}/Pawn and Resources/Pawn/Blue Pawn" : $"{TS}/Units/Blue Units/{name}";
            string enemyFolder = type == UnitType.Pawn ? $"{TS}/Pawn and Resources/Pawn/Red Pawn" : $"{TS}/Units/Red Units/{name}";

            AnimationClip idleClip;
            data.playerController = MakeController(playerFolder, attackKey, $"{Root}/Generated/{name}_Player.controller", out idleClip);
            data.icon = FirstSprite(idleClip);
            AnimationClip unused;
            data.enemyController = MakeController(enemyFolder, attackKey, $"{Root}/Generated/{name}_Enemy.controller", out unused);

            EditorUtility.SetDirty(data);
            return data;
        }

        // ======================= Monsters (Tiny Swords Enemy Pack) =======================

        /// <summary>
        /// Enemy roster: 4 chapters, 4-5 monsters each, plus one boss per chapter.
        /// Existing Enemy_*.asset files keep their stats (your tweaks are safe); animations are always rebuilt.
        /// charH = character height in pixels (sets the HP bar height).
        /// </summary>
        static Dictionary<string, UnitData> MakeMonsters()
        {
            var d = new Dictionary<string, UnitData>();
            EnsureFolder(Root + "/Generated/Enemy");
            // Chapter 1: Dark Forest (boss Hex Shaman)
            d["Torch Goblin"] = MakeMonster("Torch Goblin", "Attack", "Run", 30, 90, 16, 0.9f, 1.0f, 1.3f, 2f, charH: 68);
            d["Spear Goblin"] = MakeMonster("Spear Goblin", "Attack Fast", "Run", 40, 80, 14, 1.3f, 1.1f, 1.3f, 3f, charH: 128);
            d["Snake"]        = MakeMonster("Snake", "Attack", "Run", 25, 55, 12, 0.8f, 0.8f, 1.6f, 2f, charH: 58);
            d["Spider"]       = MakeMonster("Spider", "Attack", "Run", 35, 70, 10, 0.9f, 0.6f, 1.5f, 2.5f, charH: 85);
            d["Hex Shaman"]   = MakeMonster("Hex Shaman", "Attack", "Run", 150, 1125, 34, 4.5f, 1.6f, 0.25f, 0f, charH: 84,
                                    projectile: "Hex Shaman/Hex Shaman_Projectile.png", scale: 1.6f, boss: true);
            // Chapter 2: Bamboo Hills (boss Panda)
            d["Gnome"]           = MakeMonster("Gnome", "Attack", "Run", 25, 60, 12, 0.8f, 0.9f, 1.4f, 2f, charH: 70);
            d["Slingshot Gnome"] = MakeMonster("Slingshot Gnome", "Shoot", "Run", 45, 50, 13, 4.5f, 1.4f, 1.1f, 3f, charH: 65,
                                    projectile: "Slingshot Gnome/Acorn_Projectile.png");
            d["Bumblebee"]       = MakeMonster("Bumblebee", "Attack", "Move", 35, 50, 12, 0.9f, 0.7f, 1.8f, 2.5f, charH: 107);
            d["Bear"]            = MakeMonster("Bear", "Attack", "Run", 60, 280, 14, 1.0f, 1.3f, 0.8f, 4f, charH: 92);
            d["Panda"]           = MakeMonster("Panda", "Attack", "Run", 150, 2100, 50, 1.2f, 1.3f, 0.25f, 0f, charH: 94, scale: 1.5f, boss: true);
            // Chapter 3: Shore and Canyon (boss Minotaur)
            d["Paddle Shark"]  = MakeMonster("Paddle Shark", "Attack", "Run", 35, 110, 16, 0.9f, 1.0f, 1.2f, 2f, charH: 73);
            d["Harpoon Shark"] = MakeMonster("Harpoon Shark", "Throw", "Run", 50, 70, 16, 4.5f, 1.5f, 1.1f, 3f, charH: 79,
                                    projectile: "Harpoon Shark/Harpoon.png");
            d["Bomb Fish"]     = MakeMonster("Bomb Fish", "Shoot", "Run", 55, 45, 30, 3.5f, 2.2f, 1.0f, 4f, charH: 68,
                                    projectile: "Bomb Fish/Bomb_Idle.png");
            d["Lizard"]        = MakeMonster("Lizard", "Attack", "Run", 40, 90, 20, 0.9f, 0.9f, 1.5f, 2.5f, charH: 127);
            d["Turtle"]        = MakeMonster("Turtle", "Attack", "Walk", 60, 340, 10, 1.0f, 1.4f, 0.7f, 4f, charH: 91);
            d["Minotaur"]      = MakeMonster("Minotaur", "Attack", "Walk", 150, 1500, 36, 1.3f, 1.8f, 0.25f, 0f, charH: 129, scale: 1.25f, boss: true);
            // Chapter 4: Troll Lands (boss Troll King)
            d["Skull"]     = MakeMonster("Skull", "Attack", "Run", 30, 85, 17, 0.9f, 1.0f, 1.2f, 2f, charH: 73);
            d["Giant Bat"] = MakeMonster("Giant Bat", "Attack", "Move", 35, 60, 14, 0.9f, 0.8f, 1.9f, 2.5f, charH: 102);
            d["Imp"]       = MakeMonster("Imp", "Attack_Loop", "Move", 45, 80, 22, 0.9f, 1.0f, 1.4f, 3f, charH: 104);
            d["Gnoll"]     = MakeMonster("Gnoll", "Throw", "Walk", 50, 70, 18, 4.5f, 1.5f, 1.1f, 3f, charH: 75,
                                    projectile: "Gnoll/Gnoll_Bone.png");
            d["Thief"]     = MakeMonster("Thief", "Attack", "Run", 45, 75, 26, 0.9f, 1.0f, 1.7f, 3f, charH: 76);
            d["Troll"]     = MakeMonster("Troll", "Attack", "Walk", 150, 3600, 65, 1.5f, 2.0f, 0.25f, 0f, charH: 211, scale: 1f, boss: true);
            return d;
        }

        // Animals controlled by the Troll King's curse (story): they sparkle away when defeated.
        static readonly string[] CursedAnimals = { "Snake", "Spider", "Bumblebee", "Bear", "Panda", "Turtle", "Giant Bat" };

        static UnitData MakeMonster(string name, string attackKey, string moveKey, int cost, float hp, float atk, float range,
            float interval, float speed, float cd, int charH, string projectile = null, float scale = 1f, bool boss = false)
        {
            string folder = $"{TSE}/{name}";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning("[Lane Lords] Enemy Pack folder not found: " + folder);
                return null;
            }

            string path = $"{Root}/Data/Enemy_{name.Replace(" ", "")}.asset";
            var data = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<UnitData>();
                data.type = UnitType.Warrior; // plain fighter behaviour
                data.displayName = name;
                data.cost = cost;
                data.maxHp = hp;
                data.attack = atk;
                data.attackRange = range;
                data.attackInterval = interval;
                data.moveSpeed = speed;
                data.spawnCooldown = cd;
                data.isRanged = projectile != null;
                data.scale = scale;
                data.isBoss = boss;
                data.barHeight = Mathf.Max(0.6f, (charH - 41) / 64f + 0.15f);
                AssetDatabase.CreateAsset(data, path);
            }
            if (projectile != null) data.projectileSprite = LoadSprite($"{TSE}/{projectile}");
            data.isCursedAnimal = System.Array.IndexOf(CursedAnimals, name) >= 0;

            string gen = $"{Root}/Generated/Enemy/{name.Replace(" ", "")}";
            AnimationClip idle = MakeSheetClip($"{folder}/{name}_Idle.png", gen + "_Idle.anim");
            AnimationClip run = MakeSheetClip($"{folder}/{name}_{moveKey}.png", gen + "_Run.anim");
            AnimationClip attack = MakeSheetClip($"{folder}/{name}_{attackKey}.png", gen + "_Attack.anim");

            string ctrlPath = gen + ".controller";
            AssetDatabase.DeleteAsset(ctrlPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            var sm = controller.layers[0].stateMachine;
            var idleState = sm.AddState("Idle");
            idleState.motion = idle;
            sm.AddState("Run").motion = run != null ? run : idle;
            sm.AddState("Attack").motion = attack != null ? attack : idle;
            sm.defaultState = idleState;

            data.playerController = controller;
            data.enemyController = controller;
            string avatar = AssetDatabase.FindAssets("Avatar t:Texture2D", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
            if (avatar != null) data.icon = LoadSprite(avatar);
            EditorUtility.SetDirty(data);
            return data;
        }

        /// <summary>Builds a 10 fps clip from a sliced sprite strip (frames named Name_0, Name_1, ...).</summary>
        static AnimationClip MakeSheetClip(string texturePath, string outPath)
        {
            var frames = AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>()
                .OrderBy(sp => FrameIndex(sp.name)).ToArray();
            if (frames.Length == 0)
            {
                Debug.LogWarning("[Lane Lords] No sprites in " + texturePath);
                return null;
            }

            const float fps = 10f;
            var clip = new AnimationClip { frameRate = fps };
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false; // Unit.cs restarts Idle/Run itself
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.DeleteAsset(outPath);
            AssetDatabase.CreateAsset(clip, outPath);
            return clip;
        }

        static int FrameIndex(string spriteName)
        {
            int us = spriteName.LastIndexOf('_');
            int n;
            return us >= 0 && int.TryParse(spriteName.Substring(us + 1), out n) ? n : 0;
        }

        static UnitData M(Dictionary<string, UnitData> m, string name)
        {
            UnitData u;
            return m.TryGetValue(name, out u) ? u : null;
        }

        /// <summary>
        /// 4 chapters x 6 stages. All stage numbers (income, multipliers, castle HP), the enemy roster, boss and
        /// ground colour are refreshed from here on every build, so tune them in this table.
        /// </summary>
        static StageCatalog MakeStages(Dictionary<string, UnitData> m)
        {
            var basic = new List<UnitType> { UnitType.Pawn, UnitType.Warrior };
            UnitData torch = M(m, "Torch Goblin"), spear = M(m, "Spear Goblin"), snake = M(m, "Snake"), spider = M(m, "Spider");
            UnitData gnome = M(m, "Gnome"), sling = M(m, "Slingshot Gnome"), bee = M(m, "Bumblebee"), bear = M(m, "Bear");
            UnitData paddle = M(m, "Paddle Shark"), harpoon = M(m, "Harpoon Shark"), bomb = M(m, "Bomb Fish"), lizard = M(m, "Lizard"), turtle = M(m, "Turtle");
            UnitData skull = M(m, "Skull"), bat = M(m, "Giant Bat"), imp = M(m, "Imp"), gnoll = M(m, "Gnoll"), thief = M(m, "Thief");

            var stages = new List<StageData>
            {
                // Chapter 1: Dark Forest
                MakeStage("1-1", basic, 5.0f,  1.00f, 600,  null, 0f, Choice(torch, 1)),
                MakeStage("1-2", basic, 5.25f, 1.00f, 650,  null, 0f, Choice(torch, 2), Choice(snake, 1)),
                MakeStage("1-3", basic, 5.5f,  1.05f, 700,  null, 0f, Choice(torch, 2), Choice(snake, 1), Choice(spider, 1)),
                MakeStage("1-4", basic, 5.75f, 1.10f, 800,  null, 0f, Choice(torch, 2), Choice(spear, 1), Choice(spider, 1)),
                MakeStage("1-5", basic, 6.0f,  1.15f, 850,  null, 0f, Choice(torch, 1), Choice(spear, 1), Choice(snake, 1), Choice(spider, 1)),
                MakeStage("1-6", basic, 6.0f,  1.10f, 950,  M(m, "Hex Shaman"), 1.15f, Choice(torch, 2), Choice(spear, 1), Choice(snake, 1), Choice(spider, 1)),
                // Chapter 2: Bamboo Hills
                MakeStage("2-1", basic, 7.5f,  1.40f, 1050, null, 0f, Choice(gnome, 2), Choice(bee, 1)),
                MakeStage("2-2", basic, 7.75f, 1.45f, 1150, null, 0f, Choice(gnome, 2), Choice(bee, 1), Choice(sling, 1)),
                MakeStage("2-3", basic, 8.0f,  1.52f, 1250, null, 0f, Choice(gnome, 2), Choice(sling, 1), Choice(bear, 0.5f)),
                MakeStage("2-4", basic, 8.75f, 1.65f, 1400, null, 0f, Choice(gnome, 1), Choice(bee, 1), Choice(sling, 1), Choice(bear, 0.5f)),
                MakeStage("2-5", basic, 9.5f,  1.80f, 1500, null, 0f, Choice(gnome, 1), Choice(bee, 1), Choice(sling, 1), Choice(bear, 1)),
                MakeStage("2-6", basic, 9.0f,  1.70f, 1650, M(m, "Panda"), 1.20f, Choice(gnome, 2), Choice(bee, 1), Choice(sling, 1), Choice(bear, 1)),
                // Chapter 3: Shore and Canyon
                MakeStage("3-1", basic, 9.0f,  1.72f, 1800, null, 0f, Choice(paddle, 2), Choice(lizard, 1)),
                MakeStage("3-2", basic, 9.25f, 1.78f, 2000, null, 0f, Choice(paddle, 2), Choice(lizard, 1), Choice(harpoon, 1)),
                MakeStage("3-3", basic, 9.5f,  1.85f, 2200, null, 0f, Choice(paddle, 1), Choice(harpoon, 1), Choice(turtle, 0.5f), Choice(lizard, 1)),
                MakeStage("3-4", basic, 9.75f, 1.90f, 2400, null, 0f, Choice(paddle, 1), Choice(harpoon, 1), Choice(bomb, 0.5f), Choice(turtle, 0.5f)),
                MakeStage("3-5", basic, 10.75f,2.08f, 2600, null, 0f, Choice(paddle, 1), Choice(lizard, 1), Choice(harpoon, 1), Choice(bomb, 1), Choice(turtle, 1)),
                MakeStage("3-6", basic, 10.0f, 1.95f, 2900, M(m, "Minotaur"), 1.75f, Choice(paddle, 2), Choice(lizard, 1), Choice(harpoon, 1), Choice(bomb, 1), Choice(turtle, 1)),
                // Chapter 4: Troll Lands
                MakeStage("4-1", basic, 11.0f, 2.10f, 3150, null, 0f, Choice(skull, 2), Choice(bat, 1)),
                MakeStage("4-2", basic, 11.5f, 2.20f, 3450, null, 0f, Choice(skull, 2), Choice(bat, 1), Choice(gnoll, 1)),
                MakeStage("4-3", basic, 12.0f, 2.30f, 3800, null, 0f, Choice(skull, 1), Choice(bat, 1), Choice(imp, 1), Choice(gnoll, 1)),
                MakeStage("4-4", basic, 12.5f, 2.42f, 4150, null, 0f, Choice(skull, 1), Choice(imp, 1), Choice(gnoll, 1), Choice(thief, 0.5f)),
                MakeStage("4-5", basic, 13.25f,2.60f, 4550, null, 0f, Choice(skull, 1), Choice(bat, 1), Choice(imp, 1), Choice(gnoll, 1), Choice(thief, 1)),
                MakeStage("4-6", basic, 11.5f, 2.30f, 5000, M(m, "Troll"), 1.60f, Choice(skull, 2), Choice(bat, 1), Choice(imp, 1), Choice(gnoll, 1), Choice(thief, 1)),
            };

            string path = $"{Root}/Data/StageCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StageCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }
            catalog.stages = stages;
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        static EnemyUnitChoice Choice(UnitData unit, float weight)
        {
            return new EnemyUnitChoice { unit = unit, weight = weight };
        }

        /// <summary>Castle distance from the centre: the map gets wider each chapter.</summary>
        static float ChapterHalfWidth(string name)
        {
            if (name.StartsWith("2-")) return 10.5f;
            if (name.StartsWith("3-")) return 11.5f;
            if (name.StartsWith("4-")) return 12.5f;
            return 9.5f;
        }

        static int ChapterColor(string name)
        {
            return TerrainColorFor(name.Length > 0 ? name[0] - '0' : 1);
        }

        /// <summary>Ground colour per chapter (1 = light green, 3 = deep green, 5 = teal, 4 = dry olive).</summary>
        static int TerrainColorFor(int chapter)
        {
            switch (chapter)
            {
                case 1: return 3; // dark forest: deep green, darkened by the chapter theme
                case 2: return 1; // bamboo hills: bright light green
                case 3: return 5; // shore canyon: teal
                case 4: return 4; // troll lands: dry olive, darkened by the chapter theme
                default: return 1;
            }
        }

        static StageData MakeStage(string name, List<UnitType> available, float enemyIncome, float mult, float enemyCastle,
            UnitData boss, float bossMult, params EnemyUnitChoice[] enemies)
        {
            string path = $"{Root}/Data/Stage_{name}.asset";
            var stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (stage == null)
            {
                stage = ScriptableObject.CreateInstance<StageData>();
                stage.stageName = name;
                stage.availableUnits = new List<UnitType>(available);
                stage.enemyIncome = enemyIncome;
                stage.enemyStatMultiplier = mult;
                stage.enemyCastleHp = enemyCastle;
                AssetDatabase.CreateAsset(stage, path);
            }
            stage.terrainColor = ChapterColor(name);
            stage.mapHalfWidth = ChapterHalfWidth(name);
            stage.enemyCastleHp = enemyCastle; // 600 at 1-1 rising to 5000 at 4-6
            // Balance patch v2: difficulty climbs inside each chapter (x-5 rehearses the boss),
            // then eases at the boss stage, where the boss has its own, smaller multiplier.
            stage.enemyIncome = enemyIncome;
            stage.enemyStatMultiplier = mult;
            stage.bossStatMultiplier = bossMult;
            stage.enemyUnits = enemies.Where(e => e.unit != null).ToList();
            stage.boss = boss;
            stage.bossAtCastleHp = 0f; // boss walks out at the start (slowly)
            stage.bossDelay = 3f;
            // Enemy pacing: ramp income and shift to stronger units over time.
            // Boss stages ramp less (the boss is the late threat); the Troll stage keeps its mix.
            stage.enemyIncomeStart = 0.8f;
            stage.enemyIncomeFullTime = 20f;
            stage.enemyIncomeMaxTime = 90f;
            stage.enemyIncomeMax = name == "4-6" ? 1.2f : boss != null ? 1.3f : 1.8f;
            stage.enemyLateShift = name == "4-6" ? 0f : 0.6f;
            EditorUtility.SetDirty(stage);
            return stage;
        }

        static RuntimeAnimatorController MakeController(string folder, string attackKey, string outPath, out AnimationClip idle)
        {
            idle = null;
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning("[Lane Lords] Folder not found: " + folder);
                return null;
            }

            idle = FindClip(folder, "Idle");
            AnimationClip run = FindClip(folder, "Run");
            AnimationClip attack = FindClip(folder, attackKey);

            AssetDatabase.DeleteAsset(outPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(outPath);
            var sm = controller.layers[0].stateMachine;
            var idleState = sm.AddState("Idle");
            idleState.motion = idle;
            sm.AddState("Run").motion = run != null ? run : idle;
            sm.AddState("Attack").motion = attack != null ? attack : idle;
            sm.defaultState = idleState;
            return controller;
        }

        /// <summary>Shortest clip name containing the key (e.g. "Idle" picks Pawn_Idle_Blue over Pawn_Idle Axe_Blue).</summary>
        static AnimationClip FindClip(string folder, string key)
        {
            return AssetDatabase.FindAssets("t:AnimationClip", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null && c.name.Contains(key))
                .OrderBy(c => c.name.Length)
                .FirstOrDefault();
        }

        static Sprite FirstSprite(AnimationClip clip)
        {
            if (clip == null) return null;
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                if (keys != null && keys.Length > 0 && keys[0].value is Sprite s) return s;
            }
            return null;
        }

        // ======================= World =======================

        const float BaseScale = 1.4f;

        // Bottom row (from the top, in pixels) of the opaque part of each building sprite.
        static readonly Dictionary<string, float> BuildingBottom = new Dictionary<string, float>
        {
            ["House1"] = 173f, ["Monastery"] = 310f, ["Castle"] = 249f, ["Tower"] = 230f,
        };

        const float BaseGroundY = -2.65f; // where the old house base stood on the ground

        /// <summary>
        /// A base with four looks (see CastleTiers): 1 House, 2 Monastery, 3 Castle + Monastery behind,
        /// 4 Castle + Monastery + Towers. The root sits on the ground; parts are placed in pixels like the mock-up.
        /// </summary>
        static Castle MakeTieredCastle(string name, string folder, float x, Team team)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(x, BaseGroundY, 0f);
            var tiers = go.AddComponent<CastleTiers>();

            tiers.tiers[0] = MakeTier(go.transform, "Tier1", folder, 1.4f, new[] { P("House1", 0, 0, 0) });
            tiers.tiers[1] = MakeTier(go.transform, "Tier2", folder, 1.0f, new[] { P("Monastery", 0, 0, 0) });
            tiers.tiers[2] = MakeTier(go.transform, "Tier3", folder, 0.8f, new[] { P("Monastery", 0, 105, 0), P("Castle", 0, 0, 1) });
            tiers.tiers[3] = MakeTier(go.transform, "Tier4", folder, 0.8f, new[]
            {
                P("Monastery", 0, 105, 0), P("Castle", 0, 0, 1), P("Tower", -165, -4, 2), P("Tower", 165, -4, 2),
            });
            // tiers 3 and 4 use the same scale, so the castle itself is the same size and tier 4 just adds the towers
            tiers.halfWidths = new[] { 0.98f, 1.0f, 1.56f, 2.25f };
            tiers.barYs = new[] { 3.85f, 4.55f, 5.0f, 5.0f };

            var castle = go.AddComponent<Castle>();
            castle.team = team;
            castle.halfWidth = tiers.halfWidths[0];
            castle.barY = tiers.barYs[0];
            castle.barScale = 1.4f;
            tiers.Show(0);
            return castle;
        }

        struct Part { public string sprite; public float x, dy; public int order; }
        static Part P(string sprite, float x, float dy, int order) => new Part { sprite = sprite, x = x, dy = dy, order = order };

        static GameObject MakeTier(Transform parent, string name, string folder, float scale, Part[] parts)
        {
            var tier = new GameObject(name);
            tier.transform.SetParent(parent, false);
            foreach (var part in parts)
            {
                Sprite sprite = LoadSprite(folder + part.sprite + ".png");
                var go = MakeSpriteObject(part.sprite, sprite, Vector3.zero);
                go.transform.SetParent(tier.transform, false);
                go.transform.localScale = Vector3.one * scale;
                float h = sprite != null ? sprite.rect.height : 192f;
                float bottom = BuildingBottom.ContainsKey(part.sprite) ? BuildingBottom[part.sprite] : h;
                // sprite pivot is its centre: lift it so its opaque bottom sits dy pixels above the ground
                go.transform.localPosition = new Vector3(part.x * scale / 64f, (part.dy + (bottom - h / 2f)) * scale / 64f, 0f);
                go.GetComponent<SpriteRenderer>().sortingOrder = -500 + part.order;
            }
            return tier;
        }

        static Castle MakeCastle(string name, string spritePath, float x, Team team)
        {
            Sprite sprite = LoadSprite(spritePath);
            var go = MakeSpriteObject(name, sprite, Vector3.zero);
            go.transform.localScale = Vector3.one * BaseScale;
            go.GetComponent<SpriteRenderer>().sortingOrder = -500;

            // Sit the building's bottom just behind the lane.
            float halfHeight = sprite != null ? sprite.bounds.extents.y * BaseScale : 2f;
            float halfWidth = sprite != null ? sprite.bounds.extents.x * BaseScale : 2f;
            // Lowered just enough that the building's bottom stays above the unit buttons
            // (buttons cover the bottom ~18.5% of the screen, i.e. up to about y = -2.86).
            const float lowerBy = 0.65f;
            go.transform.position = new Vector3(x, LaneY - 0.4f + halfHeight * 0.85f - lowerBy, 0f);

            var castle = go.AddComponent<Castle>();
            castle.team = team;
            castle.halfWidth = halfWidth * 0.8f;
            castle.barY = halfHeight + 0.35f;
            castle.barScale = 1.4f;
            return castle;
        }

        /// <summary>Water everywhere, with a big flat grass island for the battlefield (Tiny Swords tiles).</summary>
        static void BuildTerrain()
        {
            string dir = TS + "/Terrain/Tileset/Tilemap Settings/";
            var waterTile = AssetDatabase.LoadAssetAtPath<TileBase>(dir + "Water Background color.asset");

            var gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>();
            gridGo.transform.position = new Vector3(0f, -0.4f, 0f); // land top lands exactly on the screen middle (camera y 0.6)
            var water = MakeTilemap(gridGo.transform, "Water", -1001);
            var ground = MakeTilemap(gridGo.transform, "Ground", -1000);

            if (waterTile != null)
            {
                for (int x = -30; x < 30; x++)
                    for (int y = -10; y < 10; y++)
                        water.SetTile(new Vector3Int(x, y, 0), waterTile);
            }
            else Debug.LogWarning("[Lane Lords] Water tile not found");

            // Lets each stage recolour the ground (Tilemap_color1..5) at runtime.
            var theme = gridGo.AddComponent<TerrainTheme>();
            theme.ground = ground;
            theme.tiles = new TileBase[5 * TerrainTheme.TilesPerColor];
            for (int c = 1; c <= 5; c++)
                for (int i = 0; i < TerrainTheme.TilesPerColor; i++)
                    theme.tiles[(c - 1) * TerrainTheme.TilesPerColor + i] =
                        AssetDatabase.LoadAssetAtPath<TileBase>(dir + $"Sliced Tiles/Tilemap_color{c}_{i}.asset");

            // Flat grass ground (nine-slice from Tilemap_color1, columns 0-2):
            //  0  1  2   top-left, top, top-right
            //  8  9 10   left, centre, right
            // 16 17 18   bottom-left, bottom, bottom-right
            // Land covers the bottom ~60% of the screen (top edge at y = 2); water above it.
            // Land = bottom half of the screen: rows -8..0 (+ grid offset -0.4 -> top edge at y 0.6)
            FillNineSlice(ground, dir, -20, 19, -8, 0, 0, 1, 2, 8, 9, 10, 16, 17, 18); // wide enough for chapter 4
        }

        static void FillNineSlice(Tilemap map, string dir, int xMin, int xMax, int yMin, int yMax,
            int tl, int t, int tr, int l, int c, int r, int bl, int b, int br)
        {
            var grid = new[,] { { tl, t, tr }, { l, c, r }, { bl, b, br } };
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    int col = x == xMin ? 0 : (x == xMax ? 2 : 1);
                    int row = y == yMax ? 0 : (y == yMin ? 2 : 1);
                    SetTileIndex(map, dir, x, y, grid[row, col]);
                }
            }
        }

        static readonly Dictionary<int, TileBase> tileCache = new Dictionary<int, TileBase>();

        static void SetTileIndex(Tilemap map, string dir, int x, int y, int index)
        {
            if (!tileCache.TryGetValue(index, out TileBase tile) || tile == null)
            {
                tile = AssetDatabase.LoadAssetAtPath<TileBase>(dir + $"Sliced Tiles/Tilemap_color1_{index}.asset");
                tileCache[index] = tile;
                if (tile == null) Debug.LogWarning("[Lane Lords] Missing tile Tilemap_color1_" + index);
            }
            if (tile != null) map.SetTile(new Vector3Int(x, y, 0), tile);
        }

        static Tilemap MakeTilemap(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        static void Decorate()
        {
            var parent = new GameObject("Decorations").transform;
            var bushes = SpritesIn(TS + "/Terrain/Decorations/Bushes");
            var rocks = SpritesIn(TS + "/Terrain/Decorations/Rocks");
            var spots = new[]
            {
                // near the shore, behind the lane
                new Vector3(-3.2f, -0.35f), new Vector3(2f, -0.2f), new Vector3(5.5f, -0.4f),
                // in front of the lane
                new Vector3(-5f, -3.3f), new Vector3(3.5f, -3.4f)
            };
            var waterRocks = SpritesIn(TS + "/Terrain/Decorations/Rocks in the Water");
            var seaSpots = new[] { new Vector3(-8f, 3.6f), new Vector3(-2.5f, 4.6f), new Vector3(3f, 3.4f), new Vector3(8.5f, 4.4f), new Vector3(-5f, 1.9f), new Vector3(6f, 1.7f) };
            for (int i = 0; i < seaSpots.Length && waterRocks.Count > 0; i++)
            {
                var rock = MakeSpriteObject("Sea Rock", waterRocks[i % waterRocks.Count], seaSpots[i]);
                rock.transform.SetParent(parent, true);
                rock.GetComponent<SpriteRenderer>().sortingOrder = -990;
            }

            for (int i = 0; i < spots.Length; i++)
            {
                var list = i % 2 == 0 ? bushes : rocks;
                if (list.Count == 0) continue;
                var go = MakeSpriteObject("Deco", list[i % list.Count], spots[i]);
                go.transform.SetParent(parent, true);
                // behind the lane: draw behind the castles (-500); in front of the lane: normal depth
                go.GetComponent<SpriteRenderer>().sortingOrder = spots[i].y > 0f ? -600 - i : Mathf.RoundToInt(-spots[i].y * 100f) - 50;
            }
        }

        // ======================= Chapter themes =======================

        const string PropArt = Root + "/Art/Props/";
        const string SkyArt = Root + "/Art/Sky/";
        static Sprite softCircle;

        [MenuItem("Tools/Lane Lords/Preview Chapter 1")] static void Preview1() { PreviewChapter(1); }
        [MenuItem("Tools/Lane Lords/Preview Chapter 2")] static void Preview2() { PreviewChapter(2); }
        [MenuItem("Tools/Lane Lords/Preview Chapter 3")] static void Preview3() { PreviewChapter(3); }
        [MenuItem("Tools/Lane Lords/Preview Chapter 4")] static void Preview4() { PreviewChapter(4); }

        /// <summary>Shows a chapter's battlefield in the open Battle scene without playing (motes stay still).</summary>
        static void PreviewChapter(int chapter)
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Lane Lords", "플레이를 멈춘 뒤에 다시 실행해 주세요.", "OK");
                return;
            }
            var theme = Object.FindAnyObjectByType<ChapterTheme>();
            var terrain = Object.FindAnyObjectByType<TerrainTheme>();
            if (theme == null)
            {
                EditorUtility.DisplayDialog("Lane Lords", "Battle 씬에서 실행해 주세요. (없으면 먼저 Build Battle Scene)", "OK");
                return;
            }
            theme.Apply(chapter);
            if (terrain != null) terrain.Recolor(TerrainColorFor(chapter));
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        static Sprite PrepareProp(string path, bool bottomPivot)
        {
            if (!System.IO.File.Exists(path)) { Debug.LogWarning("[Lane Lords] Missing prop: " + path); return null; }
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                SpriteAlignment want = bottomPivot ? SpriteAlignment.BottomCenter : SpriteAlignment.Center;
                if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
                    || !Mathf.Approximately(importer.spritePixelsPerUnit, 64f) || (SpriteAlignment)settings.spriteAlignment != want)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 64f;
                    importer.mipmapEnabled = false;
                    importer.filterMode = bottomPivot ? FilterMode.Point : FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)want;
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite[] Frames(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sp => FrameIndex(sp.name)).ToArray();
        }

        /// <summary>A scenery sprite whose opaque bottom stands at footY (sorted by depth unless an order is given).</summary>
        static GameObject ThemeProp(Transform parent, string name, Sprite sprite, float x, float footY, float scale, Color tint,
            int order = int.MinValue, bool flip = false)
        {
            var go = MakeSpriteObject(name, sprite, Vector3.zero);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;
            float minY = sprite != null ? sprite.bounds.min.y : 0f;
            go.transform.position = new Vector3(x, footY - minY * scale, 0f);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = tint;
            sr.flipX = flip;
            sr.sortingOrder = order != int.MinValue ? order : Mathf.RoundToInt(-footY * 100f);
            return go;
        }

        static GameObject ThemeAnim(Transform parent, string name, Sprite[] frames, float x, float footY, float scale, Color tint,
            float fps, int order = int.MinValue, bool flip = false, float pauseMin = 0f, float pauseMax = 0f, bool hide = false)
        {
            if (frames == null || frames.Length == 0) return null;
            // start on a middle frame: the first frame of effects like fire is almost empty
            var go = ThemeProp(parent, name, frames[frames.Length / 2], x, footY, scale, tint, order, flip);
            var fb = go.AddComponent<SpriteFlipbook>();
            fb.frames = frames;
            fb.fps = fps;
            fb.pause = new Vector2(pauseMin, pauseMax);
            fb.hideWhileWaiting = hide;
            return go;
        }

        static AmbientMotes ThemeMotes(Transform parent, string name, AmbientMotes.Kind kind, int count, Vector2 min, Vector2 max,
            Color color, Vector2 size, int order, Vector2 velocity, float maxAlpha, Sprite[] sprites = null, int seed = 1, Vector2[] spots = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var motes = go.AddComponent<AmbientMotes>();
            motes.kind = kind;
            motes.areaMin = min;
            motes.areaMax = max;
            motes.velocity = velocity;
            motes.maxAlpha = maxAlpha;
            var rnd = new System.Random(seed);
            int n = spots != null ? spots.Length : count;
            for (int i = 0; i < n; i++)
            {
                Sprite sp = sprites != null && sprites.Length > 0 ? sprites[i % sprites.Length] : softCircle;
                Vector3 pos = spots != null ? (Vector3)spots[i]
                    : new Vector3(Mathf.Lerp(min.x, max.x, (float)rnd.NextDouble()), Mathf.Lerp(min.y, max.y, (float)rnd.NextDouble()), 0f);
                var m = MakeSpriteObject("Mote", sp, pos);
                m.transform.SetParent(go.transform, true);
                m.transform.localScale = new Vector3(size.x, size.y, 1f);
                var sr = m.GetComponent<SpriteRenderer>();
                sr.color = new Color(color.r, color.g, color.b, maxAlpha * 0.8f);
                sr.sortingOrder = order;
            }
            return motes;
        }

        static void BuildChapterThemes(Transform forest, GameObject camGo)
        {
            softCircle = PrepareProp(PropArt + "soft_circle.png", false);
            Sprite vignetteSprite = PrepareProp(PropArt + "vignette.png", false);
            var bamboo = new[] { PrepareProp(PropArt + "bamboo_1.png", true), PrepareProp(PropArt + "bamboo_2.png", true), PrepareProp(PropArt + "bamboo_3.png", true) };

            string trees = TS + "/Pawn and Resources/Wood/Trees/";
            Sprite[] pine1 = Frames(trees + "Tree1.png"), pine2 = Frames(trees + "Tree2.png");
            Sprite[] round3 = Frames(trees + "Tree3.png"), round4 = Frames(trees + "Tree4.png");
            Sprite[] bush = Frames(TS + "/Terrain/Decorations/Bushes/Bush 1.png");
            Sprite[] bush2 = Frames(TS + "/Terrain/Decorations/Bushes/Bush 2.png");
            Sprite[] sheep = Frames(TS + "/Pawn and Resources/Meat/Sheep/Sheep_Grass.png");
            Sprite[] splash = Frames(TS + "/Particle FX/Water Splash.png");
            Sprite[] duck = Frames(TS + "/Terrain/Decorations/Rubber Duck/Rubber duck.png");
            Sprite[] fire1 = Frames(TS + "/Particle FX/Fire_01.png"), fire2 = Frames(TS + "/Particle FX/Fire_02.png"), fire3 = Frames(TS + "/Particle FX/Fire_03.png");
            var clouds = new List<Sprite>();
            foreach (var n in new[] { "01", "02", "03", "05" })
            {
                var c = LoadSprite(TS + $"/Terrain/Decorations/Clouds/Clouds_{n}.png");
                if (c != null) clouds.Add(c);
            }
            string black = TS + "/Buildings/Black Buildings/";

            var root = new GameObject("Chapter Themes");
            var theme = root.AddComponent<ChapterTheme>();
            var grid = GameObject.Find("Grid");
            if (grid != null)
            {
                var g = grid.transform.Find("Ground");
                var w = grid.transform.Find("Water");
                if (g != null) theme.ground = g.GetComponent<Tilemap>();
                if (w != null) theme.water = w.GetComponent<Tilemap>();
            }
            var deco = GameObject.Find("Decorations");
            theme.sharedProps = deco != null ? new[] { forest, deco.transform } : new[] { forest };

            // Vignette follows the camera; it sits under the castles and scenery, so it only darkens ground and water.
            var vig = MakeSpriteObject("Vignette", vignetteSprite, Vector3.zero);
            vig.transform.SetParent(camGo.transform, false);
            vig.transform.localPosition = new Vector3(0f, 0f, 10f);
            vig.transform.localScale = new Vector3(5.2f, 4.2f, 1f);
            theme.vignette = vig.GetComponent<SpriteRenderer>();
            theme.vignette.sortingOrder = -510;

            theme.looks = new ChapterLook[4];
            for (int i = 0; i < 4; i++)
            {
                var set = new GameObject($"Chapter {i + 1}");
                set.transform.SetParent(root.transform, false);
                theme.looks[i] = new ChapterLook { set = set };
            }

            // Layout: the chapter's sky picture covers everything above the shore (y 0.6), following the camera at half speed.
            // A horizon tree line sits on the shore in front of it but behind the castles (order <= -500), across every map width.
            // The lane is at y = -1.7, and the unit buttons cover everything below y = -2.9.
            const int SkyOrder = -985, FarRow = -720, NearRow = -700;
            var skies = new Sprite[4];
            for (int i = 0; i < 4; i++) skies[i] = PrepareProp(SkyArt + $"sky_ch{i + 1}.png", true);
            for (int i = 0; i < 4; i++)
            {
                if (skies[i] == null) continue;
                var sky = ThemeProp(theme.looks[i].set.transform, "Sky", skies[i], 0f, 0.6f, 1f, Color.white, SkyOrder);
                sky.AddComponent<SkyParallax>().factor = 0.5f;
            }

            // ---------- Chapter 1: Dark Forest (night sky, two dark pine rows, purple mist, fireflies, eyes in the bushes) ----------
            {
                var L = theme.looks[0];
                Transform t = L.set.transform;
                L.groundTint = new Color(0.60f, 0.70f, 0.72f);
                L.waterTint = new Color(0.42f, 0.50f, 0.68f);
                L.propTint = new Color(0.55f, 0.66f, 0.70f);
                L.vignette = new Color(0.06f, 0.02f, 0.14f, 0.6f);
                Color pineTint = new Color(0.50f, 0.62f, 0.68f);
                Color farTint = new Color(0.38f, 0.48f, 0.58f);
                var rnd = new System.Random(7);
                for (float x = -17f; x <= 17f; x += 0.7f)
                    ThemeAnim(t, "Far Pine", rnd.Next(2) == 0 ? pine1 : pine2, x + (float)(rnd.NextDouble() * 0.4 - 0.2), 0.62f,
                        0.62f + (float)rnd.NextDouble() * 0.08f, farTint, 7f, FarRow);
                for (float x = -17f; x <= 17f; x += 1.0f)
                    ThemeAnim(t, "Pine", rnd.Next(2) == 0 ? pine1 : pine2, x + (float)(rnd.NextDouble() * 0.6 - 0.3), 0.35f,
                        0.72f + (float)rnd.NextDouble() * 0.08f, pineTint, 8f, NearRow);
                Vector2[] bushSpots = { new Vector2(-2.4f, -0.1f), new Vector2(1.3f, -0.15f), new Vector2(4.4f, -0.05f) };
                var eyes = new List<Vector2>();
                for (int i = 0; i < bushSpots.Length; i++)
                {
                    ThemeAnim(t, "Dark Bush", i % 2 == 0 ? bush : bush2, bushSpots[i].x, bushSpots[i].y, 0.8f, new Color(0.40f, 0.50f, 0.55f), 6f);
                    float cy = bushSpots[i].y + 0.45f;
                    eyes.Add(new Vector2(bushSpots[i].x - 0.12f, cy));
                    eyes.Add(new Vector2(bushSpots[i].x + 0.12f, cy));
                }
                ThemeMotes(t, "Eyes", AmbientMotes.Kind.Eyes, 0, Vector2.zero, Vector2.zero, new Color(1f, 0.92f, 0.35f),
                    new Vector2(0.13f, 0.1f), 10, Vector2.zero, 1f, spots: eyes.ToArray());
                ThemeMotes(t, "Mist (back)", AmbientMotes.Kind.Drift, 8, new Vector2(-17f, -0.4f), new Vector2(17f, 0.5f),
                    new Color(0.62f, 0.48f, 0.82f), new Vector2(5f, 1.3f), 40, new Vector2(0.18f, 0f), 0.42f, seed: 3);
                ThemeMotes(t, "Mist (lane)", AmbientMotes.Kind.Drift, 7, new Vector2(-17f, -2.9f), new Vector2(17f, -2.1f),
                    new Color(0.62f, 0.48f, 0.82f), new Vector2(5f, 1.0f), 40, new Vector2(-0.12f, 0f), 0.32f, seed: 4);
                ThemeMotes(t, "Fireflies", AmbientMotes.Kind.Firefly, 16, new Vector2(-9f, -2.8f), new Vector2(9f, 1.6f),
                    new Color(0.85f, 1f, 0.45f), new Vector2(0.16f, 0.16f), 60, Vector2.zero, 0.95f, seed: 5);
            }

            // ---------- Chapter 2: Bamboo Hills (clear sky, swaying bamboo line with autumn trees, sheep, cloud shadows) ----------
            {
                var L = theme.looks[1];
                Transform t = L.set.transform;
                var rnd = new System.Random(11);
                for (float x = -17f; x <= 17f; x += 0.75f)
                {
                    float jx = x + (float)(rnd.NextDouble() * 0.3 - 0.15);
                    if (rnd.NextDouble() < 0.12)
                    {
                        ThemeAnim(t, "Autumn Tree", rnd.Next(2) == 0 ? round3 : round4, jx, 0.5f, 0.68f, Color.white, 8f, NearRow + 5);
                        continue;
                    }
                    float y = 0.55f - (float)rnd.NextDouble() * 0.1f;
                    var b = ThemeProp(t, "Bamboo", bamboo[rnd.Next(bamboo.Length)], jx, y, 0.5f + (float)rnd.NextDouble() * 0.1f,
                        Color.white, NearRow + Mathf.RoundToInt((0.6f - y) * 100f), rnd.Next(2) == 0);
                    var sway = b.AddComponent<PropSway>();
                    sway.angle = 1.8f;
                    sway.speed = 0.8f + (float)rnd.NextDouble() * 0.4f;
                }
                ThemeAnim(t, "Sheep", sheep, -1.7f, -0.1f, 0.75f, Color.white, 8f, int.MinValue, false, 1f, 4f);
                ThemeAnim(t, "Sheep", sheep, 2.7f, 0.0f, 0.75f, Color.white, 8f, int.MinValue, true, 1f, 4f);
                ThemeAnim(t, "Sheep", sheep, 3.4f, -0.25f, 0.7f, Color.white, 8f, int.MinValue, false, 2f, 5f);
                ThemeMotes(t, "Clouds", AmbientMotes.Kind.Drift, 3, new Vector2(-18f, 3.6f), new Vector2(18f, 4.8f),
                    Color.white, new Vector2(0.5f, 0.5f), -980, new Vector2(0.12f, 0f), 1f, clouds.ToArray(), seed: 12);
                ThemeMotes(t, "Cloud Shadows", AmbientMotes.Kind.Drift, 4, new Vector2(-18f, -2.6f), new Vector2(18f, 0.2f),
                    new Color(0.1f, 0.2f, 0.1f), new Vector2(7f, 2.6f), -505, new Vector2(0.15f, 0f), 0.10f, seed: 13);
            }

            // ---------- Chapter 3: Shore Canyon (sea and cliffs in the sky picture, tree line with two gaps onto the sea) ----------
            {
                var L = theme.looks[2];
                Transform t = L.set.transform;
                L.waterTint = new Color(0.62f, 0.82f, 1f);
                var rnd = new System.Random(31);
                Vector2[] gaps = { new Vector2(-6.5f, -3.5f), new Vector2(0.5f, 3.6f) };
                Sprite[][] mix = { pine1, pine2, round3, round4 };
                for (float x = -17f; x <= 17f; x += 0.85f)
                {
                    bool inGap = false;
                    foreach (var g in gaps) if (x > g.x && x < g.y) inGap = true;
                    if (inGap) continue;
                    ThemeAnim(t, "Shore Tree", mix[rnd.Next(mix.Length)], x + (float)(rnd.NextDouble() * 0.4 - 0.2),
                        0.45f + (float)rnd.NextDouble() * 0.12f, 0.7f, Color.white, 8f, NearRow);
                }
                Sprite rock1 = LoadSprite(TS + "/Terrain/Decorations/Rocks/Rock1.png"), rock3 = LoadSprite(TS + "/Terrain/Decorations/Rocks/Rock3.png");
                foreach (var g in gaps)
                {
                    ThemeProp(t, "Shore Rock", rock1, g.x + 0.6f, 0.45f, 1f, Color.white, NearRow + 10);
                    ThemeProp(t, "Shore Rock", rock3, g.y - 0.7f, 0.5f, 1f, Color.white, NearRow + 10);
                    ThemeAnim(t, "Splash", splash, (g.x + g.y) / 2f - 0.6f, 0.75f, 0.55f, Color.white, 12f, SkyOrder + 5, false, 2f, 6f, true);
                    ThemeAnim(t, "Splash", splash, (g.x + g.y) / 2f + 0.7f, 0.85f, 0.5f, Color.white, 12f, SkyOrder + 5, false, 3f, 7f, true);
                }
                var d = ThemeAnim(t, "Rubber Duck", duck, 2.4f, 0.95f, 0.9f, Color.white, 4f, SkyOrder + 6);
                if (d != null) { var bob = d.AddComponent<PropSway>(); bob.angle = 4f; bob.bob = 0.04f; bob.speed = 1.5f; }
            }

            // ---------- Chapter 4: Troll Lands (red dusk with the troll castle, dead trees, burning ruins, embers) ----------
            {
                var L = theme.looks[3];
                Transform t = L.set.transform;
                L.groundTint = new Color(0.72f, 0.66f, 0.70f);
                L.waterTint = new Color(0.40f, 0.33f, 0.46f);
                L.propTint = new Color(0.52f, 0.46f, 0.50f);
                L.vignette = new Color(0.14f, 0.02f, 0.04f, 0.6f);
                Color ruinTint = new Color(0.62f, 0.56f, 0.64f);
                Color deadTint = new Color(0.40f, 0.36f, 0.40f);
                var rnd = new System.Random(21);
                var deadTrees = new[] { PrepareProp(PropArt + "dead_tree_1.png", true), PrepareProp(PropArt + "dead_tree_2.png", true) };
                for (float x = -17f; x <= 17f; x += 1.2f)
                    ThemeProp(t, "Dead Tree", deadTrees[rnd.Next(2)], x + (float)(rnd.NextDouble() * 0.6 - 0.3), 0.5f + (float)rnd.NextDouble() * 0.1f,
                        0.66f + (float)rnd.NextDouble() * 0.08f, new Color(0.85f, 0.80f, 0.84f), FarRow);
                ThemeProp(t, "Ruin", LoadSprite(black + "House1.png"), -2.7f, 0.3f, 0.75f, ruinTint, NearRow);
                ThemeProp(t, "Ruin", LoadSprite(black + "House3.png"), 3.2f, 0.3f, 0.75f, ruinTint, NearRow);
                ThemeProp(t, "Ruin", LoadSprite(black + "House2.png"), 6.0f, 0.35f, 0.68f, ruinTint, NearRow, true);
                ThemeAnim(t, "Fire", fire1, -2.9f, 1.0f, 1.3f, Color.white, 10f, NearRow + 10);
                ThemeAnim(t, "Fire", fire2, 3.4f, 0.95f, 1.3f, Color.white, 10f, NearRow + 10);
                ThemeAnim(t, "Fire", fire3, 5.8f, 1.0f, 1.1f, Color.white, 10f, NearRow + 10);
                ThemeAnim(t, "Fire", fire1, 1.9f, -0.05f, 1.2f, Color.white, 10f);
                for (int i = 1; i <= 3; i++)
                    ThemeProp(t, "Stump", LoadSprite(trees + $"Stump {i}.png"), -3.4f + i * 2.6f, -0.2f + (i % 2) * 0.12f, 0.7f, deadTint);
                ThemeMotes(t, "Haze", AmbientMotes.Kind.Drift, 7, new Vector2(-17f, -0.3f), new Vector2(17f, 1.2f),
                    new Color(0.22f, 0.18f, 0.24f), new Vector2(5f, 1.5f), 40, new Vector2(0.1f, 0f), 0.30f, seed: 22);
                ThemeMotes(t, "Embers", AmbientMotes.Kind.Ember, 20, new Vector2(-9f, -2.8f), new Vector2(9f, 4f),
                    new Color(1f, 0.55f, 0.18f), new Vector2(0.1f, 0.1f), 60, new Vector2(0f, 0.6f), 0.9f, seed: 23);
            }

            theme.Apply(1);
            var terrain = Object.FindAnyObjectByType<TerrainTheme>();
            if (terrain != null) terrain.Recolor(TerrainColorFor(1));
        }

        static GameObject MakeSpriteObject(string name, Sprite sprite, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            return go;
        }

        // ======================= UI =======================

        static void BuildUI(BattleManager bm, Spawner spawner)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform root = canvasGo.transform;
            AddSkin(canvasGo);

            var ui = canvasGo.AddComponent<BattleUI>();

            // Top bar
            ui.goldText = MakeText(root, "Gold", "Gold", 40, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1000f, 60f));
            ui.playerHpFill = MakeHpBar(root, "Player HP", new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(0f, 1f),
                new Color(0.3f, 0.6f, 1f), Image.OriginHorizontal.Left, out ui.playerHpText);
            ui.enemyHpFill = MakeHpBar(root, "Enemy HP", new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(1f, 1f),
                new Color(1f, 0.35f, 0.35f), Image.OriginHorizontal.Right, out ui.enemyHpText);

            // Speed button
            var speedBtn = MakeButton(root, "Speed", new Vector2(1f, 1f), new Vector2(-40f, -120f), new Vector2(1f, 1f), new Vector2(120f, 70f),
                new Color(0.95f, 0.85f, 0.55f));
            ui.speedText = MakeText(speedBtn.transform, "Label", "x1", 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 70f));
            UnityEventTools.AddPersistentListener(speedBtn.onClick, new UnityAction(bm.ToggleSpeed));

            // Pause MENU, top-right (the speed button sits below it)
            Sprite pBlue = LoadSprite(TS + "/UI Elements/Buttons/BigBlueButton_Regular.png");
            Sprite pRed = LoadSprite(TS + "/UI Elements/Buttons/BigRedButton_Regular.png");
            var pause = canvasGo.AddComponent<PauseMenuUI>();
            var pauseBtn = MakeButton(root, "Menu Button", new Vector2(1f, 1f), new Vector2(-30f, -25f), new Vector2(1f, 1f),
                new Vector2(170f, 90f), Color.white);
            pauseBtn.GetComponent<Image>().sprite = pBlue;
            pauseBtn.GetComponent<Image>().pixelsPerUnitMultiplier = 2.5f;
            MakeText(pauseBtn.transform, "Label", "MENU", 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(170f, 90f));
            UnityEventTools.AddPersistentListener(pauseBtn.onClick, new UnityAction(pause.Open));

            var pausePanel = UIObject("Pause Panel", root);
            var ppRt = (RectTransform)pausePanel.transform;
            ppRt.anchorMin = Vector2.zero;
            ppRt.anchorMax = Vector2.one;
            ppRt.offsetMin = Vector2.zero;
            ppRt.offsetMax = Vector2.zero;
            pausePanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var pausePaper = UIObject("Paper", pausePanel.transform);
            SetRect(pausePaper, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(0.5f, 0.5f), new Vector2(620f, 720f));
            var pausePaperImg = pausePaper.AddComponent<Image>();
            pausePaperImg.sprite = LoadSprite(TS + "/UI Elements/Papers/RegularPaper.png");
            pausePaperImg.type = Image.Type.Sliced;
            pausePaperImg.pixelsPerUnitMultiplier = 1.2f;

            var pauseRibbon = UIObject("Ribbon", pausePanel.transform);
            SetRect(pauseRibbon, new Vector2(0.5f, 0.5f), new Vector2(0f, 350f), new Vector2(0.5f, 0.5f), new Vector2(680f, 190f));
            var pauseRibbonImg = pauseRibbon.AddComponent<Image>();
            pauseRibbonImg.sprite = LoadSprite(TS + "/UI Elements/Ribbons/BigRibbons 5.png");
            pauseRibbonImg.type = Image.Type.Sliced;
            MakeText(pauseRibbon.transform, "Title", "PAUSED", 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(560f, 110f));

            // In-battle shop (opened from the pause menu; the game stays paused while it is open)
            var battleShop = canvasGo.AddComponent<UpgradeShopUI>();

            AddMenuButton(pausePaper.transform, "Resume", "RESUME", 185f, pBlue, new UnityAction(pause.Resume), 380f);
            AddMenuButton(pausePaper.transform, "Shop", "SHOP", 95f, LoadSprite(TS + "/UI Elements/Buttons/BigRedButton_Regular.png"), new UnityAction(battleShop.Open), 380f);
            AddMenuButton(pausePaper.transform, "Restart", "RESTART", 5f, pBlue, new UnityAction(pause.Restart), 380f);
            AddMenuButton(pausePaper.transform, "Stages", "STAGES", -85f, pBlue, new UnityAction(pause.BackToStages), 380f);
            pause.musicLabel = AddMenuButton(pausePaper.transform, "Music", "Music: 60%", -175f, pRed, new UnityAction(pause.CycleMusic), 380f);
            pause.sfxLabel = AddMenuButton(pausePaper.transform, "Effects", "Effects: 80%", -265f, pRed, new UnityAction(pause.CycleSfx), 380f);
            pause.panel = pausePanel;
            pausePanel.SetActive(false);

            BuildShopPanel(battleShop, root, pBlue, LoadSprite(TS + "/UI Elements/Buttons/BigRedButton_Regular.png"));
            // Gems shown inside the shop window in battle (the stage select screen has its own counter)
            battleShop.gemsText = MakeText(battleShop.panel.transform.Find("Paper"), "Gems", "Gems 0", 36, TextAnchor.MiddleRight,
                new Vector2(0.5f, 0.5f), new Vector2(600f, 300f), new Vector2(360f, 60f));
            battleShop.gemsText.color = InkColor;
            battleShop.gemsText.GetComponent<Outline>().enabled = false;

            // Unit buttons
            int n = spawner.playerUnits.Length;
            ui.unitButtons = new Button[n];
            ui.cooldownOverlays = new Image[n];
            const float size = 170f, gap = 20f;
            float startX = -(n - 1) * (size + gap) / 2f;
            for (int i = 0; i < n; i++)
            {
                UnitData d = spawner.playerUnits[i];
                var btn = MakeButton(root, "Btn_" + d.displayName, new Vector2(0.5f, 0f), new Vector2(startX + i * (size + gap), 30f),
                    new Vector2(0.5f, 0f), new Vector2(size, size), Color.white);
                // Parchment card; BattleUI fades it out when the unit can't be summoned
                var cardImg = btn.GetComponent<Image>();
                cardImg.sprite = LoadSprite(TS + "/UI Elements/Papers/SpecialPaper.png");
                cardImg.type = Image.Type.Sliced;
                cardImg.pixelsPerUnitMultiplier = 1.6f;
                btn.transition = Selectable.Transition.None;
                btn.gameObject.AddComponent<CanvasGroup>();

                var icon = UIObject("Icon", btn.transform);
                SetRect(icon, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(0.5f, 0.5f), new Vector2(200f, 200f));
                var iconImg = icon.AddComponent<Image>();
                iconImg.sprite = d.icon;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                MakeText(btn.transform, "Cost", d.cost.ToString(), 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(size, 40f));
                MakeText(btn.transform, "Name", d.displayName, 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(size, 30f));

                var overlay = UIObject("Cooldown", btn.transform);
                SetRect(overlay, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(size, size));
                var ov = overlay.AddComponent<Image>();
                ov.sprite = uiSprite;
                ov.color = new Color(0.25f, 0.15f, 0.05f, 0.35f); // brownish wash over the parchment while recharging
                ov.type = Image.Type.Filled;
                ov.fillMethod = Image.FillMethod.Vertical;
                ov.fillOrigin = (int)Image.OriginVertical.Top;
                ov.fillAmount = 0f;
                ov.raycastTarget = false;

                UnityEventTools.AddIntPersistentListener(btn.onClick, new UnityAction<int>(spawner.SpawnByIndex), i);
                ui.unitButtons[i] = btn;
                ui.cooldownOverlays[i] = ov;
            }

            BuildTutorial(canvasGo, root, ui);

            // Stage label on a small scroll, top-left
            var stagePanel = UIObject("Stage Label", root);
            SetRect(stagePanel, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(0f, 1f), new Vector2(330f, 140f));
            var stageImg = stagePanel.AddComponent<Image>();
            stageImg.sprite = LoadSprite(TS + "/UI Elements/Banners/Banner.png");
            stageImg.type = Image.Type.Sliced;
            stageImg.pixelsPerUnitMultiplier = 3f;
            stageImg.raycastTarget = false;
            ui.stageText = MakeText(stagePanel.transform, "Label", "STAGE 1-1", 40, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(8f, 8f), new Vector2(300f, 60f));
            ui.stageText.color = InkColor;
            ui.stageText.GetComponent<Outline>().enabled = false;

            // Result panel: dark overlay, paper with details, team-coloured ribbon with the title
            var panel = UIObject("Result Panel", root);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var paper = UIObject("Paper", panel.transform);
            SetRect(paper, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(0.5f, 0.5f), new Vector2(860f, 460f));
            var paperImg = paper.AddComponent<Image>();
            paperImg.sprite = LoadSprite(TS + "/UI Elements/Papers/RegularPaper.png");
            paperImg.type = Image.Type.Sliced;
            paperImg.pixelsPerUnitMultiplier = 1.2f;

            bm.resultText = MakeText(paper.transform, "Details", "Stars 3 / 3", 46, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(760f, 160f));
            bm.resultText.color = InkColor;
            bm.resultText.GetComponent<Outline>().enabled = false;

            bm.victoryRibbon = LoadSprite(TS + "/UI Elements/Ribbons/BigRibbons 1.png"); // blue
            bm.defeatRibbon = LoadSprite(TS + "/UI Elements/Ribbons/BigRibbons 2.png");  // red
            var ribbon = UIObject("Ribbon", panel.transform);
            SetRect(ribbon, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(0.5f, 0.5f), new Vector2(780f, 210f));
            bm.resultRibbon = ribbon.AddComponent<Image>();
            bm.resultRibbon.sprite = bm.victoryRibbon;
            bm.resultRibbon.type = Image.Type.Sliced;
            bm.resultRibbon.pixelsPerUnitMultiplier = 1f;
            bm.resultTitle = MakeText(ribbon.transform, "Title", "VICTORY", 72, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(600f, 120f));

            Sprite blueButton = LoadSprite(TS + "/UI Elements/Buttons/BigBlueButton_Regular.png");
            Sprite redButton = LoadSprite(TS + "/UI Elements/Buttons/BigRedButton_Regular.png");
            ResultButton(paper.transform, "Retry", "RETRY", -285f, blueButton, new UnityAction(bm.Restart));
            ResultButton(paper.transform, "Stages", "STAGES", -95f, blueButton, new UnityAction(bm.BackToStageSelect));
            ResultButton(paper.transform, "Shop", "SHOP", 95f, redButton, new UnityAction(bm.GoToShop));
            bm.nextStageButton = ResultButton(paper.transform, "Next", "NEXT", 285f, blueButton, new UnityAction(bm.NextStage)).gameObject;

            bm.resultNotice = MakeText(paper.transform, "Notice", "", 30, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(800f, 50f));
            bm.resultNotice.color = new Color32(255, 214, 90, 255);
            bm.resultPanel = panel;
            panel.SetActive(false);

            // The pause overlay must draw over (and block) everything else, including the unit buttons.
            pausePanel.transform.SetAsLastSibling();

            BuildStoryDialog(canvasGo, root);
        }

        const string StoryFontPath = Root + "/Fonts/Galmuri11.ttf";

        /// <summary>Story dialogue overlay: big pixel portraits, name ribbon, typewriter text (Galmuri pixel font).</summary>
        static void BuildStoryDialog(GameObject canvasGo, Transform root)
        {
            var story = canvasGo.AddComponent<StoryDialogUI>();
            Font storyFont = AssetDatabase.LoadAssetAtPath<Font>(StoryFontPath);
            if (storyFont == null) { Debug.LogWarning("[Lane Lords] Story font missing: " + StoryFontPath); storyFont = uiFont; }

            var panel = UIObject("Story Panel", root);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f);
            var tap = panel.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(tap.onClick, new UnityAction(story.Next));

            // Portraits stand on the bottom edge, either side of the text box.
            story.leftPortrait = MakePortrait(panel.transform, "Left Portrait", -700f);
            story.rightPortrait = MakePortrait(panel.transform, "Right Portrait", 700f);

            var box = UIObject("Box", panel.transform);
            SetRect(box, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(0.5f, 0f), new Vector2(1100f, 290f));
            var boxImg = box.AddComponent<Image>();
            boxImg.sprite = LoadSprite(TS + "/UI Elements/Papers/RegularPaper.png");
            boxImg.type = Image.Type.Sliced;
            boxImg.pixelsPerUnitMultiplier = 1.2f;
            boxImg.raycastTarget = false;

            story.allyRibbon = LoadSprite(TS + "/UI Elements/Ribbons/BigRibbons 1.png");
            story.enemyRibbon = LoadSprite(TS + "/UI Elements/Ribbons/BigRibbons 2.png");
            var ribbon = UIObject("Name Ribbon", box.transform);
            SetRect(ribbon, new Vector2(0.5f, 1f), new Vector2(-330f, 10f), new Vector2(0.5f, 0.5f), new Vector2(400f, 120f));
            story.nameRibbon = ribbon.AddComponent<Image>();
            story.nameRibbon.sprite = story.allyRibbon;
            story.nameRibbon.type = Image.Type.Sliced;
            story.nameRibbon.raycastTarget = false;
            story.nameText = MakeText(ribbon.transform, "Name", "Monk", 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(360f, 70f));
            story.nameText.font = storyFont;

            story.bodyText = MakeText(box.transform, "Body", "", 36, TextAnchor.UpperLeft, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(960f, 170f));
            story.bodyText.font = storyFont;
            story.bodyText.color = InkColor;
            story.bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            story.bodyText.lineSpacing = 1.2f;
            story.bodyText.GetComponent<Outline>().enabled = false;

            story.nextHint = MakeText(box.transform, "Next Hint", "▼", 36, TextAnchor.MiddleCenter, new Vector2(1f, 0f), new Vector2(-70f, 55f), new Vector2(60f, 60f));
            story.nextHint.font = storyFont;
            story.nextHint.color = InkColor;
            story.nextHint.GetComponent<Outline>().enabled = false;

            var skip = MakeButton(panel.transform, "Skip", new Vector2(1f, 1f), new Vector2(-30f, -25f), new Vector2(1f, 1f), new Vector2(220f, 90f), Color.white);
            skip.GetComponent<Image>().sprite = LoadSprite(TS + "/UI Elements/Buttons/BigBlueButton_Regular.png");
            skip.GetComponent<Image>().pixelsPerUnitMultiplier = 2.5f;
            story.skipLabel = MakeText(skip.transform, "Label", "SKIP", 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(220f, 90f));
            story.skipLabel.font = storyFont;
            UnityEventTools.AddPersistentListener(skip.onClick, new UnityAction(story.Skip));

            story.speakerKeys = new[] { "monk", "commander", "panda", "panda_guide", "shaman", "goblin", "gnome", "shark", "minotaur", "skull", "troll",
                                        "snake", "bear", "turtle", "bat" };
            // Big crisp portraits (Assets/_Game/Art/Story/Portraits); the small avatars are the fallback.
            story.speakerSprites = new[]
            {
                Portrait("monk", TS + "/UI Elements/Human Avatars/Avatars_04.png"),
                Portrait("commander", TS + "/UI Elements/Human Avatars/Avatars_01.png"),
                Portrait("panda_cursed", TSE + "/Panda/Panda_Avatar.png"),
                Portrait("panda_guide", TSE + "/Panda/Panda_Avatar.png"),
                Portrait("shaman", TSE + "/Hex Shaman/Hex Shaman_Avatar.png"),
                Portrait("goblin", TSE + "/Torch Goblin/Torch Goblin_Avatar.png"),
                Portrait("gnome", TSE + "/Gnome/Gnome_Avatar.png"),
                Portrait("shark", TSE + "/Harpoon Shark/Harpoon Shark_Avatar.png"),
                Portrait("minotaur", TSE + "/Minotaur/Minotaur__Avatar.png"),
                Portrait("skull", TSE + "/Skull/Skull_Avatar.png"),
                Portrait("troll", TSE + "/Troll/Troll_Avatar.png"),
                Portrait("snake", TSE + "/Snake/Snake_Avatar.png"),
                Portrait("bear", TSE + "/Bear/Bear_Avatar.png"),
                Portrait("turtle", TSE + "/Turtle/Turtle_Avatar.png"),
                Portrait("bat", TSE + "/Giant Bat/Giant Bat_Avatar.png"),
            };

            // Full-screen pictures for chapter title cards and the ending (hidden until used)
            var back = UIObject("Story Backdrop", root);
            var backRt = (RectTransform)back.transform;
            backRt.anchorMin = new Vector2(0.5f, 0.5f);
            backRt.anchorMax = new Vector2(0.5f, 0.5f);
            backRt.sizeDelta = new Vector2(1920f, 1080f);
            story.backdrop = back.AddComponent<Image>();
            story.backdrop.raycastTarget = false;
            var backFit = back.AddComponent<AspectRatioFitter>();
            backFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backFit.aspectRatio = 16f / 9f;
            story.chapterBackdrops = new[]
            {
                PrepareSprite(StoryArt + "chapter1.png"), PrepareSprite(StoryArt + "chapter2.png"),
                PrepareSprite(StoryArt + "chapter3.png"), PrepareSprite(StoryArt + "chapter4.png"),
            };
            story.endingBackdrop = PrepareSprite(StoryArt + "ending.png");
            back.SetActive(false);

            // Big banner: chapter title card, "BOSS!", "THE END"
            var banner = UIObject("Story Banner", root);
            SetRect(banner, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(0.5f, 0.5f), new Vector2(1000f, 230f));
            story.bannerRibbon = banner.AddComponent<Image>();
            story.bannerRibbon.sprite = story.enemyRibbon;
            story.bannerRibbon.type = Image.Type.Sliced;
            story.bannerRibbon.raycastTarget = false;
            story.bannerTitle = MakeText(banner.transform, "Title", "BOSS!", 84, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(900f, 110f));
            story.bannerTitle.font = storyFont;
            story.bannerSub = MakeText(banner.transform, "Sub", "", 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(900f, 70f));
            story.bannerSub.font = storyFont;
            story.banner = banner;
            banner.SetActive(false);

            story.panel = panel;
            panel.transform.SetAsLastSibling();
            panel.SetActive(false);
        }

        /// <summary>1-1 tutorial: a speech bubble above the unit buttons and a bouncing hand that points at a button.</summary>
        static void BuildTutorial(GameObject canvasGo, Transform root, BattleUI ui)
        {
            var tut = canvasGo.AddComponent<TutorialUI>();
            tut.battleUI = ui;

            var bubble = UIObject("Tutorial Bubble", root);
            SetRect(bubble, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(0.5f, 0f), new Vector2(1000f, 150f));
            var img = bubble.AddComponent<Image>();
            img.sprite = LoadSprite(TS + "/UI Elements/Papers/RegularPaper.png");
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.5f;
            img.raycastTarget = false;
            tut.bubbleText = MakeText(bubble.transform, "Text", "", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(900f, 120f));
            tut.bubbleText.color = InkColor;
            tut.bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tut.bubbleText.GetComponent<Outline>().enabled = false;
            tut.bubble = bubble;

            var hand = UIObject("Tutorial Hand", root);
            SetRect(hand, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(128f, 128f));
            hand.transform.localRotation = Quaternion.Euler(0f, 0f, 180f); // the cursor points up; flip it to point down at the button
            var handImg = hand.AddComponent<Image>();
            handImg.sprite = LoadSprite(TS + "/UI Elements/Cursors/Cursor_02.png");
            handImg.raycastTarget = false;
            tut.hand = (RectTransform)hand.transform;
            hand.AddComponent<CanvasGroup>().ignoreParentGroups = true; // stays solid on a faded unit button

            bubble.SetActive(false);
            hand.SetActive(false);
        }

        static Sprite Portrait(string name, string fallback)
        {
            return PrepareSprite(StoryArt + "Portraits/portrait_" + name + ".png") ?? LoadSprite(fallback);
        }

        static Image MakePortrait(Transform parent, string name, float x)
        {
            var go = UIObject(name, parent);
            SetRect(go, new Vector2(0.5f, 0f), new Vector2(x, 20f), new Vector2(0.5f, 0f), new Vector2(420f, 420f));
            var img = go.AddComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        static readonly Color InkColor = new Color32(70, 45, 30, 255);

        /// <summary>One shop row: "Name Lv.N" | effect | buy button with cost.</summary>
        static void ShopRow(Transform parent, string name, float centerX, float y, Sprite buttonSprite,
            out Text nameText, out Text effectText, out Text costText, out Button button)
        {
            nameText = MakeText(parent, name + " Name", "Name  Lv.0", 32, TextAnchor.MiddleLeft,
                new Vector2(0.5f, 0.5f), new Vector2(centerX - 130f, y), new Vector2(380f, 56f));
            nameText.color = InkColor;
            nameText.GetComponent<Outline>().enabled = false;

            effectText = MakeText(parent, name + " Effect", "+0%", 32, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(centerX + 95f, y), new Vector2(140f, 56f));
            effectText.color = new Color32(40, 110, 60, 255);
            effectText.GetComponent<Outline>().enabled = false;

            button = MakeButton(parent, name + " Buy", new Vector2(0.5f, 0.5f), new Vector2(centerX + 270f, y), new Vector2(0.5f, 0.5f),
                new Vector2(200f, 72f), Color.white);
            var img = button.GetComponent<Image>();
            if (buttonSprite != null) img.sprite = buttonSprite;
            img.pixelsPerUnitMultiplier = 3f;
            var colors = button.colors;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            button.colors = colors;
            costText = MakeText(button.transform, "Cost", "20 Gems", 26, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(200f, 72f));
        }

        static Button ArrowButton(Transform parent, string name, string label, float x, Sprite sprite, UnityAction onClick)
        {
            var btn = MakeButton(parent, name, new Vector2(0.5f, 0.5f), new Vector2(x, -40f), new Vector2(0.5f, 0.5f),
                new Vector2(130f, 130f), Color.white);
            var img = btn.GetComponent<Image>();
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Simple; img.preserveAspect = true; }
            MakeText(btn.transform, "Label", label, 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(130f, 130f));
            UnityEventTools.AddPersistentListener(btn.onClick, onClick);
            return btn;
        }

        /// <summary>A stacked menu button; returns its label.</summary>
        static Text AddMenuButton(Transform parent, string name, string label, float y, Sprite sprite, UnityAction onClick, float width = 300f)
        {
            var btn = MakeButton(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(0.5f, 0.5f),
                new Vector2(width, 80f), Color.white);
            var img = btn.GetComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            img.pixelsPerUnitMultiplier = 2.5f;
            var text = MakeText(btn.transform, "Label", label, 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(width, 80f));
            UnityEventTools.AddPersistentListener(btn.onClick, onClick);
            return text;
        }

        /// <summary>The shop window (units + upgrades). Used on the stage select screen and in the battle pause menu.</summary>
        static void BuildShopPanel(UpgradeShopUI shop, Transform root, Sprite blueBtn, Sprite redBtn)
        {
            // Shop panel
            var shopPanel = UIObject("Upgrade Panel", root);
            var spRt = (RectTransform)shopPanel.transform;
            spRt.anchorMin = Vector2.zero;
            spRt.anchorMax = Vector2.one;
            spRt.offsetMin = Vector2.zero;
            spRt.offsetMax = Vector2.zero;
            shopPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var shopPaper = UIObject("Paper", shopPanel.transform);
            SetRect(shopPaper, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(0.5f, 0.5f), new Vector2(1640f, 760f));
            var shopPaperImg = shopPaper.AddComponent<Image>();
            shopPaperImg.sprite = LoadSprite(TS + "/UI Elements/Papers/RegularPaper.png");
            shopPaperImg.type = Image.Type.Sliced;
            shopPaperImg.pixelsPerUnitMultiplier = 1.2f;

            var shopRibbon = UIObject("Ribbon", shopPanel.transform);
            SetRect(shopRibbon, new Vector2(0.5f, 0.5f), new Vector2(0f, 370f), new Vector2(0.5f, 0.5f), new Vector2(760f, 200f));
            var shopRibbonImg = shopRibbon.AddComponent<Image>();
            shopRibbonImg.sprite = LoadSprite(TS + "/UI Elements/Ribbons/BigRibbons 3.png"); // yellow
            shopRibbonImg.type = Image.Type.Sliced;
            MakeText(shopRibbon.transform, "Title", "SHOP", 60, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(600f, 110f));

            // Column headers
            const float leftX = -400f, rightX = 400f;
            var unitsHeader = MakeText(shopPaper.transform, "Units Header", "UNIT TRAINING", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(leftX, 240f), new Vector2(600f, 60f));
            unitsHeader.color = InkColor;
            unitsHeader.GetComponent<Outline>().enabled = false;
            var boostHeader = MakeText(shopPaper.transform, "Boosts Header", "CASTLE UPGRADES", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(rightX, 240f), new Vector2(600f, 60f));
            boostHeader.color = InkColor;
            boostHeader.GetComponent<Outline>().enabled = false;

            int nu = UnitProgress.ShopUnits.Length;
            shop.unitNameTexts = new Text[nu];
            shop.unitEffectTexts = new Text[nu];
            shop.unitCostTexts = new Text[nu];
            shop.unitButtons = new Button[nu];
            for (int i = 0; i < nu; i++)
            {
                ShopRow(shopPaper.transform, "Unit " + i, leftX, 140f - i * 100f, blueBtn,
                    out shop.unitNameTexts[i], out shop.unitEffectTexts[i], out shop.unitCostTexts[i], out shop.unitButtons[i]);
                UnityEventTools.AddIntPersistentListener(shop.unitButtons[i].onClick, new UnityAction<int>(shop.BuyUnit), i);
            }

            int n = Upgrades.All.Length;
            shop.nameTexts = new Text[n];
            shop.effectTexts = new Text[n];
            shop.costTexts = new Text[n];
            shop.buyButtons = new Button[n];
            for (int i = 0; i < n; i++)
            {
                ShopRow(shopPaper.transform, "Upgrade " + i, rightX, 140f - i * 100f, blueBtn,
                    out shop.nameTexts[i], out shop.effectTexts[i], out shop.costTexts[i], out shop.buyButtons[i]);
                UnityEventTools.AddIntPersistentListener(shop.buyButtons[i].onClick, new UnityAction<int>(shop.Buy), i);
            }

            shop.feedbackText = MakeText(shopPaper.transform, "Feedback", "", 34, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -215f), new Vector2(1200f, 56f));
            shop.feedbackText.GetComponent<Outline>().enabled = false;
            var oddsNote = MakeText(shopPaper.transform, "Odds Note", "Unit level-up odds: Lv.1-15 100% / Lv.16-25 80% / Lv.26-35 60% / Lv.36-50 40%. +10% after each fail. Fail keeps your level.", 22, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -250f), new Vector2(1500f, 40f));
            oddsNote.color = InkColor;
            oddsNote.GetComponent<Outline>().enabled = false;

            var closeShop = MakeButton(shopPaper.transform, "Close", new Vector2(0.5f, 0.5f), new Vector2(0f, -325f), new Vector2(0.5f, 0.5f),
                new Vector2(240f, 90f), Color.white);
            closeShop.GetComponent<Image>().sprite = redBtn;
            closeShop.GetComponent<Image>().pixelsPerUnitMultiplier = 2.5f;
            MakeText(closeShop.transform, "Label", "CLOSE", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(240f, 90f));
            UnityEventTools.AddPersistentListener(closeShop.onClick, new UnityAction(shop.Close));
            shop.panel = shopPanel;
            shopPanel.SetActive(false);
        }

        /// <summary>Small red "!" in the top-right corner of a button.</summary>
        static GameObject MakeBadge(Transform button, Sprite sprite)
        {
            var go = UIObject("Badge", button);
            SetRect(go, new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(0.5f, 0.5f), new Vector2(56f, 56f));
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            if (sprite == null) img.color = new Color(0.9f, 0.2f, 0.2f);
            MakeText(go.transform, "Mark", "!", 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(56f, 56f));
            go.SetActive(false);
            return go;
        }

        static Button ResultButton(Transform parent, string name, string label, float x, Sprite sprite, UnityAction onClick)
        {
            var btn = MakeButton(parent, name, new Vector2(0.5f, 0.5f), new Vector2(x, -135f), new Vector2(0.5f, 0.5f),
                new Vector2(175f, 90f), Color.white);
            var img = btn.GetComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            img.pixelsPerUnitMultiplier = 2.5f;
            MakeText(btn.transform, "Label", label, 32, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(175f, 90f));
            UnityEventTools.AddPersistentListener(btn.onClick, onClick);
            return btn;
        }

        // ======================= Stage select scene =======================

        static void BuildStageSelectScene(StageCatalog catalog)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(71, 171, 169, 255);
            camGo.AddComponent<AudioListener>();

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform root = canvasGo.transform;
            AddSkin(canvasGo);

            // Map background with a light dark veil so the buttons stay readable
            var mapSprite = PrepareSprite(StoryArt + "stage_map.png");
            if (mapSprite != null)
            {
                var map = UIObject("Map Background", root);
                var mapRt = (RectTransform)map.transform;
                mapRt.anchorMin = new Vector2(0.5f, 0.5f);
                mapRt.anchorMax = new Vector2(0.5f, 0.5f);
                mapRt.sizeDelta = new Vector2(1920f, 1080f);
                var mapImg = map.AddComponent<Image>();
                mapImg.sprite = mapSprite;
                mapImg.raycastTarget = false;
                var mapFit = map.AddComponent<AspectRatioFitter>();
                mapFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                mapFit.aspectRatio = 16f / 9f;
                var veil = UIObject("Map Veil", root);
                var veilRt = (RectTransform)veil.transform;
                veilRt.anchorMin = Vector2.zero; veilRt.anchorMax = Vector2.one; veilRt.offsetMin = Vector2.zero; veilRt.offsetMax = Vector2.zero;
                var veilImg = veil.AddComponent<Image>();
                veilImg.color = new Color(0.05f, 0.08f, 0.12f, 0.35f);
                veilImg.raycastTarget = false;
            }

            // Big scroll in the middle
            var scroll = UIObject("Scroll", root);
            SetRect(scroll, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(0.5f, 0.5f), new Vector2(1180f, 760f));
            var scrollImg = scroll.AddComponent<Image>();
            scrollImg.sprite = LoadSprite(TS + "/UI Elements/Banners/Banner.png");
            scrollImg.type = Image.Type.Sliced;
            scrollImg.pixelsPerUnitMultiplier = 1f;
            scrollImg.raycastTarget = false;

            // Blue ribbon title
            var ribbon = UIObject("Title Ribbon", root);
            SetRect(ribbon, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(0.5f, 0.5f), new Vector2(820f, 210f));
            var ribbonImg = ribbon.AddComponent<Image>();
            ribbonImg.sprite = LoadSprite(TS + "/UI Elements/Ribbons/BigRibbons 1.png");
            ribbonImg.type = Image.Type.Sliced;
            ribbonImg.raycastTarget = false;
            var chapterTitle = MakeText(ribbon.transform, "Title", "CHAPTER 1", 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(700f, 120f));

            var ui = canvasGo.AddComponent<StageSelectUI>();
            ui.chapterTitle = chapterTitle;
            ui.catalog = catalog;
            ui.unlockedSprite = LoadSprite(TS + "/UI Elements/Buttons/BigBlueButton_Regular.png");
            ui.lockedSprite = LoadSprite(TS + "/UI Elements/Buttons/BigRedButton_Regular.png");

            int count = Mathf.Min(6, catalog.stages.Count); // one page = one chapter of 6 stages
            ui.stageButtons = new Button[count];
            ui.stageLabels = new Text[count];
            ui.starLabels = new Text[count];
            const int columns = 3;
            const float w = 260f, h = 200f, gapX = 60f, gapY = 50f;
            for (int i = 0; i < count; i++)
            {
                int col = i % columns, row = i / columns;
                float x = (col - (columns - 1) / 2f) * (w + gapX);
                float y = 70f - row * (h + gapY);
                var btn = MakeButton(root, "Stage " + catalog.stages[i].stageName, new Vector2(0.5f, 0.5f), new Vector2(x, y),
                    new Vector2(0.5f, 0.5f), new Vector2(w, h), Color.white);
                var img = btn.GetComponent<Image>();
                img.sprite = ui.unlockedSprite;
                img.pixelsPerUnitMultiplier = 1.5f;
                var colors = btn.colors;
                colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 1f);
                btn.colors = colors;

                ui.stageLabels[i] = MakeText(btn.transform, "Name", catalog.stages[i].stageName, 64, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(w, 90f));
                ui.starLabels[i] = MakeText(btn.transform, "Stars", "NEW", 30, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(w, 50f));

                UnityEventTools.AddIntPersistentListener(btn.onClick, new UnityAction<int>(ui.Play), i);
                ui.stageButtons[i] = btn;
            }

            // Chapter arrows on both sides of the scroll
            Sprite roundBtn = LoadSprite(TS + "/UI Elements/Buttons/SmallBlueRoundButton_Regular.png");
            ui.prevButton = ArrowButton(root, "Prev Chapter", "<", -680f, roundBtn, new UnityAction(ui.PrevPage));
            ui.nextButton = ArrowButton(root, "Next Chapter", ">", 680f, roundBtn, new UnityAction(ui.NextPage));

            // ---------- Upgrade shop ----------
            var shop = canvasGo.AddComponent<UpgradeShopUI>();
            Sprite blueBtn = ui.unlockedSprite;
            Sprite redBtn = ui.lockedSprite;

            // Gems counter, top-right (left of the MENU button)
            var gemsPanel = UIObject("Gems", root);
            SetRect(gemsPanel, new Vector2(1f, 1f), new Vector2(-220f, -20f), new Vector2(1f, 1f), new Vector2(300f, 130f));
            var gemsImg = gemsPanel.AddComponent<Image>();
            gemsImg.sprite = LoadSprite(TS + "/UI Elements/Banners/Banner.png");
            gemsImg.type = Image.Type.Sliced;
            gemsImg.pixelsPerUnitMultiplier = 3f;
            gemsImg.raycastTarget = false;
            shop.gemsText = MakeText(gemsPanel.transform, "Label", "Gems 0", 40, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(8f, 8f), new Vector2(280f, 60f));
            shop.gemsText.color = InkColor;
            shop.gemsText.GetComponent<Outline>().enabled = false;

            BuildShopPanel(shop, root, blueBtn, redBtn);

            // ---------- Top-right MENU: Shop / Settings ----------
            var menu = canvasGo.AddComponent<MenuUI>();
            menu.shop = shop;

            var menuBtn = MakeButton(root, "Menu Button", new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(1f, 1f),
                new Vector2(170f, 110f), Color.white);
            menuBtn.GetComponent<Image>().sprite = blueBtn;
            menuBtn.GetComponent<Image>().pixelsPerUnitMultiplier = 2f;
            MakeText(menuBtn.transform, "Label", "MENU", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(170f, 110f));
            UnityEventTools.AddPersistentListener(menuBtn.onClick, new UnityAction(menu.ToggleMenu));

            // Dropdown under the menu button
            var dropdown = UIObject("Menu Panel", root);
            SetRect(dropdown, new Vector2(1f, 1f), new Vector2(-20f, -150f), new Vector2(1f, 1f), new Vector2(360f, 710f));
            var ddImg = dropdown.AddComponent<Image>();
            ddImg.sprite = LoadSprite(TS + "/UI Elements/Papers/RegularPaper.png");
            ddImg.type = Image.Type.Sliced;
            ddImg.pixelsPerUnitMultiplier = 1.5f;
            var shopEntry = AddMenuButton(dropdown.transform, "Shop", "SHOP", 237f, blueBtn, new UnityAction(menu.OpenShop));
            Sprite badgeSprite = LoadSprite(TS + "/UI Elements/Buttons/TinyRoundRedButton.png");
            menu.shopBadges = new[]
            {
                MakeBadge(menuBtn.transform, badgeSprite),
                MakeBadge(shopEntry.transform.parent, badgeSprite),
            };
            AddMenuButton(dropdown.transform, "Daily", "DAILY REWARD", 142f, blueBtn, new UnityAction(menu.OpenDaily));
            AddMenuButton(dropdown.transform, "Codex", "CODEX", 47f, blueBtn, new UnityAction(menu.OpenCodex));
            var achEntry = AddMenuButton(dropdown.transform, "Achievements", "ACHIEVEMENTS", -48f, blueBtn, new UnityAction(menu.OpenAchievements));
            menu.achievementBadge = MakeBadge(achEntry.transform.parent, badgeSprite);
            AddMenuButton(dropdown.transform, "Settings", "SETTINGS", -143f, blueBtn, new UnityAction(menu.OpenSettings));
            AddMenuButton(dropdown.transform, "Close", "CLOSE", -238f, redBtn, new UnityAction(menu.ToggleMenu));
            menu.menuPanel = dropdown;

            // Settings window
            var settings = UIObject("Settings Panel", root);
            var setRt = (RectTransform)settings.transform;
            setRt.anchorMin = Vector2.zero;
            setRt.anchorMax = Vector2.one;
            setRt.offsetMin = Vector2.zero;
            setRt.offsetMax = Vector2.zero;
            settings.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var setPaper = UIObject("Paper", settings.transform);
            SetRect(setPaper, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(0.5f, 0.5f), new Vector2(720f, 740f));
            var setPaperImg = setPaper.AddComponent<Image>();
            setPaperImg.sprite = LoadSprite(TS + "/UI Elements/Papers/RegularPaper.png");
            setPaperImg.type = Image.Type.Sliced;
            setPaperImg.pixelsPerUnitMultiplier = 1.2f;

            var setRibbon = UIObject("Ribbon", settings.transform);
            SetRect(setRibbon, new Vector2(0.5f, 0.5f), new Vector2(0f, 365f), new Vector2(0.5f, 0.5f), new Vector2(700f, 190f));
            var setRibbonImg = setRibbon.AddComponent<Image>();
            setRibbonImg.sprite = LoadSprite(TS + "/UI Elements/Ribbons/BigRibbons 5.png"); // navy
            setRibbonImg.type = Image.Type.Sliced;
            MakeText(setRibbon.transform, "Title", "SETTINGS", 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(560f, 110f));

            menu.musicLabel = AddMenuButton(setPaper.transform, "Music", "Music: 60%", 200f, blueBtn, new UnityAction(menu.CycleMusic), 420f);
            menu.sfxLabel = AddMenuButton(setPaper.transform, "Effects", "Effects: 80%", 112f, blueBtn, new UnityAction(menu.CycleSfx), 420f);
            menu.languageLabel = AddMenuButton(setPaper.transform, "Language", "Language: Auto", 24f, blueBtn, new UnityAction(menu.CycleLanguage), 420f);
            AddMenuButton(setPaper.transform, "Credits", "CREDITS", -64f, blueBtn, new UnityAction(menu.OpenCredits), 420f);
            menu.resetLabel = AddMenuButton(setPaper.transform, "Reset", "Reset progress", -152f, redBtn, new UnityAction(menu.ResetProgress), 420f);
            AddMenuButton(setPaper.transform, "Close", "CLOSE", -262f, blueBtn, new UnityAction(menu.CloseSettings), 260f);
            menu.settingsPanel = settings;

            // First-unlock guide: MENU -> SHOP -> unlock button
            var guide = canvasGo.AddComponent<ShopGuideUI>();
            guide.menuButton = (RectTransform)menuBtn.transform;
            guide.shopEntry = (RectTransform)shopEntry.transform.parent;
            guide.menuPanel = dropdown;
            guide.shop = shop;
            var gBubble = UIObject("Guide Bubble", root);
            SetRect(gBubble, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(0.5f, 0f), new Vector2(1100f, 140f));
            var gImg = gBubble.AddComponent<Image>();
            gImg.sprite = LoadSprite(TS + "/UI Elements/Papers/RegularPaper.png");
            gImg.type = Image.Type.Sliced;
            gImg.pixelsPerUnitMultiplier = 1.5f;
            gImg.raycastTarget = false;
            guide.bubbleText = MakeText(gBubble.transform, "Text", "", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 110f));
            guide.bubbleText.color = InkColor;
            guide.bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            guide.bubbleText.GetComponent<Outline>().enabled = false;
            guide.bubble = gBubble;
            var gHand = UIObject("Guide Hand", root);
            SetRect(gHand, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(110f, 110f));
            var gHandImg = gHand.AddComponent<Image>();
            gHandImg.sprite = LoadSprite(TS + "/UI Elements/Cursors/Cursor_02.png");
            gHandImg.raycastTarget = false;
            guide.hand = (RectTransform)gHand.transform;
            gBubble.SetActive(false);
            gHand.SetActive(false);

            dropdown.SetActive(false);
            settings.SetActive(false);

            EditorSceneManager.SaveScene(scene, StageSelectScenePath);
        }

        // ======================= Title scene =======================

        /// <summary>Puts the shared sprites/font for code-built windows (daily reward, achievements, codex, credits) on a canvas.</summary>
        static void AddSkin(GameObject canvasGo)
        {
            var skin = canvasGo.AddComponent<UISkin>();
            skin.font = uiFont;
            skin.paper = LoadSprite(TS + "/UI Elements/Papers/RegularPaper.png");
            skin.ribbon = LoadSprite(TS + "/UI Elements/Ribbons/BigRibbons 5.png");
            skin.blueButton = LoadSprite(TS + "/UI Elements/Buttons/BigBlueButton_Regular.png");
            skin.redButton = LoadSprite(TS + "/UI Elements/Buttons/BigRedButton_Regular.png");
            skin.badge = LoadSprite(TS + "/UI Elements/Buttons/TinyRoundRedButton.png");
            skin.banner = LoadSprite(TS + "/UI Elements/Banners/Banner.png");
            skin.codexUnits = codexUnits;
            skin.catalog = builtCatalog;
        }

        /// <summary>Makes a PNG under Assets import as a single crisp sprite. Returns null if the file is missing.</summary>
        static Sprite PrepareSprite(string path)
        {
            if (!System.IO.File.Exists(path)) return null;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void BuildTitleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(71, 171, 169, 255);
            camGo.AddComponent<AudioListener>();

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform root = canvasGo.transform;
            AddSkin(canvasGo);
            var title = canvasGo.AddComponent<TitleScreen>();

            // Background: fills the screen, cropping the edges if the aspect differs (swap the PNG to change it)
            var bg = UIObject("Background", root);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(1920f, 1080f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = PrepareSprite(TitleArt + "title_bg.png");
            bgImg.raycastTarget = false;
            var fit = bg.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = bgImg.sprite != null ? bgImg.sprite.rect.width / bgImg.sprite.rect.height : 16f / 9f;

            // Logo: title_logo.png if it exists, otherwise a ribbon with the game name in the pixel font
            var logo = UIObject("Logo", root);
            SetRect(logo, new Vector2(0.5f, 0.5f), new Vector2(0f, 290f), new Vector2(0.5f, 0.5f), new Vector2(1100f, 300f));
            title.logo = (RectTransform)logo.transform;
            Sprite logoSprite = PrepareSprite(TitleArt + "title_logo.png");
            if (logoSprite != null)
            {
                var li = logo.AddComponent<Image>();
                li.sprite = logoSprite;
                li.preserveAspect = true;
                li.raycastTarget = false;
            }
            else
            {
                var rib = UIObject("Ribbon", logo.transform);
                SetRect(rib, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(1100f, 260f));
                var ri = rib.AddComponent<Image>();
                ri.sprite = LoadSprite(TS + "/UI Elements/Ribbons/BigRibbons 1.png");
                ri.type = Image.Type.Sliced;
                ri.raycastTarget = false;
                title.logoText = MakeText(logo.transform, "Name", "Lane Lords", 76, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 22f), new Vector2(1000f, 170f));
                title.logoText.color = new Color32(255, 230, 140, 255);
                var o = title.logoText.GetComponent<Outline>();
                o.effectDistance = new Vector2(4f, -4f);
            }

            // Sparkles around the logo
            var spark = new List<RectTransform>();
            Vector2[] spots = { new Vector2(-560f, 380f), new Vector2(520f, 400f), new Vector2(-470f, 180f), new Vector2(590f, 210f), new Vector2(-150f, 450f), new Vector2(260f, 440f) };
            for (int i = 0; i < spots.Length; i++)
            {
                var s = UIObject("Sparkle " + i, root);
                SetRect(s, new Vector2(0.5f, 0.5f), spots[i], new Vector2(0.5f, 0.5f), new Vector2(26f, 26f));
                var si = s.AddComponent<Image>();
                si.sprite = uiSprite;
                si.raycastTarget = false;
                spark.Add((RectTransform)s.transform);
            }
            title.sparkles = spark.ToArray();

            title.tapText = MakeText(root, "Tap", "TAP TO START", 54, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(900f, 90f));
            title.versionText = MakeText(root, "Version", "v1.0", 28, TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(-120f, 30f), new Vector2(200f, 40f));

            // Full-screen invisible button: any tap starts the game
            var tap = MakeButton(root, "Tap To Start", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(4000f, 4000f), new Color(0f, 0f, 0f, 0f));
            tap.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(tap.onClick, new UnityAction(title.StartGame));

            EditorSceneManager.SaveScene(scene, TitleScenePath);
        }

        static Image MakeHpBar(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 pivot, Color color,
            Image.OriginHorizontal origin, out Text label)
        {
            var bg = UIObject(name, parent);
            SetRect(bg, anchor, pos, pivot, new Vector2(520f, 50f));
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = uiSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0f, 0f, 0f, 0.6f);

            var fillGo = UIObject("Fill", bg.transform);
            var frt = (RectTransform)fillGo.transform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(5f, 5f);
            frt.offsetMax = new Vector2(-5f, -5f);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = uiSprite;
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)origin;
            fill.raycastTarget = false;

            label = MakeText(bg.transform, "Label", "", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 50f));
            return fill;
        }

        static Button MakeButton(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 pivot, Vector2 size, Color color)
        {
            var go = UIObject(name, parent);
            SetRect(go, anchor, pos, pivot, size);
            var img = go.AddComponent<Image>();
            img.sprite = uiSprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        /// <summary>Galmuri pixel font (Korean + English). Falls back to Unity's built-in font if it is missing.</summary>
        static Font LoadUIFont()
        {
            var f = AssetDatabase.LoadAssetAtPath<Font>(StoryFontPath);
            return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        static Text MakeText(Transform parent, string name, string text, int fontSize, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = UIObject(name, parent);
            SetRect(go, anchor, pos, new Vector2(0.5f, 0.5f), size);
            var t = go.AddComponent<Text>();
            t.font = uiFont;
            if (Loc.HasStatic(text)) go.AddComponent<LocalizedText>().english = text;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = Color.white;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        static GameObject UIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void SetRect(GameObject go, Vector2 anchor, Vector2 pos, Vector2 pivot, Vector2 size)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // ======================= Helpers =======================

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Debug.LogWarning("[Lane Lords] Missing asset: " + path);
            return asset;
        }

        static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).FirstOrDefault();
            if (sprite == null) Debug.LogWarning("[Lane Lords] Missing sprite: " + path);
            return sprite;
        }

        static List<Sprite> SpritesIn(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return new List<Sprite>();
            return AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Select(p => AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().OrderBy(s => s.name).FirstOrDefault())
                .Where(s => s != null)
                .ToList();
        }

        static void AddSceneToBuild(string path)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
