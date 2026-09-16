import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const manifest=JSON.parse(await readFile(new URL('../governance/backlog/2026-09-16-recovered-source-manifest.json', import.meta.url)));
const queue=JSON.parse(await readFile(new URL('../governance/backlog/2026-09-16-manual-queue.json', import.meta.url)));
const valid=new Set(['RECOVERED_SOURCE','MISSING_CANONICAL_SOURCE']);

test('every seeded queue item has a precise recovery result',()=>{
  const byId=new Map(manifest.items.map(x=>[x.source_id,x]));
  assert.equal(queue.items.length,9);
  for(const item of queue.items){
    const recovered=byId.get(item.source_id);
    assert.ok(recovered,`missing recovery result for ${item.source_id}`);
    assert.ok(valid.has(recovered.recovery_status),`invalid recovery status for ${item.source_id}`);
  }
});

test('recovered multi-recipient packages include explicit recipient manifests',()=>{
  for(const id of ['stars-stripes-accountability','clarity-2-congressional','ib-iba-kremlev-trumpjr-oversight']){
    const item=manifest.items.find(x=>x.source_id===id);
    assert.equal(item.recovery_status,'RECOVERED_SOURCE');
    assert.equal(item.recipient_manifest_status,'RECIPIENT_MANIFEST_RECOVERED');
    assert.ok(item.recipients.length>=2);
  }
});

test('manual queue no longer uses ambiguous pre-recovery placeholders',()=>{
  for(const item of queue.items) assert.doesNotMatch(item.status_hint,/PREPARED_PACKAGE_REQUIRED|SPLIT_RECIPIENTS_REQUIRED|RECIPIENT_PROFILE_REQUIRED|RECIPIENT_RECONCILIATION_REQUIRED/);
});
