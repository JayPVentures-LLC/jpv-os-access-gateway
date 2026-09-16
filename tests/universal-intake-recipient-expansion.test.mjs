import test from 'node:test';
import assert from 'node:assert/strict';
import { expandRecipients } from '../governance/universal-intake-recipient-expansion.mjs';

const base={source_id:'oversight-1',request:{request_id:'R0',recipient:{authority_id:'MULTI',authority_name:'Multiple'},subject:'Oversight',requested_action:'Review',provenance:{package_hash:'sha256:p'}},attachments:[{name:'pkg.pdf',sha256:'abc'}]};

test('single recipient package passes through with recipient normalized',()=>{
  const out=expandRecipients({...base,recipients:[{authority_id:'HOUSE',authority_name:'House'}]});
  assert.equal(out.length,1);
  assert.equal(out[0].request.recipient.authority_id,'HOUSE');
  assert.deepEqual(out[0].request.provenance,base.request.provenance);
});

test('multi-recipient package expands deterministically with unique request ids',()=>{
  const pkg={...base,recipients:[{authority_id:'HOUSE',authority_name:'House'},{authority_id:'SENATE',authority_name:'Senate'}]};
  const a=expandRecipients(pkg),b=expandRecipients(structuredClone(pkg));
  assert.equal(a.length,2);
  assert.notEqual(a[0].request.request_id,a[1].request.request_id);
  assert.equal(a[0].request.request_id,b[0].request.request_id);
  assert.deepEqual(a[0].attachments,base.attachments);
});

test('package without explicit recipients keeps canonical recipient',()=>{
  const out=expandRecipients(base);
  assert.equal(out.length,1);
  assert.equal(out[0].request.request_id,'R0');
});
