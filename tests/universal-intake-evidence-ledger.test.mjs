import test from 'node:test';
import assert from 'node:assert/strict';
import { createEvidenceRecord, appendEvidence } from '../governance/universal-intake-evidence.mjs';

const receipt={request_id:'UPIS-1',external_tracking_id:'T-1',received_at:'2026-09-16T10:00:00Z',receiving_authority:'CDT',status:'SUBMITTED',evidence:{confirmation_url:'https://example.test/r/1'},fingerprint:'sha256:fp'};

test('creates tamper-evident evidence record without session secrets',()=>{
  const record=createEvidenceRecord(receipt,{package_hash:'sha256:pkg',attachment_hashes:['a','b']});
  assert.equal(record.request_id,'UPIS-1');
  assert.equal(record.package_hash,'sha256:pkg');
  assert.match(record.record_hash,/^sha256:/);
  assert.equal(JSON.stringify(record).includes('cookie'),false);
});

test('appendEvidence delegates immutable record to storage and returns storage receipt',async()=>{
  const writes=[];
  const result=await appendEvidence(receipt,{package_hash:'sha256:pkg'}, {append:async record=>{writes.push(record);return {ledger_id:'L1',stored_at:'2026-09-16T10:01:00Z'};}});
  assert.equal(writes.length,1); assert.equal(result.ledger_id,'L1'); assert.equal(result.record_hash,writes[0].record_hash);
});

test('appendEvidence fails closed without append-only store',async()=>{
  await assert.rejects(()=>appendEvidence(receipt,{package_hash:'sha256:pkg'},{}),/append-only/i);
});
