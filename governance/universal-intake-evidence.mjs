import { createHash } from 'node:crypto';

function hash(value) {
  return `sha256:${createHash('sha256').update(JSON.stringify(value)).digest('hex')}`;
}

export function createEvidenceRecord(receipt, context = {}) {
  if (!receipt?.request_id || !receipt?.external_tracking_id || !receipt?.received_at) throw new Error('receipt evidence incomplete');
  const record = {
    request_id: receipt.request_id,
    external_tracking_id: receipt.external_tracking_id,
    received_at: receipt.received_at,
    receiving_authority: receipt.receiving_authority ?? null,
    status: receipt.status ?? 'SUBMITTED',
    fingerprint: receipt.fingerprint ?? null,
    evidence: structuredClone(receipt.evidence ?? {}),
    package_hash: context.package_hash ?? null,
    attachment_hashes: [...(context.attachment_hashes ?? [])]
  };
  return { ...record, record_hash: hash(record) };
}

export async function appendEvidence(receipt, context = {}, store = {}) {
  if (typeof store.append !== 'function') throw new Error('append-only evidence store is required');
  const record = createEvidenceRecord(receipt, context);
  const storageReceipt = await store.append(structuredClone(record));
  if (!storageReceipt?.ledger_id || !storageReceipt?.stored_at) throw new Error('evidence store did not return durable receipt');
  return { ...storageReceipt, record_hash: record.record_hash };
}
