Unpack and Repack `*.mcdat` files for [Light.VN](https://lightvn.net/) engine games.  
Only support files with the `.mcdat` extension. For `.vndat`, try [Light.vnTools](https://github.com/Chenx221/Light.vnTools/tree/main)  
Usage:  
```bash
LightVNTool.exe -u/-p/-patch <path to folder>
```
-u: Unpack mcdat  
-p: Repack mcdat  
-patch: Make patch to update original content. Keep the directory structure of the `output` folder. Delete the files you don't want to update, but keep `0.mcdat.json`. Then it will gengerate `Patch` folder. Copy it to game's root directory.
