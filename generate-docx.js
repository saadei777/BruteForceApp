// Builds an editable Word (.docx) version of the test report.
const fs = require('fs');
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  ImageRun, AlignmentType, HeadingLevel, BorderStyle, WidthType, ShadingType,
  LevelFormat, PageBreak, VerticalAlign,
} = require('docx');

const F = 'Arial';
const MONO = 'Consolas';
const NAVY = '1F2A44';
const ACCENT = '2E5AAC';
const FILL = '1F6FD6';
const CONTENT_W = 9026; // A4, 1-inch margins

// ---------- helpers ----------
const t = (text, opts = {}) => new TextRun({ text, font: F, ...opts });
const mono = (text, opts = {}) => new TextRun({ text, font: MONO, size: 19, ...opts });
const p = (children, opts = {}) =>
  new Paragraph({ children: Array.isArray(children) ? children : [children], spacing: { after: 120 }, ...opts });

const h1 = (text) => new Paragraph({ heading: HeadingLevel.HEADING_1, children: [t(text, { bold: true })] });
const h2 = (text) => new Paragraph({
  heading: HeadingLevel.HEADING_2,
  border: { bottom: { style: BorderStyle.SINGLE, size: 8, color: '89A0D0', space: 3 } },
  children: [t(text, { bold: true })],
});
const h3 = (text) => new Paragraph({ heading: HeadingLevel.HEADING_3, children: [t(text, { bold: true })] });

const bullet = (children) =>
  new Paragraph({ numbering: { reference: 'bullets', level: 0 }, spacing: { after: 80 },
    children: Array.isArray(children) ? children : [children] });

function image(file, w, h, caption) {
  const [nw, nh] = sizeOf(file);
  const height = Math.round((w * nh) / nw);
  return [
    new Paragraph({
      alignment: AlignmentType.CENTER, spacing: { before: 120, after: 60 },
      children: [new ImageRun({
        type: 'png', data: fs.readFileSync(file),
        transformation: { width: w, height: height },
        altText: { title: caption, description: caption, name: file },
      })],
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER, spacing: { after: 200 },
      children: [t(caption, { italics: true, size: 18, color: '5A6478' })],
    }),
  ];
}
function sizeOf(file) {
  // dimensions hard-coded (queried once) to avoid an image-parsing dependency
  const m = {
    'UML_ClassDiagram.png': [2680, 2640],
    'screenshots/01_initial.png': [640, 680],
    'screenshots/02_password_generated.png': [800, 850],
    'screenshots/03_password_found.png': [800, 850],
    'screenshots/04_benchmark.png': [800, 850],
  };
  return m[file] || [800, 850];
}

const border = { style: BorderStyle.SINGLE, size: 4, color: 'C5CEE4' };
const borders = { top: border, bottom: border, left: border, right: border,
  insideHorizontal: border, insideVertical: border };

function cell(content, width, { header = false, span } = {}) {
  const kids = Array.isArray(content) ? content : [new Paragraph({ children: [t(content, { size: 19, bold: header })] })];
  return new TableCell({
    width: { size: width, type: WidthType.DXA },
    columnSpan: span,
    shading: header ? { fill: 'E6ECF9', type: ShadingType.CLEAR, color: 'auto' } : undefined,
    margins: { top: 60, bottom: 60, left: 110, right: 110 },
    verticalAlign: VerticalAlign.CENTER,
    children: kids,
  });
}

// Build a table from rows of [text or {runs:[...]}], first row = header
function makeTable(widths, rows) {
  return new Table({
    width: { size: CONTENT_W, type: WidthType.DXA },
    columnWidths: widths,
    borders,
    rows: rows.map((cells, ri) =>
      new TableRow({
        tableHeader: ri === 0,
        children: cells.map((c, ci) => {
          if (c && c.__cell) return c.build(widths[ci]);
          const para = (typeof c === 'string')
            ? new Paragraph({ children: [t(c, { size: 19, bold: ri === 0 })] })
            : new Paragraph({ children: c.runs });
          return cell([para], widths[ci], { header: ri === 0 });
        }),
      })
    ),
  });
}
const runsCell = (runs) => ({ __cell: true, build: (w) => cell([new Paragraph({ children: runs })], w) });

// ---------- content ----------
const children = [];

// Cover
children.push(
  new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 2600, after: 0 },
    children: [t('TEST REPORT', { color: '76819C', size: 22, characterSpacing: 60 })] }),
  new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 120, after: 80 },
    children: [t('Brute-Force Password Cracker', { bold: true, size: 56, color: NAVY })] }),
  new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 360 },
    children: [t('Multi-threaded password recovery  ·  WPF / .NET 8 / C#', { size: 26, color: '41506F' })] }),
);
const coverRow = (label, value, fill = true) => new Paragraph({
  alignment: AlignmentType.CENTER, spacing: { after: 120 },
  children: [ t(label + '   ', { bold: true, size: 24, color: NAVY }),
    t(value, fill ? { bold: true, size: 24, color: FILL } : { size: 24 }) ],
});
children.push(
  coverRow('Author:', '[ Your full name ]'),
  coverRow('Student ID:', '[ Your ID ]'),
  coverRow('Course / Group:', '[ Course — Group ]'),
  coverRow('Date:', '4 June 2026', false),
  coverRow('GitHub repository:', '[ https://github.com/<you>/BruteForceApp ]'),
  new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 600 },
    children: [t('Each task committed as a separate version (v1.0 – v1.3) — see commit history & README.md',
      { size: 18, color: '8893AB', italics: true })] }),
  new Paragraph({ children: [new PageBreak()] }),
);

// 1. Overview
children.push(h1('1. Project Overview'));
children.push(p([
  t('This application recovers a hashed password by brute force. A random password is created and hashed with '),
  t('SHA-256 + a constant static salt', { bold: true }),
  t('; the attacker side then searches every possible character combination — starting from length 1 — across multiple threads until the matching plaintext is found. The program is built with '),
  t('WPF on .NET 8', { bold: true }),
  t(' and has a graphical interface for password creation, starting/stopping the attack, live progress, elapsed time, and the recovered password. It also benchmarks '),
  t('single-threaded vs multi-threaded', { bold: true }), t(' execution.'),
]));
children.push(p([
  t('Following the requirements, '),
  t('each major responsibility lives in its own class and file', { bold: true }),
  t(', and the brute-force '), t('generator', { bold: true }), t(' and '),
  t('validator', { bold: true }), t(' are implemented independently.'),
]));
children.push(h3('Class / file structure'));
children.push(makeTable([2150, 2050, 4826], [
  ['File', 'Class', 'Responsibility'],
  [runsCell([mono('PasswordManager.cs')]), runsCell([mono('PasswordManager')]),
   { runs: [t('Creates the random password (length [4,6)) and hashes any string with SHA-256 + static salt.', { size: 19 })] }],
  [runsCell([mono('PasswordValidator.cs')]), runsCell([mono('PasswordValidator')]),
   { runs: [t('Independently checks whether a candidate’s hash equals the target hash.', { size: 19 })] }],
  [runsCell([mono('BruteForceGenerator.cs')]), runsCell([mono('BruteForceGenerator')]),
   { runs: [t('Generates all combinations from length 1 → 6; maps an index to a candidate for partitioning.', { size: 19 })] }],
  [runsCell([mono('BruteForceEngine.cs')]), runsCell([mono('BruteForceEngine')]),
   { runs: [t('Coordinates the multi-threaded attack: partitions the search space, raises progress/found events, stops all threads on success.', { size: 19 })] }],
  [runsCell([mono('PerformanceLogger.cs')]), runsCell([mono('PerformanceLogger')]),
   { runs: [t('Runs and logs the single-thread vs multi-thread benchmark and computes the speedup.', { size: 19 })] }],
  [runsCell([mono('MainWindow.xaml(.cs)')]), runsCell([mono('MainWindow')]),
   { runs: [t('The GUI; wires the classes together and updates the display on the UI thread.', { size: 19 })] }],
]));

// 2. UML
children.push(new Paragraph({ children: [new PageBreak()] }));
children.push(h1('2. UML Class Diagram'));
children.push(p(t('The diagram below shows the classes, their attributes and methods, and the relationships between them — composition (filled diamond), aggregation (hollow diamond) and dependency (dashed «uses» arrow), with multiplicities.')));
children.push(...image('UML_ClassDiagram.png', 560,
  'UML class diagram — layered by dependency. MainWindow (GUI) owns the manager and logger and creates the engine; engine and logger depend on the independent validator and generator; both rely on PasswordManager.'));

// 3. Requirements
children.push(new Paragraph({ children: [new PageBreak()] }));
children.push(h1('3. Functionality — Requirement by Requirement'));
children.push(p(t('Every item from the brief is implemented. The table maps each requirement to where and how it is met.')));
const okCell = (w) => cell([new Paragraph({ children: [t('Done', { size: 19, bold: true, color: '1F8A4C' })] })], w);
const reqRow = (req, how) => [
  { runs: [t(req, { size: 19 })] },
  { __cell: true, build: okCell },
  { runs: how },
];
children.push(makeTable([3100, 1000, 4926], [
  ['Requirement', 'Status', 'How it is implemented'],
  reqRow('UML class diagram', [t('Section 2; sources ', { size: 19 }), mono('UML_ClassDiagram.puml/.html/.png'), t('.', { size: 19 })]),
  reqRow('GUI: creation, start/stop, progress, elapsed, result', [t('MainWindow.xaml — Generate, Start/Stop, progress bar + %, elapsed timer, found-password field, log pane.', { size: 19 })]),
  reqRow('Each major function in a separate class/file', [t('Six classes in six files (see Section 1).', { size: 19 })]),
  reqRow('(a) SHA-256 with a constant static salt', [mono('PasswordManager.HashPassword()'), t(' hashes ', { size: 19 }), mono('SALT + input'), t('; ', { size: 19 }), mono('SALT'), t(' is a ', { size: 19 }), mono('const'), t('.', { size: 19 })]),
  reqRow('(b) Password length random in [4,6)', [mono('_random.Next(4, 6)'), t(' → 4 or 5 characters.', { size: 19 })]),
  reqRow('(c) Brute force length 1 → 6, length unknown', [t('Engine loops ', { size: 19 }), mono('length = 1 → 6'), t('; it never reads the real length.', { size: 19 })]),
  reqRow('(d) Multi-threading (Thread / Task)', [t('Engine uses ', { size: 19 }), mono('Task'), t('; the benchmark also uses raw ', { size: 19 }), mono('Thread'), t('.', { size: 19 })]),
  reqRow('(e) Use at most (CPU cores − 1)', [mono('Math.Max(1, Environment.ProcessorCount - 1)'), t(' → 7 threads on this 8-core machine.', { size: 19 })]),
  reqRow('(f) GUI: start/stop, progress, elapsed, found password', [t('See the screenshots in Section 5.', { size: 19 })]),
  reqRow('5. Demonstrate parallel (not sequential) execution', [t('Threads search disjoint index ranges simultaneously; the benchmark shows multi-thread checks more candidates in less wall-clock time.', { size: 19 })]),
  reqRow('6. Stop all threads immediately once found', [t('A shared ', { size: 19 }), mono('CancellationTokenSource'), t(' is cancelled on the first match; every thread checks the token each iteration.', { size: 19 })]),
  reqRow('7. Generator and validator separate & independent', [mono('BruteForceGenerator'), t(' produces candidates; ', { size: 19 }), mono('PasswordValidator'), t(' only compares hashes — neither references the other.', { size: 19 })]),
  reqRow('8. Log single- vs multi-thread performance', [mono('PerformanceLogger'), t(' + the “⚡ Benchmark” button; results in Section 4.', { size: 19 })]),
]));
children.push(h3('Key design points'));
children.push(bullet([t('Hashing: ', { bold: true }), mono('SHA256.HashData(UTF8(SALT + candidate))'), t(', hex-encoded. The salt is a single constant shared by creation and verification.')]));
children.push(bullet([t('Independent search space: ', { bold: true }), t('the engine treats the alphabet (a–z, 26 chars) as a number base. Candidate i of a given length is the base-26 representation of i ('), mono('IndexToCombination'), t('). Each thread owns a contiguous slice '), mono('[start, end)'), t(' and generates candidates on the fly — true parallelism with no shared list.')]));
children.push(bullet([t('Stop-on-found: ', { bold: true }), t('the matching thread cancels the shared token; all other threads observe it and return within one iteration.')]));

// 4. Performance
children.push(new Paragraph({ children: [new PageBreak()] }));
children.push(h1('4. Performance: Single-thread vs Multi-thread'));
children.push(p(t('The benchmark runs the same brute force twice against the same hash — first on one thread, then on (CPU cores − 1) threads — and records the wall-clock time and the number of candidates checked. Example run (8 logical cores → 7 worker threads; recovered password “itifg”, length 5):')));
children.push(makeTable([2400, 1300, 1600, 2300, 1426], [
  ['Mode', 'Threads', 'Time elapsed', 'Candidates checked', 'Result'],
  [{ runs: [t('Single-thread', { size: 19 })] }, { runs: [t('1', { size: 19 })] }, { runs: [t('4.446 s', { size: 19 })] }, { runs: [t('4,470,551', { size: 19 })] }, { runs: [mono('itifg')] }],
  [{ runs: [t('Multi-thread', { size: 19 })] }, { runs: [t('7', { size: 19 })] }, { runs: [t('1.783 s', { size: 19, bold: true })] }, { runs: [t('4,714,812', { size: 19 })] }, { runs: [mono('itifg')] }],
]));
children.push(p([t('Speedup (single ÷ multi):  ', { bold: true }), t('2.49×', { bold: true, color: ACCENT, size: 24 })]));
children.push(p(t('The multi-threaded run checked more candidates (4.71M vs 4.47M) yet finished in well under half the time. That is the signature of genuine parallel execution: several threads scan different parts of the keyspace at once, so more total work is done per second. The speedup is below the theoretical 7× because the workload is small (seconds), so thread start-up and the early short lengths add fixed overhead; the gap widens for larger search spaces.')));

// 5. Screenshots
children.push(new Paragraph({ children: [new PageBreak()] }));
children.push(h1('5. Screenshots of the Running Program'));
children.push(...image('screenshots/02_password_generated.png', 430,
  '(1) After “Generate Password”: a random plaintext and its SHA-256 hash are shown; the app reports 7 threads will be used.'));
children.push(...image('screenshots/03_password_found.png', 430,
  '(2) After “Start Attack”: the password “itifg” is recovered in 3.495 s after checking 4,580,000 candidates; progress reaches 100%.'));
children.push(new Paragraph({ children: [new PageBreak()] }));
children.push(...image('screenshots/04_benchmark.png', 430,
  '(3) After “⚡ Benchmark”: the log shows the single-thread vs multi-thread comparison and the 2.49× speedup.'));

// 6. Challenges
children.push(h1('6. Challenges Faced'));
children.push(bullet([t('Crash the instant the password was found. ', { bold: true }), t('The engine waited with '), mono('Task.WaitAll(tasks, token)'), t('. When a thread found the password it cancelled that same token, so '), mono('WaitAll'), t(' threw '), mono('OperationCanceledException'), t(', which propagated onto the UI thread and closed the window. Fix: wait with a plain '), mono('Task.WaitAll(tasks)'), t(' and guard the awaited call.')]));
children.push(bullet([t('Memory blow-up from materialising candidates. ', { bold: true }), t('The first version built a list/queue of every candidate of a length — millions of strings for length 5+. Fix: index-range partitioning with '), mono('IndexToCombination'), t(', so each thread generates its slice on the fly with no large allocation.')]));
children.push(bullet([t('Meaningless progress bar. ', { bold: true }), t('Measuring progress against the full length-1→6 keyspace (~321M) left a short password showing ~0%. Fix: the denominator grows to include each length as it is reached, and is pinned to 100% on success.')]));
children.push(bullet([t('Demonstrability vs. key space. ', { bold: true }), t('With the full 62-char alphabet, a length-5 password is ~916M combinations — too slow to demo live. The brief fixes the length but not the alphabet, so a 26-char lowercase set keeps a length 4–5 crack to a few seconds.')]));
children.push(bullet([t('Capturing real screenshots. ', { bold: true }), t('The states above were produced by driving the live WPF app through Windows UI Automation, not staged mock-ups.')]));

// 7. How to run
children.push(h1('7. How to Build & Run'));
const codeLine = (s) => new Paragraph({ shading: { fill: 'F3F5FB', type: ShadingType.CLEAR, color: 'auto' },
  spacing: { after: 0 }, children: [mono(s, { size: 18 })] });
children.push(codeLine('cd BruteForceApp'));
children.push(codeLine('dotnet run        # or open BruteForceApp.csproj in Visual Studio and press F5'));
children.push(codeLine(' '));
children.push(codeLine('# Compiled debug build:'));
children.push(codeLine('bin\\Debug\\net8.0-windows\\BruteForceApp.exe'));
children.push(p([t('Repository version history: ', { size: 19, color: '717C96' }),
  mono('v1.0 implementation', { size: 18 }), t('  ', { size: 19 }),
  mono('v1.1 UML diagram', { size: 18 }), t('  ', { size: 19 }),
  mono('v1.2 fixes + benchmark/screenshots', { size: 18 }), t('  ', { size: 19 }),
  mono('v1.3 report', { size: 18 }), t('.', { size: 19, color: '717C96' })],
  { spacing: { before: 160 } }));

// ---------- document ----------
const doc = new Document({
  creator: 'BruteForceApp',
  styles: {
    default: { document: { run: { font: F, size: 22 } } },
    paragraphStyles: [
      { id: 'Heading1', name: 'Heading 1', basedOn: 'Normal', next: 'Normal', quickFormat: true,
        run: { size: 32, bold: true, font: F, color: NAVY },
        paragraph: { spacing: { before: 280, after: 140 }, outlineLevel: 0 } },
      { id: 'Heading2', name: 'Heading 2', basedOn: 'Normal', next: 'Normal', quickFormat: true,
        run: { size: 28, bold: true, font: F, color: NAVY },
        paragraph: { spacing: { before: 220, after: 120 }, outlineLevel: 1 } },
      { id: 'Heading3', name: 'Heading 3', basedOn: 'Normal', next: 'Normal', quickFormat: true,
        run: { size: 24, bold: true, font: F, color: '2A3858' },
        paragraph: { spacing: { before: 160, after: 80 }, outlineLevel: 2 } },
    ],
  },
  numbering: {
    config: [{ reference: 'bullets', levels: [
      { level: 0, format: LevelFormat.BULLET, text: '•', alignment: AlignmentType.LEFT,
        style: { paragraph: { indent: { left: 540, hanging: 280 } } } }] }],
  },
  sections: [{
    properties: { page: {
      size: { width: 11906, height: 16838 },
      margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 },
    } },
    children,
  }],
});

Packer.toBuffer(doc).then((buf) => {
  fs.writeFileSync('TestReport.docx', buf);
  console.log('done: TestReport.docx (' + buf.length + ' bytes)');
});
