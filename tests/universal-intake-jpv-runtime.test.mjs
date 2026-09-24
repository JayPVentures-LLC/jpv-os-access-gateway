import test from 'node:test';
import assert from 'node:assert/strict';
import { createJpvNativeDriverFactory } from '../governance/universal-intake-jpv-runtime.mjs';

test('requires a verified JPV_NATIVE deployment target', async()=>{
  const factory=createJpvNativeDriverFactory({jpvDeploy:async()=>null,transport:async()=>({ok:true})});
  await assert.rejects(()=>factory({authority_id:'CA_CDT_PRA',session_handle:'s',fingerprint:'f'}),/JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE/);
});

test('rejects external provider runtime even when reachable', async()=>{
  const factory=createJpvNativeDriverFactory({jpvDeploy:async()=>({platform:'VERCEL',verified:true,endpoint:'https://x.example'}),transport:async()=>({ok:true})});
  await assert.rejects(()=>factory({authority_id:'CA_CDT_PRA',session_handle:'s',fingerprint:'f'}),/external provider runtime/i);
});

test('creates semantic driver for verified JPV_NATIVE target', async()=>{
  const calls=[];
  const factory=createJpvNativeDriverFactory({
    jpvDeploy:async()=>({platform:'JPV_NATIVE',verified:true,target:'jpv-native-primary',revision:'abc',endpoint:'https://runtime.jpv.internal'}),
    transport:async payload=>{calls.push(payload);if(payload.op==='DETECT_AUTHORIZATION_DENIAL') return {ok:true,denial:null};return payload.op==='SUBMIT'?{ok:true,tracking_id:'T1',received_at:'2026-09-16T13:00:00Z',confirmation_url:'https://r'}:{ok:true};}
  });
  const driver=await factory({authority_id:'CA_CDT_PRA',session_handle:'sess',fingerprint:'fp'});
  await driver.open('https://portal.example');
  const denial=await driver.detectAuthorizationDenial();
  const receipt=await driver.submit({request_id:'R1',fingerprint:'fp'});
  assert.equal(denial,null);
  assert.equal(receipt.tracking_id,'T1');
  assert.equal(calls[0].platform,'JPV_NATIVE');
  assert.equal(calls[0].session_handle,'sess');
});
