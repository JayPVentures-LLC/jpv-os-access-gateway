# Microsoft Teams Notification Setup for CI/CD

This project is now fully automated to notify your team in Microsoft Teams after every production deployment.

## How it works

- The `.github/workflows/teams-notify.yml` workflow listens for completion of the `deploy-appservice` workflow.
- On completion (success or failure), it sends a message to your Teams channel with:
  - Workflow name
  - Status (success/failure)
  - Commit message
  - Actor (who triggered)
  - Run link

## Setup Instructions

1. **Create an Incoming Webhook in Teams:**
   - Go to your Teams channel > Connectors > Incoming Webhook.
   - Name it (e.g., "JPV-OS Deploys") and copy the webhook URL.
2. **Add the webhook URL as a GitHub secret:**
   - Go to your repo > Settings > Secrets and variables > Actions.
   - Add a new secret named `MSTEAMS_WEBHOOK_URL` with the webhook URL value.
3. **Done!**
   - All future deployments will notify your team automatically.

## Troubleshooting

- If notifications do not appear, check the Actions logs for `teams-notify.yml`.
- Ensure the webhook URL is correct and the secret is set.
- The notification will only trigger after the `deploy-appservice` workflow completes.

---

For advanced customization, see the [skitionek/notify-microsoft-teams](https://github.com/skitionek/notify-microsoft-teams) GitHub Action documentation.
