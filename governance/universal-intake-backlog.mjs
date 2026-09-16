import { createHash } from 'node:crypto';

function makeId(item) {
  const seed = JSON.stringify({source_id:item.source_id,authority_id:item.authority_id,subject:item.subject,package_hash:item.provenance?.package_hash});
  return `UPIS-BACKLOG-${createHash('sha256').update(seed).digest('hex').slice(0,16).toUpperCase()}`;
}

function duplicateReceipt(item, receipts) {
  const hash = item.provenance?.package_hash;
  if (!hash) return null;
  return receipts.find(r => r.package_hash === hash) ?? null;
}

export function classifyBacklogItem(item, receipts = []) {
  const duplicate = duplicateReceipt(item, receipts);
  if (duplicate) return { status: duplicate.status === 'RESOLVED' ? 'RESOLVED' : 'SUBMITTED', duplicate_of: duplicate.request_id, external_tracking_id: duplicate.external_tracking_id ?? null };
  const missing = [];
  if (!item.authority_id || !item.authority_name) missing.push('authority');
  if (!item.subject) missing.push('subject');
  if (!item.requester?.name) missing.push('requester.name');
  if (!item.requested_action) missing.push('requested_action');
  if (!item.provenance?.package_hash) missing.push('provenance.package_hash');
  if (missing.length) return { status:'ACTION_REQUIRED', missing };
  return { status:'READY_TO_ROUTE' };
}

export function importBacklog(items, receipts = []) {
  return items.map(item => {
    const classification = classifyBacklogItem(item, receipts);
    const request = {
      request_id: item.request_id ?? makeId(item),
      request_type: item.request_type ?? 'OTHER',
      requester: structuredClone(item.requester ?? {}),
      recipient: { authority_id:item.authority_id, authority_name:item.authority_name },
      subject:item.subject,
      summary:item.summary ?? item.requested_action ?? '',
      requested_action:item.requested_action,
      provenance: structuredClone(item.provenance ?? {}),
      ...(item.records_scope ? {records_scope:structuredClone(item.records_scope)} : {}),
      ...(item.attachments ? {attachments:structuredClone(item.attachments)} : {}),
      status:'NEW'
    };
    return { source_id:item.source_id ?? null, request, ...classification };
  });
}
