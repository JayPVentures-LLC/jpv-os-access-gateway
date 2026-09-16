import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';
import { runProductionDrain } from '../governance/universal-intake-production-drain.mjs';

function integrityErrors({queue,manifest,sourceRecords,registry}){
  const errors=[];
  if(!Array.isArray(queue?.items)) errors.push('queue.items missing');
  if(!Array.isArray(manifest?.items)) errors.push('manifest.items missing');
  if(!Array.isArray(sourceRecords?.records)) errors.push('sourceRecords.records missing');
  if(!registry?.authorities) errors.push('registry.authorities missing');
  for(const item of queue?.items ?? []) if(!item?.source_id) errors.push('queue item missing source_id');
  return errors;
}

function assertRuntimeAuthority(deps){
  const platform=deps?.runtimeAuthority?.platform;
  if(platform && platform!=='JPV_NATIVE') throw new Error(`external provider runtime is not admissible: ${platform}`);
}

export async function executeProductionDrainCommand({queue,manifest,sourceRecords,registry,receipts=[],deps={}}){
  assertRuntimeAuthority(deps);
  const errors=integrityErrors({queue,manifest,sourceRecords,registry});
  if(errors.length) return {exit_code:2,integrity_errors:errors,items:[],summary:{},evidence:[]};

  const recoveredIds=new Set(sourceRecords.records.map(x=>x.source_id));
  const seedItems=queue.items.map(item=>({source_id:item.source_id}));
  const manifestById=new Map(manifest.items.map(x=>[x.source_id,x]));

  // A recovered-source manifest is authoritative for whether recovery was attempted.
  // Missing canonical sources intentionally have no sourceRecords entry and are allowed
  // to flow into runProductionDrain() as precise fail-closed states.
  for(const seed of seedItems){
    const recovery=manifestById.get(seed.source_id);
    if(!recovery) errors.push(`recovery result missing for ${seed.source_id}`);
    if(recovery?.recovery_status==='RECOVERED_SOURCE' && !recoveredIds.has(seed.source_id)) errors.push(`recovered source record missing for ${seed.source_id}`);
  }
  if(errors.length) return {exit_code:2,integrity_errors:errors,items:[],summary:{},evidence:[]};

  const result=await runProductionDrain({seedItems,sourceRecords:sourceRecords.records,receipts,registry,deps});
  return {exit_code:0,integrity_errors:[],...result};
}

async function loadJson(path){ return JSON.parse(await readFile(path,'utf8')); }

export async function main({deps={}}={}){
  const root=resolve(fileURLToPath(new URL('..',import.meta.url)));
  const queue=await loadJson(resolve(root,'governance/backlog/2026-09-16-manual-queue.json'));
  const manifest=await loadJson(resolve(root,'governance/backlog/2026-09-16-recovered-source-manifest.json'));
  const sourceRecords=await loadJson(resolve(root,'governance/backlog/2026-09-16-production-source-records.json'));
  const registry=await loadJson(resolve(root,'governance/universal-intake-authorities.json'));
  const receipts=[];
  const result=await executeProductionDrainCommand({queue,manifest,sourceRecords,registry,receipts,deps});
  process.stdout.write(`${JSON.stringify(result,null,2)}\n`);
  return result.exit_code;
}

if(process.argv[1] && resolve(process.argv[1])===fileURLToPath(import.meta.url)){
  main().then(code=>{process.exitCode=code;}).catch(error=>{
    process.stderr.write(`${error.stack ?? error}\n`);
    process.exitCode=2;
  });
}
