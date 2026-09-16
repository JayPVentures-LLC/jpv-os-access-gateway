import test from 'node:test';
import assert from 'node:assert/strict';
import {
  normalizeRequest,
  selectTransport,
  normalizeReceipt,
  transitionSubmission
} from '../governance/universal-public-intake.mjs';

const base = {
  request_id: 'UPIS-2026-000001',
  request_type: 'PUBLIC_RECORDS',
  requester: { name: 'Jay Price', organization: 'JPV' },
  recipient: { authority_name: 'NIST', jurisdiction: 'US-FED' },
  subject: 'Frontier AI standards provenance',
  requested_action: 'Produce responsive records electronically.',
  provenance: { package_hash: 'sha256:abc123' }
};

test('normalizes one canonical request without transport-specific mutation', () => {
  const out = normalizeRequest(base);
  assert.equal(out.request_id, base.request_id);
  assert.equal(out.status, 'NEW');
  assert.deepEqual(out.requester, base.requester);
  assert.deepEqual(out.provenance, base.provenance);
});

test('fails closed when required canonical fields are missing', () => {
  assert.throws(() => normalizeRequest({ ...base, provenance: undefined }), /provenance/);
});

test('routes to the highest-authority executable transport', () => {
  const registry = {
    NIST: { transports: [{ kind: 'PORTAL', executable: false }, { kind: 'EMAIL', executable: true, endpoint: 'foia@nist.gov' }] }
  };
  assert.deepEqual(selectTransport(base, registry), { kind: 'EMAIL', executable: true, endpoint: 'foia@nist.gov' });
});

test('returns MANUAL_REQUIRED only when no executable transport exists', () => {
  const registry = { NIST: { transports: [{ kind: 'PORTAL', executable: false, endpoint: 'https://example.test' }] } };
  assert.deepEqual(selectTransport(base, registry), { kind: 'MANUAL_REQUIRED', executable: false, endpoint: 'https://example.test' });
});

test('normalizes external receipts into canonical lifecycle states', () => {
  const receipt = normalizeReceipt(base.request_id, { tracking_id: 'NIST-26-1', status: 'received', received_at: '2026-09-16T09:00:00Z' }, 'NIST');
  assert.equal(receipt.status, 'SUBMITTED');
  assert.equal(receipt.external_tracking_id, 'NIST-26-1');
  assert.equal(receipt.request_id, base.request_id);
});

test('prevents SUBMITTED without auditable transport evidence', () => {
  assert.throws(() => transitionSubmission({ ...normalizeRequest(base), status: 'ROUTED' }, 'SUBMITTED'), /receipt/);
});

test('allows SUBMITTED with receipt evidence and preserves provenance', () => {
  const request = { ...normalizeRequest(base), status: 'ROUTED' };
  const receipt = { external_tracking_id: 'NIST-26-1', received_at: '2026-09-16T09:00:00Z' };
  const out = transitionSubmission(request, 'SUBMITTED', receipt);
  assert.equal(out.status, 'SUBMITTED');
  assert.deepEqual(out.provenance, base.provenance);
  assert.deepEqual(out.receipt, receipt);
});
