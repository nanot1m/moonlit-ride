import {mkdir,copyFile,writeFile} from 'node:fs/promises';
// Keep the original Three.js edition available without overwriting the Unity landing page.
await mkdir('docs/classic',{recursive:true});
for(const file of ['index.html','bundle.js'])await copyFile(file,'docs/classic/'+file);
await copyFile('vendor/LICENSE','docs/classic/THREE-LICENSE.txt');
await writeFile('docs/.nojekyll','');
