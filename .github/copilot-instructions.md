# JPV-OS Stripe / GitHub / Azure Rules

Work only inside the existing repository.

Billing flow:
Frontend -> lookup_key -> backend resolver -> canonical pricing JSON -> Stripe price_id.

Never:
- hardcode Stripe price IDs
- expose Stripe secrets
- accept frontend-provided price IDs
- create duplicate checkout systems
- scaffold external projects

Required lookup keys:
- member_access_monthly
- member_access_annual
- vip_venture_monthly
- vip_venture_annual
- creator_lane_monthly
- operator_monthly
- enterprise_monthly

Secrets must come only from environment variables, GitHub Actions secrets, Azure App Settings, or approved hosting secrets.
