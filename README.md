# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# UnityEssentials – ResourceLoader

A tiny helper to load assets in a project that may use **Addressables** and/or **Resources**.

All APIs are under the `UnityEssentials` namespace:

```csharp
using UnityEssentials;
```

## API

There are only two public methods:

- `ResourceLoader.TryGet<T>(keyOrPath, cacheResource = false, tryResourcesFallback = true)`
- `ResourceLoader.InstantiatePrefab(keyOrPath, instantiatedName = null, parent = null, tryResourcesFallback = true)`

Both methods always try **Addressables first** (when `UNITY_ADDRESSABLES` is defined), and only fall back to `Resources` if `tryResourcesFallback` is `true`.

> Note: Addressables loading is currently synchronous (`WaitForCompletion`). If you need async variants, we can add them.

## Examples

Load a USS / UXML / any asset:

```csharp
var style = ResourceLoader.TryGet<StyleSheet>("UI/Styles/Main");
```

Load and cache:

```csharp
var icon = ResourceLoader.TryGet<Texture2D>("Icons/MyIcon", cacheResource: true);
```

Instantiate a prefab:

```csharp
var hud = ResourceLoader.InstantiatePrefab("Prefabs/HUD", "HUD", parentTransform);
```

Addressables-only behavior (disable `Resources` fallback):

```csharp
var hud = ResourceLoader.InstantiatePrefab("Prefabs/HUD", tryResourcesFallback: false);
```

## Notes

- `keyOrPath` can be either:
  - an Addressables key/address (when Addressables are enabled), or
  - a Resources path relative to a `Resources/` folder (without extension) if fallback is enabled.
- Caching only applies to `TryGet<T>`.
