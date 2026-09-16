import { createHash } from 'node:crypto';

function derivedId(sourceId, requestId, authorityId) {
  const digest=createHash('sha256').update(JSON.stringify({source_id:sourceId,request_id:requestId,authority_id:authorityId})).digest('hex').slice(0,16).toUpperCase();
  return `UPIS-DERIVED-${digest}`;
}

export function expandRecipients(pkg) {
  const recipients=Array.isArray(pkg?.recipients)&&pkg.recipients.length?pkg.recipients:null;
  if(!recipients) return [structuredClone(pkg)];
  return recipients.map(recipient=>({
    ...structuredClone(pkg),
    recipients:[structuredClone(recipient)],
    request:{
      ...structuredClone(pkg.request),
      request_id:derivedId(pkg.source_id,pkg.request?.request_id,recipient.authority_id),
      recipient:structuredClone(recipient),
      provenance:structuredClone(pkg.request?.provenance ?? {})
    }
  }));
}
