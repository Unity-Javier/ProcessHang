# ProcessHang

A small Unity project for training. It reproduces **process hangs** in the Unity Editor and in Players, so you can practice spotting, diagnosing and explaining them.

Each scenario is kept small on purpose. You open a scene, press Play (or exit Play mode, or trigger a domain reload), and watch what happens to the process.

---

## Requirements

| | |
|---|---|
| **Unity** | `2022.3.49f1` (see `ProjectSettings/ProjectVersion.txt`) |
| **OS** | macOS (the native plugin in `Hang02` is a `.dylib`) |
| **CPU** | `hangingplugin.dylib` is built for **x86_64 only**. On Apple Silicon, use the Intel (Rosetta) Editor, or the plugin won't load and you'll get a `DllNotFoundException` instead of a hang. |

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
```

---

## Scenario 1: Finalizers (`Hang01`, `SampleScene`)

**Setup:** `HangWithFinalizer` creates a `ClassWithFinalizer` in `Start()` and keeps it in a public field. `ClassWithFinalizer` has a finalizer (`~ClassWithFinalizer()`).

**Key idea:** Finalizers do **not** run on the main thread. They run on the GC's **finalizer thread**, at times you don't control: after a collection, on domain reload (entering or exiting Play mode, script recompiles), or at shutdown. If one finalizer blocks, the finalizer thread stops. Anything waiting on that thread stops too, and that can include the Editor itself.

**Exercise:**

1. Open `SampleScene` and press Play. You should see `Created class with finalizer` in the Console. Nothing bad happens yet.
2. In `ClassWithFinalizer.cs`, uncomment the body of the finalizer. It does a blocking HTTP call (`GetStringAsync(...).Result`) inside the finalizer.
3. Enter and exit Play mode, or force a recompile, and watch the Editor.
4. Discuss:
   - Which thread is the finalizer running on?
   - Why is `.Result` (sync-over-async) in a finalizer especially dangerous?
   - What is the Editor waiting for during the domain reload?

**Takeaway:** Keep finalizers short, non-blocking and free of I/O. Use `IDisposable` for deterministic cleanup, or `OnDestroy` / `OnDisable` for Unity objects.

---

## Scenario 2: Native code on a background task (`Hang02`, `SerializationHang`)

**Setup:** `ExamplePlugin` (in `HangAsync.cs`) calls `Hang()` from `hangingplugin.dylib` inside `Task.Run(...)` and `await`s it from `async void Start()`.

`Hang()` is just an infinite loop in native code:

```asm
_Hang:
    pushq %rbp
    movq  %rsp, %rbp
    jmp   .            ; spin forever
```

**Key idea:** The main thread stays responsive at first, because the work is on a thread-pool thread. But managed code **cannot interrupt a thread that is stuck in native code**. When Unity later needs to stop all managed threads, for example on a domain reload (exiting Play mode, recompiling scripts) or on quit, it waits for a thread that never comes back.

**Exercise:**

1. Open `SerializationHang` and press Play. You'll see `Process will hang` in the Console. The Editor still looks fine.
2. Exit Play mode, or edit any script to trigger a recompile.
3. Observe the Editor. Then take a sample of the process (see below) and find:
   - the thread spinning inside `Hang`
   - the main thread and what it's waiting on
4. Discuss:
   - Why did the hang show up *later*, and not when the task started?
   - How would you design a native API so long-running work can be cancelled (a cancel flag, a timeout, a cooperative check)?

**Takeaway:** Long-running native calls need a way out. Pass in a cancellation flag the native side checks, stop the work in `OnDisable` / `OnApplicationQuit`, and avoid `async void` for work whose lifetime you need to control.

---

## Investigating a hang (macOS)

When the Editor stops responding, collect evidence before you kill it:

| What | How |
|---|---|
| **Editor log** | `~/Library/Logs/Unity/Editor.log`. Check the last lines before the freeze. |
| **Process sample** | Activity Monitor → select *Unity* → ⚙︎ → **Sample Process**, or `sample <pid> 10 -file hang.txt` |
| **All thread stacks** | `lldb -p <pid>` then `thread backtrace all` |
| **CPU usage** | A thread at 100% CPU usually means a spin (Scenario 2). An idle process usually means a deadlock or a blocking wait (Scenario 1). |

Find the Unity PID with `pgrep -fl Unity`.

---

## Notes

- This project is **for training only**. The code is broken on purpose. Don't copy these patterns into production.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` and generated `Assets/Content/` are git-ignored. Unity regenerates them on first open.
