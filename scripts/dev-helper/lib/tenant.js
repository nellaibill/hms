const crypto = require('crypto');
const logs = require('./logs');

// Schema-backed feature keys (FeatureCatalog.SchemaBacked in HMS.Shared.Kernel) — every
// module that actually has a DbContext/migrations. Mandatory ones are unioned in server-side
// regardless, so listing all of them just gives a freshly-seeded dev tenant every module to
// test against instead of only the mandatory subset.
const SCHEMA_BACKED_FEATURES = [
  'identity', 'masters', 'patients', 'documents', 'branding',
  'hr', 'calendar', 'products', 'ipd', 'pharmacy', 'billing',
  'messages-and-notifications',
];

async function platformLogin(config) {
  const { baseUrl } = config.backend;
  const res = await fetch(`${baseUrl}/api/platform/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: config.backend.platformAdminEmail, password: config.backend.platformAdminPassword }),
  });

  const body = await res.json().catch(() => null);
  if (!res.ok) {
    throw new Error(`Platform Admin login failed (HTTP ${res.status}): ${body?.message || 'unknown error'}`);
  }
  if (body?.data?.mfaRequired) {
    throw new Error('Platform Admin has MFA enabled — this tool cannot complete an MFA login. Disable MFA for this dev account, or register the hospital manually.');
  }
  const token = body?.data?.token;
  if (!token) {
    throw new Error('Login succeeded but no token was returned.');
  }
  return token;
}

// Registers a new hospital via the real POST /api/platform/hospitals endpoint
// (HospitalsController.Create) — the same path a Platform Admin uses in the Platform
// Portal. Requires the Platform Admin account to already be seeded (Database > Run
// Migrations does this) with a password matching config.backend.platformAdminPassword.
async function createHospital(config, fields) {
  logs.append('tenant', `Logging in as Platform Admin (${config.backend.platformAdminEmail})...`, 'cmd');
  const token = await platformLogin(config);
  logs.append('tenant', 'Login OK. Registering hospital...', 'ok');

  const d = config.tenantSeedDefaults;
  const body = {
    hospitalName: fields.hospitalName || fields.hospitalCode,
    hospitalCode: fields.hospitalCode,
    mobileNumber: fields.mobileNumber || d.mobileNumber,
    address: fields.address || d.address,
    city: fields.city || d.city,
    state: fields.state || d.state,
    pincode: fields.pincode || d.pincode,
    superAdminUsername: fields.superAdminUsername,
    superAdminFirstName: fields.superAdminFirstName || d.superAdminFirstName,
    superAdminLastName: fields.superAdminLastName || d.superAdminLastName,
    superAdminEmail: fields.superAdminEmail || `${fields.superAdminUsername}@${d.superAdminEmailDomain}`,
    superAdminPhoneNumber: fields.superAdminPhoneNumber || fields.mobileNumber || d.mobileNumber,
    superAdminPassword: fields.superAdminPassword,
    enabledFeatureKeys: SCHEMA_BACKED_FEATURES,
  };

  const res = await fetch(`${config.backend.baseUrl}/api/platform/hospitals`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
      'Idempotency-Key': crypto.randomUUID(),
    },
    body: JSON.stringify(body),
  });

  const responseBody = await res.json().catch(() => null);
  if (!res.ok) {
    const message = responseBody?.message || JSON.stringify(responseBody?.validationErrors) || 'unknown error';
    logs.append('tenant', `Hospital registration failed (HTTP ${res.status}): ${message}`, 'error');
    throw new Error(message);
  }

  logs.append('tenant', `Hospital "${body.hospitalName}" (${body.hospitalCode}) registered.`, 'ok');
  return responseBody?.data;
}

module.exports = { createHospital, SCHEMA_BACKED_FEATURES };
