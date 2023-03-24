# AERO_Console
This program is a graphical user interface for Mark [Drela's Athena Vortex Lattice (AVL)](https://web.mit.edu/drela/Public/web/avl/) and [XFoil](https://web.mit.edu/drela/Public/web/xfoil/). It is originally developed to help students in the Aerodynamics and Performance course (MECH-4671) at the [University of Windsor](https://www.uwindsor.ca/).  by [Dr. Afshin Rahimi](https://www.arahimi.ca/). You can watch a series of video tutorials for this on [YouTube](https://youtube.com/playlist?list=PLxcxum3Kgj_ZdFPVwpr5niSOyrRWJFYSp).

## Background
Since AVL and XFoil are beneficial, free, and powerful tools, they were chosen for the teaching purposes in the course. However, the user interface is somewhat outdated, and many students had issues working with the DOS command window. Therefore, I tried to alleviate this pain by developing a graphical user interface that helps mainly with the design process for the AVL and has other functionalities as well.

## How to Install

> Note: This software is only compatible with Windows OS. If you are
> using another OS, you can use the University's virtual machines or lab
> computers that run Windows to use this software.

You can download the latest version of the stand-alone program [from here](https://github.com/drarahimi/AERO_Console/releases/latest). After download:

 1. Make sure the file is in a folder that can read/write files to your computer storage without additional permissions.
 2. Open the .exe file and accept to run it if you get a message that it is not from a trusted publisher. It is because I am not a registered published with Microsoft and Windows.
 3. The program will unpack some required files to run locally in the same location as it is stored and will show the main window for you.
 4. You should see the following screen or a slightly different version of it.

![main_window](https://user-images.githubusercontent.com/35072497/101848794-5bcdaf80-3b24-11eb-9f4d-15fa1de50791.png)

## Features
- Colored and Indented AVL file codes
![1](https://user-images.githubusercontent.com/35072497/101849098-f8904d00-3b24-11eb-83df-f93a114a16e4.gif)
- Graphical User Interface 
![2](https://user-images.githubusercontent.com/35072497/101849194-32615380-3b25-11eb-8bb0-c77bb1e0b402.gif)
- Quick add of geometry, mass or run file
![3](https://user-images.githubusercontent.com/35072497/101849408-a1d74300-3b25-11eb-9185-87ce4890b62e.gif)
- Tooltip help for each element in the geometry, mass and run file
![4](https://user-images.githubusercontent.com/35072497/101849562-e82ca200-3b25-11eb-8ac5-3a834a0f16b9.gif)
- Quick Trefftz view
[5](https://user-images.githubusercontent.com/35072497/101852381-83744600-3b2b-11eb-879b-e5f6da721ad3.gif)
- Quick 3D Geometry view
![6](https://user-images.githubusercontent.com/35072497/101852572-faa9da00-3b2b-11eb-8160-1aee0ecdcf84.gif)
- Live node highlighter
![7](https://user-images.githubusercontent.com/35072497/101855084-dc92a880-3b30-11eb-96b5-78ac8cfaa247.gif)
- Multiple Layouts
![8](https://user-images.githubusercontent.com/35072497/101855205-106dce00-3b31-11eb-8e3d-ee7d04a86977.gif)
- Ability to show section nodes
![9](https://user-images.githubusercontent.com/88632553/227632545-0969575f-88aa-448a-be43-807a408603e5.gif)
- Ability to overlay mass file (distribution)
![10](https://user-images.githubusercontent.com/88632553/227632688-0d1dee0d-5baf-4122-9f86-c8b70248a430.gif)
- Ability to overlay control surfaces on the main geometry
![11](https://user-images.githubusercontent.com/88632553/227632891-febabb19-245d-483a-8043-069d734079dc.gif)
- Ability to zoom/fit all design elements into view with one click
![12](https://user-images.githubusercontent.com/88632553/227632798-3d1fa90b-2fc8-4f3b-9c02-0e8caafcc891.gif)
## Basic Information

**FileNames**

Note that the program is always working with the following filenames:
|Filename| Purpose |Directory|
|--|--|--|
| test.avl | Geometry File | Root*
| test.mas | Mass File | Root*
| test.run | Run File | Root*

*Root is where the main executable file is stored

So, it is crucial to *move files to another location or rename them after you are done working with* **the test.avl** or **test.mass** or **test.run** files. Otherwise, the program will override the existing files and you may lose your work.

**Structure**

When using this program to generate/edit .avl files, it is essential to follow the structure below.
+-AVL template
   |-Surface template
   |--Section template
   |--Control template

This means that the first element is the AVL template; next, you have to add a surface, and in each surface, you can have as many sections (minimum of 2), and in each section, you can add control elements. However, the nesting has to be in this order; otherwise, the geometry will be corrupted and calculations will be incorrect.
- Each surface needs at least 2 sections to be loaded; otherwise the program freezes
- Make sure when you insert surfaces, change Xle, Yle, Zle values so that the plane can be created and does not have a 0 surface area
