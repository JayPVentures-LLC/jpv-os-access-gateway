import test from 'node:test';
import assert from 'node:assert/strict';
import { createJpvNativeDriverFactory } from '../governance/universal-intake-jpv-runtime.mjs';

const ctx={authority_id:'CA_CDT_PRA',session_handle:'s',fingerprint:'f'};

test('requires a verified JPV_NATIVE deployment target', async()=>{
  const factory=createJpvNativeDriverFactory({jpvDeploy:async()=>null,resolveRoute:async()=>null,transport:async()=>({ok:true,readback_verified:true})});
  await assert.rejects(()=>factory(ctx),/JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE/);
});

test('rejects external provider runtime even when reachable', async()=>{
  const factory=createJpvNativeDriverFactory({jpvDeploy:async()=>({platform:'VERCEL',verified:true,target_id:'x'}),resolveRoute:async()=>null,transport:async()=>({ok:true,readback_verified:true})});
  await assert.rejects(()=>factory(ctx),/external provider runtime/i);
});

test('canonical target identity is independent from the current route', async()=>{
  const calls=[];
  let route='route-a';
  const factory=createJpvNativeDriverFactory({
    jpvDeploy:async()=>({platform:'JPV_NATIVE',verified:true,target_id:'jpv-native-primary',revision:'abc'}),
    resolveRoute:async()=>({verified:true,transport:'JPV_POWERSHELL',route}),
    transport:async payload=>{calls.push(payload);return {ok:true,readback_verified:true};}
  });
  const driver=await factory(ctx);
  await driver.open('https://portal.example');
  route='route-b';
  await driver.fill('name','Jay');
  assert.equal(calls[0].target_id,'jpv-native-primary');
  assert.equal(calls[1].target_id,'jpv-native-primary');
  assert.equal(calls[0].route,'route-a');
  assert.equal(calls[1].route,'route-b');
});

test('route failure does not redefine canonical JPV target state', async()=>{
  let available=true;
  const factory=createJpvNativeDriverFactory({
    jpvDeploy:async()=>({platform:'JPV_NATIVE',verified:true,target_id:'jpv-native-primary',revision:'abc'}),
    resolveRoute:async()=>available?{verified:true,transport:'JPV_POWERSHELL',route:'local'}:null,
    transport:async()=>({ok:true,readback_verified:true})
  });
  const driver=await factory(ctx);
  await driver.open('https://portal.example');
  available=false;
  await assert.rejects(()=>driver.open('https://portal.example'),/JPV_NATIVE_ROUTE_UNAVAILABLE/);
});

test('successful actuation requires authoritative readback', async()=>{
  const factory=createJpvNativeDriverFactory({
    jpvDeploy:async()=>({platform:'JPV_NATIVE',verified:true,target_id:'jpv-native-primary',revision:'abc'}),
    resolveRoute:async()=>({verified:true,transport:'JPV_POWERSHELL',route:'local'}),
    transport:async()=>({ok:true})
  });
  const driver=await factory(ctx);
  await assert.rejects(()=>driver.open('https://portal.example'),/authoritative readback missing/i);
});
