import test from 'node:test';
import assert from 'node:assert/strict';
import { getAuthority, resolveAuthorityTransport } from '../governance/universal-intake-registry.mjs';

const registry = {
  authorities: {
    NIST_FOIA: {
      id: 'NIST_FOIA',
      authority_name: 'NIST',
      transports: [
        { kind: 'PORTAL', executable: false, endpoint: 'https://example.test/nist' },
        { kind: 'EMAIL', executable: true, endpoint: 'foia@nist.gov' }
      ]
    }
  }
};

const request = { recipient: { authority_id: 'NIST_FOIA', authority_name: 'NIST' } };

test('returns a registered authority by stable id', () => {
  assert.equal(getAuthority(registry, 'NIST_FOIA').authority_name, 'NIST');
});

test('fails closed for missing or malformed authority records', () => {
  assert.throws(() => getAuthority({}, 'NIST_FOIA'), /registry/i);
  assert.throws(() => getAuthority({ authorities: {} }, 'NIST_FOIA'), /authority/i);
});

test('prefers an executable route over a portal handoff', () => {
  const resolved = resolveAuthorityTransport(request, registry);
  assert.equal(resolved.kind, 'EMAIL');
  assert.equal(resolved.endpoint, 'foia@nist.gov');
});

test('returns manual handoff only when no executable route exists', () => {
  const manual = structuredClone(registry);
  manual.authorities.NIST_FOIA.transports = [{ kind: 'PORTAL', executable: false, endpoint: 'https://example.test/nist' }];
  assert.deepEqual(resolveAuthorityTransport(request, manual), {
    kind: 'MANUAL_REQUIRED', executable: false, endpoint: 'https://example.test/nist'
  });
});
