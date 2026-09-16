import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const registry = JSON.parse(await readFile(new URL('../governance/universal-intake-authorities.json', import.meta.url)));

const executableEmail = ['NIST_FOIA', 'FTC_FOIA', 'UK_DSIT_FOI', 'DOJ_FOIA'];
for (const id of executableEmail) {
  test(`${id} exposes an executable email route`, () => {
    const authority = registry.authorities[id];
    assert.ok(authority, `missing ${id}`);
    const route = authority.transports.find(t => t.kind === 'EMAIL' && t.executable === true);
    assert.ok(route?.endpoint);
    assert.ok(authority.source_url);
    assert.match(authority.reviewed_at, /^2026-09-16$/);
  });
}

const portalOnly = ['CA_CDT_PRA', 'EU_COMMISSION_1049'];
for (const id of portalOnly) {
  test(`${id} remains portal-only until an executable adapter is available`, () => {
    const authority = registry.authorities[id];
    assert.ok(authority, `missing ${id}`);
    assert.equal(authority.transports.some(t => t.executable === true), false);
    assert.ok(authority.transports.find(t => t.kind === 'PORTAL')?.endpoint);
    assert.ok(authority.source_url);
  });
}

test('DOJ referral route preserves a portal fallback and explains its scope', () => {
  const authority = registry.authorities.DOJ_FOIA;
  assert.equal(authority.transports.find(t => t.kind === 'EMAIL' && t.executable)?.endpoint, 'MRUFOIA.Requests@usdoj.gov');
  assert.ok(authority.transports.find(t => t.kind === 'PORTAL')?.endpoint);
  assert.match(authority.notes, /component/i);
});
