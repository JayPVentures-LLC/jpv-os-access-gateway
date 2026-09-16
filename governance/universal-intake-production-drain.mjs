import { recoverBacklogPackages } from './universal-intake-recovery.mjs';
import { expandRecipients } from './universal-intake-recipient-expansion.mjs';
import { drainBacklog } from './universal-intake-drain.mjs';
import { appendEvidence } from './universal-intake-evidence.mjs';

function preparedFromPackage(pkg){
  const r=pkg.request;
  return {
    source_id:pkg.source_id,
    request_id:r.request_id,
    authority_id:r.recipient?.authority_id,
    authority_name:r.recipient?.authority_name,
    request_type:r.request_type,
    requester:r.requester,
    subject:r.subject,
    summary:r.summary,
    requested_action:r.requested_action,
    provenance:r.provenance,
    records_scope:r.records_scope,
    attachments:pkg.attachments ?? r.attachments ?? []
  };
}

function classifyActionRequired(item){
  const reason=String(item?.handoff?.reason ?? 'ACTION_REQUIRED');
  if(reason.includes('JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE')) return 'ACTION_REQUIRED:JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE';
  if(['MFA','CAPTCHA','IDENTITY_CONFIRMATION','TERMS_ACCEPTANCE','ACCOUNT_PROFILE_COMPLETION'].some(x=>reason.includes(x))) return 'ACTION_REQUIRED:HUMAN_GATE';
  return 'ACTION_REQUIRED';
}

export async function runProductionDrain({seedItems=[],sourceRecords=[],receipts=[],registry,deps={}}={}){
  const recovered=recoverBacklogPackages(seedItems,sourceRecords);
  const results=[];
  const evidence=[];
  for(const recoveredItem of recovered){
    if(recoveredItem.status!=='RECOVERED'){
      results.push({source_id:recoveredItem.source_id,status:`ACTION_REQUIRED:${recoveredItem.blocker.code}`,blocker:recoveredItem.blocker});
      continue;
    }
    const expanded=expandRecipients(recoveredItem.package);
    for(const pkg of expanded){
      const drained=await drainBacklog([preparedFromPackage(pkg)],receipts,registry,deps);
      const item=drained.items[0];
      if(item.status==='SUBMITTED' && item.receipt){
        const ev=await appendEvidence(item.receipt,{
          package_hash:item.request?.provenance?.package_hash,
          attachment_hashes:(pkg.attachments??[]).map(a=>a.sha256).filter(Boolean)
        },deps.evidenceStore ?? {});
        evidence.push({request_id:item.request.request_id,...ev});
        results.push({...item,source_id:pkg.source_id,status:'SUBMITTED'});
      }else if(item.status==='SUBMITTED'||item.status==='RESOLVED'){
        results.push({...item,source_id:pkg.source_id});
      }else{
        results.push({...item,source_id:pkg.source_id,status:classifyActionRequired(item)});
      }
    }
  }
  const summary={};
  for(const item of results) summary[item.status]=(summary[item.status]??0)+1;
  return {items:results,summary,evidence};
}
