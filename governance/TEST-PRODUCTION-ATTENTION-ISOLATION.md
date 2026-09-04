# Test / Production Attention Isolation

Status: REQUIRED
Authority: JayPVentures LLC enterprise governance

## Invariant

Synthetic, demo, sample, fixture, preview, staging, QA, sandbox, training, mock, or otherwise non-production content MUST NOT reach a production attention surface unless the Founder explicitly authorizes that specific test delivery.

A production attention surface includes push notifications, email notifications, SMS, calls, badges, banners, inboxes, dashboards, feeds, alerts, or other surfaces reasonably capable of creating a real-world belief, decision, obligation, interruption, or financial expectation.

## Financial-event hard gate

Non-production content representing a loan approval, credit decision, payout, deposit, transfer, invoice, payment, debt, balance, account restriction, fraud/security event, contract, legal obligation, deadline, or other material financial/legal event MUST be blocked from production attention surfaces. Merely adding `DEMO`, `TEST`, or disclaimer copy is not sufficient isolation.

## Admission contract

Before delivery to a production attention surface, the routing layer MUST establish all of the following:

1. `environment == production`.
2. `synthetic == false` and `demo == false` and `fixture == false` and `preview == false`.
3. The event has authoritative production provenance.
4. The recipient and delivery surface are authorized for the event class.
5. Material financial/legal claims have an authoritative source reference suitable for verification.
6. The event passes the existing founder-action/relevance boundary.

Missing, ambiguous, stale, or contradictory metadata MUST fail closed.

## Required architecture

- Test identities, addresses, tokens, topics, queues, templates, datasets, and notification channels MUST be namespace-separated from production.
- Test and staging credentials MUST NOT possess production notification-delivery authority.
- Production credentials MUST reject synthetic/test payloads even if a caller attempts to route them.
- Template rendering and layout tests MUST terminate in a non-production sink by default.
- Production recipient identifiers MUST be prohibited in fixtures and automated rendering tests.
- A test requiring a real production recipient is an explicit, scoped exception and MUST identify the authorizing person, purpose, channel, event class, expiration, and audit receipt.

## Presentation defense in depth

Where synthetic content is displayed inside an explicitly authorized test surface, it MUST carry persistent machine-readable test provenance and conspicuous human-readable test treatment. This is defense in depth only; presentation labeling does not authorize production delivery.

## Regression handling

Any synthetic/test content observed on a production attention surface is a routing failure. The response is:

`detect -> suppress equivalent delivery paths -> identify source/provenance -> close environment/credential/recipient boundary -> regression test -> verify -> terminal receipt`

Do not treat recurrence as a preference problem or require the Founder to repeatedly dismiss equivalent artifacts.

## Verification requirements

CI/runtime verification MUST include negative tests proving that:

- demo financial events cannot address production recipients;
- production delivery rejects missing provenance/environment metadata;
- staging/test credentials cannot publish to production notification channels;
- rendering/layout fixtures terminate in non-production sinks;
- disclaimer text cannot convert synthetic content into production-admissible content;
- explicit test authorization is scoped and expires;
- equivalent synthetic payloads remain suppressed across email, push, SMS, dashboard, and feed routing where those surfaces exist.

A deployment is not verified merely because this document exists. Closure requires runtime enforcement plus passing regression evidence on every executable delivery surface.