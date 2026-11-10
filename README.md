# ProcessHang

A small Unity project for training. It reproduces **process hangs** in the Unity Editor and in Players, so you can practice spotting, diagnosing and explaining them.

Each scenario is kept small on purpose. You open a scene, press Play (or exit Play mode, or trigger a domain reload), and watch what happens to the process.

---

## Requirements

| | |
|---|---|
| **Unity** | `2022.3.49f1` (see `ProjectSettings/ProjectVersion.txt`) |
| **OS** | macOS (the native plugin in `Hang02` is a `.dylib`) |
| **CPU** | `hangingplugin.dylib` is a universal binary (arm64 + x86_64), so it works on both Apple Silicon and Intel Macs. |

> ⚠️ **Heads-up:** some of these scenarios freeze the Editor on purpose. Save your work in other projects first, and expect to force-quit Unity (`⌘⌥⎋` or `kill -9 <pid>`).

---

## Project layout

```
Assets/
├── Hang01/                     # Scenario 1 – finalizers
│   ├── ClassWithFinalizer.cs   # Plain C# class with a ~finalizer
│   └── HangWithFinalizer.cs    # MonoBehaviour that holds an instance in a public field
├── Hang02/                     # Scenario 2 – native code on a background thread
│   ├── HangAsync.cs            # MonoBehaviour (ExamplePlugin) that calls into the plugin via Task.Run
│   └── hangingplugin.dylib     # Native plugin: HelloFromCpp() and Hang()
└── Scenes/
    ├── SampleScene.unity       # Cube + HangWithFinalizer   → Scenario 1
    └── SerializationHang.unity # Capsule + ExamplePlugin    → Scenario 2
NativePlugin/                   # Source for hangingplugin.dylib (outside Assets, not imported)
├── hangingplugin.cpp
└── build.sh                    # Rebuilds the universal dylib into Assets/Hang02
```

---

## Scenario 1: Finalizers (`Hang01`, `SampleScene`)

**Setup:** `HangWithFinalizer` creates a `ClassWithFinalizer` in `Start()` and keeps it in a public field. `ClassWithFinalizer` has a finalizer (`~ClassWithFinalizer()`).

**Key idea:** Finalizers do **not** run on the main thread. They run on the GC's **finalizer thread**, at times you don't control: after a collection, on domain reload (entering or exiting Play mode, script recompiles), or at shutdown. If one finalizer blocks, the finalizer thread stops. Anything waiting on that thread stops too, and that can include the Editor itself.

---

## Scenario 2: Native code on a background task (`Hang02`, `SerializationHang`)

**Setup:** `ExamplePlugin` (in `HangAsync.cs`) calls `Hang()` from `hangingplugin.dylib` inside `Task.Run(...)` and `await`s it from `async void Start()`.

`Hang()` is just an infinite loop in native code (see `NativePlugin/hangingplugin.cpp`):

```cpp
void Hang()
{
    volatile bool keepSpinning = true;
    while (keepSpinning)
    {
    }
}
```

To rebuild the plugin, run `NativePlugin/build.sh`. You need the Xcode command line tools.

**Key idea:** The main thread stays responsive at first, because the work is on a thread-pool thread. But managed code **cannot interrupt a thread that is stuck in native code**. When Unity later needs to stop all managed threads, for example on a domain reload (exiting Play mode, recompiling scripts) or on quit, it waits for a thread that never comes back.

---

## Notes

- This project is **for training only**. The code is broken on purpose. Don't copy these patterns into production.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` and generated `Assets/Content/` are git-ignored. Unity regenerates them on first open.
