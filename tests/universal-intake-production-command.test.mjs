import test from 'node:test';
import assert from 'node:assert/strict';
import { executeProductionDrainCommand } from '../scripts/run-universal-intake-production-drain.mjs';

const queue={items:[{source_id:'missing'},{source_id:'portal'}]};
const manifest={items:[{source_id:'missing',recovery_status:'MISSING_CANONICAL_SOURCE'},{source_id:'portal',recovery_status:'RECOVERED_SOURCE'}]};
const sourceRecords={records:[{source_id:'portal',request:{request_id:'R1',request_type:'PUBLIC_RECORDS',requester:{name:'Jay'},recipient:{authority_id:'PORTAL',authority_name:'Portal'},subject:'Records',summary:'Records',requested_action:'Produce',provenance:{package_hash:'sha256:x'}}}]};
const registry={authorities:{PORTAL:{id:'PORTAL',authority_name:'Portal',transports:[{kind:'PORTAL',executable:true,endpoint:'https://portal.example',profile:'p'}]}}};

test('returns precise states when native capacity is unavailable',async()=>{
  const result=await executeProductionDrainCommand({queue,manifest,sourceRecords,registry,receipts:[],deps:{loadPortalProfile:async()=>({id:'p',semantic_fields:{}}),portalWorker:async()=>{throw new Error('JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE');}}});
  assert.equal(result.exit_code,0);
  assert.equal(result.summary['ACTION_REQUIRED:MISSING_CANONICAL_SOURCE'],1);
  assert.equal(result.summary['ACTION_REQUIRED:JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE'],1);
});

test('rejects any runtime dependency admitting an external provider',async()=>{
  await assert.rejects(()=>executeProductionDrainCommand({queue:{items:[{source_id:'portal'}]},manifest,sourceRecords,registry,receipts:[],deps:{runtimeAuthority:{platform:'VERCEL'}}}),/external provider runtime/i);
});

test('integrity failures return non-zero command result',async()=>{
  const result=await executeProductionDrainCommand({queue:{items:[{}]},manifest:{items:[]},sourceRecords:{records:[]},registry,receipts:[],deps:{}});
  assert.equal(result.exit_code,2);
  assert.ok(result.integrity_errors.length);
});
