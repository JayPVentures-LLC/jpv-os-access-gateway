import { readFile } from 'node:fs/promises';
import { join } from 'node:path';

function dataRoot() {
  if (process.env.JPV_AGENCY_SAFETY_DATA_DIR) return process.env.JPV_AGENCY_SAFETY_DATA_DIR;
  const base = process.env.JPV_OUTBOUND_DATA_DIR ?? process.env.JPV_CLAIMS_DATA_DIR ?? '/var/data/jpv';
  return join(base, 'agency-safety');
}

async function readJsonArray(path, unavailableReason) {
  try {
    const parsed = JSON.parse(await readFile(path, 'utf8'));
    if (!Array.isArray(parsed)) throw new Error('expected array');
    return parsed;
  } catch {
    const error = new Error(unavailableReason);
    error.code = unavailableReason;
    throw error;
  }
}

export function portalTargetResourceId(input) {
  return input.transport?.target_resource_id ?? input.transport?.endpoint;
}

export async function authorizePortalTarget(input, options = {}) {
  const targetResourceId = portalTargetResourceId(input);
  if (!targetResourceId) return { allowed:false, reason:'missing_target_resource' };
  const requestedMethod = input.transport?.agency_method ?? 'PORTAL_SUBMIT';
  const root = options.dataDir ?? dataRoot();
  const denials = await readJsonArray(join(root, 'target-denials.json'), 'authoritative_denial_state_unavailable');
  const prior = denials.find(x => x?.target_resource_id === targetResourceId || x?.TargetResourceId === targetResourceId);
  if (!prior) return { allowed:true, reason:'allow' };

  const securityAuthorizationId = input.request?.security_testing_authorization_id;
  if (!securityAuthorizationId) return { allowed:false, reason:'third_party_authorization_denial_circumvention' };

  const grants = await readJsonArray(join(root, 'security-testing-grants.json'), 'security_testing_authorization_unavailable');
  const grant = grants.find(x => (x?.authorization_id ?? x?.AuthorizationId) === securityAuthorizationId);
  const grantTarget = grant?.target_resource_id ?? grant?.TargetResourceId;
  const grantMethod = grant?.method ?? grant?.Method;
  const validUntil = grant?.valid_until ?? grant?.ValidUntil;
  if (!grant || grantTarget !== targetResourceId || String(grantMethod).toUpperCase() !== requestedMethod.toUpperCase() ||
      !validUntil || Date.parse(validUntil) <= Date.now()) {
    return { allowed:false, reason:'third_party_authorization_denial_circumvention' };
  }
  return { allowed:true, reason:'authorized_security_testing_exception' };
}
