import { normalizeReceipt, selectTransport, transitionSubmission } from './universal-public-intake.mjs';
import { executePortal } from './universal-intake-portal.mjs';

function renderEmailBody(request) {
  const lines = [request.summary ?? request.requested_action, '', request.requested_action];
  if (Array.isArray(request.records_scope) && request.records_scope.length) lines.push('', 'Requested scope:', ...request.records_scope.map(item => `- ${item}`));
  lines.push('', `Canonical request ID: ${request.request_id}`);
  return lines.filter((line, index, array) => !(line === '' && array[index - 1] === '')).join('\n');
}

function manualHandoff(request, authority, transport, extra = {}) {
  return { request_id: request.request_id, authority_id: authority.id, authority_name: authority.authority_name, profile: authority.profile ?? transport.profile ?? null, endpoint: transport.endpoint ?? null, subject: request.subject, requested_action: request.requested_action, canonical_request: structuredClone(request), provenance: structuredClone(request.provenance), ...extra };
}

async function executeEmail(request, authority, transport, deps) {
  if (typeof deps.runtimeActuate !== 'function') throw new Error('EMAIL transport requires JPV Runtime actuation');
  const actuation = await deps.runtimeActuate({ operation: 'EMAIL_SEND', request_id: request.request_id, authority_id: authority.id, transport: structuredClone(transport), payload: { to: transport.endpoint, subject: request.subject, body: renderEmailBody(request) } });
  if (actuation?.readback_verified !== true) throw new Error('JPV authoritative readback missing: EMAIL_SEND');
  const external = actuation.result;
  return normalizeReceipt(request.request_id, external, authority.authority_name);
}

async function executeApi(request, authority, transport, deps) {
  if (typeof deps.runtimeActuate !== 'function') throw new Error('API transport requires JPV Runtime actuation');
  const actuation = await deps.runtimeActuate({ operation: 'API_REQUEST', request_id: request.request_id, authority_id: authority.id, transport: structuredClone(transport), payload: { endpoint: transport.endpoint, method: transport.method ?? 'POST', headers: { 'content-type': 'application/json', 'x-upis-request-id': request.request_id, ...(transport.headers ?? {}) }, body: structuredClone(request) } });
  if (actuation?.readback_verified !== true) throw new Error('JPV authoritative readback missing: API_REQUEST');
  return normalizeReceipt(request.request_id, actuation.result, authority.authority_name);
}

export async function executeIntake(request, authority, deps = {}) {
  if (!authority?.authority_name || !Array.isArray(authority.transports)) throw new Error('invalid authority');
  const transport = selectTransport(request, { [request.recipient.authority_name]: { transports: authority.transports } });
  const routed = request.status === 'ROUTED' ? request : { ...request, status: 'ROUTED' };

  if (transport.kind === 'MANUAL_REQUIRED') return { request: transitionSubmission(routed, 'ACTION_REQUIRED'), transport, handoff: manualHandoff(request, authority, transport) };

  if (transport.kind === 'PORTAL') {
    if (typeof deps.runtimePortalWorker !== 'function') throw new Error('PORTAL transport requires JPV Runtime portal worker');
    const portal = await executePortal(request, authority, transport, { ...deps, portalWorker: deps.runtimePortalWorker });
    if (portal.state === 'HUMAN_REQUIRED') return { request: transitionSubmission(routed, 'ACTION_REQUIRED'), transport, handoff: manualHandoff(request, authority, transport, { reason: portal.reason, ...(portal.resume_token ? { resume_token: portal.resume_token } : {}) }) };
    const receipt = { request_id: request.request_id, external_tracking_id: portal.tracking_id, received_at: portal.received_at, receiving_authority: authority.authority_name, status: 'SUBMITTED', evidence: portal.evidence, fingerprint: portal.fingerprint };
    return { request: transitionSubmission(routed, 'SUBMITTED', { external_tracking_id: receipt.external_tracking_id, received_at: receipt.received_at }), transport, receipt };
  }

  let receipt;
  if (transport.kind === 'EMAIL') receipt = await executeEmail(request, authority, transport, deps);
  else if (transport.kind === 'API') receipt = await executeApi(request, authority, transport, deps);
  else throw new Error(`executable transport adapter not implemented: ${transport.kind}`);
  return { request: transitionSubmission(routed, 'SUBMITTED', { external_tracking_id: receipt.external_tracking_id, received_at: receipt.received_at }), transport, receipt };
}
