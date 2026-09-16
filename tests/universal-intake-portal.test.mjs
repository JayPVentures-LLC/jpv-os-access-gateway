import test from 'node:test';
import assert from 'node:assert/strict';
import { executePortal, portalFingerprint } from '../governance/universal-intake-portal.mjs';

const request = {
  request_id: 'UPIS-2026-PORTAL-1',
  request_type: 'PUBLIC_RECORDS',
  requester: { name: 'Jay Price', organization: 'JPV' },
  recipient: { authority_id: 'CA_CDT_PRA', authority_name: 'California Department of Technology' },
  subject: 'Frontier AI standards provenance',
  summary: 'Produce responsive records.',
  requested_action: 'Produce responsive records electronically.',
  provenance: { package_hash: 'sha256:abc' },
  status: 'ROUTED'
};
const authority = { id: 'CA_CDT_PRA', authority_name: 'California Department of Technology' };
const transport = { kind: 'PORTAL', executable: true, endpoint: 'https://example.test', profile: 'ca-cdt-pra' };

test('generates a stable idempotency fingerprint', () => {
  assert.equal(portalFingerprint(request, authority), portalFingerprint(structuredClone(request), authority));
});

test('returns verified receipt and evidence after worker submission', async () => {
  const result = await executePortal(request, authority, transport, {
    loadPortalProfile: async () => ({ id: 'ca-cdt-pra', fields: {} }),
    portalWorker: async input => ({ state: 'SUBMITTED', tracking_id: 'CDT-123', received_at: '2026-09-16T10:00:00Z', evidence: { confirmation_url: 'https://example.test/receipt/123', screenshot_hash: 'sha256:shot' }, fingerprint: input.fingerprint })
  });
  assert.equal(result.state, 'SUBMITTED');
  assert.equal(result.tracking_id, 'CDT-123');
  assert.equal(result.evidence.screenshot_hash, 'sha256:shot');
});

test('fails closed to human-required state for CAPTCHA or MFA', async () => {
  const result = await executePortal(request, authority, transport, {
    loadPortalProfile: async () => ({ id: 'ca-cdt-pra', fields: {} }),
    portalWorker: async () => ({ state: 'HUMAN_REQUIRED', reason: 'MFA', resume_token: 'resume-1' })
  });
  assert.deepEqual(result, { state: 'HUMAN_REQUIRED', reason: 'MFA', resume_token: 'resume-1' });
});

test('rejects submitted worker output without verifiable evidence', async () => {
  await assert.rejects(() => executePortal(request, authority, transport, {
    loadPortalProfile: async () => ({ id: 'ca-cdt-pra', fields: {} }),
    portalWorker: async () => ({ state: 'SUBMITTED', tracking_id: 'CDT-123', received_at: '2026-09-16T10:00:00Z' })
  }), /evidence/i);
});

test('unknown portal states fail closed', async () => {
  const result = await executePortal(request, authority, transport, {
    loadPortalProfile: async () => ({ id: 'ca-cdt-pra', fields: {} }),
    portalWorker: async () => ({ state: 'ALIEN_PAGE' })
  });
  assert.equal(result.state, 'HUMAN_REQUIRED');
  assert.equal(result.reason, 'UNKNOWN_PORTAL_STATE');
});
