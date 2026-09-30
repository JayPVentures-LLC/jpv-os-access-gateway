# Provider-Neutral Deployment Boundary

Status: REQUIRED
Authority: JayPVentures LLC enterprise infrastructure authority

JPV Nexus inherits the mandatory `PROVIDER_NEUTRAL` runtime contract. JPV Runtime is the persistent execution and completion authority; hosting providers, routes, and adapters are bounded delivery infrastructure only and do not become architecture authority.

Microsoft Azure is not an admitted production dependency for this repository. Azure deployment workflows, Azure deployment identities, publish profiles, OIDC bootstrap code, and equivalent Azure-specific production coupling must not be introduced while the JPV-OS external-provider registry classifies Azure as non-authoritative and retiring.

Deployment admission must come through JPV governance and runtime policy. If no admitted execution capability exists, this repository must fail closed rather than inventing a provider, silently reintroducing a retired provider, or treating provider success as terminal JPV success.

Terminal deployment success requires the JPV provider-neutral chain: repository integrity, authorized head, governance admission, JPV Runtime invocation, bounded actuator health when one is required, JPV-authoritative deployed revision readback, exact revision match, and normalized JPV receipt. Route or provider failure alone does not redefine canonical JPV state; without authoritative readback the state is `UNVERIFIED`.
