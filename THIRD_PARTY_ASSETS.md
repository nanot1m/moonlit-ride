# Third-party artwork

The head is derived from the MakeHuman Community hm08 base mesh and adult female macro targets, downloaded 2026-09-25 from https://github.com/makehumancommunity/makehuman/tree/master/makehuman/data . Only asset data is included; no MakeHuman application source code is incorporated.

Files in `art/third-party/makehuman/`:
- `base.obj`: upstream Git blob d26635e9326e3cca30778fd7b9c00062b03cce09.
- `caucasian-female-young.target`: 9d1f0cbeedc9a6a51abe33f1ebb5fa7c5a7edbf1.
- `universal-female-young-averagemuscle-averageweight.target`: 3f92e2939e50403aa89607e4889639ab337bcead.
- `LICENSE.ASSETS.md`: upstream CC0 dedication.

License: CC0, confirmed by the file headers and https://static.makehumancommunity.org/about/license.html . Original rights holders listed by MakeHuman: Data Collection AB, Joel Palmius, Jonas Hauquier. The local Blender script applies the targets, extracts the head, and converts coordinates for the rider. Clothing, hair and tile textures are original generated artwork.

The full clothed body now also derives from the base mesh. default_weights.mhw and default.mhskel are MakeHuman core rig data, both explicitly licensed CC0 in their JSON headers. The original weights are aggregated to 14 cycling bones; three material sections cover bodice/sleeves, forearms/hands and leggings. AnatomicalRider skins the mesh to the IK pose.

## Quaternius Ultimate Stylized Nature

Nine models and their colour textures are from [Ultimate Stylized Nature](https://quaternius.com/packs/ultimatestylizednature.html), by Quaternius, CC0 1.0. Downloaded 2026-09-26 from the public Google Drive folder linked by the author: https://drive.google.com/drive/folders/1IV3bXHzkNvuNWFHPi4KPx-G4ghuxIuT-

Models: NormalTree_1, NormalTree_3, PineTree_2, PineTree_4, MapleTree_1, Bush_Large, Grass_Large, Rock_1, Rock_2. Selected editable source: art/third-party/quaternius/CoastalNature.blend. Runtime FBX and PNG files: Unity/Assets/MoonlitRide/Resources/Nature. Origins were grounded and transforms baked in Blender. Materials and wind shader are adapted for Unity's built-in renderer.

The author's License.txt is preserved verbatim alongside the assets; its heading says Ultimate Platformer Pack, while the originating nature pack page independently explicitly declares CC0. License: https://creativecommons.org/publicdomain/zero/1.0/

## Bicycle — Poly by Google

"Bicycle" by Poly by Google, https://poly.pizza/m/5pBoRkAPQk6, licensed under Creative Commons Attribution 3.0: https://creativecommons.org/licenses/by/3.0/ . Downloaded 2026-09-26 from the model page's GLB URL. The original is preserved at art/third-party/poly-bicycle/Bicycle.glb.

Adapted for Moonlit Ride: separated frame/steering, fitted geometry to rider and wheelbase, changed materials, replaced wheels with smooth rims and crossed spokes, rebuilt animated cranks/pedals and added chain, carrier, brake levers and lamp details. Source: art/CityBicycle.blend; reproducible adaptation: art/build_bicycle.py (using the separated original in SourceParts.blend). No endorsement by the original author is implied.
