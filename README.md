# Trinity Mod Loader
Trinity Mod Loader is a small utility to make and manage mods, while also providing file extraction utilities.

## Initial Usage
Trinity Mod Loader requires the oo2core_8_win64.dll and an up-to-date GFPAKHashCache.bin.
On initial boot it will ask for your romfs path, once specified the tool will load.

## Adding a Mod
1. Add as many mod archive files using the "Add Mod" button. (make sure the contents represent how they would on the romfs)
2. When you're done adding mods, simply hit the "Apply Mods" button. This will generate files either in a LayeredFS folder in the same directory as the program, or a directory of your choice if you set an output directory.
3. Drag and drop the generated files in the output folder into your atmosphere layeredfs directory or your emulator of choice's mod folder.

## Creating a Mod
1. Extract files to modify using ``View > Tree View``.
2. Mark the modified files by right clicking on them to modify your trpfd for testing.
3. Once you've tested your mod, zip the modified files in their place in romfs and ship it!

Note: We'll be adding a way to add some mod metadata to keep better track of your mods soon.

## Source Code
The canonical repository for this tool and the GFTool.Core which provies serializers for Trinity files can be found at [https://github.com/pkZukan/gftool/](https://github.com/pkZukan/gftool/).

## Feature Suggestions
Please discuss feature suggestions on the [pokemodding discord.](https://discord.gg/hcVusTVW) Our aim is to make a stable and user friendly tool, so please understand if your dream feature isn't developed immediately.
