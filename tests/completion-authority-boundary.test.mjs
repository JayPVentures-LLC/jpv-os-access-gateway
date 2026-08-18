import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const scriptPath = path.resolve(__dirname, '../scripts/Invoke-JPVCanonicalStripeAzureConvergence.ps1');

test('access gateway convergence receipt is local verification, not terminal JPV completion', () => {
  const source = fs.readFileSync(scriptPath, 'utf8');
  assert.doesNotMatch(source, /state\s*=\s*'VERIFIED_COMPLETE'/);
  assert.match(source, /state\s*=\s*'LOCAL_VERIFIED'/);
  assert.match(source, /terminal_authority\s*=\s*\$false/);
  assert.match(source, /completion_authority\s*=\s*'jaypVLabs\/JPV-OS'/);
});
