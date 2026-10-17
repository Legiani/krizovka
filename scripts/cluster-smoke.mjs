// Run inside the admin container: kubectl exec -i ... -- node --input-type=module.
const cases = [
  ['API live', 'http://krizovka-api:8080/health/live', 200],
  ['API database', 'http://krizovka-api:8080/health/ready', 200],
  ['Users live', 'http://krizovka-users:8080/health/live', 200],
  ['Users database', 'http://krizovka-users:8080/health/ready', 200],
  ['Admin live', 'http://krizovka-admin:3000/health/live', 200],
  ['Admin users page', 'http://krizovka-admin:3000/uzivatele', 200],
  ['Admin organizations page', 'http://krizovka-admin:3000/organizace', 200],
  ['Admin guides page', 'http://krizovka-admin:3000/metodiky', 200],
  ['Client live', 'http://krizovka-client/health/live', 200],
  ['Client deep link', 'http://krizovka-client/metodiky', 200],
  ['API rejects anonymous session', 'http://krizovka-api:8080/api/session', 401],
  ['API rejects invalid bearer', 'http://krizovka-api:8080/api/session', 401,
    { headers: { authorization: 'Bearer invalid-smoke-test-token' } }],
  ['Admin proxy reaches API', 'http://krizovka-admin:3000/api/session', 401],
  ['Client proxy reaches API', 'http://krizovka-client/api/session', 401],
  ['Users rejects anonymous role list', 'http://krizovka-users:8080/v1/us/backoffice/roles', 401],
  ['Context requires internal API key', 'http://krizovka-users:8080/v1/us/context/validate', 401,
    { method: 'POST', headers: { 'content-type': 'application/json' }, body: '{"token":"invalid"}' }],
  ['Registration webhook requires secret', 'http://krizovka-users:8080/v1/us/webhooks/identity/registration', 401,
    { method: 'POST', headers: { 'content-type': 'application/json' }, body: '{}' }],
  ['Login rejects nonexistent account', 'http://krizovka-users:8080/v1/us/backoffice/auth/login', 401,
    { method: 'POST', headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ email: 'smoke-test@example.invalid', password: 'NotAnExistingUser123!' }) }],
];
let failed = 0;
for (const [label, url, expected, options = {}] of cases) {
  try {
    const response = await fetch(url, { ...options, signal: AbortSignal.timeout(20_000) });
    await response.arrayBuffer();
    if (response.status !== expected) throw new Error(`HTTP ${response.status}, expected ${expected}`);
    console.log(`PASS ${label}: ${response.status}`);
  } catch (error) {
    console.error(`FAIL ${label}: ${error.message}`);
    failed++;
  }
}
if (failed) process.exitCode = 1;
