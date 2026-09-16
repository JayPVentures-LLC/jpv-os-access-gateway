import test from 'node:test';
import assert from 'node:assert/strict';
import { runProductionDrain } from '../governance/universal-intake-production-drain.mjs';

const registry={authorities:{
  NIST_FOIA:{id:'NIST_FOIA',authority_name:'NIST',transports:[{kind:'EMAIL',executable:true,endpoint:'foia@nist.gov'}]},
  CA_CDT_PRA:{id:'CA_CDT_PRA',authority_name:'California Department of Technology',transports:[{kind:'PORTAL',executable:true,endpoint:'https://portal.example',profile:'ca-cdt-pra'}]}
}};
const seed=[{source_id:'email'},{source_id:'portal'},{source_id:'missing'}];
const sourceRecords=[
 {source_id:'email',request:{request_id:'R-email',request_type:'PUBLIC_RECORDS',requester:{name:'Jay'},recipient:{authority_id:'NIST_FOIA',authority_name:'NIST'},subject:'Email request',summary:'Records',requested_action:'Produce',provenance:{package_hash:'sha256:email'}}},
 {source_id:'portal',request:{request_id:'R-portal',request_type:'PUBLIC_RECORDS',requester:{name:'Jay'},recipient:{authority_id:'CA_CDT_PRA',authority_name:'California Department of Technology'},subject:'Portal request',summary:'Records',requested_action:'Produce',provenance:{package_hash:'sha256:portal'}}}
];

test('submits recovered email route and appends durable evidence',async()=>{
  const stored=[];
  const out=await runProductionDrain({seedItems:[seed[0]],sourceRecords:[sourceRecords[0]],receipts:[],registry,deps:{sendEmail:async()=>({tracking_id:'T-email',received_at:'2026-09-16T13:00:00Z',status:'received'}),evidenceStore:{append:async r=>{stored.push(r);return {ledger_id:'L1',stored_at:'2026-09-16T13:00:01Z'};}}}});
  assert.equal(out.items[0].status,'SUBMITTED');
  assert.equal(stored.length,1);
  assert.equal(out.evidence.length,1);
});

test('portal human gate becomes precise ACTION_REQUIRED:HUMAN_GATE',async()=>{
  const out=await runProductionDrain({seedItems:[seed[1]],sourceRecords:[sourceRecords[1]],receipts:[],registry,deps:{loadPortalProfile:async()=>({id:'ca-cdt-pra',semantic_fields:{}}),portalWorker:async()=>({state:'HUMAN_REQUIRED',reason:'MFA',resume_token:'r1'}),evidenceStore:{append:async()=>{throw new Error('should not append');}}}});
  assert.equal(out.items[0].status,'ACTION_REQUIRED:HUMAN_GATE');
  assert.equal(out.items[0].handoff.reason,'MFA');
});

test('missing source remains precise ACTION_REQUIRED:MISSING_CANONICAL_SOURCE',async()=>{
  const out=await runProductionDrain({seedItems:[seed[2]],sourceRecords:[],receipts:[],registry,deps:{}});
  assert.equal(out.items[0].status,'ACTION_REQUIRED:MISSING_CANONICAL_SOURCE');
});

test('existing receipt suppresses duplicate transmission',async()=>{
  let sent=0;
  const out=await runProductionDrain({seedItems:[seed[0]],sourceRecords:[sourceRecords[0]],receipts:[{request_id:'old',package_hash:'sha256:email',status:'SUBMITTED',external_tracking_id:'old-T'}],registry,deps:{sendEmail:async()=>{sent++;return {};}}});
  assert.equal(sent,0);
  assert.equal(out.items[0].status,'SUBMITTED');
  assert.equal(out.items[0].duplicate_of,'old');
});

test('native runtime capacity blocker is classified precisely',async()=>{
  const out=await runProductionDrain({seedItems:[seed[1]],sourceRecords:[sourceRecords[1]],receipts:[],registry,deps:{loadPortalProfile:async()=>({id:'ca-cdt-pra',semantic_fields:{}}),portalWorker:async()=>{throw new Error('JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE');}}});
  assert.equal(out.items[0].status,'ACTION_REQUIRED:JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE');
});
