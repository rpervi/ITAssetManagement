import { expect, test } from '@playwright/test';

const username = process.env.ITAM_E2E_USERNAME!;
const password = process.env.ITAM_E2E_PASSWORD!;
const suffix = `${Date.now()}-${Math.random().toString(16).slice(2, 8)}`;
const categoryName = `E2E Hardware ${suffix}`;
const individualProductName = `Dell Latitude Laptop ${suffix}`;
const bulkProductName = `Logitech Wireless Mouse ${suffix}`;
const employeeName = `Rahul Sharma ${suffix}`;

test('authenticates and exercises category, product, stock, employee, and assignment flows', async ({
  page,
  context,
  request,
}) => {
  const pageErrors: string[] = [];
  const serverErrors: string[] = [];
  page.on('pageerror', (error) => pageErrors.push(error.message));
  page.on('response', (response) => {
    if (response.url().includes('/api/') && response.status() >= 500) {
      serverErrors.push(`${response.status()} ${response.url()}`);
    }
  });

  await page.goto('/login');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page.getByText('Username is required.')).toBeVisible();
  await expect(page.getByText('Password is required.')).toBeVisible();

  await page.getByLabel('Username').fill(`invalid-${suffix}`);
  await page.getByRole('textbox', { name: 'Password' }).fill('Wrong!Password123');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page.getByRole('alert')).toContainText('Invalid username or password');

  await page.getByLabel('Username').fill(username);
  await page.getByRole('textbox', { name: 'Password' }).fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).toHaveURL(/dashboard/);
  await expect(page.getByRole('button', { name: 'Sign out' })).toBeVisible();
  const xsrfRequestToken = await page.evaluate(() =>
    document.cookie
      .split('; ')
      .find((cookie) => cookie.startsWith('__Host-ITAM.Xsrf-Request='))
      ?.split('=')
      .slice(1)
      .join('='),
  );
  expect(xsrfRequestToken).toBeTruthy();

  const unauthenticated = await request.get('/api/assetcategories');
  expect(unauthenticated.status()).toBe(401);

  await page.goto('/asset-categories');
  await expect(page.getByRole('heading', { name: 'Asset Categories' })).toBeVisible();
  await page.getByRole('button', { name: 'Create New' }).click();
  const categoryDialog = page.getByRole('dialog');
  await categoryDialog.getByLabel('Category name').fill(categoryName);
  await categoryDialog.getByLabel('Description').fill('Generated Playwright test category');
  const categoryCreateResponse = page.waitForResponse((response) =>
    response.url().endsWith('/api/assetcategories')
      && response.request().method() === 'POST',
  );
  await categoryDialog.getByRole('button', { name: 'Create category' }).click();
  const createdCategory = await categoryCreateResponse;
  const requestHeaders = await createdCategory.request().allHeaders();
  const browserCookies = await context.cookies();
  const antiforgeryCookie = browserCookies.find(
    (cookie) => cookie.name === '__Host-ITAM.Antiforgery',
  );
  const sentCookieNames = (requestHeaders.cookie ?? '')
    .split(';')
    .map((cookie) => cookie.trim().split('=')[0]);
  const xsrfHeaderMatchesCookie = requestHeaders['x-xsrf-token'] === xsrfRequestToken;
  const antiforgeryCookieSent = sentCookieNames.includes('__Host-ITAM.Antiforgery');
  expect(
    createdCategory.status(),
    `${await createdCategory.text()} (XSRF header matches cookie: ${xsrfHeaderMatchesCookie}, antiforgery cookie: ${JSON.stringify(antiforgeryCookie && { domain: antiforgeryCookie.domain, path: antiforgeryCookie.path, secure: antiforgeryCookie.secure })}, sent names: ${sentCookieNames.join(',')})`,
  ).toBe(201);
  await expect(categoryDialog).toBeHidden();
  await expect(page.getByText(categoryName)).toBeVisible();

  await page.getByRole('button', { name: 'Create New' }).click();
  const duplicateDialog = page.getByRole('dialog');
  await duplicateDialog.getByLabel('Category name').fill(`  ${categoryName.toUpperCase()}  `);
  const duplicateResponse = page.waitForResponse((response) =>
    response.url().endsWith('/api/assetcategories')
      && response.request().method() === 'POST',
  );
  await duplicateDialog.getByRole('button', { name: 'Create category' }).click();
  const rejectedDuplicate = await duplicateResponse;
  expect(rejectedDuplicate.status(), await rejectedDuplicate.text()).toBe(409);
  await expect(duplicateDialog.getByRole('alert')).toBeVisible();
  await expect(duplicateDialog.getByLabel('Category name')).toHaveValue(
    `  ${categoryName.toUpperCase()}  `,
  );
  await duplicateDialog.getByRole('button', { name: 'Cancel' }).click();

  const categoryListResponse = page.waitForResponse((response) =>
    response.url().endsWith('/api/assetcategories')
      && response.request().method() === 'GET',
  );
  await page.goto('/asset-products');
  expect((await categoryListResponse).status()).toBe(200);
  await page.getByRole('button', { name: 'Create New' }).click();
  const productDialog = page.getByRole('dialog');
  await productDialog.getByLabel('Product name').fill(individualProductName);
  await productDialog.getByLabel('Asset category').click();
  await page.getByRole('option', { name: categoryName }).click();
  await productDialog.getByLabel('Brand').fill('Dell');
  await productDialog.getByLabel('Model').fill('Latitude E2E');
  await productDialog.getByLabel('Tracking type').click();
  await page.getByRole('option', { name: 'Individual' }).click();
  await productDialog.getByRole('button', { name: 'Save Product' }).click();
  await expect(productDialog).toBeHidden();
  await expect(page.getByText(individualProductName)).toBeVisible();

  await page.getByRole('button', { name: 'Create New' }).click();
  const bulkProductDialog = page.getByRole('dialog');
  await bulkProductDialog.getByLabel('Product name').fill(bulkProductName);
  await bulkProductDialog.getByLabel('Asset category').click();
  await page.getByRole('option', { name: categoryName }).click();
  await bulkProductDialog.getByLabel('Brand').fill('Logitech');
  await bulkProductDialog.getByLabel('Tracking type').click();
  await page.getByRole('option', { name: 'Bulk' }).click();
  await bulkProductDialog.getByRole('button', { name: 'Save Product' }).click();
  await expect(bulkProductDialog).toBeHidden();
  await expect(page.getByText(bulkProductName)).toBeVisible();

  await page.goto('/employees');
  await page.getByRole('button', { name: 'Create New' }).click();
  const employeeDialog = page.getByRole('dialog');
  await employeeDialog.getByLabel('Employee Code').fill(`EMP-${suffix}`);
  await employeeDialog.getByLabel('Employee Name').fill(employeeName);
  await employeeDialog.getByLabel('Email').fill(`rahul-${suffix}@example.test`);
  await employeeDialog.getByLabel('Department').fill('Development');
  await employeeDialog.getByRole('button', { name: 'Save Employee' }).click();
  await expect(employeeDialog).toBeHidden();
  await expect(page.getByText(employeeName)).toBeVisible();

  await page.goto('/receive-stock');
  await page.getByLabel('Asset product').click();
  await page.getByRole('option', { name: new RegExp(individualProductName) }).click();
  for (let index = 1; index < 5; index++) {
    await page.getByRole('button', { name: 'Add asset' }).click();
  }
  const assetRows = page.locator('.asset-row');
  await expect(assetRows).toHaveCount(5);
  for (let index = 0; index < 5; index++) {
    const row = assetRows.nth(index);
    await row.getByLabel('Serial number (optional)').fill(`E2E-${suffix}-${index + 1}`);
    await row.getByLabel('Purchase date').fill('2026-10-18');
    await row.getByLabel('Warranty expiry').fill('2027-10-18');
    await row.getByLabel('Purchase cost').fill('65000');
  }
  const individualReceiptResponse = page.waitForResponse((response) =>
    response.url().includes('/api/stock/receive/individual')
      && response.request().method() === 'POST',
  );
  await page.getByRole('button', { name: 'Receive Stock' }).click();
  const createdIndividualReceipt = await individualReceiptResponse;
  expect(createdIndividualReceipt.status(), await createdIndividualReceipt.text()).toBe(201);
  await expect(page.getByText(/Stock received/).last()).toBeVisible();

  await page.getByRole('radio', { name: /Bulk Stock/ }).click();
  await page.getByLabel('Asset product').click();
  await page.getByRole('option', { name: new RegExp(bulkProductName) }).click();
  await page.getByLabel('Received quantity').fill('25');
  await page.getByLabel('Unit purchase price').fill('1200');
  await page.getByLabel('Purchase date').fill('2026-10-18');
  await page.getByLabel('Warranty expiry').fill('2027-10-18');
  await page.getByLabel('Supplier / vendor').fill(`E2E Supplier ${suffix}`);
  const bulkReceiptResponse = page.waitForResponse((response) =>
    response.url().includes('/api/stock/receive/bulk')
      && response.request().method() === 'POST',
  );
  await page.getByRole('button', { name: 'Receive Stock' }).click();
  expect((await bulkReceiptResponse).status()).toBe(201);
  await expect(page.getByText(/Stock received/).last()).toBeVisible();

  await page.goto('/inventory');
  await expect(page.getByText(individualProductName)).toBeVisible();
  await expect(page.getByText(bulkProductName)).toBeVisible();
  const laptopRow = page.locator('tr').filter({ hasText: individualProductName });
  const mouseRow = page.locator('tr').filter({ hasText: bulkProductName });
  await expect(laptopRow).toContainText('5');
  await expect(mouseRow).toContainText('25');

  await page.goto('/assign-assets');
  await page.getByRole('button', { name: 'Create Assignment' }).click();
  const assignmentDialog = page.getByRole('dialog');
  await assignmentDialog.getByLabel('Employee').click();
  await page.getByRole('option', { name: new RegExp(employeeName) }).click();
  await assignmentDialog.getByLabel('Asset Product').click();
  await page.getByRole('option', { name: new RegExp(individualProductName) }).click();
  const assetCheckbox = assignmentDialog.getByRole('checkbox').first();
  await expect(assetCheckbox).toBeVisible();
  await assetCheckbox.check();
  const assignmentResponse = page.waitForResponse((response) =>
    response.url().endsWith('/api/assetassignments')
      && response.request().method() === 'POST',
  );
  await assignmentDialog.getByRole('button', { name: 'Assign Assets' }).click();
  expect((await assignmentResponse).status()).toBe(201);
  await expect(assignmentDialog).toBeHidden();
  await expect(page.getByText(employeeName)).toBeVisible();

  await page.getByRole('button', { name: 'Sign out' }).click();
  await expect(page).toHaveURL(/login/);
  await page.goto('/assign-assets');
  await expect(page).toHaveURL(/login/);

  const cookies = await context.cookies();
  expect(cookies.some((cookie) => cookie.name === '__Host-ITAM.Auth')).toBe(false);
  expect(pageErrors).toEqual([]);
  expect(serverErrors).toEqual([]);
});

for (const viewport of [
  { name: 'desktop', width: 1920, height: 1080 },
  { name: 'laptop', width: 1366, height: 768 },
  { name: 'tablet', width: 768, height: 1024 },
  { name: 'mobile', width: 390, height: 844 },
]) {
  test(`login screen is usable at ${viewport.name} dimensions`, async ({ page }) => {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    await page.goto('/login');
    await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible();
    await expect(page.getByLabel('Username')).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Password' })).toBeVisible();
    const overflows = await page.locator('body').evaluate((body) =>
      body.scrollWidth > document.documentElement.clientWidth,
    );
    expect(overflows).toBe(false);
  });
}
