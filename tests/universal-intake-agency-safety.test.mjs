import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { authorizePortalTarget } from '../governance/universal-intake-agency-safety.mjs';

async function withState(denials, grants, fn) {
  const dir = await mkdtemp(join(tmpdir(), 'jpv-agency-'));
  try {
    await writeFile(join(dir,'target-denials.json'), JSON.stringify(denials));
    await writeFile(join(dir,'security-testing-grants.json'), JSON.stringify(grants));
    await fn(dir);
  } finally { await rm(dir,{recursive:true,force:true}); }
}

const input = (overrides={}) => ({
  request:{request_id:'r1',...(overrides.request??{})},
  transport:{endpoint:'https://example.test/portal',agency_method:'PORTAL_SUBMIT',...(overrides.transport??{})}
});

test('allows unrelated target when authoritative denial store has no matching denial', async () => {
  await withState([{target_resource_id:'https://blocked.test',evidence_id:'d1'}],[],async dataDir=>{
    const out=await authorizePortalTarget(input(),{dataDir});
    assert.equal(out.allowed,true);
  });
});

test('denies matching prior target denial without trusted security grant', async () => {
  await withState([{target_resource_id:'https://example.test/portal',evidence_id:'d1'}],[],async dataDir=>{
    const out=await authorizePortalTarget(input(),{dataDir});
    assert.equal(out.allowed,false);
    assert.equal(out.reason,'third_party_authorization_denial_circumvention');
  });
});

test('caller supplied grant id is insufficient unless present in authoritative grant store', async () => {
  await withState([{target_resource_id:'https://example.test/portal',evidence_id:'d1'}],[],async dataDir=>{
    const out=await authorizePortalTarget(input({request:{security_testing_authorization_id:'sec-1'}}),{dataDir});
    assert.equal(out.allowed,false);
  });
});

test('grant must match target method and remain unexpired', async () => {
  const denial=[{target_resource_id:'https://example.test/portal',evidence_id:'d1'}];
  const validUntil=new Date(Date.now()+60_000).toISOString();
  await withState(denial,[{authorization_id:'sec-1',target_resource_id:'https://example.test/portal',method:'GET',valid_until:validUntil}],async dataDir=>{
    const out=await authorizePortalTarget(input({request:{security_testing_authorization_id:'sec-1'}}),{dataDir});
    assert.equal(out.allowed,false);
  });
  await withState(denial,[{authorization_id:'sec-1',target_resource_id:'https://example.test/portal',method:'PORTAL_SUBMIT',valid_until:'2000-01-01T00:00:00Z'}],async dataDir=>{
    const out=await authorizePortalTarget(input({request:{security_testing_authorization_id:'sec-1'}}),{dataDir});
    assert.equal(out.allowed,false);
  });
  await withState(denial,[{authorization_id:'sec-1',target_resource_id:'https://example.test/portal',method:'PORTAL_SUBMIT',valid_until:validUntil}],async dataDir=>{
    const out=await authorizePortalTarget(input({request:{security_testing_authorization_id:'sec-1'}}),{dataDir});
    assert.equal(out.allowed,true);
  });
});

test('missing or malformed authoritative state fails closed', async () => {
  const dir = await mkdtemp(join(tmpdir(), 'jpv-agency-'));
  try {
    await assert.rejects(()=>authorizePortalTarget(input(),{dataDir:dir}),/authoritative_denial_state_unavailable/);
    await writeFile(join(dir,'target-denials.json'),'{bad');
    await assert.rejects(()=>authorizePortalTarget(input(),{dataDir:dir}),/authoritative_denial_state_unavailable/);
  } finally { await rm(dir,{recursive:true,force:true}); }
});
