# JPV Nexus Firebase Foundation

## Authority

- Application repository: `JayPVentures-LLC/jpv-os-access-gateway`
- Google Cloud project: `jpv-nexus-production-502019`
- Governance authority: `JayPVentures-LLC/jpv-governance`
- Runtime boundary: JPV Nexus routes privileged operations through JPV Core.

## Current security baseline

Firestore and Cloud Storage are deny-by-default for every client, including authenticated administrators. This is intentional. Firebase custom claims describe identity context but do not independently grant production data access.

Privileged operations must:

1. verify the Firebase ID token server-side;
2. validate `jpv_role`, `jpv_lanes`, `environment`, and `schema_version`;
3. enforce resource-level authorization;
4. execute through a workload identity with least privilege;
5. append a safe JPV Ledger decision event.

## Local verification

From the repository root:

```powershell
.\scripts\verify-firebase-project.ps1
```

From `firebase/`:

```powershell
npm install
npm run test:emulators
```

The project verification script is read-only. It confirms the active identity, exact project, billing linkage, and Firebase visibility.

## Deployment gate

This foundation does not deploy resources or modify IAM. Production provisioning requires a separate reviewed change with:

- an approved Firebase product and region inventory;
- workload identity and least-privilege IAM plan;
- App Check configuration;
- Secret Manager boundaries;
- retention and export decisions for JPV Ledger;
- backup and rollback evidence;
- explicit production approval.

## Rollback

Before deployment, rollback is deletion of this configuration branch. After any future deployment, rollback must use a separately approved plan that restores the prior rules release and service revision. Never delete production data as a rollback mechanism.
