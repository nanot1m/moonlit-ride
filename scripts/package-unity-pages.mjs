import { cp, mkdir, readFile, readdir, stat, writeFile } from 'node:fs/promises';
const source = 'Unity/Builds/WebGL';
const index = await readFile(`${source}/index.html`, 'utf8');
if (!index.includes('createUnityInstance') || index.includes('{{{')) throw new Error('Build Unity WebGL before packaging.');
const files = await readdir(`${source}/Build`);
for (const suffix of ['.loader.js', '.data.unityweb', '.framework.js.unityweb', '.wasm.unityweb']) {
  const matches = files.filter(file => file.endsWith(suffix));
  if (matches.length !== 1) throw new Error(`Expected one ${suffix} file in a clean WebGL build.`);
  if ((await stat(`${source}/Build/${matches[0]}`)).size > 99 * 1024 * 1024) throw new Error('Build exceeds the GitHub per-file size limit.');
  if (!index.includes(matches[0])) throw new Error(`Unreferenced build file: ${matches[0]}`);
}
await mkdir('docs', {recursive:true});
await cp(source, 'docs', {recursive:true});
await writeFile('docs/.nojekyll', '');
console.log('Unity WebGL packaged into docs/. Keep docs/classic/ for the original edition.');
