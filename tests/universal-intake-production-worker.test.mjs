import test from 'node:test';
import assert from 'node:assert/strict';
import { acquirePortalSession, stripSessionSecrets } from '../governance/universal-intake-session.mjs';
import { createBrowserPortalWorker } from '../governance/universal-intake-browser-worker.mjs';

const request = {
  request_id:'UPIS-2026-PROD-1', request_type:'PUBLIC_RECORDS', requester:{name:'Jay Price',email:'jay@example.test'},
  recipient:{authority_id:'CA_CDT_PRA',authority_name:'California Department of Technology'}, subject:'Records', summary:'Produce records',
  requested_action:'Produce electronically', provenance:{package_hash:'sha256:req'}, attachments:[{name:'evidence.pdf',sha256:'abc'}]
};


test('session boundary returns only scoped ephemeral handle metadata', async () => {
  const session = await acquirePortalSession({ authority_id:'CA_CDT_PRA', request_id:request.request_id }, {
    sessionProvider: async () => ({ handle:'session-1', expires_at:'2026-09-16T11:00:00Z', scope:['CA_CDT_PRA'], cookies:['secret'], token:'secret' }),
    now: () => new Date('2026-09-16T10:00:00Z')
  });
  assert.equal(session.handle,'session-1');
  assert.deepEqual(session.scope,['CA_CDT_PRA']);
  assert.equal('cookies' in session,false);
  assert.equal('token' in session,false);
});

test('session boundary rejects wrong scope and expired sessions', async () => {
  await assert.rejects(() => acquirePortalSession({authority_id:'CA_CDT_PRA',request_id:'x'}, {sessionProvider:async()=>({handle:'x',expires_at:'2026-09-16T09:00:00Z',scope:['CA_CDT_PRA']}),now:()=>new Date('2026-09-16T10:00:00Z')}), /expired/i);
  await assert.rejects(() => acquirePortalSession({authority_id:'CA_CDT_PRA',request_id:'x'}, {sessionProvider:async()=>({handle:'x',expires_at:'2026-09-16T11:00:00Z',scope:['OTHER']}),now:()=>new Date('2026-09-16T10:00:00Z')}), /scope/i);
});

test('secret stripping recursively removes sensitive session material', () => {
  assert.deepEqual(stripSessionSecrets({handle:'h',token:'x',cookies:[1],nested:{password:'p',ok:true}}), {handle:'h',nested:{ok:true}});
});

test('browser worker maps semantic fields, uploads attachments and returns evidence', async () => {
  const calls=[];
  const worker=createBrowserPortalWorker({agencySafetyGate:async()=>({allowed:true,reason:'allow'}),
    sessionProvider: async()=>({handle:'sess',expires_at:'2026-09-16T11:00:00Z',scope:['CA_CDT_PRA']}),
    now:()=>new Date('2026-09-16T10:00:00Z'),
    driverFactory: async()=>({
      open:async x=>calls.push(['open',x]), fill:async(a,b)=>calls.push(['fill',a,b]), upload:async(a,b)=>calls.push(['upload',a,b]),
      detectAuthorizationDenial:async()=>null, detectHumanGate:async()=>null, validate:async()=>({ok:true,lossy_transformations:[]}), submit:async()=>({tracking_id:'CDT-9',received_at:'2026-09-16T10:02:00Z',confirmation_url:'https://example.test/r/9',screenshot_hash:'sha256:shot'})
    })
  });
  const result=await worker({request,authority:{id:'CA_CDT_PRA'},transport:{endpoint:'https://example.test'},profile:{semantic_fields:{requester_name:'requester.name',subject:'subject'},attachments_field:'attachments'},fingerprint:'sha256:fp'});
  assert.equal(result.state,'SUBMITTED');
  assert.equal(result.tracking_id,'CDT-9');
  assert.equal(result.fingerprint,'sha256:fp');
  assert.ok(calls.some(c=>c[0]==='upload'));
});

test('browser worker stops on human gate without submitting', async () => {
  let submitted=false;
  const worker=createBrowserPortalWorker({agencySafetyGate:async()=>({allowed:true,reason:'allow'}),sessionProvider:async()=>({handle:'s',expires_at:'2026-09-16T11:00:00Z',scope:['EU_COMMISSION_1049']}),now:()=>new Date('2026-09-16T10:00:00Z'),driverFactory:async()=>({open:async()=>{},fill:async()=>{},upload:async()=>{},detectAuthorizationDenial:async()=>null, detectHumanGate:async()=>({reason:'MFA',resume_token:'r1'}),validate:async()=>({ok:true,lossy_transformations:[]}),submit:async()=>{submitted=true;}})});
  const result=await worker({request:{...request,recipient:{authority_id:'EU_COMMISSION_1049'}},authority:{id:'EU_COMMISSION_1049'},transport:{endpoint:'https://example.test'},profile:{semantic_fields:{}},fingerprint:'fp'});
  assert.equal(result.state,'HUMAN_REQUIRED'); assert.equal(result.reason,'MFA'); assert.equal(submitted,false);
});

test('browser worker rejects lossy transformation before submission', async () => {
  const worker=createBrowserPortalWorker({agencySafetyGate:async()=>({allowed:true,reason:'allow'}),sessionProvider:async()=>({handle:'s',expires_at:'2026-09-16T11:00:00Z',scope:['CA_CDT_PRA']}),now:()=>new Date('2026-09-16T10:00:00Z'),driverFactory:async()=>({open:async()=>{},fill:async()=>{},upload:async()=>{},detectAuthorizationDenial:async()=>null, detectHumanGate:async()=>null,validate:async()=>({ok:false,lossy_transformations:['summary truncated']}),submit:async()=>({})})});
  const result=await worker({request,authority:{id:'CA_CDT_PRA'},transport:{endpoint:'https://example.test'},profile:{semantic_fields:{}},fingerprint:'fp'});
  assert.equal(result.state,'HUMAN_REQUIRED'); assert.equal(result.reason,'LOSSY_TRANSFORMATION');
});


test('browser worker denies before session and navigation when agency safety gate denies', async () => {
  let opened=false;
  const worker=createBrowserPortalWorker({
    agencySafetyGate:async()=>({allowed:false,reason:'third_party_authorization_denial_circumvention'}),
    sessionProvider:async()=>({handle:'s',expires_at:'2026-09-16T11:00:00Z',scope:['CA_CDT_PRA']}),
    now:()=>new Date('2026-09-16T10:00:00Z'),
    driverFactory:async()=>({open:async()=>{opened=true;}})
  });
  const result=await worker({request,authority:{id:'CA_CDT_PRA'},transport:{endpoint:'https://example.test'},profile:{semantic_fields:{}},fingerprint:'fp'});
  assert.equal(result.state,'ACTION_REQUIRED');
  assert.equal(result.reason,'third_party_authorization_denial_circumvention');
  assert.equal(opened,false);
});


test('browser worker records observed authorization denial and stops before fill or submit', async () => {
  const calls=[];
  let recorded=null;
  const worker=createBrowserPortalWorker({
    agencySafetyGate:async()=>({allowed:true,reason:'allow'}),
    agencyDenialRecorder:async(input,denial)=>{recorded={input,denial};return {recorded:true};},
    sessionProvider:async()=>({handle:'s',expires_at:'2026-09-16T11:00:00Z',scope:['CA_CDT_PRA']}),
    now:()=>new Date('2026-09-16T10:00:00Z'),
    driverFactory:async()=>({
      open:async()=>calls.push('open'),
      detectAuthorizationDenial:async()=>({denied:true,evidence_id:'deny-http-403',denied_at_utc:'2026-09-16T10:01:00Z'}),
      fill:async()=>calls.push('fill'),
      detectHumanGate:async()=>null,
      validate:async()=>({ok:true,lossy_transformations:[]}),
      submit:async()=>{calls.push('submit');return {};}
    })
  });
  const result=await worker({request,authority:{id:'CA_CDT_PRA'},transport:{endpoint:'https://example.test'},profile:{semantic_fields:{subject:'subject'}},fingerprint:'fp'});
  assert.equal(result.state,'ACTION_REQUIRED');
  assert.equal(result.reason,'THIRD_PARTY_AUTHORIZATION_DENIED');
  assert.equal(recorded.denial.evidence_id,'deny-http-403');
  assert.deepEqual(calls,['open']);
});

test('browser worker fails closed when authorization-boundary detector is unavailable', async () => {
  const worker=createBrowserPortalWorker({
    agencySafetyGate:async()=>({allowed:true,reason:'allow'}),
    sessionProvider:async()=>({handle:'s',expires_at:'2026-09-16T11:00:00Z',scope:['CA_CDT_PRA']}),
    now:()=>new Date('2026-09-16T10:00:00Z'),
    driverFactory:async()=>({open:async()=>{}})
  });
  const result=await worker({request,authority:{id:'CA_CDT_PRA'},transport:{endpoint:'https://example.test'},profile:{semantic_fields:{}},fingerprint:'fp'});
  assert.equal(result.state,'ACTION_REQUIRED');
  assert.equal(result.reason,'AUTHORIZATION_BOUNDARY_DETECTION_UNAVAILABLE');
});
