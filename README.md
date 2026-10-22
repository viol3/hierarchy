# Viol3's Hierarchy

A cleaner, more useful Hierarchy window. See what every object holds at a glance, toggle objects and components without opening the Inspector, and keep big scenes organized with colors, icons, folders and headers.

> **Early development:** This package is at an early stage (v0.0.1), so you may run into bugs or rough edges. If something breaks, behaves unexpectedly or a feature is missing, please [open an issue](https://github.com/viol3/hierarchy/issues). Your Unity version and steps to reproduce help a lot. Feedback and suggestions are very welcome.

## Features

**See more at a glance**
- **Component icons:** every row lists its components on the right. Disabled components are dimmed and missing scripts show a warning icon
- **Smart row icons:** the cube in front of a name becomes the object's main component, so cameras, lights, canvases and audio sources stand out
- **Folder icons:** empty objects that only group other objects get a folder icon
- **Tree lines** connect children to their parents, and **zebra stripes** shade every other row

**Organize your scenes**
- **Row colors:** tint any row with a soft gradient or a solid fill. Hold Alt to color a whole branch at once
- **Custom icons:** give any row an icon from a palette of Unity's built-in icons
- **Custom palette colors:** add your own colors with Unity's color picker
- **Headers:** name an object `--- Environment` and it becomes a section header
- **Shared with your team:** colors and icons are saved in `ProjectSettings` and travel through version control
- **Copies keep their style:** duplicated and pasted objects bring their colors and icons along

**Work faster**
- **Toggles:** turn objects and components on and off right in the Hierarchy, with undo
- **Component windows:** edit a single component in a small floating Inspector without changing your selection
- **Toolbar:** create folders and headers, collapse everything, and search from a compact toolbar
- **Isolate:** Shift+E collapses everything except the object under the mouse
- **Default parent:** D makes the object under the mouse the place where dragged-in prefabs land

Works in Prefab Mode and with multiple loaded scenes. Every feature can be turned off in Preferences.

## Installation

### From a Git URL

1. Open **Window > Package Manager**.
2. Click **+** and choose **Add package from git URL...**
3. Enter:

```
https://github.com/viol3/hierarchy.git
```

### From disk

Clone or download this repository, then in the Package Manager choose **+ > Add package from disk...** and select `package.json`.

## Quick reference

Everything works as soon as the package is installed.

| Action | Where | What it does |
| --- | --- | --- |
| Click | Checkbox at the right of a row | Turns the object on or off (the whole selection if the row is selected) |
| Click | Component icon | Enables or disables that component |
| Ctrl+click (Cmd+click on macOS) | Component icon | Opens the component in a floating Inspector |
| Alt+click | Row | Opens the style palette to pick a color and an icon |
| Alt + pick a color | Style palette | Colors the row and all its children |
| Right-click | Custom color in the palette | Removes that color |
| Shift+E | Mouse over a row | Isolates the row: everything else collapses |
| D | Mouse over a row | Makes the row the default parent, or clears it |
| Ctrl+D, Ctrl+C / Ctrl+V | Selected rows | Copies keep their colors and icons |

Shift+E and D need the Hierarchy to have keyboard focus, so click into it first.

## Usage

### Toggling objects and components

Click the **checkbox** at the right of a row to turn that object on or off. If the row is selected, every selected object is toggled.

Click a **component icon** to enable or disable that component; hover it to see the component's name. **Ctrl+click** (Cmd+click on macOS) opens the component in a small floating Inspector instead, without changing your selection. Components that have no enabled checkbox, like Mesh Filter, open with a plain click. The window closes when you click elsewhere; click its lock button or drag it by the header to keep it open, and press Esc to close it.

### Row colors and icons

**Alt+click** a row to open the style palette. If the row is selected, the whole selection is styled.

- **Colors:** pick a color to tint the row. Hold **Alt** while picking to color the children too: every child without a color of its own gets a dimmer version, including children you add later. **✕** removes the color.
- **Your own colors:** click **+** after the colors to open Unity's color picker. The rows preview the color while you pick, and it joins the palette when the picker closes; Esc cancels. Right-click a custom color to remove it.
- **Icons:** pick an icon to replace the one in front of the name. The icon row's **✕** brings back the automatic icon.

Duplicating or copying and pasting styled objects carries their colors and icons over to the copies, children included. Row colors are also in the right-click menu under **Viol3's Hierarchy > Color**.

Styles and custom colors are stored in `ProjectSettings/Viol3Hierarchy.asset`; commit that file to share them with your team. A scene has to be saved before its objects can be styled, and row styles are not available in Prefab Mode yet.

### Row icons

Unless you pick an icon yourself, the icon in front of a name shows the object's main component: the first one that isn't a Transform, Mesh Filter, Mesh Renderer, Collider or Rigidbody. Empty objects that only group children show a folder, and prefab instances keep Unity's prefab icon.

### Headers

Any object whose name starts with `---` is drawn as a section header: `--- Lighting` shows as **LIGHTING**. A header's row color becomes its background.

Create one with the toolbar's header button or by right-clicking and choosing **Viol3's Hierarchy > Create Header**. It is placed above the selected object and tagged `EditorOnly`, so it is stripped from builds.

### Toolbar

The Hierarchy toolbar keeps Unity's **+** menu and adds these buttons on the right:

- **Folder:** creates an empty object with a folder icon below the selected object, ready to rename
- **Header:** creates a section header above the selected object, ready to rename
- **Collapse:** folds every object back in, leaving only the scenes open
- **Search:** brings Unity's search field back; it folds away again once the search is cleared

### Isolate

Hover a row and press **Shift+E** to focus on it. Every other object collapses and the path down to the row opens up. The row itself is expanded, and its children keep the state they had.

### Default parent

Hover a row and press **D** to make it the default parent of its scene: prefabs and objects you drag into the scene are placed under it. The row shows its name in bold with a **Default parent** label. Press **D** over it again to clear it.

This is Unity's own default parent, so one set from Unity's context menu shows the same way, and like Unity's it lasts for the editor session.

## Settings

Open **Edit > Preferences > Viol3's Hierarchy** (on macOS **Unity > Settings > Viol3's Hierarchy**) to:

- Turn the whole package or each feature on or off
- Switch row colors between gradient and solid
- Change the header prefix (default `---`)
- See how many objects are styled and clear all row colors and icons

These settings are per user and stored in EditorPrefs.

## Requirements

- Unity 2021.3 or newer
- Editor only. Nothing is added to your builds

The toolbar, collapsing, isolating and the custom color picker use Unity editor internals that have no public API. They have been checked against Unity 6000.0. If a Unity version changes those internals, the affected feature turns itself off or logs a warning instead of breaking the Hierarchy.

## Reporting issues

Please [open an issue](https://github.com/viol3/hierarchy/issues) and include:

- Your Unity version
- Steps to reproduce
- Any errors from the Console

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## License

Released under the [MIT License](LICENSE.md).
