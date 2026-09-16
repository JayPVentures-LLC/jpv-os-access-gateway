import test from 'node:test';
import assert from 'node:assert/strict';
import { executeIntake } from '../governance/universal-intake-adapters.mjs';

const request = {
  request_id: 'UPIS-2026-000002', request_type: 'PUBLIC_RECORDS',
  requester: { name: 'Jay Price', organization: 'JPV', email: 'jay@example.test' },
  recipient: { authority_id: 'NIST_FOIA', authority_name: 'NIST' },
  subject: 'Records request', summary: 'Produce responsive records.',
  requested_action: 'Produce responsive records electronically.',
  provenance: { package_hash: 'sha256:abc' }, status: 'ROUTED'
};

test('executes an email route and returns an auditable canonical receipt', async () => {
  const authority = { id: 'NIST_FOIA', authority_name: 'NIST', transports: [{ kind: 'EMAIL', executable: true, endpoint: 'foia@nist.gov' }] };
  const sent = [];
  const result = await executeIntake(request, authority, { sendEmail: async payload => { sent.push(payload); return { tracking_id: 'gmail:123', received_at: '2026-09-16T10:00:00Z', status: 'received' }; } });
  assert.equal(sent[0].to, 'foia@nist.gov'); assert.equal(result.receipt.status, 'SUBMITTED'); assert.equal(result.request.status, 'SUBMITTED');
});

test('executes an API route through injected fetch and requires tracking evidence', async () => {
  const authority = { id: 'API_TEST', authority_name: 'API Test', transports: [{ kind: 'API', executable: true, endpoint: 'https://api.example.test/requests' }] };
  const result = await executeIntake(request, authority, { fetch: async () => ({ ok: true, json: async () => ({ tracking_id: 'api-1', received_at: '2026-09-16T10:01:00Z', status: 'accepted' }) }) });
  assert.equal(result.receipt.external_tracking_id, 'api-1');
});

test('fails closed when executable transport lacks receipt evidence', async () => {
  const authority = { id: 'NIST_FOIA', authority_name: 'NIST', transports: [{ kind: 'EMAIL', executable: true, endpoint: 'foia@nist.gov' }] };
  await assert.rejects(() => executeIntake(request, authority, { sendEmail: async () => ({ status: 'received' }) }), /tracking_id|receipt/i);
});

test('returns a complete manual handoff without false submission', async () => {
  const authority = { id: 'DOJ_FOIA', authority_name: 'NIST', profile: 'US-FOIA', transports: [{ kind: 'PORTAL', executable: false, endpoint: 'https://www.foia.gov/' }] };
  const result = await executeIntake(request, authority, {});
  assert.equal(result.transport.kind, 'MANUAL_REQUIRED'); assert.equal(result.request.status, 'ACTION_REQUIRED'); assert.equal(result.receipt, undefined);
});

test('executable portal delegates to authenticated worker and records evidence', async () => {
  const authority = { id: 'CA_CDT_PRA', authority_name: 'NIST', transports: [{ kind: 'PORTAL', executable: true, endpoint: 'https://example.test', profile: 'ca-cdt-pra' }] };
  const result = await executeIntake(request, authority, {
    loadPortalProfile: async () => ({ id: 'ca-cdt-pra', fields: {} }),
    portalWorker: async input => ({ state: 'SUBMITTED', tracking_id: 'CDT-1', received_at: '2026-09-16T10:02:00Z', evidence: { confirmation_url: 'https://example.test/r/1' }, fingerprint: input.fingerprint })
  });
  assert.equal(result.request.status, 'SUBMITTED'); assert.equal(result.receipt.external_tracking_id, 'CDT-1'); assert.ok(result.receipt.evidence.confirmation_url);
});

test('portal human-required state stays ACTION_REQUIRED and resumable', async () => {
  const authority = { id: 'EU', authority_name: 'NIST', transports: [{ kind: 'PORTAL', executable: true, endpoint: 'https://example.test', profile: 'eu' }] };
  const result = await executeIntake(request, authority, {
    loadPortalProfile: async () => ({ id: 'eu', fields: {} }), portalWorker: async () => ({ state: 'HUMAN_REQUIRED', reason: 'CAPTCHA', resume_token: 'resume-eu' })
  });
  assert.equal(result.request.status, 'ACTION_REQUIRED'); assert.equal(result.handoff.reason, 'CAPTCHA'); assert.equal(result.handoff.resume_token, 'resume-eu');
});
