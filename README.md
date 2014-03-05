Lemma
=====

[![Lemma](http://i.imgur.com/ztNwGTjl.png)](http://lemmagame.com)

[**Lemma**](http://lemmagame.com) is an **immersive first-person parkour** game.
Every parkour move has the potential to **modify the environment**.
Surfaces pop in and out of existence at will.

What is this?
-------------

All the code and some of the assets for Lemma are open source. Everything you need to create your own single-player campaign is there, including a level editor.

Getting started
---------------

1. Install [Microsoft Visual C# 2010](http://go.microsoft.com/?linkid=9709939) and [XNA Game Studio 4](http://www.microsoft.com/en-us/download/details.aspx?id=23714).
1. Clone the repository.
1. Copy the contents of the `Template` folder into the empty `Game` folder.
1. Open `Lemma.sln` in Visual C# 2010.
1. [Set the active solution configuration](http://msdn.microsoft.com/en-us/library/wx0123s5(v=vs.100).aspx) to `Release`.
1. Build and run the solution.
1. Have fun playing around in the test level!

Editor
------

To edit maps, [set the active solution configuration](http://msdn.microsoft.com/en-us/library/wx0123s5(v=vs.100).aspx) to `Debug` and run the solution. This will open the editor:

[![Lemma in-game editor](http://i.imgur.com/GKfxHull.jpg)](http://i.imgur.com/GKfxHul.jpg)

On the left is a list of properties you can edit. Click the small box to the right of the `MapFile` property, type `intro` and press Tab. This will load the `intro` map.

### Controls

The controls are somewhat similar to [Blender](http://blender.org):

- Hold the middle mouse button and use WASD to move. Hold Shift to move faster.
- Hold Alt + scroll wheel to zoom in and out
- Right-click an object to select it. Its properties will appear on the left side of the screen.
-- To edit a string property, click it, type, then press Tab to accept the change.
-- To edit a numeric property, hold down left mouse button and use the scroll wheel. Hold Shift to change the number faster.
- You can select multiple objects by holding Shift.
- Once you have selected one or more objects, hit G to "grab" and move them. Left-click to accept the change or right-click to cancel.
- R to rotate.
- X to delete.
- Shift + D to duplicate.
- Hit Space at any time to see an autocomplete menu of available commands.
- Ctrl + S to save.
- Ctrl + R to start playing the level. To switch back to edit mode, hit Esc and select "Switch to edit mode".
- To create a new object, deselect all objects (Ctrl + A) and press Space. Look for the object you want to add, or start typing it.

### Voxel editing

[![Lemma voxel editor](http://i.imgur.com/9FXf5Bol.jpg)](http://i.imgur.com/9FXf5Bo.jpg)

- Right-click a voxel object and press Tab to switch into voxel edit mode. (Press Tab again to switch back to normal mode)
- The four glowing white shapes indicate the current voxel cell.
- In voxel edit mode, you are always in "look" mode, you don't need to hold down middle mouse button. Use WASD to move.
- Use Q and E to scroll through the available materials. You can see the current selected material in the `Brush` property.
-- The default material is `(Procedural)` which generates interesting shapes using Perlin noise.
- Left-click to fill the current cell with the selected material. Right-click to empty the current cell.
- Use the scroll wheel to change the brush size.
- Hold F while moving on a box to extend or shrink the box.
- Hold the middle mouse button and move with WASD to create a voxel selection.
-- Left-click or right-click to fill or empty the whole selection.
-- Once selected, hit G and use WASD to move the selected voxels. Left-click to accept the change or right-click to cancel.
-- Hit C to do the same thing, but copy the voxels rather than moving them.

### Saving maps

The editor saves the maps from the same location it loads them. If you're running from Visual Studio, that will be in `bin\x86\Debug\Game`. If you Clean or Rebuild the project, it will wipe out your maps.

Run the `save-maps.bat` batch file (just double-click it) in the `Lemma` folder to copy the maps from the `bin` folder back into your Visual Studio project.
Note that when you build the project, it will overwrite the maps in your `bin` folder!
So the recommended practice is: when you're at a stopping point, close the editor, run `save-maps.bat`, then run the editor again. That will keep everything in sync.

### Adding new maps and scripts

To create a new map:

1. Enter the name of a map that does not exist in the `MapFile` property, start editing, and save it.
1. Run `save-maps.bat` to copy the map into the Visual Studio project.
1. Add the map file to the `Game.XNA` project. In the file's properties, set the Build Action to "None" and set "Copy to Output Directory" to "Copy if newer".

Scripts work the same way. Check `intro.cs` for an example of a simple script.

Blender pipeline
----------------

To make or edit models, you'll need [Blender](http://blender.org). The asset pipeline only works with [Blender 2.66](http://download.blender.org/release/Blender2.66/). Here's how to get started:

1. Install Blender 2.66.
1. Find the file `export_fbx.py` in the `PipelineExtensions` project.
1. Copy the file into the Blender program folder under `2.66\scripts\addons\io_scene_fbx` (it should replace an existing file).
1. Open Blender and make sure the [FBX export addon is enabled](http://wiki.blender.org/index.php/Doc:2.6/Manual/Extensions/Python/Add-Ons#Enabling_and_Disabling).
1. Open a .blend file and select File -> Export -> Autodesk FBX.

License
=======

The game content is closed-source, but everything you see in this repository is made available under the following license.

The MIT License (MIT)
---------------------

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.