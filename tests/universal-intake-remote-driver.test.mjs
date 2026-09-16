import test from 'node:test';
import assert from 'node:assert/strict';
import { createRemoteDriverFactory } from '../governance/universal-intake-remote-driver.mjs';

test('remote driver sends only session handle and semantic operations',async()=>{
  const calls=[];
  const factory=createRemoteDriverFactory({endpoint:'https://worker.example.test',fetch:async(url,init)=>{calls.push([url,JSON.parse(init.body)]);return {ok:true,json:async()=>({ok:true,tracking_id:'T1',received_at:'2026-09-16T12:00:00Z',confirmation_url:'https://r'})};}});
  const driver=await factory({authority_id:'CA_CDT_PRA',session_handle:'sess-1',fingerprint:'fp'});
  await driver.open('https://portal.example.test'); await driver.fill('subject','Records'); const receipt=await driver.submit({request_id:'R1',fingerprint:'fp'});
  assert.equal(receipt.tracking_id,'T1');
  assert.equal(JSON.stringify(calls).includes('password'),false);
  assert.equal(calls[0][1].session_handle,'sess-1');
});

test('remote driver fails closed on worker errors',async()=>{
  const factory=createRemoteDriverFactory({endpoint:'https://worker.example.test',fetch:async()=>({ok:false,status:503,json:async()=>({})})});
  const driver=await factory({authority_id:'X',session_handle:'s',fingerprint:'f'});
  await assert.rejects(()=>driver.open('https://portal.example.test'),/503/);
});
