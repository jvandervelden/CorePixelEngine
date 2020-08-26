![Build Pixel Engine](https://github.com/jvandervelden/CorePixelEngine/workflows/Build%20Pixel%20Engine/badge.svg?branch=master)

# Core Pixel Engine

Conversion of olcPixelGameEngine: https://github.com/OneLoneCoder/olcPixelGameEngine.

To utilize, clone, and add as a subproject in your solution.

# OpenGL.Net

This module contains an OpenGL version of the CorePixelEngine Renderer.
The module utilizes the OpenGL.Net project: https://github.com/luca-piccioni/OpenGL.Net, and has a submodule in this repo.
The nuget package of OpenGL.Net can be used but at this point Nuget only has dotnet coreapp v2 and is missing the dotnet standard 2.0 version. 
This will cause package manager warnings.

# Windows

The windows module contains a windows version of the CorePixelEngine Platform. It utilizes windows forms from dotnet coreapp v3.

# Linux

The linux module contains a linux version of the CorePixelEngine Platform. It utilizes X11 windows system with the help of the X11.Net project: https://github.com/ajnewlands/X11.Net
