using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;

namespace CatnipCart.Editor
{
    public class WebGLBuilder
    {
        /// <summary>
        /// Path to the GitHub Pages deployment repo (CatnipKart).
        /// Build outputs go directly here so you only need to push.
        /// </summary>
        static readonly string DeployPath = @"C:\Users\kyanc\My project (3)\docs";

        [MenuItem("Catnip Cart/Build WebGL + Deploy")]
        public static void BuildAndDeploy()
        {
            if (BuildWebGL())
            {
                PushToGitHubPages();
            }
        }

        [MenuItem("Catnip Cart/Build WebGL (Local Only)")]
        public static void BuildOnly()
        {
            BuildWebGL();
        }

        static bool BuildWebGL()
        {
            UnityEngine.Debug.Log("🐾 Starting WebGL Build...");

            // Always ensure shader materials exist and regenerate scene
            // so it references them — prevents texture variant stripping
            string scenePath = "Assets/_CatnipCart/Scenes/CatnipGardens.unity";
            AutoSceneSetup.CreateRaceScene();

            // Clean output directory first to remove obsolete uncompressed build files
            string buildDir = Path.Combine(DeployPath, "Build");
            if (Directory.Exists(buildDir))
            {
                try { Directory.Delete(buildDir, true); }
                catch (System.Exception ex) { UnityEngine.Debug.LogWarning($"Could not clean build dir: {ex.Message}"); }
            }

            // Set up player settings for WebGL
            // Enable Brotli compression and decompression fallback so it works seamlessly on GitHub Pages
            // while reducing transfer sizes by ~80%!
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            
            // Build directly to the GitHub Pages repo docs/ folder
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { scenePath };
            buildPlayerOptions.locationPathName = DeployPath;
            buildPlayerOptions.target = BuildTarget.WebGL;
            buildPlayerOptions.options = BuildOptions.None;

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            
            UnityEngine.Debug.Log($"Build ended with result: {report.summary.result}");
            
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                UnityEngine.Debug.Log("✅ WebGL Build completed successfully!");
                // Replace the default index.html with our custom loading screen
                InjectCustomLoadingScreen();
                return true;
            }
            else
            {
                UnityEngine.Debug.LogError("❌ WebGL Build failed!");
                return false;
            }
        }

        /// <summary>
        /// Auto-commit and push the build to the CatnipKart GitHub Pages repo.
        /// </summary>
        static void PushToGitHubPages()
        {
            string repoPath = Path.GetDirectoryName(DeployPath); // "My project (3)"
            UnityEngine.Debug.Log($"🚀 Pushing build to GitHub Pages from: {repoPath}");

            try
            {
                RunGit(repoPath, "add docs/");
                RunGit(repoPath, "commit -m \"Update WebGL build\"");
                RunGit(repoPath, "push");
                UnityEngine.Debug.Log("✅ Deployed to GitHub Pages! Site will update in ~1 minute.");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Git push failed: {ex.Message}");
            }
        }

        static void RunGit(string workDir, string args)
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", args)
            {
                WorkingDirectory = workDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = System.Diagnostics.Process.Start(psi);
            proc.WaitForExit(60000);
            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            if (proc.ExitCode != 0)
                throw new System.Exception($"git {args} failed: {error}");
            if (!string.IsNullOrEmpty(output))
                UnityEngine.Debug.Log(output.Trim());
        }

        /// <summary>
        /// After the build, replace the generated index.html with our custom loading
        /// screen. We scan the Build/ folder for the actual filenames Unity generated
        /// and inject them into our template.
        /// </summary>
        static void InjectCustomLoadingScreen()
        {
            string docsPath = DeployPath;
            string buildDir = Path.Combine(docsPath, "Build");

            if (!Directory.Exists(buildDir))
            {
                Debug.LogWarning("docs/Build/ not found — skipping custom loading screen.");
                return;
            }

            // Find the build artifact filenames
            var files = Directory.GetFiles(buildDir).Select(Path.GetFileName).ToArray();
            string loaderFile = files.FirstOrDefault(f => f.EndsWith(".loader.js"));
            string dataFile = files.FirstOrDefault(f => f.EndsWith(".data") || f.EndsWith(".data.br") || f.EndsWith(".data.gz") || f.EndsWith(".data.unityweb"));
            string frameworkFile = files.FirstOrDefault(f => f.EndsWith(".framework.js") || f.EndsWith(".framework.js.br") || f.EndsWith(".framework.js.gz") || f.EndsWith(".framework.js.unityweb"));
            string codeFile = files.FirstOrDefault(f => f.EndsWith(".wasm") || f.EndsWith(".wasm.br") || f.EndsWith(".wasm.gz") || f.EndsWith(".wasm.unityweb"));

            if (loaderFile == null || dataFile == null || frameworkFile == null)
            {
                Debug.LogWarning($"Could not identify build files in {buildDir}. Files found: {string.Join(", ", files)}");
                return;
            }

            // Copy logo to docs/
            string logoSrc = Path.Combine(Application.dataPath, "WebGLTemplates", "CatnipCart", "logo.png");
            string logoDst = Path.Combine(docsPath, "logo.png");
            if (File.Exists(logoSrc))
                File.Copy(logoSrc, logoDst, true);

            // Generate the custom index.html with real filenames
            string html = GenerateLoadingPage(loaderFile, dataFile, frameworkFile, codeFile);
            string indexPath = Path.Combine(docsPath, "index.html");
            File.WriteAllText(indexPath, html);

            Debug.Log("🐾 Custom Catnip Cart loading screen injected!");
        }

        static string GenerateLoadingPage(string loader, string data, string framework, string code)
        {
            string codeBlock = code != null
                ? $"codeUrl: buildUrl + \"/{code}\","
                : "";

            return $@"<!DOCTYPE html>
<html lang=""en-us"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>Catnip Cart</title>
  <meta name=""description"" content=""Catnip Cart — a cat-themed kart racing game! Race as adorable cats on wild tracks."">
  <style>
    @import url('https://fonts.googleapis.com/css2?family=Outfit:wght@400;700;900&display=swap');
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    html, body {{
      width: 100%; height: 100%; overflow: hidden;
      background: #0a0a12; font-family: 'Outfit', sans-serif;
    }}
    #unity-canvas {{
      width: 100%; height: 100%; display: block; background: #0a0a12;
    }}
    #loading-overlay {{
      position: fixed; inset: 0; z-index: 100;
      display: flex; flex-direction: column; align-items: center; justify-content: center;
      background: radial-gradient(ellipse at 50% 40%, #1a1430 0%, #0a0a12 70%);
      transition: opacity 0.8s ease, visibility 0.8s ease;
    }}
    #loading-overlay.hidden {{
      opacity: 0; visibility: hidden; pointer-events: none;
    }}
    #loading-overlay::before {{
      content: ''; position: absolute; inset: 0;
      background-image:
        radial-gradient(2px 2px at 20% 30%, rgba(255,180,50,0.3) 0%, transparent 100%),
        radial-gradient(2px 2px at 60% 70%, rgba(255,120,200,0.2) 0%, transparent 100%),
        radial-gradient(1px 1px at 80% 20%, rgba(120,200,255,0.3) 0%, transparent 100%),
        radial-gradient(1px 1px at 40% 80%, rgba(255,220,100,0.2) 0%, transparent 100%),
        radial-gradient(2px 2px at 10% 60%, rgba(180,100,255,0.15) 0%, transparent 100%),
        radial-gradient(1px 1px at 90% 50%, rgba(100,255,180,0.2) 0%, transparent 100%);
      animation: sparkle 4s ease-in-out infinite alternate;
    }}
    @keyframes sparkle {{
      0% {{ opacity: 0.4; transform: scale(1); }}
      100% {{ opacity: 1; transform: scale(1.05); }}
    }}
    .logo-container {{
      position: relative; margin-bottom: 36px;
      animation: float 3s ease-in-out infinite;
    }}
    @keyframes float {{
      0%, 100% {{ transform: translateY(0); }}
      50% {{ transform: translateY(-12px); }}
    }}
    .logo-glow {{
      position: absolute; inset: -30px; border-radius: 50%;
      background: radial-gradient(circle, rgba(255,160,40,0.25) 0%, transparent 70%);
      animation: pulse-glow 2s ease-in-out infinite;
    }}
    @keyframes pulse-glow {{
      0%, 100% {{ transform: scale(1); opacity: 0.6; }}
      50% {{ transform: scale(1.2); opacity: 1; }}
    }}
    .logo-img {{
      width: 140px; height: 140px; object-fit: contain; position: relative;
      filter: drop-shadow(0 0 20px rgba(255,160,40,0.5));
    }}
    .game-title {{
      font-size: 48px; font-weight: 900; letter-spacing: 2px;
      background: linear-gradient(135deg, #ffb840 0%, #ff6b9d 50%, #c084fc 100%);
      -webkit-background-clip: text; -webkit-text-fill-color: transparent;
      background-clip: text; margin-bottom: 8px; text-transform: uppercase;
      filter: drop-shadow(0 2px 12px rgba(255,107,157,0.3));
    }}
    .game-subtitle {{
      font-size: 14px; font-weight: 400; color: rgba(255,255,255,0.4);
      letter-spacing: 6px; text-transform: uppercase; margin-bottom: 48px;
    }}
    .progress-container {{ width: min(360px, 80vw); position: relative; }}
    .progress-track {{
      width: 100%; height: 6px; background: rgba(255,255,255,0.08);
      border-radius: 3px; overflow: hidden; position: relative;
    }}
    .progress-fill {{
      height: 100%; width: 0%; border-radius: 3px;
      background: linear-gradient(90deg, #ffb840, #ff6b9d, #c084fc);
      background-size: 200% 100%;
      animation: shimmer 2s linear infinite;
      transition: width 0.3s ease-out; position: relative;
    }}
    .progress-fill::after {{
      content: ''; position: absolute; right: 0; top: -3px;
      width: 12px; height: 12px; border-radius: 50%; background: #fff;
      box-shadow: 0 0 16px rgba(255,160,40,0.8), 0 0 32px rgba(255,107,157,0.4);
    }}
    @keyframes shimmer {{
      0% {{ background-position: 200% 0; }}
      100% {{ background-position: -200% 0; }}
    }}
    .progress-text {{
      margin-top: 16px; font-size: 13px; color: rgba(255,255,255,0.35);
      text-align: center; letter-spacing: 3px; text-transform: uppercase;
    }}
    .paw-prints {{
      position: absolute; bottom: 60px; display: flex; gap: 28px; opacity: 0.15;
    }}
    .paw {{ font-size: 18px; animation: paw-step 1.6s ease-in-out infinite; }}
    .paw:nth-child(2) {{ animation-delay: 0.2s; }}
    .paw:nth-child(3) {{ animation-delay: 0.4s; }}
    .paw:nth-child(4) {{ animation-delay: 0.6s; }}
    .paw:nth-child(5) {{ animation-delay: 0.8s; }}
    @keyframes paw-step {{
      0%, 100% {{ opacity: 0.1; transform: translateY(0) scale(0.8); }}
      50% {{ opacity: 0.4; transform: translateY(-6px) scale(1); }}
    }}
  </style>
</head>
<body>
  <div id=""unity-container"">
    <canvas id=""unity-canvas"" tabindex=""-1""></canvas>
  </div>
  <div id=""loading-overlay"">
    <div class=""logo-container"">
      <div class=""logo-glow""></div>
      <img class=""logo-img"" src=""logo.png"" alt=""Catnip Cart"">
    </div>
    <div class=""game-title"">Catnip Cart</div>
    <div class=""game-subtitle"">Ready, Set, Purr!</div>
    <div class=""progress-container"">
      <div class=""progress-track"">
        <div class=""progress-fill"" id=""progress-fill""></div>
      </div>
      <div class=""progress-text"" id=""progress-text"">Loading...</div>
    </div>
    <div class=""paw-prints"">
      <span class=""paw"">🐾</span>
      <span class=""paw"">🐾</span>
      <span class=""paw"">🐾</span>
      <span class=""paw"">🐾</span>
      <span class=""paw"">🐾</span>
    </div>
  </div>
  <script>
    var buildUrl = ""Build"";
    var config = {{
      dataUrl: buildUrl + ""/{data}"",
      frameworkUrl: buildUrl + ""/{framework}"",
      {codeBlock}
      streamingAssetsUrl: ""StreamingAssets"",
      companyName: ""CatnipRealms"",
      productName: ""Catnip Cart"",
      productVersion: ""1.0"",
    }};
    var pf = document.getElementById(""progress-fill"");
    var pt = document.getElementById(""progress-text"");
    var ov = document.getElementById(""loading-overlay"");
    var cv = document.querySelector(""#unity-canvas"");
    function resize() {{ cv.width = window.innerWidth; cv.height = window.innerHeight; }}
    resize(); window.addEventListener(""resize"", resize);
    var s = document.createElement(""script"");
    s.src = buildUrl + ""/{loader}"";
    s.onload = function() {{
      createUnityInstance(cv, config, function(p) {{
        var pct = Math.round(p * 100);
        pf.style.width = pct + ""%"";
        if (pct < 30) pt.textContent = ""Herding cats..."";
        else if (pct < 60) pt.textContent = ""Tuning engines..."";
        else if (pct < 90) pt.textContent = ""Scattering catnip..."";
        else pt.textContent = ""Almost there..."";
      }}).then(function(inst) {{
        pf.style.width = ""100%"";
        pt.textContent = ""Let's race!"";
        setTimeout(function() {{ ov.classList.add(""hidden""); }}, 600);
      }}).catch(function(msg) {{ alert(msg); }});
    }};
    document.body.appendChild(s);
  </script>
</body>
</html>";
        }
    }
}
