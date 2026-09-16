import test from 'node:test';
import assert from 'node:assert/strict';
import { recoverBacklogPackages } from '../governance/universal-intake-recovery.mjs';

const seed=[{source_id:'x'},{source_id:'y'}];
const record={source_id:'x',request:{request_id:'R1',subject:'Records',provenance:{package_hash:'sha256:pkg'}},attachments:[{name:'a.pdf',sha256:'abc'}],receipts:[{external_tracking_id:'T1'}]};

test('recovers exact canonical package and preserves evidence',()=>{
  const out=recoverBacklogPackages(seed,[record]);
  assert.equal(out[0].status,'RECOVERED');
  assert.equal(out[0].package.request.subject,'Records');
  assert.deepEqual(out[0].package.attachments,record.attachments);
  assert.deepEqual(out[0].package.receipts,record.receipts);
});

test('missing source fails closed without synthesizing request text',()=>{
  const out=recoverBacklogPackages(seed,[record]);
  assert.equal(out[1].status,'ACTION_REQUIRED');
  assert.deepEqual(out[1].blocker,{code:'MISSING_CANONICAL_SOURCE',source_id:'y'});
  assert.equal('package' in out[1],false);
});

test('duplicate source records are rejected as ambiguous',()=>{
  assert.throws(()=>recoverBacklogPackages([{source_id:'x'}],[record,structuredClone(record)]),/duplicate canonical source/i);
});
