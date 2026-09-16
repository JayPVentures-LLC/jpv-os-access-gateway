import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const registry=JSON.parse(await readFile(new URL('../governance/universal-intake-authorities.json',import.meta.url)));

test('OSD/JS FOIA uses recovered executable email route',()=>{
  const authority=registry.authorities.OSD_JS_FOIA;
  assert.ok(authority);
  assert.equal(authority.transports.find(t=>t.kind==='EMAIL'&&t.executable)?.endpoint,'osd.mc-alex.oatsd-pclt.mbx.foia-policy@mail.mil');
});

test('Armed Services congressional routes remain non-executable without verified transmission adapter',()=>{
  for(const id of ['HASC_OVERSIGHT','SASC_OVERSIGHT']){
    const authority=registry.authorities[id];
    assert.ok(authority);
    assert.equal(authority.transports.some(t=>t.executable===true),false);
  }
});
