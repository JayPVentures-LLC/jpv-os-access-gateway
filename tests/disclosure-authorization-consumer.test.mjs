import test from 'node:test';
import assert from 'node:assert/strict';
import { authorizeClaimConsumerAction } from '../governance/claim-truth-consumer.mjs';

const base = {
  claim_id: 'c-public',
  status: 'KNOWN',
  provenance_ref: 'urn:jpv:claim:c-public',
  action: 'PUBLIC_ROUTE',
  privacy_state: 'PUBLIC_AUTHORIZED',
  authorization_ref: 'urn:jpv:authorization:1',
  authorization_scope: {
    audiences: ['FRIENDS'], purposes: ['PERSONAL'], media: ['SPOKEN'], content_refs: ['RELATIONSHIP_STATUS'],
    valid_from: '2026-08-18T00:00:00Z', valid_until: null, revoked_at: null
  },
  requested_disclosure: {
    audience: 'FRIENDS', purpose: 'PERSONAL', medium: 'SPOKEN', content_refs: ['RELATIONSHIP_STATUS'],
    at: '2026-08-18T03:00:00Z', derived_from: [], source_privacy_state: 'PUBLIC_AUTHORIZED'
  }
};

test('public routing requires current scoped authorization', () => {
  const result = authorizeClaimConsumerAction(base);
  assert.equal(result.decision, 'ALLOW');
  assert.equal(result.may_publish, true);
  assert.equal(result.authorization_ref, base.authorization_ref);
});

test('private state cannot be converted to public routing', () => {
  const result = authorizeClaimConsumerAction({...base, privacy_state: 'PRIVATE'});
  assert.equal(result.decision, 'DENY');
  assert.ok(result.defects.includes('PUBLIC_AUTHORIZATION_REQUIRED'));
});

test('audience expansion is denied downstream', () => {
  const result = authorizeClaimConsumerAction({...base, requested_disclosure: {...base.requested_disclosure, audience: 'PUBLIC'}});
  assert.equal(result.decision, 'DENY');
  assert.ok(result.defects.includes('DISCLOSURE_AUDIENCE_NOT_AUTHORIZED'));
});

test('purpose and medium expansion are denied downstream', () => {
  const purpose = authorizeClaimConsumerAction({...base, requested_disclosure: {...base.requested_disclosure, purpose: 'MARKETING'}});
  assert.ok(purpose.defects.includes('DISCLOSURE_PURPOSE_NOT_AUTHORIZED'));
  const medium = authorizeClaimConsumerAction({...base, requested_disclosure: {...base.requested_disclosure, medium: 'SCREENSHOT'}});
  assert.ok(medium.defects.includes('DISCLOSURE_MEDIUM_NOT_AUTHORIZED'));
});

test('revoked authorization blocks future public routing', () => {
  const result = authorizeClaimConsumerAction({...base, authorization_scope: {...base.authorization_scope, revoked_at: '2026-08-18T02:00:00Z'}});
  assert.equal(result.decision, 'DENY');
  assert.ok(result.defects.includes('DISCLOSURE_AUTHORIZATION_REVOKED'));
});

test('derived private content remains private downstream', () => {
  const result = authorizeClaimConsumerAction({...base, requested_disclosure: {...base.requested_disclosure, derived_from: ['urn:jpv:private:1'], source_privacy_state: 'PRIVATE'}});
  assert.equal(result.decision, 'DENY');
  assert.ok(result.defects.includes('DERIVED_DISCLOSURE_INHERITS_PRIVATE_STATE'));
});

test('unclear scope fails closed', () => {
  const result = authorizeClaimConsumerAction({...base, requested_disclosure: {audience: 'FRIENDS'}});
  assert.equal(result.decision, 'DENY');
  assert.ok(result.defects.includes('DISCLOSURE_SCOPE_INCOMPLETE'));
});
