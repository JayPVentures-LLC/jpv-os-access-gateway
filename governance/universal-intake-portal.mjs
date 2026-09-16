import { createHash } from 'node:crypto';

export function portalFingerprint(request, authority) {
  const canonical = JSON.stringify({
    request_id: request.request_id,
    package_hash: request.provenance?.package_hash,
    authority_id: authority.id,
    subject: request.subject
  });
  return `sha256:${createHash('sha256').update(canonical).digest('hex')}`;
}

export async function executePortal(request, authority, transport, deps = {}) {
  if (typeof deps.loadPortalProfile !== 'function') throw new Error('PORTAL transport requires loadPortalProfile dependency');
  if (typeof deps.portalWorker !== 'function') throw new Error('PORTAL transport requires authenticated portalWorker dependency');
  const profile = await deps.loadPortalProfile(transport.profile);
  if (!profile?.id) throw new Error('portal profile is missing or invalid');
  const fingerprint = portalFingerprint(request, authority);
  const output = await deps.portalWorker({
    request: structuredClone(request),
    authority: structuredClone(authority),
    transport: structuredClone(transport),
    profile: structuredClone(profile),
    fingerprint
  });

  if (output?.state === 'HUMAN_REQUIRED') {
    return {
      state: 'HUMAN_REQUIRED',
      reason: output.reason ?? 'HUMAN_INTERACTION_REQUIRED',
      ...(output.resume_token ? { resume_token: output.resume_token } : {})
    };
  }
  if (output?.state !== 'SUBMITTED') {
    return { state: 'HUMAN_REQUIRED', reason: 'UNKNOWN_PORTAL_STATE' };
  }
  if (!output.tracking_id || !output.received_at || !output.evidence || Object.keys(output.evidence).length === 0) {
    throw new Error('portal SUBMITTED output requires tracking_id, received_at, and verifiable evidence');
  }
  if (output.fingerprint && output.fingerprint !== fingerprint) throw new Error('portal receipt fingerprint mismatch');
  return {
    state: 'SUBMITTED',
    tracking_id: output.tracking_id,
    received_at: output.received_at,
    evidence: structuredClone(output.evidence),
    fingerprint
  };
}
