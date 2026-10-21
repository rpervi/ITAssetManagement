import { defineConfig, devices } from '@playwright/test';
import path from 'node:path';

const frontendUrl = process.env.ITAM_E2E_FRONTEND_URL ?? 'https://localhost:4301';
const apiUrl = process.env.ITAM_E2E_API_URL ?? 'https://localhost:7179';
const sslCertificate = process.env.ITAM_E2E_SSL_CERT;
const sslKey = process.env.ITAM_E2E_SSL_KEY;
const testConnectionString = process.env.ITAM_E2E_CONNECTION_STRING;
const username = process.env.ITAM_E2E_USERNAME;
const password = process.env.ITAM_E2E_PASSWORD;

if (!testConnectionString || !/;\s*Database\s*=\s*ITAssetManagement_TestDB(?:;|$)/i.test(testConnectionString)) {
  throw new Error(
    'Set ITAM_E2E_CONNECTION_STRING to a connection string targeting ITAssetManagement_TestDB.',
  );
}

if (!username || !password) {
  throw new Error('Set ITAM_E2E_USERNAME and ITAM_E2E_PASSWORD to synthetic test credentials.');
}

if (!sslCertificate || !sslKey) {
  throw new Error('Set ITAM_E2E_SSL_CERT and ITAM_E2E_SSL_KEY to a local HTTPS certificate and key.');
}

const apiProject = path.resolve(
  __dirname,
  '..',
  'backend',
  'ITAssetManagement.Api',
  'ITAssetManagement.Api.csproj',
);

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  timeout: 60_000,
  expect: { timeout: 10_000 },
  use: {
    baseURL: frontendUrl,
    browserName: 'chromium',
    headless: true,
    ignoreHTTPSErrors: true,
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: [
    {
      command: `dotnet run --project "${apiProject}" --launch-profile https`,
      url: `${apiUrl}/api/health`,
      reuseExistingServer: false,
      timeout: 120_000,
      ignoreHTTPSErrors: true,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ConnectionStrings__DefaultConnection: testConnectionString,
      },
    },
    {
      command: `npm start -- --port 4301 --ssl --ssl-cert "${sslCertificate}" --ssl-key "${sslKey}"`,
      url: frontendUrl,
      reuseExistingServer: false,
      timeout: 120_000,
    },
  ],
});
