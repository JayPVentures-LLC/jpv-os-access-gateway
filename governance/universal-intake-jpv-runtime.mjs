function assertNativeTarget(target){
  if(!target) throw new Error('JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE');
  if(target.platform!=='JPV_NATIVE') throw new Error('external provider runtime is not admissible');
  if(target.verified!==true) throw new Error('JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE: target not verified');

  // A verified JPV target is canonical state. A route is derived actuator state,
  // never the identity or authority of the target itself.
  const targetId=target.target_id ?? target.target;
  if(!targetId) throw new Error('JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE: canonical target identity missing');
  return {...target,target_id:targetId};
}

function assertRoute(route){
  if(!route) throw new Error('JPV_NATIVE_ROUTE_UNAVAILABLE');
  if(route.verified!==true) throw new Error('JPV_NATIVE_ROUTE_UNAVAILABLE: route not verified');
  if(!route.transport) throw new Error('JPV_NATIVE_ROUTE_UNAVAILABLE: transport missing');
  return route;
}

export function createJpvNativeDriverFactory(deps={}){
  if(typeof deps.jpvDeploy!=='function') throw new Error('jpvDeploy dependency is required');
  if(typeof deps.resolveRoute!=='function') throw new Error('JPV route resolver dependency is required');
  if(typeof deps.transport!=='function') throw new Error('JPV native transport dependency is required');

  return async function jpvNativeDriverFactory(context){
    const target=assertNativeTarget(await deps.jpvDeploy({
      capability:'universal-intake-browser-worker',
      authority_id:context.authority_id,
      fingerprint:context.fingerprint,
      required_platform:'JPV_NATIVE'
    }));

    const invoke=async(op,payload={})=>{
      // Resolve a fresh route for each transition. Route loss cannot mutate
      // canonical JPV target state; it only makes this actuation attempt unavailable.
      const route=assertRoute(await deps.resolveRoute({
        platform:'JPV_NATIVE',
        target_id:target.target_id,
        revision:target.revision,
        authority_id:context.authority_id,
        session_handle:context.session_handle,
        fingerprint:context.fingerprint,
        op
      }));

      const out=await deps.transport({
        platform:'JPV_NATIVE',
        target_id:target.target_id,
        revision:target.revision,
        transport:route.transport,
        route:route.route ?? route.address ?? null,
        authority_id:context.authority_id,
        session_handle:context.session_handle,
        fingerprint:context.fingerprint,
        op,
        ...payload
      });
      if(!out?.ok) throw new Error(`JPV native browser operation failed: ${op}`);
      if(out.readback_verified!==true) throw new Error(`JPV authoritative readback missing: ${op}`);
      return out;
    };

    return {
      open:url=>invoke('OPEN',{url}),
      fill:(field,value)=>invoke('FILL',{field,value}),
      upload:(field,attachment)=>invoke('UPLOAD',{field,attachment}),
      detectAuthorizationDenial:async()=>{const out=await invoke('DETECT_AUTHORIZATION_DENIAL');return out.denial??null;},
      detectHumanGate:async()=>{const out=await invoke('DETECT_HUMAN_GATE');return out.gate??null;},
      validate:ctx=>invoke('VALIDATE',{request_id:ctx.request?.request_id,profile_id:ctx.profile?.id}),
      submit:ctx=>invoke('SUBMIT',{request_id:ctx.request_id,fingerprint:ctx.fingerprint})
    };
  };
}
