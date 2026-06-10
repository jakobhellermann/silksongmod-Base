# Silksong Mod Development

## Decompilation

Full game decompilation at `../../game/decomp/`. Always check there before guessing at types, method signatures, or
field names.

## Hot Reload

`dotnet build` copies the DLL into `BepInEx/scripts/` — BepInEx ScriptEngine picks it up and reloads the plugin
automatically. No game restart needed.

## Logs

BepInEx log: `/home/jakob/.config/r2modmanPlus-local/HollowKnightSilksong/profiles/Default/BepInEx/LogOutput.log`

Write logs with `Log.Info(...)` (wrapper around BepInEx logger, initialized in plugin Awake). When something doesn't
work, add log lines first, reproduce, then read the log. Don't guess.

## SilksongPlayground Dev Server

The mod runs an HTTP server on `localhost:8742`. Use `curl` to call it. Extend it whenever a new debug action would be
useful — adding a route is cheap. Existing endpoints:

```
GET /scene-tree                          — full GameObject hierarchy as JSON
GET /screenshot                          — PNG screenshot
GET /inspect?path=GO/path&component=Foo  — dump all fields/properties of a component
GET /set-active?name=Foo&active=true     — toggle GameObjects by name (contains match)
GET /set-field?path=...&component=...&field=...&value=...
GET /invoke?path=...&component=...&method=...
GET /teleport-scene?scene=SceneName
GET /quit-to-menu
GET /map-borders                         — reinstall MapRoomBorders
GET /black-sprites?nameContains=sc_floor
```

`path` is always the full hierarchy path, e.g. `Game_Map_Hornet(Clone)/Abyss/room_01`.

## Harmony Patches — Private Methods

Harmonizer validates patch targets at compile time and emits HARMONIZE001 for private methods it can't resolve. Suppress it on the method declaration:

```csharp
[HarmonyPrefix]
[HarmonyPatch(typeof(Foo), "PrivateMethod", typeof(ArgType))]
#pragma warning disable HARMONIZE001
private static void Patch(Foo __instance) { ... }
#pragma warning restore HARMONIZE001
```

The patch still fires correctly at runtime.

## Harmony Patches

Group patches by feature in one class:

```csharp
[HarmonyPatch]
public class LateTransitionFacing {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.FadeSceneIn))]
    private static void FadeSceneIn(GameManager __instance) { ... }
}
```

## BepInEx Config

```csharp
internal static ConfigEntry<bool> MyFlag = null!;
// in Awake:
MyFlag = Config.Bind("Section", "Key", defaultValue);
```

Values are persisted to `BepInEx/config/io.github.jakobhellermann.silksongplayground.cfg` and survive restarts.

## Unity Lifecycle Methods

`Update`, `Awake`, `OnEnable`, `OnGUI`, `OnPostRender` etc. silently swallow exceptions unless you catch them. Always
wrap the body:

```csharp
private void Update() {
    try {
        // ...
    } catch (Exception e) { Log.Error(e); }
}
```

## Working Style

- Never trust hypotheses blindly — verify with logs or HTTP endpoints first.
- Work incrementally: get something visible, then build on it.
- When a patch doesn't fire as expected, search the decomp for the actual caller and patch that instead.

## Unity Canvas / UI

- `Canvas.GetComponent<RectTransform>().rect` is `(0,0)` in `Awake` — the layout runs after the first frame. Never cache canvas dimensions in `Awake`; compute them lazily on first use.
- Use stretch-anchors (`anchorMin=0,0` / `anchorMax=1,1`, `sizeDelta=0`) instead of setting `sizeDelta = Screen.width/height`. `Screen` coordinates and canvas coordinates diverge with `CanvasScaler`.
- For a full-screen overlay text widget: `anchorMin=Vector2.zero`, `anchorMax=Vector2.one`, `offsetMin/offsetMax` for padding, `TextAnchor.LowerRight` to pin text to the bottom-right.

## Keeping This File Up To Date

Update this file at the end of every session and whenever a recurring problem or new insight comes up. The goal is to
fix root causes and improve the process, not just patch symptoms. If something was confusing, hard to debug, or took
multiple attempts — write down why and what to do differently next time.
