function assertNativeTarget(target){
  if(!target) throw new Error('JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE');
  if(target.platform!=='JPV_NATIVE') throw new Error('external provider runtime is not admissible');
  if(target.verified!==true) throw new Error('JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE: target not verified');
  if(!target.endpoint) throw new Error('JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE: target endpoint missing');
  return target;
}

export function createJpvNativeDriverFactory(deps={}){
  if(typeof deps.jpvDeploy!=='function') throw new Error('jpvDeploy dependency is required');
  if(typeof deps.transport!=='function') throw new Error('JPV native transport dependency is required');
  return async function jpvNativeDriverFactory(context){
    const target=assertNativeTarget(await deps.jpvDeploy({
      capability:'universal-intake-browser-worker',
      authority_id:context.authority_id,
      fingerprint:context.fingerprint,
      required_platform:'JPV_NATIVE'
    }));
    const invoke=async(op,payload={})=>{
      const out=await deps.transport({
        platform:'JPV_NATIVE',target:target.target,revision:target.revision,endpoint:target.endpoint,
        authority_id:context.authority_id,session_handle:context.session_handle,fingerprint:context.fingerprint,op,...payload
      });
      if(!out?.ok) throw new Error(`JPV native browser operation failed: ${op}`);
      return out;
    };
    return {
      open:url=>invoke('OPEN',{url}),
      fill:(field,value)=>invoke('FILL',{field,value}),
      upload:(field,attachment)=>invoke('UPLOAD',{field,attachment}),
      detectHumanGate:async()=>{const out=await invoke('DETECT_HUMAN_GATE');return out.gate??null;},
      validate:ctx=>invoke('VALIDATE',{request_id:ctx.request?.request_id,profile_id:ctx.profile?.id}),
      submit:ctx=>invoke('SUBMIT',{request_id:ctx.request_id,fingerprint:ctx.fingerprint})
    };
  };
}
