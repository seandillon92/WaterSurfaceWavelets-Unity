![Movie_031](https://github.com/seandillon92/WaterSurfaceWavelets-Plugins/assets/51912249/f7495a91-6ccd-4d7b-92bf-36bcdab9aab1)
# Water Surface Wavelets
Unity Package for interactive water surfaces.

## Features
### **Realistic Rendering**
![WavesRender1](https://github.com/seandillon92/WaterSurfaceWavelets-Plugins/assets/51912249/ca93c9fc-c173-4c29-9c35-a641ee02b4ea)
Waves are rendered as trochoidal waves using a spectrum input.

### **Environment-aware Simulation** 
![Environment1](https://github.com/seandillon92/WaterSurfaceWavelets-Plugins/assets/51912249/420be1bc-d484-44af-97d8-0b56b2015c2c)

Waves take their shape based on the environment. This can be simulated in the Editor or at Runtime, in real-time.
### **Interactive Simulation**
![BoatInteractions](https://github.com/seandillon92/WaterSurfaceWavelets-Plugins/assets/51912249/fad6baf6-b793-43e4-8a79-e4fd81c63625)

The public API allows you to generate real-time disturbances at Runtime, used for the oar waves and the boat wake in the above image.

## Supported Platforms
### Win, Mac, Linux :heavy_check_mark: 
Developed and tested on Win64 and DX11/12. All other desktop platforms and APIs *should* work.

### Android, iOS ⚠️
Builds are possible but performance might suffer.

### WebGL ❌
WebGL does not support Compute Shaders. The simulation, which is the main component that relies on Compute Shaders, can be extended with a CPU/Unity Jobs implementation to support WebGL.

## Supported Rendering Pipelines
TBA

## Supported Editor Versions
TBA

## Performance
TBA

## Installation
TBA

## Usage
TBA

## Credits
This work is a GPU implementation of [1]. More precisely, it's a Unity port of https://github.com/lecopivo/WaterSurfaceWavelets. Additionally, I ported the simulation code to Compute Shaders and created extra tooling.
### References
[1] Stefan Jeschke, Tomáš Skřivan, Matthias Müller-Fischer, Nuttapong Chentanez, Miles Macklin, and Chris Wojtan. 2018. Water surface wavelets. ACM Trans. Graph. 37, 4, Article 94 (August 2018), 13 pages. https://doi.org/10.1145/3197517.3201336
