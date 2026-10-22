# Changelog

All notable changes to this package are documented in this file.

## [0.0.1] - 2026-10-22

### Added
- Component icons on every row; click an icon to enable or disable that component. Missing scripts show a warning icon.
- Floating component windows: Ctrl/Cmd+click a component icon to edit that component on its own; pin or drag to keep it open.
- Active toggle on every row that also works on the whole selection.
- Smart row icons that show the object's main component instead of the default cube.
- Folder icons for empty objects that only group other objects.
- Tree lines connecting children to their parents, and zebra stripes.
- Style palette: Alt+click a row to pick its color and icon. Hold Alt while picking a color to color the children too.
- Gradient or solid row colors and custom row icons, saved in ProjectSettings and shared through version control.
- Custom palette colors: add your own with the + button, remove them with a right-click.
- Duplicated and pasted objects keep their row colors and icons.
- Section headers for objects whose name starts with `---`, plus menu items that create a header or a folder.
- Toolbar buttons to create a folder or a header and to collapse every object; the search field folds into an icon.
- Isolate shortcut: press Shift+E over a row to collapse everything else and open the path down to it.
- Default parent shortcut: press D over a row to make it the scene's default parent; the row shows a Default parent label.
- Preferences page to turn each feature on or off.
