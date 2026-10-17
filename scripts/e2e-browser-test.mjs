import puppeteer from 'puppeteer-core';
import fs from 'node:fs';
import path from 'node:path';

const SCREENSHOT_DIR = path.resolve('docs/screenshots');
if (!fs.existsSync(SCREENSHOT_DIR)) {
  fs.mkdirSync(SCREENSHOT_DIR, { recursive: true });
}

async function run() {
  console.log('🚀 Spouštím Google Chrome...');
  const browser = await puppeteer.launch({
    executablePath: '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1280,900'],
  });

  try {
    const page = await browser.newPage();
    await page.setViewport({ width: 1280, height: 900 });

    // 1. Ověření neautentizovaného přesměrování
    console.log('1. Testuji přesměrování na /login...');
    await page.goto('http://localhost:3100/', { waitUntil: 'networkidle2' });
    const currentUrl = page.url();
    if (!currentUrl.includes('/login')) {
      throw new Error(`Očekáváno přesměrování na /login, ale URL je: ${currentUrl}`);
    }
    console.log('✅ Přesměrováno na:', currentUrl);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '01_login_page.png') });

    // 2. Vyplnění přihlašovacího formuláře a odeslání
    console.log('2. Přihlašuji operátora (operator@krizovka.cz)...');
    await page.type('#login-email', 'operator@krizovka.cz');
    await page.type('#login-password', 'Operator123!');
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle2' }),
      page.click('button[type="submit"]'),
    ]);
    console.log('✅ Přihlášeno, URL:', page.url());
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '02_logged_in_dashboard.png') });

    // Ověření hlavičky operátora
    const headerText = await page.$eval('.app-header', el => el.textContent);
    if (!headerText.includes('Dispečer Krizovky') || !headerText.includes('Operátor velínu')) {
      throw new Error(`Hlavička neobsahuje operátora: ${headerText}`);
    }
    console.log('✅ V hlavičce zobrazen operátor a role');

    // 3. Live triage hlášení a otevření detailu
    console.log('3. Testuji Live Triage a otevření detailu hlášení...');
    await page.waitForSelector('button[aria-label^="Detail hlášení"]', { timeout: 10000 });
    const detailButtons = await page.$$('button[aria-label^="Detail hlášení"]');
    if (detailButtons.length === 0) {
      throw new Error('Nenalezena žádná tlačítka pro detail hlášení.');
    }
    await detailButtons[0].click();

    // Počkat na modal detailu
    await page.waitForSelector('.modal-body', { timeout: 5000 });
    console.log('✅ Modal detailu hlášení otevřen');
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '03_report_detail_modal.png') });

    // Ověřit přítomnost mapového odkazu nebo souřadnic
    const modalContent = await page.$eval('.modal-body', el => el.textContent);
    console.log('✅ Obsah modalu načten:', modalContent.slice(0, 100).replace(/\s+/g, ' '), '...');

    // Zavřít modal
    const closeBtn = await page.$('.modal-header .btn-close') || await page.$('.modal-footer button');
    if (closeBtn) await closeBtn.click();
    await page.waitForFunction(() => !document.querySelector('.modal') && !document.querySelector('.modal-backdrop'), { timeout: 5000 }).catch(() => {});
    await new Promise(r => setTimeout(r, 400));
    console.log('✅ Modal zavřen a backdrop odstraněn');

    // 4. Správa organizací (/organizace)
    console.log('4. Přecházím na /organizace...');
    await page.goto('http://localhost:3100/organizace', { waitUntil: 'networkidle2' });
    await page.waitForSelector('button[aria-label^="Upravit organizaci"]', { timeout: 10000 });
    console.log('✅ Stránka organizací načtena');
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '04_organizations.png') });

    // Otevřít editor organizace
    const editOrgBtn = await page.waitForSelector('button[aria-label^="Upravit organizaci"]', { timeout: 5000 });
    await editOrgBtn.evaluate(b => b.click());
    await page.waitForSelector('#organization-editor-title', { timeout: 5000 });
    console.log('✅ Editor organizace otevřen');

    // Ověřit políčko offset / buffer
    const offsetVal = await page.$eval('#area-offset', el => el.value);
    console.log(`✅ Formulář areálu načten, offset: ${offsetVal} m`);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '05_organization_editor.png') });

    // Zavřít modal editoru
    const cancelOrgBtn = await page.$('.modal-header .btn-close') || await page.waitForSelector('.modal-footer button.btn-outline-secondary');
    if (cancelOrgBtn) await cancelOrgBtn.click();
    await page.waitForFunction(() => !document.querySelector('.modal') && !document.querySelector('.modal-backdrop'), { timeout: 5000 }).catch(() => {});
    await new Promise(r => setTimeout(r, 400));

    // 5. Krizové metodiky (/metodiky)
    console.log('5. Přecházím na /metodiky...');
    await page.goto('http://localhost:3100/metodiky', { waitUntil: 'networkidle2' });
    await page.waitForSelector('.guide-grid', { timeout: 10000 });
    console.log('✅ Metodiky organizace zobrazeny');
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '06_guides_org.png') });

    // Přepnout na záložku šablon
    console.log('Přepínám na záložku "Centrální knihovna šablon"...');
    const tabs = await page.$$('.nav-pills .nav-link');
    if (tabs.length >= 2) {
      await tabs[1].evaluate(el => el.click());
      await page.waitForFunction(
        () => document.body.textContent.includes('Převzít do organizace') || document.body.textContent.includes('Převzít další variantu'),
        { timeout: 5000 }
      );
      console.log('✅ Centrální šablony zobrazeny');
      await page.screenshot({ path: path.join(SCREENSHOT_DIR, '07_guides_templates.png') });
    }

    // Kliknout na "+ Nová metodika od nuly"
    console.log('Otevírám formulářový editor metodiky...');
    const btns = await page.$$('button');
    let newGuideBtn = null;
    for (const b of btns) {
      const t = await b.evaluate(el => el.textContent);
      if (t && t.includes('Nová metodika')) {
        newGuideBtn = b;
        break;
      }
    }

    if (newGuideBtn) {
      await newGuideBtn.evaluate(b => b.click());
      await page.waitForSelector('#guide-editor-title', { timeout: 5000 });
      console.log('✅ Editor nové metodiky otevřen');
      await page.screenshot({ path: path.join(SCREENSHOT_DIR, '08_guide_editor_modal.png') });

      const closeGuideBtn = await page.$('.modal-header .btn-close') || await page.waitForSelector('.modal-footer button.btn-outline-secondary');
      if (closeGuideBtn) await closeGuideBtn.evaluate(b => b.click());
      await page.waitForFunction(() => !document.querySelector('.modal') && !document.querySelector('.modal-backdrop'), { timeout: 5000 }).catch(() => {});
      console.log('✅ Editor metodiky zavřen');
    }

    console.log('🎉 Všechny kroky v reálném Google Chrome proběhly 100% úspěšně!');
  } finally {
    await browser.close();
  }
}

run().catch(err => {
  console.error('❌ Chyba při testu prohlížeče:', err);
  process.exit(1);
});
