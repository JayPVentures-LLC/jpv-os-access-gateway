#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";

const ROOT = process.cwd();
const CONTRACT_RELATIVE = "authority/nexus-authority.json";
const CONTRACT_FILE = path.join(ROOT, ...CONTRACT_RELATIVE.split("/"));
const DOC_FILE = path.join(ROOT, "docs", "COMMERCIAL-ACCESS-SETUP.md");
const REQUIRED_ACCESS_STATES = ["requested", "checkout_started", "payment_confirmed", "active", "past_due", "cancelled", "revoked", "manual_review"];
const REQUIRED_ROLES = ["Free Access", "Member Access", "VIP Venture", "Creator Lane", "Operator Access", "Enterprise", "Sovereign Review"];
const REQUIRED_AUDIT_EVENTS = ["access_requested", "checkout_started", "payment_confirmed", "entitlement_granted", "entitlement_changed", "payment_failed", "subscription_cancelled", "entitlement_revoked", "manual_review_required"];
const BLOCKED_PRICE_PATTERNS = [
  /\$\s*\d/,
  /Example Price/i,
  /Package Pricing/i
];

function fail(message) {
  console.error(`JPV NEXUS AUTHORITY FAIL: ${message}`);
  process.exitCode = 1;
}

function readJson(file) {
  if (!fs.existsSync(file)) {
    fail(`${path.relative(ROOT, file)} missing.`);
    return null;
  }

  try {
    return JSON.parse(fs.readFileSync(file, "utf8"));
  } catch (error) {
    fail(`${path.relative(ROOT, file)} is not valid JSON: ${error.message}`);
    return null;
  }
}

function read(file) {
  return fs.existsSync(file) ? fs.readFileSync(file, "utf8") : "";
}

function requireItems(actual, required, label) {
  if (!Array.isArray(actual)) {
    fail(`${label} must be an array.`);
    return;
  }

  for (const item of required) {
    if (!actual.includes(item)) fail(`${label} missing: ${item}`);
  }
}

function main() {
  const contract = readJson(CONTRACT_FILE);
  if (!contract) return;

  if (contract.system !== "jpv-nexus") fail("contract.system must be jpv-nexus.");
  if (contract.product !== "JPV Nexus") fail("contract.product must be JPV Nexus.");
  if (contract.authorityLane !== "access") fail("authorityLane must be access.");

  requireItems(contract.accessStates, REQUIRED_ACCESS_STATES, "accessStates");
  requireItems(contract.entitlementRoles, REQUIRED_ROLES, "entitlementRoles");
  requireItems(contract.auditEvents, REQUIRED_AUDIT_EVENTS, "auditEvents");

  if (!Array.isArray(contract.mustNotOwn)) {
    fail("mustNotOwn must be an array.");
  } else if (!contract.mustNotOwn.includes("pricing authority")) {
    fail("mustNotOwn must include pricing authority.");
  }

  if (!String(contract.pricingAuthority || "").includes("must not define final prices")) fail("pricingAuthority must state JPV Nexus must not define final prices.");

  const commercialAccessDoc = read(DOC_FILE);
  for (const pattern of BLOCKED_PRICE_PATTERNS) {
    if (pattern.test(commercialAccessDoc)) fail(`commercial access setup still contains application-owned pricing pattern: ${pattern}`);
  }

  if (!process.exitCode) console.log("JPV NEXUS AUTHORITY PASS: Nexus authority validated.");
}

main();
