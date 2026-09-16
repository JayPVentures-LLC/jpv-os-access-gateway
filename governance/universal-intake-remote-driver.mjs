export function createRemoteDriverFactory(config={}) {
  if(!config.endpoint) throw new Error('remote browser worker endpoint is required');
  const fetchImpl=config.fetch??globalThis.fetch;
  if(typeof fetchImpl!=='function') throw new Error('fetch implementation is required');
  return async function remoteDriverFactory(context){
    async function operation(op,payload={}){
      const response=await fetchImpl(`${config.endpoint.replace(/\/$/,'')}/v1/portal-operation`,{
        method:'POST',headers:{'content-type':'application/json',...(config.authorization?{authorization:config.authorization}:{})},
        body:JSON.stringify({authority_id:context.authority_id,session_handle:context.session_handle,fingerprint:context.fingerprint,op,...payload})
      });
      if(!response?.ok) throw new Error(`remote browser worker failed: ${response?.status??'unknown'}`);
      return response.json();
    }
    return {
      open:url=>operation('OPEN',{url}),
      fill:(field,value)=>operation('FILL',{field,value}),
      upload:(field,attachment)=>operation('UPLOAD',{field,attachment}),
      detectHumanGate:async()=>{const out=await operation('DETECT_HUMAN_GATE');return out.gate??null;},
      validate:async context=>operation('VALIDATE',{request_id:context.request?.request_id,profile_id:context.profile?.id}),
      submit:context=>operation('SUBMIT',{request_id:context.request_id,fingerprint:context.fingerprint})
    };
  };
}
