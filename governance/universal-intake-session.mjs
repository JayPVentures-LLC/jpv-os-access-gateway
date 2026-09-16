const SECRET_KEYS = new Set(['token','access_token','refresh_token','password','cookies','cookie','authorization','secret']);

export function stripSessionSecrets(value) {
  if (Array.isArray(value)) return value.map(stripSessionSecrets);
  if (!value || typeof value !== 'object') return value;
  const out = {};
  for (const [key,val] of Object.entries(value)) {
    if (SECRET_KEYS.has(key.toLowerCase())) continue;
    out[key] = stripSessionSecrets(val);
  }
  return out;
}

export async function acquirePortalSession(context, deps = {}) {
  if (typeof deps.sessionProvider !== 'function') throw new Error('sessionProvider is required');
  const raw = await deps.sessionProvider({ authority_id: context.authority_id, request_id: context.request_id });
  if (!raw?.handle || !raw?.expires_at || !Array.isArray(raw.scope)) throw new Error('invalid portal session');
  const now = deps.now ? deps.now() : new Date();
  if (new Date(raw.expires_at).getTime() <= now.getTime()) throw new Error('portal session expired');
  if (!raw.scope.includes(context.authority_id)) throw new Error('portal session scope does not authorize authority');
  const clean = stripSessionSecrets(raw);
  return { handle: clean.handle, expires_at: clean.expires_at, scope: clean.scope };
}
