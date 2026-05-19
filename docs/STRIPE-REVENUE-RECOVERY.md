# Stripe Revenue Recovery Guide

## Dashboard Configuration Steps

1. **Enable Smart Retries**
   - Go to: Billing → Revenue recovery → Retries
   - Enable Smart Retries for failed payments

2. **Enable Email Notifications**
   - Go to: Settings → Billing → Email notifications
   - Enable failed payment emails
   - Enable expiring card emails
   - Enable upcoming renewal emails (if available)

3. **Enable Customer Portal (Optional)**
   - Go to: Billing → Customer portal
   - Enable payment method update
   - Enable subscription cancellation/update (optional, for self-management)

## Webhook Events to Handle

- `checkout.session.completed`
- `invoice.payment_succeeded`
- `invoice.payment_failed`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`

## Best Practices

- Always use test mode for development and validation.
- Validate webhook signatures using STRIPE_WEBHOOK_SECRET.
- Never expose Stripe secrets or price IDs to the client.
- Document all Stripe environment variables in deployment guides.

---

This guide ensures your Stripe integration is ready for revenue recovery and automated customer communication.
