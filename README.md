Lemma
=====

[![Lemma](http://i.imgur.com/Nb6pffrl.png)](http://lemmagame.com)

[**Lemma**](http://lemmagame.com) is an **immersive first-person parkour** game.
Every parkour move has the potential to **modify the environment**.
Surfaces pop in and out of existence at will.

What is this?
-------------

All the code and some of the assets for Lemma are open source. Everything you need
to create your own single-player campaign is there, including a level editor.

Is it good code?
------------------

Not really. The project started in 2010 and served as a test bed for a lot of
hairbrained ideas I had back then. Consider it an example of what *not* to do.

Getting started
---------------

1. Install [Microsoft Visual C# 2013](http://msdn.microsoft.com/en-us/library/dd831853.aspx)
and [XNA Game Studio 4.0 Refresh](https://msxna.codeplex.com/releases/view/117230).
1. Clone the repository.
1. Open `Lemma.sln`.
1. [Set the active solution configuration](http://msdn.microsoft.com/en-us/library/wx0123s5(v=vs.100).aspx)
to `Release`.
1. Build and run the solution.
1. On the main menu, hit "switch to edit mode" to open the level editor.

Level editor
------------
See the [Official level editor guide](http://steamcommunity.com/sharedfiles/filedetails/?id=273022369).

Blender pipeline
----------------

To make or edit models, you'll need [Blender](http://blender.org). The asset
pipeline has been tested with [Blender 2.70a](http://download.blender.org/release/Blender2.70/).
Here's how to get started:

1. Install Blender 2.70a.
1. Find the file `export_fbx.py` in the `PipelineExtensions` project.
1. Copy the file into the Blender program folder under
`2.70\scripts\addons\io_scene_fbx` (it should replace an existing file).
1. Open Blender and make sure the [FBX export addon is enabled](http://wiki.blender.org/index.php/Doc:2.6/Manual/Extensions/Python/Add-Ons#Enabling_and_Disabling).
1. Open a .blend file and select File -> Export -> Autodesk FBX.
1. Use the following export settings for animations:

[![Blender FBX animation export settings](http://i.imgur.com/RRhsEey.png)](http://i.imgur.com/RRhsEey.png)

License
=======

The following textures are adapted from [CGTextures](http://www.cgtextures.com)
source images, and are therefore only made available here for modding purposes:

	Lemma/Content/Textures/red-rock.png
	Lemma/Content/Textures/red-rock-normal.png
	Lemma/Content/Textures/rock-grass.png
	Lemma/Content/Textures/rock-grass-normal.png
	Lemma/Content/Textures/hex.png
	Lemma/Content/Textures/hex-normal.png
	Lemma/Content/Textures/lattice.png
	Lemma/Content/Textures/lattice-normal.png

To use them for your own purposes, you must re-download them from CGTextures
and follow their usage guidelines.

The singleplayer game content lives in a closed-source Git submodule. However,
everything in this repository not listed in the "Internet Credits" section of
`attribution.txt` is made available under the following license:

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