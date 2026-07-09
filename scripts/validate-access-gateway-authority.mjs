#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";

const ROOT = process.cwd();
const CONTRACT_FILE = path.join(ROOT, "authority", "access-gateway-authority.json");
const DOC_FILE = path.join(ROOT, "docs", "COMMERCIAL-ACCESS-SETUP.md");
const REQUIRED_ACCESS_STATES = ["requested", "checkout_started", "payment_confirmed", "active", "past_due", "cancelled", "revoked", "manual_review"];
const REQUIRED_ROLES = ["Free Access", "Member Access", "VIP Venture", "Creator Lane", "Operator Access", "Enterprise", "Sovereign Review"];
const REQUIRED_AUDIT_EVENTS = ["access_requested", "checkout_started", "payment_confirmed", "entitlement_granted", "entitlement_changed", "payment_failed", "subscription_cancelled", "entitlement_revoked", "manual_review_required"];
const BLOCKED_PRICE_PATTERNS = [
  /\$25\/mo/i,
  /\$250\/yr/i,
  /\$100\/mo/i,
  /\$500\/mo/i,
  /Example Price/i,
  /Package Pricing/i
];

function fail(message) {
  console.error(`ACCESS GATEWAY AUTHORITY FAIL: ${message}`);
  process.exitCode = 1;
}

function readJson(file) {
  if (!fs.existsSync(file)) fail(`${path.relative(ROOT, file)} missing.`);
  return JSON.parse(fs.readFileSync(file, "utf8"));
}

function read(file) {
  return fs.existsSync(file) ? fs.readFileSync(file, "utf8") : "";
}

function requireItems(actual, required, label) {
  for (const item of required) {
    if (!actual.includes(item)) fail(`${label} missing: ${item}`);
  }
}

function main() {
  const contract = readJson(CONTRACT_FILE);
  if (contract.system !== "access-gateway") fail("contract.system must be access-gateway.");
  if (contract.authorityLane !== "access") fail("authorityLane must be access.");

  requireItems(contract.accessStates || [], REQUIRED_ACCESS_STATES, "accessStates");
  requireItems(contract.entitlementRoles || [], REQUIRED_ROLES, "entitlementRoles");
  requireItems(contract.auditEvents || [], REQUIRED_AUDIT_EVENTS, "auditEvents");

  if ((contract.mustNotOwn || []).includes("pricing authority") === false) fail("mustNotOwn must include pricing authority.");
  if (!String(contract.pricingAuthority || "").includes("must not define final prices")) fail("pricingAuthority must state gateway must not define final prices.");

  const commercialAccessDoc = read(DOC_FILE);
  for (const pattern of BLOCKED_PRICE_PATTERNS) {
    if (pattern.test(commercialAccessDoc)) fail(`commercial access setup still contains gateway-owned pricing pattern: ${pattern}`);
  }

  if (!process.exitCode) console.log("ACCESS GATEWAY AUTHORITY PASS: access authority validated.");
}

main();
