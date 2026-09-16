import { selectTransport } from './universal-public-intake.mjs';

export function getAuthority(registry, authorityId) {
  if (!registry || typeof registry !== 'object' || !registry.authorities || typeof registry.authorities !== 'object') {
    throw new Error('invalid authority registry');
  }
  const authority = registry.authorities[authorityId];
  if (!authority || authority.id !== authorityId || !authority.authority_name || !Array.isArray(authority.transports)) {
    throw new Error(`authority not registered or malformed: ${authorityId}`);
  }
  return structuredClone(authority);
}

export function resolveAuthorityTransport(request, registry) {
  const authorityId = request?.recipient?.authority_id;
  if (!authorityId) throw new Error('request recipient.authority_id is required');
  const authority = getAuthority(registry, authorityId);
  return selectTransport({ recipient: { authority_name: authority.authority_name } }, {
    [authority.authority_name]: { transports: authority.transports }
  });
}
