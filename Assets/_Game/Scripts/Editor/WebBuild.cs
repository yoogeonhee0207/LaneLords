using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TinySwordsBattle.EditorTools
{
    /// <summary>
    /// Menu: Tools > Lane Lords > Build WebGL
    /// Sets the web player settings, shrinks the audio, and builds to ../Builds/WebGL (next to the project folder).
    /// The build opens on any static host (itch.io etc.) thanks to the decompression fallback.
    /// </summary>
    public static class WebBuild
    {
        const string TemplateName = "PROJECT:TinySwords";

        public static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/WebGL"));

        [MenuItem("Tools/Lane Lords/Build WebGL")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Lane Lords", "플레이를 멈춘 뒤에 다시 실행해 주세요.", "OK");
                return;
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog("Lane Lords",
                    "웹 빌드 모듈이 설치되어 있지 않아요.\n\nUnity Hub > 설치(Installs) > 이 Unity 버전의 톱니바퀴 > 모듈 추가(Add modules) > Web Build Support 를 설치한 뒤 Unity를 다시 열어 주세요.", "OK");
                return;
            }

            ApplyPlayerSettings();
            ShrinkAudio();

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("Lane Lords", "빌드할 씬이 없어요. 먼저 Build Battle Scene 을 실행해 주세요.", "OK");
                return;
            }

            Directory.CreateDirectory(OutputPath);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });

            var summary = report.summary;
            long bytes = DirectorySize(OutputPath);
            string msg = summary.result == BuildResult.Succeeded
                ? $"웹 빌드 완료!\n\n위치: {OutputPath}\n크기: {bytes / 1048576f:0.0} MB\n걸린 시간: {summary.totalTime.TotalMinutes:0.0}분"
                : $"웹 빌드 실패: {summary.result}\n자세한 내용은 Console 창을 확인해 주세요.";
            Debug.Log("[Lane Lords] " + msg.Replace("\n", " "));
            File.WriteAllText(Path.Combine(OutputPath, "..", "webgl_build_result.txt"), msg);
            EditorUtility.DisplayDialog("Lane Lords", msg, "OK");
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "TinySwordsBattle";
            PlayerSettings.productName = "Lane Lords";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = false;

            PlayerSettings.WebGL.template = TemplateName;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true; // works on hosts that don't send gzip headers
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
        }

        /// <summary>Vorbis compression for all game audio: music at 50% quality, effects at 40% and mono.</summary>
        static void ShrinkAudio()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Game/Resources" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                bool music = path.Contains("/BGM/");
                var s = importer.defaultSampleSettings;
                float quality = music ? 0.5f : 0.4f;
                bool changed = s.compressionFormat != AudioCompressionFormat.Vorbis || !Mathf.Approximately(s.quality, quality)
                               || importer.forceToMono == music;
                if (!changed) continue;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = quality;
                s.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                importer.defaultSampleSettings = s;
                importer.forceToMono = !music;
                importer.SaveAndReimport();
            }
        }

        static long DirectorySize(string dir)
        {
            if (!Directory.Exists(dir)) return 0;
            return new DirectoryInfo(dir).GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        }
    }
}
