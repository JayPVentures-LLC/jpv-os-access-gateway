import test from 'node:test';
import assert from 'node:assert/strict';
import { drainBacklog } from '../governance/universal-intake-drain.mjs';

const registry={authorities:{
  NIST_FOIA:{id:'NIST_FOIA',authority_name:'NIST',transports:[{kind:'EMAIL',executable:true,endpoint:'foia@nist.gov'}]},
  CA_CDT_PRA:{id:'CA_CDT_PRA',authority_name:'California Department of Technology',transports:[{kind:'PORTAL',executable:true,endpoint:'https://example.test',profile:'ca-cdt-pra'}]}
}};
const prepared=(id,authority_id,authority_name)=>({source_id:id,authority_id,authority_name,request_type:'PUBLIC_RECORDS',requester:{name:'Jay Price',organization:'JPV'},subject:`Request ${id}`,summary:'Produce records',requested_action:'Produce electronically',provenance:{package_hash:`sha256:${id}`}});

test('drains ready email work and preserves already-submitted items',async()=>{
  const out=await drainBacklog([prepared('a','NIST_FOIA','NIST'),prepared('b','NIST_FOIA','NIST')],[{request_id:'old',package_hash:'sha256:b',status:'SUBMITTED',external_tracking_id:'T-old'}],registry,{sendEmail:async()=>({tracking_id:'T-new',received_at:'2026-09-16T12:00:00Z',status:'received'})});
  assert.equal(out.summary.SUBMITTED,2);
  assert.equal(out.items.find(x=>x.source_id==='a').request.status,'SUBMITTED');
  assert.equal(out.items.find(x=>x.source_id==='b').duplicate_of,'old');
});

test('portal human gate produces one precise resumable handoff',async()=>{
  const out=await drainBacklog([prepared('c','CA_CDT_PRA','California Department of Technology')],[],registry,{loadPortalProfile:async()=>({id:'ca-cdt-pra',semantic_fields:{}}),portalWorker:async()=>({state:'HUMAN_REQUIRED',reason:'MFA',resume_token:'resume-c'})});
  assert.equal(out.summary.ACTION_REQUIRED,1);
  assert.equal(out.items[0].handoff.reason,'MFA');
  assert.equal(out.items[0].handoff.resume_token,'resume-c');
});

test('unknown authority is isolated as ACTION_REQUIRED rather than aborting batch',async()=>{
  const out=await drainBacklog([prepared('x','UNKNOWN','Unknown')],[],registry,{});
  assert.equal(out.items[0].status,'ACTION_REQUIRED');
  assert.match(out.items[0].handoff.reason,/authority/i);
});
