# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

AssetStudio 是一个 Windows 桌面工具，用于探索、提取和导出 Unity 游戏资源。支持 Unity 版本 3.4 - 2022.3，可导出纹理、精灵、音频、网格、着色器、MonoBehaviour 脚本和 Lua 字节码。

这是 Perfare 已归档 AssetStudio 的 Fork 版本，持续开发中，新增了对新版 Unity 的支持和 Lua 字节码反编译功能。

## 构建命令

### 前置要求
- Visual Studio 2022 或更新版本
- FBX SDK 2020.2.1（AssetStudioFBXNative 项目必需，需单独安装并配置 include/library 路径）

### MSBuild 构建（命令行）
```bash
# 还原 NuGet 包
nuget restore

# 构建指定目标框架
msbuild /t:AssetStudioGUI:publish /p:Configuration=Release /p:TargetFramework=net6.0-windows /p:SelfContained=false

# 可用目标框架: net472, net5.0-windows, net6.0-windows
# 输出路径: AssetStudioGUI/bin/Release/{framework}/publish/
```

### Visual Studio 构建
打开 `AssetStudio.sln` 构建解决方案。GUI 项目通过 post-build 目标自动将原生 DLL（AssetStudioFBXNative.dll, Texture2DDecoderNative.dll）复制到输出目录。

## 架构

```
AssetStudioGUI (WinForms 入口)
    │
    ├── AssetStudio (核心库)
    │   ├── AssetsManager.cs - 资源加载/处理的核心类
    │   ├── SerializedFile.cs - Unity .assets 文件解析
    │   ├── BundleFile.cs - AssetBundle 解析
    │   ├── FileReader.cs - 文件 I/O 抽象层
    │   ├── Classes/ - Unity 类型定义 (Mesh, Texture2D, AnimationClip 等)
    │   ├── LuaDecompile/ - Lua 字节码反编译处理器
    │   └── Dependencies/ - 内嵌 Python/Lua 反编译器 (ljd, luadec)
    │
    ├── AssetStudioUtility (转换器)
    │   ├── Texture2DConverter.cs - 纹理导出 (png, tga, jpeg, bmp)
    │   ├── ModelConverter.cs - 3D 模型转换 (OBJ, FBX)
    │   ├── ShaderConverter.cs - 着色器反编译 (SPIR-V, SMOL-V)
    │   ├── AudioClipConverter.cs - 音频转换 (FSB → WAV)
    │   ├── MonoBehaviourConverter.cs - 脚本导出 (JSON)
    │   └── CSspv/ - SPIR-V 跨平台编译器
    │
    ├── AssetStudioFBXWrapper → AssetStudioFBXNative.dll (C++ FBX SDK)
    │   └── FbxExporter.cs - FBX 导出封装
    │
    └── Texture2DDecoderWrapper → Texture2DDecoderNative.dll (C++)
        └── TextureDecoder.cs - ASTC, BCN, ETC, PVRTC, Crunch 解码
```

### 关键入口点
- `AssetStudioGUI/Program.cs` - 应用程序入口
- `AssetStudio/AssetsManager.cs` - 核心资源加载 (LoadFiles, LoadFolder, Load)
- `AssetStudioGUI/Studio.cs` - 静态辅助类，管理全局状态 (assetsManager, assemblyLoader, exportableAssets)

### 原生库互操作
原生 C++ 库通过 `AssetStudio.PInvoke` 使用 P/Invoke。输出目录需要同时包含 x86 和 x64 DLL，放置在 `x86/` 和 `x64/` 子目录中。

## 多目标框架

项目针对多个框架以确保广泛兼容性：
- `net472` - .NET Framework 4.7.2 (Windows 传统版)
- `net5.0-windows` / `net6.0-windows` - 现代 .NET (WinForms 仅限 Windows)

框架特定代码使用条件编译：
```xml
<ItemGroup Condition=" '$(TargetFramework)' != 'net472' ">
  <PackageReference Include="OpenTK" Version="4.6.7" />
</ItemGroup>
```

## 资源加载流程

1. `AssetsManager.LoadFiles()` / `LoadFolder()` - 入口点
2. `FileReader` - 检测文件类型 (AssetsFile, BundleFile, WebFile, GZip, Brotli, Zip)
3. `SerializedFile` - 解析 Unity .assets 格式 (header, types, objects)
4. `ObjectReader` - 基于 TypeTree 读取单个资源数据
5. `Classes/` 类型类 - 反序列化特定 Unity 类型 (Mesh, Texture2D 等)

## 关键依赖

### NuGet 包
- Newtonsoft.Json 13.0.1 - JSON 序列化
- K4os.Compression.LZ4 - LZ4 压缩
- Mono.Cecil 0.11.3 - 程序集检查 (MonoBehaviour 导出)
- SixLabors.ImageSharp.Drawing - 图像处理
- OpenTK 3.1.0/4.6.7 - OpenGL 预览 (框架依赖版本)

### 内嵌依赖 (AssetStudio/Dependencies/)
- `ljd/` - LuaJIT 字节码反编译器 (Python)
- `luadec/` - Lua 5.1/5.2/5.3 反编译器
- `python/` - 内嵌 Python 3.8 运行时

### 内部库
- `7zip/` - LZMA 压缩 (C# 移植)
- `Brotli/` - Brotli 解压缩 (C# 移植)
- `Smolv/` - SMOL-V 着色器解压缩器
- `CSspv/` - SPIR-V 跨平台编译器

## 重要说明

### MonoBehaviour 导出
需要程序集 DLL 进行正确的反序列化。首次导出 MonoBehaviour 资源时，用户必须选择 `Managed` 文件夹（IL2CPP 游戏则选择 Il2CppDumper 输出目录）。

### Lua 字节码反编译
默认禁用。通过 GUI 启用：Options → Decompile Lua。使用内嵌 Python 运行时和反编译脚本。

### FBX 导出
需要基于 FBX SDK 2020.2.1 构建的原生 AssetStudioFBXNative.dll。C++ 项目必须在 `.vcxproj` 中配置正确的 FBX SDK 路径。

### 版本支持
添加新 Unity 版本支持时：
1. 若格式变更，更新 `SerializedFileFormatVersion` 枚举
2. 有新平台时更新 `BuildTarget` 枚举
3. Unity 新增字段时在 `Classes/` 中添加/更新类型定义
4. 使用新 Unity 版本的实际资源进行测试