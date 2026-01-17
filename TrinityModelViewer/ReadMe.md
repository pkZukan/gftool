# TrinityModelViewer Navigation (UI Guide)

Guide on how to use Trinity Model Viewer Basic Buttons and Functions

## Menus

## File Button

- **Open Button**: Clears Scene and Opens Model
- **Import Button**: Adds model to scene
- **Open GFPAK**: See LA model loading for details.
- **Recents**: Shows last 5 loaded trmdls
- **Last Model**: Quickly loads previous model loaded
- **Export Trinity**: Exports entire Model Set (trmsh, trmbf, trmdl, trskl, and trmmt/trmtr if edited)
- **Export Trinity (Edited Only)...**: Should only export the edited files

Loading Window: Shows Loading progress with cancel button (If you're fast enough to click it)
<img width="695" height="481" alt="image" src="https://github.com/user-attachments/assets/bb4dd49f-cd02-4a64-9ac7-8f79974a1848" />
## View Button

### Scene display toggles

- **Wireframe**: Displays wireframe view for scene
- **Show Skeleton**: Shows 3d model skeleton (if present)
- **Use Rare TRMTR Materials**: Displays shiny variant for pokemon
- **Enable Normal Maps**: Enables normal maps for current model
- **Enable AO**: Enables AO maps for current Model
- **Use Vertex Colors**: Toggles vertex colors
<img width="687" height="470" alt="image" src="https://github.com/user-attachments/assets/fe332370-49c1-4bd0-b92b-96b8b1eb74e7" />

### Shading

- **Lit**: Most fleshed out shader, adds lighting to most models
- **Toon**: Fun shader, not fleshed out, very basic toon lighting
- **Legacy**: Old Shader, simpler output flatter shading
<img width="686" height="474" alt="image" src="https://github.com/user-attachments/assets/91791aa3-26bf-465d-ac73-83afbf91fc30" />

### Display (GBuffer debug views)

Enables only the texture of the selected map (if present)

- **Albedo**
- **Normal**
- **Specular**
- **AO**
- **Depth**
<img width="689" height="473" alt="image" src="https://github.com/user-attachments/assets/92b82554-0bdf-42fe-ac1f-483d92028efe" />

### Shader Debug

Only works if Use backup IkCharacter shader is active

Enables only the layer of the noted name

See materials section for more info about each

<img width="690" height="477" alt="image" src="https://github.com/user-attachments/assets/8a5a7768-1ce2-4fe6-8387-2c8a44c53f85" />

### Skinning

Warning: Risky DO NOT touch if you dont know what you're doing

- **Deterministic skinning/animation** changes skinning path to not rely on heuristics (Testing)
- **Auto map blend indices**: Automatically picks the best blend-index mapping for the model (uses heuristics; helps when bones look wrong due to different index spaces)
- **Remap indices via joint info**: Tries to remap skinning indices using the model's joint-info table (fixes some models that index bones differently)
- **Use TRSKL inverse binds**: Uses inverse bind matrices from the model skeleton (TRSKL) when available (can improve pose/skin accuracy)
- **Swap blend order (WXYZ)**: Swaps the ordering of the 4 bone indices/weights in the shader (some models pack influences in a different channel order)
- **Transpose skin matrices**: Transposes bone matrices when uploading to the shader (matrix convention toggle; can fix "exploding" skinning if conventions mismatch)
- **Reload loaded models**: Re-loads currently loaded models so the skinning/animation settings above fully re-apply (recommended after changing mapping options)
<img width="689" height="472" alt="image" src="https://github.com/user-attachments/assets/14ed4f97-f00f-48ee-a1b5-009a7f5fe0fa" />

Use Backup IkCharacter Shader: A relic of rewriting the entire shader pipeline halfway through. It may produce a less "glossy" look for a lot of models which could make them look more accurate. Also is required to be turned on to use Shader Debug buttons

### Performance HUD

Shows a small perf hud with values showing frame allocation times

### Log Performance Spikes

See name on tin

### Vsync

Enables Vsync

## Settings Button

- **Enable dark mode**: Enables dark theme for the UI (unless you want to keep your eyes burned)
- **Load all LODs**: Loads every LOD in the TRMDL instead of just the default
- **Auto-generate LODs on export (temporary: duplicate LOD0)**: When exporting, duplicates LOD0 into the other LOD slots (placeholder if you want working lods without making them)
- **Export model_pc_base/p0_base.trskl on export (protag preview helper)**: Export helper for model_pc models if you want to open (only use for fun)
- **Enable debug logs**: Enables extra logging in the message window (useful for debugging missing textures/shaders/skinning) PLEASE TURN THIS ON IF YOU'RE MAKING AN ISSUE
- **Auto load animations**: Automatically searches for and loads nearby .tranm/.gfbanm animations after loading a model
- **Auto load first model when opening GFPAK**: When opening a GFPAK, automatically loads the first .trmdl found (otherwise you pick from the GFPAK browser)
- **Show multiple models at once**: Disables "solo model" behavior so newly loaded models don't hide older models (also uses all models as animation targets when an animation is playing)
- **Shader mapping (Auto detects ZA vs SCVI; GFPAK forces LA)**: Controls how TRMTR technique names map to runtime shaders (leave Auto unless you know a shader isn't using the right game source)

<img width="542" height="500" alt="image" src="https://github.com/user-attachments/assets/ea2becaf-94ff-401c-b7b6-db84d877957e" />

### Extracted game assets (fallback)

- **Use extracted game files when local assets are missing**: If a texture/animation isn't found next to the model, tries to load it from an extracted "out" directory (helps when viewing isolated files like exported models)
- **Active game root**: Picks which extracted layout to use for fallback path mapping (ZA uses ik_* roots, SV uses non-ik roots).
- **ZA out root**: Path to your extracted ZA out folder
- **SV out root**: Path to your extracted SV out folder

NOTE: This directory should be the the one that contains your ik_pokemon/pokemon or ik_chara/chara folder.

<img width="946" height="571" alt="image" src="https://github.com/user-attachments/assets/a8a0b025-bc5d-41b3-872f-6d892da3dcf9" />

## Help menu

Shows control information

<img width="296" height="224" alt="image" src="https://github.com/user-attachments/assets/ff286806-63f9-4f1a-85a6-7a8f262cb7b8" />

## Tabs

### Scene tab

Shows node tree for models

<img width="646" height="682" alt="image" src="https://github.com/user-attachments/assets/60df8429-e19d-41ca-aec7-d399c20c8717" />

### Top Model Node

Shows name of model

Right Click Actions:

- Modify: Opens glTF import/export window
- Delete: Deletes model from scene
- Hide: Hides model from scene

<img width="631" height="675" alt="image" src="https://github.com/user-attachments/assets/9e7ad251-888d-4f30-9bc0-e47f2beaab37" />

Meshes Node: Shows mesh names

### Mesh Name Node

Right click actions:

- Hide Mesh
- Assign Materials: Overrides all material sections with one material
- UV Overrides: Lets you change UV used for certain maps

<img width="647" height="670" alt="image" src="https://github.com/user-attachments/assets/1402ef22-d234-40fc-aaf3-30f6833288f1" />

### Materials Node

Selecting a material shows which verts in that mesh use that material

Right Click actions

- Hide Section
- Reassign Material...: Reassigns materials for that material group
- UV Overrides

<img width="635" height="672" alt="image" src="https://github.com/user-attachments/assets/65b7ec8b-5f1e-4402-abee-588d13274525" />

Armature node:

Shows bone name list

<img width="637" height="687" alt="image" src="https://github.com/user-attachments/assets/260ef567-5f18-4c44-ab26-bf40ae2cf98f" />

### Animations Tab

Load Button:

Adds Selected animations to list

Export Model+All Anims:

Exports model and anims to gltf

<img width="635" height="687" alt="image" src="https://github.com/user-attachments/assets/b78a0caf-9ba6-4234-939a-58610090085d" />

Animation List:

Shows a list of all animations

Double click an animation to play it

Right click an animation to export it with model or just by itself

<img width="633" height="682" alt="image" src="https://github.com/user-attachments/assets/5bab54b5-d7ee-4a7d-82cd-fb0e417bbeaf" />

- Play: Plays anim
- Pause: Pauses anim on frame it was on
- Stop: Clears anim and resets model to bind pose
- Loop: Turns non looping anims into looping ones
- Scroll bar, shows animation progress

### Json Editor Tab

Shows all binary files loaded into scene

<img width="635" height="675" alt="image" src="https://github.com/user-attachments/assets/d6fbef2e-5f66-42e8-8e58-dbbd5ac4bd61" />

Double click to open and edit json converted binary

Right Click Actions:

- Edit: opens json converted binary to text editor
- Copy path copies file path to clipboard

Refresh: Refreshes binaries in scene if they didnt all load

Add file...: Select an external binary file to edit (not actually added to scene, this just acts as a binary->json->binary editor)

<img width="632" height="674" alt="image" src="https://github.com/user-attachments/assets/ae0d8154-7238-4555-87fe-9b8c20bb8582" />

### Json Window

Shows json converted binary

Allows editing and search through ctrl+f

TRMMT and TRMTR Only

Allows edits that can apply to scene through apply button and allows for material export through export button

<img width="966" height="753" alt="image" src="https://github.com/user-attachments/assets/71b0dcfa-7322-448b-8584-ac5d48673bec" />

### Object Tab

Doesn't work, is preserved for potential future use.

<img width="635" height="722" alt="image" src="https://github.com/user-attachments/assets/5cbab544-c3ad-426f-af2f-125068f826e2" />

### Materials Tab

Export edited material... button: export any modifications made to materials (will only export modified materials)

Shows list of materials

Selecting a material highlights it

<img width="626" height="717" alt="image" src="https://github.com/user-attachments/assets/9bb79844-cdd4-4fc5-9b4d-bb254c2f362d" />

### Materials Side Tabs

#### Textures

Shows texture names and file locations slots and samplers, right clicking a material allows for import export of that texture for viewing in scene (png, jpg, bmp) NOTE: no bntx export

Texture Preview: Lets you toggle between different channels on the preview texture, and allows for channel replacement which can be written on texture export NOTE:still not bntx

<img width="948" height="711" alt="image" src="https://github.com/user-attachments/assets/678e72e5-a847-4733-9255-c310c53b0746" />

#### Samplers

Shows sampler settings used by textures in this material (index, Repeat U/V/W, border color, and raw State0-State8 values). Mostly useful for debugging wrap/filter behavior; currently read-only.

<img width="551" height="369" alt="image" src="https://github.com/user-attachments/assets/b45e7d22-783d-4ffc-a083-d3add92494eb" />

#### Params

Shows a list of parameters some of which are editable, please check materials section of your desired game in its relevant materials section

Editable Options:

- True/False for enabling/Disabling
- Base color and Base color layer swatches open up a color picker

<img width="951" height="714" alt="image" src="https://github.com/user-attachments/assets/0a15b287-750e-477b-8b31-2689737db4a7" />

#### UVs

Shows UV information

UV Texture Preview:

Shows Selected UV0/1 of selected mesh for that material

<img width="945" height="719" alt="image" src="https://github.com/user-attachments/assets/d4cffc65-4961-4288-af6d-2fb1a1962be9" />

Wrap:

Allows switching between wrap modes

<img width="949" height="712" alt="image" src="https://github.com/user-attachments/assets/33def347-9be8-482f-b8eb-aa8802271dc2" />

#### Variations

Variations, shows variations in model as dictated by trmmt

Material Set: Allows switching between material sets

Clicking the swatches on the colors allows for color switching.

<img width="954" height="721" alt="image" src="https://github.com/user-attachments/assets/0739d0c8-e871-431a-92c3-a06055cbf68b" />

## Messages At Bottom

Shows log printout in real time.

## FAQ

**The model I imported has white parts what do I do?**

If its a ZA model try using the backup shader, if that doesnt work it will need a shader fix, please report it in the pokemodding discord.

**How come I can't export GFPAK yet?**

It's not implemented yet and won't be unless theres demand for it.

**The model I imported has a broken animation, what do I do?**

Some models have unique skinning quirks, please report it in the pokemodding discord.

**Why isn't there bntx export, how am I supposed to edit textures?**

Use Switch-Toolbox for now.

**Why wont the model load on import?**

I haven't seen a model that doesn't load yet, but if you find one please report it on the pokemodding discord.

**How do I import Maps?**

Check out the Scene viewer once its done :).

**There seems to be Z-Fighting on the model, what do I do?**

Hide the offending mesh, if you truly believe its an import issue, keep it to yourself.

**I have extracted game assets set but my materials/skeletons/textures arent loading, what do I do?**

Did you check if you have the correct active game root for your game?

**I can't see my model, did it not import?**

Make sure you use the controls to move the camera so you can actually see your model.
