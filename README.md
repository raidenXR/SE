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


:TODO

- develop further the SDL3 zig-lib to have a a more complete and useful API to can actually   
render 'real geometries' with it.
- fix (or total overhaul) the [zig2dotnet](https://github.com/raidenXR/zig2dotnet) for better - more robust parsing of Zig files and F#/C# bindings generation.   


#### Instructions

To build `src/SE-renderer`  Zig compiler (0.14 stable) is required.   
**Make sure that SDL3-devel in available on your machine too**. 
Also it references the dotnet package from [zdotnet-repo](https://github.com/raidenXR/zdotnet), so make sure to clone it, and fix the url to match the relative path on your machine, in `src/SE-renderer/build.zig.zon`.  
```
    .dependencies = .{
        .dotnet = .{
            .url = "../../../../zig/zdotnet/",
            .hash = "dotnet-0.1.0-Xe2rzg9lAQDt8mbQbYrX79liDpq_trtw-ftDLsUzDKW8",
        },
    },
```  

When `zig-out/lib/libSE-renderer.so` gets compiled, copy that to the directory `SE-core/tests/natives`  
alongside the `libSDL3.so` (or SDL3.dll if your are on windows).  

To build `src/SE-core`, just  
```
cd src/SE-core
dotnet build
cd tests
dotnet fsi ecs_test.fsx  # to run the example for the ECS part
dotnet fsi renderer_test.fsx  # to run the example for the SDL3 renderer zig lib
```
And then 

**P.S.** great thanks to [SDL3_examples](https://github.com/TheSpydog/SDL_gpu_examples/tree/main) for providing all these valuable examples, and the whole SDL3 GPU API team!!!

