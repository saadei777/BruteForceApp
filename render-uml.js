// One-off helper: render UML_ClassDiagram.html to a PNG for the report.
const puppeteer = require('puppeteer');

(async () => {
  const browser = await puppeteer.launch();
  const page = await browser.newPage();
  await page.setViewport({ width: 1340, height: 1320, deviceScaleFactor: 2 });
  await page.goto('http://localhost:8099/UML_ClassDiagram.html', { waitUntil: 'networkidle0' });
  const canvas = await page.$('.canvas');
  // Capture the title + canvas region
  await page.screenshot({ path: 'UML_ClassDiagram.png', clip: { x: 0, y: 0, width: 1340, height: 1320 } });
  await browser.close();
  console.log('done');
})();

