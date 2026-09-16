import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const registry = JSON.parse(await readFile(new URL('../governance/universal-intake-authorities.json', import.meta.url)));

for (const id of ['NIST_FOIA', 'FTC_FOIA', 'UK_DSIT_FOI', 'DOJ_FOIA']) {
  test(`${id} exposes an executable email route`, () => {
    const authority = registry.authorities[id];
    assert.ok(authority); assert.ok(authority.transports.find(t => t.kind === 'EMAIL' && t.executable)?.endpoint); assert.ok(authority.source_url);
  });
}

for (const id of ['CA_CDT_PRA', 'EU_COMMISSION_1049']) {
  test(`${id} exposes only worker-backed executable portal routing`, () => {
    const authority = registry.authorities[id];
    const route = authority.transports.find(t => t.kind === 'PORTAL');
    assert.ok(route?.endpoint); assert.equal(route.executable, true); assert.equal(route.worker_required, true); assert.ok(route.profile); assert.ok(authority.source_url);
  });
}

test('DOJ referral route preserves portal fallback and component scope', () => {
  const authority = registry.authorities.DOJ_FOIA;
  assert.equal(authority.transports.find(t => t.kind === 'EMAIL' && t.executable)?.endpoint, 'MRUFOIA.Requests@usdoj.gov');
  assert.ok(authority.transports.find(t => t.kind === 'PORTAL')?.endpoint); assert.match(authority.notes, /component/i);
});
