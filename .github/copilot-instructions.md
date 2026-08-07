# JPV-OS Stripe / GitHub / Azure Rules

Work only inside the existing repository and preserve JPV-OS commercial authority boundaries.

Canonical billing flow:

`public offer -> canonical lookup key -> governed backend resolver -> live Stripe price verification -> Checkout -> webhook -> entitlement -> audit receipt`

Pricing authority: `JPV-OS-v2.1.0` sourced from `jaypVLabs/JPV-OS` canonical pricing governance. The access gateway consumes pricing authority; it does not define or lower prices independently.

Never:
- hardcode Stripe price IDs in source;
- expose Stripe secrets;
- accept frontend-provided Stripe price IDs or amounts;
- create duplicate checkout systems;
- bypass canonical pricing through Wix or another commerce route;
- accept partial pricing configuration;
- silently reuse legacy lookup keys;
- commit generated processor pricing maps as source authority;
- scaffold external projects;
- introduce GitHub Actions as a required execution dependency.

Required canonical subscription lookup keys:
- member_access_monthly
- member_access_annual
- creator_infrastructure_monthly
- creator_infrastructure_annual
- partner_infrastructure_monthly
- partner_infrastructure_annual
- enterprise_infrastructure_monthly
- enterprise_infrastructure_annual

Legacy commercial lookup keys including `vip_venture_*`, `creator_lane_*`, `operator_monthly`, and `enterprise_monthly` must be rejected for new checkout activity. Entitlement role names may remain independently named where required for access compatibility; they are not pricing authority.

Production price IDs and secrets must come from governed runtime environment configuration such as Azure App Settings. Runtime checkout must verify the actual Stripe Price object against canonical amount, currency, interval, lookup key, active state, and pricing-authority metadata before creating a Checkout Session.
