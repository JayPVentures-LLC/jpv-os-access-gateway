import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

for (const name of ['ca-cdt-pra', 'eu-commission-1049']) {
  test(`${name} is semantic and fail-closed`, async () => {
    const profile = JSON.parse(await readFile(new URL(`../governance/portal-profiles/${name}.json`, import.meta.url)));
    assert.equal(profile.id, name);
    assert.equal(profile.selectors_in_core, false);
    assert.ok(profile.semantic_fields.canonical_request_id);
    assert.ok(profile.human_gates.includes('CAPTCHA'));
    assert.ok(profile.human_gates.includes('MFA'));
    assert.ok(profile.receipt_requirements.includes('tracking_id'));
    assert.ok(profile.receipt_requirements.includes('received_at'));
  });
}
