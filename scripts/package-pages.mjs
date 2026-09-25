import {mkdir,copyFile,writeFile} from 'node:fs/promises';
await mkdir('docs',{recursive:true});
for(const file of ['index.html','bundle.js'])await copyFile(file,'docs/'+file);
await copyFile('vendor/LICENSE','docs/THREE-LICENSE.txt');
await writeFile('docs/.nojekyll','');
