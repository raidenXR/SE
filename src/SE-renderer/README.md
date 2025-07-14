#### DirectXCompiler

Download dxc from github releases [https://github.com/microsoft/DirectXShaderCompiler/releases](URL)  


#### Shader Compilation

For offline compilation of shaders with dxc (directX compiler) use the following commands.   

```
dxc -T vs_6_4 -E VS source/color.hlsl -Fo compiled/color_vs.dxil     # for vertex shader
dxc -T ps_6_4 -E PS source/color.hlsl -Fo compiled/color_ps.dxil     # for pixel shader
```

To compile to SPIR-V binary format use **-spirv**  .

```
dxc -spirv -T vs_6_4 -E VS source/color.hlsl -Fo compiled/color_vs.spv     # for vertex shader
dxc -spirv -T ps_6_4 -E PS source/color.hlsl -Fo compiled/color_ps.spv     # for pixel shader
```

Shader `source/` code, along with the `compiled/` `.spv` binaries are stored in `shaders/` directory.   
   
   

**P.S** ~~Bindings for F# and C# contained in bindings folder.~~   
Ignore the bindings for the time being. These are from an older commit...   
The bindings are generated with [zig2dotnet_v2](https://github.com/raidenXR/zig2dotnet)  
Generate bindings with 

```
cd bindings
zig2dotnet_v2 "../src/rendererFS.zig" "librendererFS" -fs
zig2dotnet_v2 "../src/rendererFS.zig" "librendererFS" -cs
```