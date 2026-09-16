import test from 'node:test';
import assert from 'node:assert/strict';
import { importBacklog, classifyBacklogItem } from '../governance/universal-intake-backlog.mjs';

const prepared={
  source_id:'legacy-1', authority_id:'CA_CDT_PRA', authority_name:'California Department of Technology', request_type:'PUBLIC_RECORDS',
  requester:{name:'Jay Price',organization:'JPV'}, subject:'Records', summary:'Produce records', requested_action:'Produce electronically',
  provenance:{package_hash:'sha256:legacy1'}
};

test('imports prepared package into canonical routable request',()=>{
  const [item]=importBacklog([prepared],[]);
  assert.match(item.request.request_id,/^UPIS-BACKLOG-/);
  assert.equal(item.status,'READY_TO_ROUTE');
  assert.equal(item.request.recipient.authority_id,'CA_CDT_PRA');
});

test('deduplicates against existing receipt package hash',()=>{
  const [item]=importBacklog([prepared],[{request_id:'UPIS-old',package_hash:'sha256:legacy1',status:'SUBMITTED',external_tracking_id:'X1'}]);
  assert.equal(item.status,'SUBMITTED');
  assert.equal(item.duplicate_of,'UPIS-old');
});

test('preserves resolved state when receipt is resolved',()=>{
  const [item]=importBacklog([prepared],[{request_id:'UPIS-old',package_hash:'sha256:legacy1',status:'RESOLVED',external_tracking_id:'X1'}]);
  assert.equal(item.status,'RESOLVED');
});

test('classifies missing authority or subject as ACTION_REQUIRED',()=>{
  assert.equal(classifyBacklogItem({...prepared,authority_id:undefined},[]).status,'ACTION_REQUIRED');
  assert.equal(classifyBacklogItem({...prepared,subject:undefined},[]).status,'ACTION_REQUIRED');
});

test('does not deduplicate unrelated package hashes',()=>{
  const [item]=importBacklog([prepared],[{request_id:'other',package_hash:'sha256:other',status:'SUBMITTED'}]);
  assert.equal(item.status,'READY_TO_ROUTE');
});
