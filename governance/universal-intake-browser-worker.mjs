import { acquirePortalSession } from './universal-intake-session.mjs';
import { authorizePortalTarget } from './universal-intake-agency-safety.mjs';

function getPath(obj, path) {
  return String(path).split('.').reduce((cur,key)=>cur?.[key], obj);
}

export function createBrowserPortalWorker(deps = {}) {
  if (typeof deps.driverFactory !== 'function') throw new Error('driverFactory is required');
  const agencySafetyGate = deps.agencySafetyGate ?? authorizePortalTarget;
  return async function browserPortalWorker(input) {
    const agencyDecision = await agencySafetyGate(input);
    if (!agencyDecision?.allowed) return { state:'ACTION_REQUIRED', reason:agencyDecision?.reason ?? 'AGENCY_SAFETY_DENY' };
    const authorityId = input.authority?.id ?? input.request?.recipient?.authority_id;
    const session = await acquirePortalSession({ authority_id: authorityId, request_id: input.request.request_id }, deps);
    const driver = await deps.driverFactory({ authority_id: authorityId, session_handle: session.handle, fingerprint: input.fingerprint });
    if (!driver) throw new Error('browser driver unavailable');

    await driver.open(input.transport.endpoint);
    for (const [semantic, sourcePath] of Object.entries(input.profile?.semantic_fields ?? {})) {
      const value = getPath(input.request, sourcePath);
      if (value !== undefined && value !== null && typeof driver.fill === 'function') await driver.fill(semantic, String(value));
    }
    if (Array.isArray(input.request.attachments) && input.request.attachments.length && typeof driver.upload === 'function') {
      for (const attachment of input.request.attachments) await driver.upload(input.profile?.attachments_field ?? 'attachments', attachment);
    }

    const gate = typeof driver.detectHumanGate === 'function' ? await driver.detectHumanGate() : null;
    if (gate) return { state:'HUMAN_REQUIRED', reason:gate.reason ?? 'HUMAN_INTERACTION_REQUIRED', ...(gate.resume_token ? {resume_token:gate.resume_token}: {}) };

    const validation = typeof driver.validate === 'function' ? await driver.validate({ request: input.request, profile: input.profile }) : {ok:true,lossy_transformations:[]};
    if (!validation?.ok || (validation.lossy_transformations?.length ?? 0) > 0) {
      return { state:'HUMAN_REQUIRED', reason:'LOSSY_TRANSFORMATION', details:validation?.lossy_transformations ?? [] };
    }

    if (typeof driver.submit !== 'function') throw new Error('browser driver submit is required');
    const receipt = await driver.submit({ fingerprint: input.fingerprint, request_id: input.request.request_id });
    if (!receipt?.tracking_id || !receipt?.received_at) throw new Error('portal submission missing receipt evidence');
    const evidence = {};
    if (receipt.confirmation_url) evidence.confirmation_url = receipt.confirmation_url;
    if (receipt.screenshot_hash) evidence.screenshot_hash = receipt.screenshot_hash;
    if (!Object.keys(evidence).length) throw new Error('portal submission missing verifiable evidence');
    return { state:'SUBMITTED', tracking_id:receipt.tracking_id, received_at:receipt.received_at, evidence, fingerprint:input.fingerprint };
  };
}
