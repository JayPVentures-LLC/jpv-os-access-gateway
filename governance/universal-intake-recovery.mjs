export function recoverBacklogPackages(seedItems, sourceRecords = []) {
  const index = new Map();
  for (const record of sourceRecords) {
    if (!record?.source_id) throw new Error('source record missing source_id');
    if (index.has(record.source_id)) throw new Error(`duplicate canonical source: ${record.source_id}`);
    index.set(record.source_id, structuredClone(record));
  }
  return seedItems.map(seed => {
    const sourceId = seed?.source_id;
    if (!sourceId) return { source_id: null, status: 'ACTION_REQUIRED', blocker: { code: 'MISSING_SOURCE_ID', source_id: null } };
    const record = index.get(sourceId);
    if (!record) return { source_id: sourceId, status: 'ACTION_REQUIRED', blocker: { code: 'MISSING_CANONICAL_SOURCE', source_id: sourceId } };
    return {
      source_id: sourceId,
      status: 'RECOVERED',
      package: {
        source_id: sourceId,
        request: structuredClone(record.request),
        attachments: structuredClone(record.attachments ?? []),
        recipients: structuredClone(record.recipients ?? []),
        receipts: structuredClone(record.receipts ?? []),
        source_ref: structuredClone(record.source_ref ?? null)
      }
    };
  });
}
