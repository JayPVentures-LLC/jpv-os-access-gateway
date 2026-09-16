import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const manifest=JSON.parse(await readFile(new URL('../governance/backlog/2026-09-16-recovered-source-manifest.json',import.meta.url)));
const sources=JSON.parse(await readFile(new URL('../governance/backlog/2026-09-16-production-source-records.json',import.meta.url)));

const sourceById=new Map(sources.records.map(x=>[x.source_id,x]));

test('no missing canonical source is silently promoted to production source record',()=>{
  for(const item of manifest.items.filter(x=>x.recovery_status==='MISSING_CANONICAL_SOURCE')){
    assert.equal(sourceById.has(item.source_id),false,item.source_id);
  }
});

test('human political-decision gates are explicit on recovered legislative/oversight packages',()=>{
  for(const id of ['clarity-2-congressional','ib-iba-kremlev-trumpjr-oversight']){
    assert.equal(sourceById.get(id).preflight_blocker.code,'HUMAN_POLITICAL_DECISION_REQUIRED');
  }
});

test('Stars and Stripes remains blocked until contact placeholders are resolved',()=>{
  assert.equal(sourceById.get('stars-stripes-accountability').preflight_blocker.code,'HUMAN_GATE_REQUIRED');
});
