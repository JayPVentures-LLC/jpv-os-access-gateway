import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const sources=JSON.parse(await readFile(new URL('../governance/backlog/2026-09-16-production-source-records.json',import.meta.url)));

test('only evidence-recovered sources enter production source records',()=>{
  const ids=new Set(sources.records.map(x=>x.source_id));
  assert.deepEqual([...ids].sort(),['clarity-2-congressional','ib-iba-kremlev-trumpjr-oversight','manhattan-da-domain-records','stars-stripes-accountability'].sort());
});

test('Manhattan request carries canonical provenance and no political gate',()=>{
  const record=sources.records.find(x=>x.source_id==='manhattan-da-domain-records');
  assert.match(record.request.provenance.package_hash,/^sha256:[a-f0-9]{64}$/);
  assert.equal(record.preflight_blocker,undefined);
});

test('political packages are recovered but remain human-decision gated',()=>{
  for(const id of ['clarity-2-congressional','ib-iba-kremlev-trumpjr-oversight']){
    const record=sources.records.find(x=>x.source_id===id);
    assert.equal(record.preflight_blocker.code,'HUMAN_POLITICAL_DECISION_REQUIRED');
    assert.ok(record.recipients.length>=2);
  }
});
