using System;
using System.Collections;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnitySceneRepacker;

namespace ExampleMod.Source;

[BepInAutoPlugin("io.github.yourgithubusername.examplemod")]
public partial class ExampleModPlugin : BaseUnityPlugin {
    private Harmony harmony = null!;

    private ConfigEntry<KeyboardShortcut> spawnShortcut = null!;

    private RepackedAssets? hollowKnightAssets;
    private RepackedAsset? tiktikAsset;
    private Coroutine? processing;

    private AsyncOperationHandle<GameObject> tiktikPrefab;

    private void Awake() {
        Log.Init(Logger);
        Log.Info($"Plugin {Name} ({Id}) has loaded!");

        try {
            harmony = Harmony.CreateAndPatchAll(GetType().Assembly);
            spawnShortcut = Config.Bind("Shortcuts", "Spawn Tiktik", new KeyboardShortcut(KeyCode.F2));

            hollowKnightAssets = new RepackedAssets(
                NextToThisGame("Hollow Knight"),
                Path.Combine(Paths.CachePath, "unity-scene-repacker"),
                "examplemod"
            );
            tiktikAsset = hollowKnightAssets.Request("Tutorial_01", "_Enemies/Crawler 2");
        } catch (Exception e) {
            Log.Info($"Plugin {Name} ({Id}) failed to initialize: {e}");
        }
    }

    private static string NextToThisGame(string installDirName) {
        var installDir = Path.GetDirectoryName(Application.dataPath)!;
        // on macOS, the data directory is <install>/<game>.app/Contents
        if (installDir.EndsWith(".app", StringComparison.OrdinalIgnoreCase)) installDir = Path.GetDirectoryName(installDir)!;
        return Path.Combine(Path.GetDirectoryName(installDir)!, installDirName);
    }

    private void Start() {
        if (hollowKnightAssets != null) processing = StartCoroutine(hollowKnightAssets.ProcessRequests());
    }

    private void Update() {
        if (spawnShortcut.Value.IsDown()) StartCoroutine(SpawnTiktik());
    }

    private IEnumerator SpawnTiktik() {
        if (!HeroController.instance) yield break;
        
        Log.Info("Spawning tiktik");

        yield return processing;
        if (!tiktikPrefab.IsValid()) tiktikPrefab = tiktikAsset!.Load();
        yield return tiktikPrefab;

        Instantiate(tiktikPrefab.Result, HeroController.instance.transform.position, Quaternion.identity);
    }

    private void OnDestroy() {
        // Clean up everything, in order to support hot reloading

        try {
            harmony.UnpatchSelf();
            hollowKnightAssets?.Dispose();
        } catch (Exception e) {
            Log.Info($"Plugin {Name} ({Id}) failed to clean up: {e}");
        }

        Log.Info($"Plugin {Name} ({Id}) has been unloaded!");
    }
}
