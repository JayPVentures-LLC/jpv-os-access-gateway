#!/usr/bin/env node

import fs from 'node:fs';
import path from 'node:path';

const root = process.cwd();
const manifestPath = path.join(root, 'authority', 'canonical-pricing-consumer.json');
const pricingPagePath = path.join(root, 'src', 'JPVOS', 'Components', 'Pages', 'Pricing.razor');
const stripeConfigPath = path.join(root, 'scripts', 'configure-full-stripe-pricing.ps1');
const stripeConvergencePath = path.join(root, 'scripts', 'Invoke-JPVCanonicalStripeAzureConvergence.ps1');
const pricingLoaderPath = path.join(root, 'src', 'JPVOS', 'Infrastructure', 'Stripe', 'StripePricingLoader.cs');
const checkoutServicePath = path.join(root, 'src', 'JPVOS', 'Infrastructure', 'Stripe', 'StripeCheckoutService.cs');
const programPath = path.join(root, 'src', 'JPVOS', 'Program.cs');
const docsPath = path.join(root, 'docs', 'COMMERCIAL-ACCESS-SETUP.md');

const obsoleteActivePaths = [
  'scripts/auto-setup-stripe-pricing-azure.ps1',
  'scripts/final-live-stripe-setup.ps1',
  'scripts/configure-stripe-dashboard-price-ids.ps1',
  'scripts/configure-live-stripe-full-integration.ps1',
  'scripts/auto-create-stripe-products-and-configure-azure.ps1',
  'src/JPVOS/Services/WixCheckoutConfig.cs',
  'docs/WIX-CHECKOUT-ROUTING.md'
];

function fail(message) {
  console.error(`CANONICAL PRICING FAIL: ${message}`);
  process.exitCode = 1;
}

function read(file) {
  if (!fs.existsSync(file)) {
    fail(`Missing required file: ${path.relative(root, file)}`);
    return '';
  }
  return fs.readFileSync(file, 'utf8');
}

function requireText(text, needle, label) {
  if (!text.includes(needle)) fail(`${label} missing required value: ${needle}`);
}

const manifest = JSON.parse(read(manifestPath));
const pricingPage = read(pricingPagePath);
const stripeConfig = read(stripeConfigPath);
const stripeConvergence = read(stripeConvergencePath);
const pricingLoader = read(pricingLoaderPath);
const checkoutService = read(checkoutServicePath);
const program = read(programPath);
const docs = read(docsPath);

requireText(pricingPage, '/api/checkout/start?lookupKey=', 'Pricing page');
requireText(pricingLoader, manifest.authorityVersion, 'Stripe pricing loader');
requireText(stripeConfig, manifest.authorityVersion, 'Stripe provisioning');
requireText(stripeConfig, 'JPV_PRICING_AUTHORITY=JPV-OS-v2.1.0', 'Stripe environment template');
requireText(stripeConvergence, manifest.authorityVersion, 'Stripe/Azure convergence');
requireText(stripeConvergence, 'JPV_PRICING_AUTHORITY', 'Stripe/Azure convergence');
requireText(program, 'AddSingleton<StripePricingLoader>()', 'Program service registration');
requireText(program, 'AddSingleton<StripeCheckoutService>()', 'Program service registration');
requireText(checkoutService, 'new PriceService()', 'Live Stripe price verification');
requireText(checkoutService, 'stripePrice.UnitAmount != expected.Amount', 'Live Stripe amount verification');
requireText(checkoutService, 'stripePrice.LookupKey', 'Live Stripe lookup-key verification');
requireText(checkoutService, 'pricing_authority', 'Live Stripe authority verification');

for (const offer of manifest.offers) {
  requireText(pricingPage, `\"${offer.key}\"`, 'Pricing page');
  requireText(pricingPage, `${offer.monthly},`, 'Pricing page');
  requireText(pricingPage, `${offer.annual},`, 'Pricing page');

  const monthlyCents = offer.monthly * 100;
  const annualCents = offer.annual * 100;
  requireText(stripeConfig, `key=\"${offer.monthlyLookupKey}\";`, 'Stripe provisioning');
  requireText(stripeConfig, `amount=${monthlyCents};`, 'Stripe provisioning');
  requireText(stripeConfig, `key=\"${offer.annualLookupKey}\";`, 'Stripe provisioning');
  requireText(stripeConfig, `amount=${annualCents};`, 'Stripe provisioning');

  requireText(pricingLoader, `[\"${offer.monthlyLookupKey}\"] = (${monthlyCents}, \"month\")`, 'Stripe pricing loader');
  requireText(pricingLoader, `[\"${offer.annualLookupKey}\"] = (${annualCents}, \"year\")`, 'Stripe pricing loader');

  requireText(stripeConvergence, `${offer.monthlyLookupKey} = @{ amount = ${monthlyCents}; interval = 'month' }`, 'Stripe/Azure convergence');
  requireText(stripeConvergence, `${offer.annualLookupKey} = @{ amount = ${annualCents}; interval = 'year' }`, 'Stripe/Azure convergence');

  requireText(docs, offer.monthlyLookupKey, 'Commercial access documentation');
  requireText(docs, offer.annualLookupKey, 'Commercial access documentation');
}

for (const legacy of manifest.legacyLookupKeysRejected) {
  requireText(pricingLoader, `\"${legacy}\"`, 'Legacy-key rejection set');
}

for (const relativePath of obsoleteActivePaths) {
  if (fs.existsSync(path.join(root, relativePath))) {
    fail(`Obsolete checkout or pricing path still active: ${relativePath}`);
  }
}

if (!pricingLoader.includes('throw new InvalidOperationException')) {
  fail('Stripe pricing loader must fail closed on pricing drift.');
}

if (!pricingLoader.includes('Partial canonical Stripe environment configuration is prohibited')) {
  fail('Partial production price configuration must fail closed.');
}

if (pricingPage.includes('WixCheckoutConfig') || pricingPage.includes('WixConfig.GetCheckoutUrl')) {
  fail('Public paid checkout still bypasses governed Stripe pricing through Wix checkout routing.');
}

if (pricingPage.includes('99,') || pricingPage.includes('199,') || pricingPage.includes('999,') || pricingPage.includes('4999,')) {
  fail('Stale public pricing detected.');
}

if (!process.exitCode) {
  console.log('PASS: canonical authority, public pricing, provisioning, Azure propagation, runtime map validation, live Stripe verification, and checkout routing are aligned.');
}
