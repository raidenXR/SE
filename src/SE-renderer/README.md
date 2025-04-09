To compile shaders with dxc (directX compiler) use the following command for vertex shaders   

```
dxc -T vs_6_4 -E main RawTriangle.vert.hlsl -Fo RawTriangle.vert.dxil
```

and for fragment shaders

```
dxc -T ps_6_4 -E main RawTriangle.frag.hlsl -Fo RawTriangle.frag.dxil
```

```
dxc -spirv -T ps_6_4 -E main RawTriangle.frag.hlsl -Fo RawTriangle.frag.spv
```

TODO: Compile SDL_Shadercross and include that in the project to compile .hlsl shaders on runtime.  

   
   

**P.S** Bindings for F# and C# contained in bindings folder.   
The bindings are generated with [zig2dotnet_v2](https://github.com/raidenXR/zig2dotnet)  
Generate bindings with 

```
cd bindings
zig2dotnet_v2 "../src/rendererFS.zig" "librendererFS" -fs
zig2dotnet_v2 "../src/rendererFS.zig" "librendererFS" -cs
```