# Original Blender rider assets

`MoonlitRider.blend` is the editable Blender 5.2 source. Clothes, hair and textures are authored for this repository. The anatomical head derives from MakeHuman CC0 assets; see ../THIRD_PARTY_ASSETS.md for provenance and the bundled license.

- `Dress`: continuous pleated cloth surface with a fitted waist and printed hem.
- `Bodice`: fitted riding bodice.
- `Head`, `HairCap`, `HairDetail`: shaped face, swept scalp, and individual surface strands.
- `Braid`: three continuous interwoven strands, skinned in Unity to 12 physical bones.

Recreate the assets with:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --python art/build_rider.py
```

The script saves the `.blend`, original PNG maps, and `RiderMeshes.json`. The JSON is an explicit-axis mesh interchange file: positions, normals, UVs, and topology from Blender. It avoids implicit FBX handedness/scale conversions. Unity's **Moonlit Ride → Import Blender rider assets** converts it to native `.asset` meshes under `Assets/MoonlitRide/Resources/Rider`. The build does this automatically. The player loads only Unity assets and does not need Blender or the JSON source.

To export manual Blender edits, run the export section of `build_rider.py` against the six named objects rather than rerunning its geometry-generation section, which recreates them.

The fabric map contains a woven ivory/indigo botanical pattern and a printed border that deforms with the same cloth surface. The hair map adds strand-scale variation. Paving includes albedo and a normal map. All texture generation is reproducible in the Blender script.


The anatomical body is exported as AnatomicalBodice, AnatomicalSkin and AnatomicalLeggings, with original MakeHuman weights mapped onto the 14-bone definition in BodyRig.json. These are clothed material sections of one anatomical surface, replacing the primitive torso and limbs in the running game.

The dress exporter welds vertices by position and writes angular coordinates to UV0, height to UV1. Fabric.shader reconstructs the print without a physical UV slit. The blouse is expanded/smoothed before export, the fingers are curved into a relaxed grip, and original feet are hidden inside the separate shoes. HairCap follows a raised temple hairline and tapers into the braid at the nape. HairDetail renders fine combed locks flowing from forehead to crown to nape, plus tapered temple wisps; avoid restoring the old hemispherical rim.
