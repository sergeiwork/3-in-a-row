#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using ThreeInARow.Presentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ThreeInARow.Editor
{
    /// <summary>Creates deterministic Session E runtime assets and the sole portrait bootstrap scene.</summary>
    public static class SectionEBuilder
    {
        private const string ArtRoot = "Assets/Game/Presentation/Art/E0/";
        private const string CatalogPath = "Assets/Resources/E0PresentationCatalog.asset";
        private const string PanelSettingsPath = "Assets/Resources/PortraitPanelSettings.asset";
        private const string ThemePath = "Assets/Game/Presentation/Runtime/PortraitRuntimeTheme.tss";
        private const string ScenePath = "Assets/Game/Scenes/PortraitGame.unity";

        [MenuItem("Three in a Row/Build Windows Player")]
        public static void BuildWindowsFromMenu()
        {
            BuildWindows();
        }

        private static void PrepareRuntime()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Game/Scenes");

            var catalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var sprites = new List<PresentationCatalog.SpriteEntry>();
            AddSprite(sprites, "gem.ember", "Board/GemMatch3/gem_ember.png");
            AddSprite(sprites, "gem.tide", "Board/GemMatch3/gem_tide.png");
            AddSprite(sprites, "gem.venom", "Board/GemMatch3/gem_venom.png");
            AddSprite(sprites, "gem.volt", "Board/GemMatch3/gem_volt.png");
            AddSprite(sprites, "gem.prism", "Board/GemMatch3/gem_prism.png");
            AddSprite(sprites, "special.prism", "Board/GemMatch3/gem_prism.png");
            AddSprite(sprites, "special.spark", "Board/GemMatch3/special_spark.png");
            AddSprite(sprites, "special.current", "Board/GemMatch3/special_current.png");
            AddSprite(sprites, "special.spore", "Board/GemMatch3/special_spore.png");
            AddSprite(sprites, "special.charge", "Board/GemMatch3/special_charge.png");

            AddIcon(sprites, "status.frozen", "frozen-block.png");
            AddIcon(sprites, "status.cracked", "cracked-glass.png");
            AddIcon(sprites, "status.anchored", "anchor.png");
            AddIcon(sprites, "status.poison", "poison-gas.png");

            AddSprite(sprites, "enemy.geode_mite", "Enemies/Generated/enemy_geode_mite.png");
            AddSprite(sprites, "enemy.frost_oracle", "Enemies/Generated/enemy_frost_oracle.png");
            AddSprite(sprites, "enemy.geode_mite_elite", "Enemies/Generated/enemy_geode_mite_elite.png");
            AddSprite(sprites, "enemy.prism_stalker", "Enemies/Generated/enemy_prism_stalker.png");
            AddSprite(sprites, "enemy.crystal_warden", "Enemies/Generated/enemy_crystal_warden.png");
            AddSprite(sprites, "enemy.crystal_tick", "Enemies/Generated/enemy_geode_mite.png");
            AddSprite(sprites, "enemy.rime_moth", "Enemies/Generated/enemy_frost_oracle.png");
            AddSprite(sprites, "enemy.anchor_crab", "Enemies/Generated/enemy_geode_mite_elite.png");
            AddSprite(sprites, "enemy.hollow_idol", "Enemies/Generated/enemy_prism_stalker.png");
            AddSprite(sprites, "enemy.fracture_golem", "Enemies/Generated/enemy_geode_mite_elite.png");
            AddSprite(sprites, "enemy.stormglass_roc", "Enemies/Generated/enemy_frost_oracle.png");
            AddSprite(sprites, "enemy.facet_engine", "Enemies/Generated/enemy_crystal_warden.png");

            AddIcon(sprites, "intent.chip", "rock.png");
            AddIcon(sprites, "intent.crack", "cracked-glass.png");
            AddIcon(sprites, "intent.chill", "snowflake-1.png");
            AddIcon(sprites, "intent.needle", "ice-spear.png");
            AddIcon(sprites, "intent.crush", "hammer-drop.png");
            AddIcon(sprites, "intent.bolt", "lightning-frequency.png");
            AddIcon(sprites, "intent.drain", "marrow-drain.png");
            AddIcon(sprites, "intent.seal", "anchor.png");
            AddIcon(sprites, "intent.shardstorm", "crystal-shine.png");
            AddIcon(sprites, "intent.freeze_anchor", "snowflake-1.png");
            AddIcon(sprites, "intent.bite", "shattered-sword.png");
            AddIcon(sprites, "intent.freeze_hit", "snowflake-1.png");
            AddIcon(sprites, "intent.claw", "shattered-sword.png");

            AddIcon(sprites, "ui.player_health", "glass-heart.png");
            AddIcon(sprites, "ui.enemy_health", "glass-heart.png");
            AddIcon(sprites, "ui.focus", "magic-swirl.png");
            AddIcon(sprites, "ui.toxic", "poison-bottle.png");
            AddIcon(sprites, "ui.shield", "bordered-shield.png");
            AddIcon(sprites, "ui.experience", "justice-star.png");
            AddIcon(sprites, "ui.level_up", "justice-star.png");
            AddIcon(sprites, "ui.victory", "laurel-crown.png");
            AddIcon(sprites, "ui.defeat", "skull-crossed-bones.png");
            AddIcon(sprites, "ui.status_feedback", "circle-sparks.png");
            AddIcon(sprites, "ui.clear_feedback", "circle-sparks.png");

            AddIcon(sprites, "skill.kindling", "small-fire.png");
            AddIcon(sprites, "skill.backdraft", "fire-shield.png");
            AddIcon(sprites, "skill.flow_state", "big-wave.png");
            AddIcon(sprites, "skill.undertow", "wave-strike.png");
            AddIcon(sprites, "skill.corrosive", "poison-gas.png");
            AddIcon(sprites, "skill.overcharge", "power-lightning.png");
            AddIcon(sprites, "skill.sunder", "shattered-sword.png");
            AddIcon(sprites, "skill.cleanse", "magic-palm.png");
            AddIcon(sprites, "skill.catalyze", "bubbling-flask.png");
            AddIcon(sprites, "skill.cinderwake", "small-fire.png");
            AddIcon(sprites, "skill.reservoir", "bordered-shield.png");
            AddIcon(sprites, "skill.concentrate", "poison-bottle.png");
            AddIcon(sprites, "skill.contagion", "poison-gas.png");
            AddIcon(sprites, "skill.static_guard", "lightning-frequency.png");
            AddIcon(sprites, "skill.live_wire", "power-lightning.png");
            AddIcon(sprites, "skill.aegis", "bordered-shield.png");
            AddIcon(sprites, "skill.infuse", "crystal-shine.png");
            AddIcon(sprites, "skill.keystone.tempered_core", "glass-heart.png");
            AddIcon(sprites, "skill.keystone.prismatic_start", "crystal-shine.png");
            AddIcon(sprites, "skill.keystone.rapid_casting", "magic-swirl.png");
            AddIcon(sprites, "skill.keystone.hard_light", "fire-shield.png");

            AddSprite(sprites, "ui.button.primary", "UI/KenneyRpg/buttonLong_blue.png");
            AddSprite(sprites, "ui.button.secondary", "UI/KenneyRpg/buttonLong_brown.png");
            AddSprite(sprites, "ui.button.disabled", "UI/KenneyRpg/buttonLong_grey.png");
            AddSprite(sprites, "feedback.clear", "Vfx/KenneySmoke/WhitePuff/whitePuff08.png");
            AddSprite(sprites, "feedback.special", "Vfx/KenneySmoke/Explosion/explosion04.png");
            AddSprite(sprites, "feedback.hit", "Vfx/KenneySmoke/Flash/flash04.png");
            AddSprite(sprites, "feedback.status_added", "Vfx/KenneySmoke/BlackSmoke/blackSmoke08.png");

            var audio = new List<PresentationCatalog.AudioEntry>();
            AddAudio(audio, "feedback.swap", "Audio/KenneyInterface/pluck_001.ogg");
            AddAudio(audio, "feedback.invalid_swap", "Audio/KenneyInterface/error_003.ogg");
            AddAudio(audio, "feedback.clear", "Audio/KenneyInterface/glass_002.ogg");
            AddAudio(audio, "feedback.special", "Audio/KenneyInterface/maximize_006.ogg");
            AddAudio(audio, "feedback.hit", "Audio/KenneyRpg/chop.ogg");
            AddAudio(audio, "feedback.sunder", "Audio/KenneyRpg/knifeSlice2.ogg");
            AddAudio(audio, "feedback.shield", "Audio/KenneyRpg/metalClick.ogg");
            AddAudio(audio, "feedback.status_added", "Audio/KenneyInterface/drop_002.ogg");
            AddAudio(audio, "feedback.status_removed", "Audio/KenneyInterface/close_002.ogg");
            AddAudio(audio, "feedback.victory", "Audio/KenneyInterface/confirmation_004.ogg");
            AddAudio(audio, "feedback.defeat", "Audio/KenneyInterface/bong_001.ogg");
            AddAudio(audio, "feedback.ui_select", "Audio/KenneyInterface/click_003.ogg");
            AddAudio(audio, "feedback.reward_confirmed", "Audio/KenneyInterface/confirmation_001.ogg");

            catalog.ReplaceEntries(sprites, audio);
            EditorUtility.SetDirty(catalog);

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.name = "Portrait Panel Settings";
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1080, 1920);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            panelSettings.clearColor = false;
            panelSettings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (panelSettings.themeStyleSheet == null)
                throw new FileNotFoundException("Missing runtime UI theme", ThemePath);
            EditorUtility.SetDirty(panelSettings);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("ThreeInARowApp");
            var document = root.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            root.AddComponent<ThreeInARowApp>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.productName = "Three in a Row: Roguelike Crystals";
            PlayerSettings.companyName = "Three in a Row";
            PlayerSettings.bundleVersion = "0.6.0";
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            AssetDatabase.SaveAssets();
        }

        public static void BuildWindows()
        {
            PrepareRuntime();
            const string outputDirectory = "Builds/Windows";
            Directory.CreateDirectory(outputDirectory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputDirectory + "/ThreeInARow.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Session E build failed: " + report.summary.result);
            Debug.Log("Session E build complete: " + report.summary.outputPath);
        }

        private static void AddIcon(List<PresentationCatalog.SpriteEntry> destination, string key, string file)
        {
            AddSprite(destination, key, "Icons/GameIconsLorc/" + file);
        }

        private static void AddSprite(List<PresentationCatalog.SpriteEntry> destination, string key, string relativePath)
        {
            var path = ArtRoot + relativePath;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new FileNotFoundException("Missing mapped E0 sprite", path);
            destination.Add(new PresentationCatalog.SpriteEntry { Key = key, Sprite = sprite });
        }

        private static void AddAudio(List<PresentationCatalog.AudioEntry> destination, string key, string relativePath)
        {
            var path = ArtRoot + relativePath;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new FileNotFoundException("Missing mapped E0 audio clip", path);
            destination.Add(new PresentationCatalog.AudioEntry { Key = key, Clip = clip });
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = path.Substring(0, path.LastIndexOf('/'));
            var name = path.Substring(path.LastIndexOf('/') + 1);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
