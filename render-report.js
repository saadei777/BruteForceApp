// Renders TestReport.html to a print-ready PDF (A4) using the bundled Chromium.
const puppeteer = require('puppeteer');
const path = require('path');

(async () => {
  const browser = await puppeteer.launch();
  const page = await browser.newPage();
  const fileUrl = 'file:///' + path.join(__dirname, 'TestReport.html').replace(/\\/g, '/');
  await page.goto(fileUrl, { waitUntil: 'networkidle0' });
  await page.pdf({
    path: 'TestReport.pdf',
    format: 'A4',
    printBackground: true,
    preferCSSPageSize: true,
  });
  await browser.close();
  console.log('done');
})();
