import { normalizeReceipt, selectTransport, transitionSubmission } from './universal-public-intake.mjs';

function renderEmailBody(request) {
  const lines = [
    request.summary ?? request.requested_action,
    '',
    request.requested_action
  ];
  if (Array.isArray(request.records_scope) && request.records_scope.length) {
    lines.push('', 'Requested scope:', ...request.records_scope.map(item => `- ${item}`));
  }
  lines.push('', `Canonical request ID: ${request.request_id}`);
  return lines.filter((line, index, array) => !(line === '' && array[index - 1] === '')).join('\n');
}

function manualHandoff(request, authority, transport) {
  return {
    request_id: request.request_id,
    authority_id: authority.id,
    authority_name: authority.authority_name,
    profile: authority.profile ?? null,
    endpoint: transport.endpoint ?? null,
    subject: request.subject,
    requested_action: request.requested_action,
    canonical_request: structuredClone(request),
    provenance: structuredClone(request.provenance)
  };
}

async function executeEmail(request, authority, transport, deps) {
  if (typeof deps.sendEmail !== 'function') throw new Error('EMAIL transport requires sendEmail dependency');
  const external = await deps.sendEmail({
    to: transport.endpoint,
    subject: request.subject,
    body: renderEmailBody(request),
    request_id: request.request_id,
    authority_id: authority.id
  });
  return normalizeReceipt(request.request_id, external, authority.authority_name);
}

async function executeApi(request, authority, transport, deps) {
  const fetchImpl = deps.fetch ?? globalThis.fetch;
  if (typeof fetchImpl !== 'function') throw new Error('API transport requires fetch dependency');
  const response = await fetchImpl(transport.endpoint, {
    method: transport.method ?? 'POST',
    headers: {
      'content-type': 'application/json',
      'x-upis-request-id': request.request_id,
      ...(transport.headers ?? {})
    },
    body: JSON.stringify(request)
  });
  if (!response?.ok) throw new Error(`API transport failed${response?.status ? `: ${response.status}` : ''}`);
  const external = await response.json();
  return normalizeReceipt(request.request_id, external, authority.authority_name);
}

export async function executeIntake(request, authority, deps = {}) {
  if (!authority?.authority_name || !Array.isArray(authority.transports)) throw new Error('invalid authority');
  const transport = selectTransport(request, { [request.recipient.authority_name]: { transports: authority.transports } });

  if (transport.kind === 'MANUAL_REQUIRED') {
    const routed = request.status === 'ROUTED' ? request : { ...request, status: 'ROUTED' };
    return {
      request: transitionSubmission(routed, 'ACTION_REQUIRED'),
      transport,
      handoff: manualHandoff(request, authority, transport)
    };
  }

  let receipt;
  if (transport.kind === 'EMAIL') receipt = await executeEmail(request, authority, transport, deps);
  else if (transport.kind === 'API') receipt = await executeApi(request, authority, transport, deps);
  else throw new Error(`executable transport adapter not implemented: ${transport.kind}`);

  const routed = request.status === 'ROUTED' ? request : { ...request, status: 'ROUTED' };
  return {
    request: transitionSubmission(routed, 'SUBMITTED', {
      external_tracking_id: receipt.external_tracking_id,
      received_at: receipt.received_at
    }),
    transport,
    receipt
  };
}
