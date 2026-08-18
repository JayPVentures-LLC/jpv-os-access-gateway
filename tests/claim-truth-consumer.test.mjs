import test from 'node:test';
import assert from 'node:assert/strict';
import { authorizeClaimConsumerAction } from '../governance/claim-truth-consumer.mjs';

const base = { claim_id:'c1', status:'KNOWN', provenance_ref:'urn:jpv:claim:c1', action:'ROUTE' };

test('preserves canonical truth status and provenance', () => {
  const result = authorizeClaimConsumerAction(base);
  assert.equal(result.decision, 'ALLOW');
  assert.equal(result.status, 'KNOWN');
  assert.equal(result.provenance_ref, base.provenance_ref);
});

test('rejects silent promotion from UNKNOWN to KNOWN', () => {
  const result = authorizeClaimConsumerAction({...base, status:'UNKNOWN', requested_status:'KNOWN'});
  assert.equal(result.decision, 'DENY');
  assert.ok(result.defects.includes('STATUS_PROMOTION_DENIED'));
});

test('allows routing of uncertainty only when the uncertainty label is preserved', () => {
  const result = authorizeClaimConsumerAction({...base, status:'INFERRED', requested_status:'INFERRED'});
  assert.equal(result.decision, 'ALLOW');
  assert.equal(result.may_enforce, false);
});

test('blocks enforcement unless status is KNOWN', () => {
  for (const status of ['UNKNOWN','INFERRED','DISPUTED']) {
    const result = authorizeClaimConsumerAction({...base, status, requested_status:status, action:'ENFORCE'});
    assert.equal(result.decision, 'DENY', status);
    assert.ok(result.defects.includes('KNOWN_REQUIRED_FOR_ENFORCEMENT'));
  }
});

test('requires provenance on every consequential consumed claim', () => {
  const result = authorizeClaimConsumerAction({...base, provenance_ref:''});
  assert.equal(result.decision, 'DENY');
  assert.ok(result.defects.includes('PROVENANCE_REQUIRED'));
});

test('preserves contradictions and correction references', () => {
  const result = authorizeClaimConsumerAction({...base, status:'DISPUTED', requested_status:'DISPUTED', contradictions:['urn:jpv:evidence:x'], correction_ref:'urn:jpv:correction:1'});
  assert.equal(result.decision, 'ALLOW');
  assert.deepEqual(result.contradictions, ['urn:jpv:evidence:x']);
  assert.equal(result.correction_ref, 'urn:jpv:correction:1');
});
