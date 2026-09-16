import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { getAuthority } from '../governance/universal-intake-registry.mjs';
import { executeIntake } from '../governance/universal-intake-adapters.mjs';

const registry = JSON.parse(await readFile(new URL('../governance/universal-intake-authorities.json', import.meta.url)));
const base = {
  request_id: 'UPIS-2026-000003',
  request_type: 'PUBLIC_RECORDS',
  requester: { name: 'Jay Price', organization: 'JPV' },
  subject: 'Universal intake test',
  summary: 'Test request',
  requested_action: 'Produce responsive records electronically.',
  provenance: { package_hash: 'sha256:def' },
  status: 'ROUTED'
};

test('canonical request reaches executable email route and returns SUBMITTED receipt', async () => {
  const request = { ...base, recipient: { authority_id: 'FTC_FOIA', authority_name: 'FTC' } };
  const authority = getAuthority(registry, 'FTC_FOIA');
  const result = await executeIntake(request, authority, {
    sendEmail: async () => ({ tracking_id: 'mail-1', received_at: '2026-09-16T11:00:00Z', status: 'received' })
  });
  assert.equal(result.transport.kind, 'EMAIL');
  assert.equal(result.request.status, 'SUBMITTED');
  assert.equal(result.receipt.request_id, request.request_id);
});

test('portal-only authority yields complete handoff without false submission state', async () => {
  const request = { ...base, recipient: { authority_id: 'CA_CDT_PRA', authority_name: 'California Department of Technology' } };
  const authority = getAuthority(registry, 'CA_CDT_PRA');
  const result = await executeIntake(request, authority, {});
  assert.equal(result.transport.kind, 'MANUAL_REQUIRED');
  assert.equal(result.request.status, 'ACTION_REQUIRED');
  assert.equal(result.handoff.request_id, request.request_id);
  assert.ok(result.handoff.endpoint.startsWith('https://'));
});
