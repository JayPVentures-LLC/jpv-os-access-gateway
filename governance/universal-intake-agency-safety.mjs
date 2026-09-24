import { mkdir, readFile, rename, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';

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

async function atomicWriteJson(path, value) {
  await mkdir(dirname(path), { recursive:true });
  const tmp=`${path}.${process.pid}.tmp`;
  await writeFile(tmp, JSON.stringify(value, null, 2) + '\n', { encoding:'utf8', flag:'wx' });
  await rename(tmp, path);
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

export async function recordPortalTargetDenial(input, denial, options = {}) {
  const targetResourceId = portalTargetResourceId(input);
  if (!targetResourceId) throw Object.assign(new Error('missing_target_resource'), { code:'missing_target_resource' });
  if (!denial?.evidence_id) throw Object.assign(new Error('authorization_denial_evidence_required'), { code:'authorization_denial_evidence_required' });
  const root = options.dataDir ?? dataRoot();
  const path = join(root, 'target-denials.json');
  const denials = await readJsonArray(path, 'authoritative_denial_state_unavailable');
  const existing = denials.find(x => (x?.target_resource_id ?? x?.TargetResourceId) === targetResourceId);
  if (existing) return { recorded:false, denial:existing };
  const record = {
    target_resource_id:targetResourceId,
    evidence_id:String(denial.evidence_id),
    denied_at_utc:denial.denied_at_utc ?? new Date().toISOString(),
    source:denial.source ?? 'runtime_authorization_boundary'
  };
  await atomicWriteJson(path, [...denials, record]);
  return { recorded:true, denial:record };
}
