import { importBacklog } from './universal-intake-backlog.mjs';
import { getAuthority } from './universal-intake-registry.mjs';
import { executeIntake } from './universal-intake-adapters.mjs';

function summarize(items) {
  const summary={READY_TO_ROUTE:0,ACTION_REQUIRED:0,SUBMITTED:0,RESOLVED:0,FAILED:0};
  for(const item of items) summary[item.status]=(summary[item.status]??0)+1;
  return summary;
}

export async function drainBacklog(preparedItems, receipts, registry, deps={}) {
  const imported=importBacklog(preparedItems,receipts);
  const output=[];
  for(const item of imported){
    if(item.status==='SUBMITTED'||item.status==='RESOLVED'||item.status==='ACTION_REQUIRED'){ output.push(item); continue; }
    try{
      const authority=getAuthority(registry,item.request.recipient.authority_id);
      const result=await executeIntake({...item.request,status:'ROUTED'},authority,deps);
      if(result.request.status==='SUBMITTED') output.push({...item,status:'SUBMITTED',request:result.request,receipt:result.receipt});
      else output.push({...item,status:'ACTION_REQUIRED',request:result.request,handoff:result.handoff});
    }catch(error){
      output.push({...item,status:'ACTION_REQUIRED',handoff:{request_id:item.request.request_id,reason:String(error?.message??error),canonical_request:item.request}});
    }
  }
  return {items:output,summary:summarize(output)};
}
