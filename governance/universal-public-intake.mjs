const REQUIRED = ['request_id','request_type','requester','recipient','subject','requested_action','provenance'];
const EXECUTION_ORDER = ['API','EMAIL','FORM','PORTAL'];
const RECEIPT_STATUS = new Map([
  ['received','SUBMITTED'], ['accepted','SUBMITTED'], ['processing','WAITING'],
  ['action_required','ACTION_REQUIRED'], ['fulfilled','RESOLVED'], ['closed','CLOSED']
]);

export function normalizeRequest(input) {
  for (const field of REQUIRED) {
    if (input?.[field] === undefined || input?.[field] === null) throw new Error(`missing required canonical field: ${field}`);
  }
  return structuredClone({ ...input, status: input.status ?? 'NEW' });
}

export function selectTransport(request, registry) {
  const authority = registry?.[request.recipient.authority_name];
  if (!authority?.transports?.length) throw new Error(`no registered transport for ${request.recipient.authority_name}`);
  const executable = [...authority.transports]
    .filter(t => t.executable === true)
    .sort((a,b) => EXECUTION_ORDER.indexOf(a.kind) - EXECUTION_ORDER.indexOf(b.kind))[0];
  if (executable) return structuredClone(executable);
  const handoff = authority.transports.find(t => t.endpoint) ?? authority.transports[0];
  return { kind: 'MANUAL_REQUIRED', executable: false, ...(handoff?.endpoint ? { endpoint: handoff.endpoint } : {}) };
}

export function normalizeReceipt(requestId, external, receivingAuthority) {
  if (!external?.tracking_id || !external?.received_at) throw new Error('receipt requires tracking_id and received_at');
  return {
    request_id: requestId,
    external_tracking_id: external.tracking_id,
    received_at: external.received_at,
    receiving_authority: receivingAuthority,
    status: RECEIPT_STATUS.get(String(external.status).toLowerCase()) ?? 'WAITING'
  };
}

export function transitionSubmission(request, nextStatus, receipt) {
  const allowed = {
    NEW: ['TRIAGED','ROUTED'], TRIAGED: ['ROUTED'], ROUTED: ['SUBMITTED','ACTION_REQUIRED'],
    SUBMITTED: ['WAITING','ACTION_REQUIRED','RESOLVED'], WAITING: ['ACTION_REQUIRED','RESOLVED'],
    ACTION_REQUIRED: ['ROUTED','SUBMITTED','CLOSED'], RESOLVED: ['CLOSED'], CLOSED: []
  };
  if (!(allowed[request.status] ?? []).includes(nextStatus)) throw new Error(`invalid transition ${request.status} -> ${nextStatus}`);
  if (nextStatus === 'SUBMITTED' && (!receipt?.external_tracking_id || !receipt?.received_at)) {
    throw new Error('SUBMITTED requires auditable transport receipt');
  }
  return structuredClone({ ...request, status: nextStatus, ...(receipt ? { receipt } : {}) });
}
