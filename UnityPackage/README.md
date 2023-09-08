![Movie_031](https://github.com/seandillon92/WaterSurfaceWavelets-Plugins/assets/51912249/f7495a91-6ccd-4d7b-92bf-36bcdab9aab1)
# Water Surface Wavelets
Unity Package for interactive water surfaces.

## Supported Platforms
### Win, Mac, Linux :heavy_check_mark: 
Developed and tested with Windows64 with DX11/12. All other desktop platforms *should* be able to run the package.

### Android, iOS ⚠️
Builds are possible but performance might suffer.

### WebGL ❌
WebGL does not support Compute Shaders. The simulation, which is the main component that relies on Compute Shaders, can be extended with a CPU/Unity Jobs implementation to support WebGL.

## Credits
This work is a GPU implementation of [1]. More precisely, it's a Unity port of https://github.com/lecopivo/WaterSurfaceWavelets where the CPU simulation code is ported to Compute Shaders and additional tooling is added.
## References
[1] Stefan Jeschke, Tomáš Skřivan, Matthias Müller-Fischer, Nuttapong Chentanez, Miles Macklin, and Chris Wojtan. 2018. Water surface wavelets. ACM Trans. Graph. 37, 4, Article 94 (August 2018), 13 pages. https://doi.org/10.1145/3197517.3201336
