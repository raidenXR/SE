this is SE (Simulation Engine) project. The concept is of some equivalent of XNA4, but targeted   
for writing simulations. The project aims to be a framework built with F#, while using a native   
lib as a renderer, built in Zig with SDL3 (in future, when Zig hits 0.16).   

Is still in early stage. (Very early stage!! -> not gonna start vigorous development before October)   

Will incorporate a parser for loading .gltf files from FreeCAD or other CAD software.  
Then by leverage some simplistic ECS (Entity-Component-System) design and creating Systems  
for manipulating geometries and other info in those .gltf files, it will run these systems-based   
simulations and rendering the results with SDL3 Zig renderer. 


Use the **scripts/gltf_formater.fsx** to format `.glft` files into more readable layout.   
Look into **tests/...fsx** files, for examples of the project.     
It contains also some simple cylinder `.gltf` object for helping with development of   

- src/gltfLoader.fs
- src/binLoader.fs   

   

