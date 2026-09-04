# Provider-Neutral Deployment Boundary

Status: REQUIRED
Authority: JayPVentures LLC enterprise infrastructure authority

The access gateway inherits the JPV-OS mandatory `PROVIDER_NEUTRAL` runtime contract. `JPV_OS` is the runtime authority; hosting providers are delivery infrastructure only and do not become architecture authority.

Microsoft Azure is not an admitted production dependency for this repository. Azure deployment workflows, Azure deployment identities, publish profiles, OIDC bootstrap code, and equivalent Azure-specific production coupling must not be introduced while the JPV-OS external-provider registry classifies Azure as non-authoritative and retiring.

Deployment admission must come from the JPV-OS provider deployment registry. If that registry reports `NO_ADMITTED_EXECUTION_CAPACITY`, this repository must fail closed rather than inventing a provider, silently reintroducing a retired provider, or treating provider success as terminal JPV success.

Terminal deployment success requires the JPV provider-neutral chain: repository integrity, authorized head, policy-selected admitted provider, successful invocation, provider health, deployed revision readback, exact revision match, and normalized JPV receipt.
