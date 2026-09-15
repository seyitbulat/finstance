// Run only against the local development stack. Creates an isolated test profile.
import assert from "node:assert/strict";
const base = process.env.TEST_UI_URL ?? "http://localhost:3000";
if (!["localhost", "127.0.0.1"].includes(new URL(base).hostname))
  throw new Error("Integration checks require a local development URL.");
const username = `ui-test-${Date.now()}`;
const headers = { "X-User-Name": username };

function syntheticStatement() {
  const stream =
    "BT /F1 12 Tf 50 750 Td (QNB) Tj 0 -30 Td (Kesim Tarihi : 28/09/2026) Tj 0 -30 Td (29/08/2026 MIGROS 150.00) Tj 0 -30 Td (15/09/2026 SHELL 250.00) Tj ET";
  const objects = [
    "<< /Type /Catalog /Pages 2 0 R >>",
    "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
    "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
    `<< /Length ${stream.length} >>\nstream\n${stream}\nendstream`,
  ];
  let pdf = "%PDF-1.4\n";
  const offsets = [0];
  objects.forEach((object, index) => {
    offsets.push(pdf.length);
    pdf += `${index + 1} 0 obj\n${object}\nendobj\n`;
  });
  const xref = pdf.length;
  pdf += `xref\n0 6\n0000000000 65535 f \n${offsets
    .slice(1)
    .map((offset) => `${String(offset).padStart(10, "0")} 00000 n \n`)
    .join("")}trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF`;
  return new File([pdf], "synthetic-qnb.pdf", { type: "application/pdf" });
}

assert.equal(
  (await fetch(`${base}/api/report?month=2026-13`, { headers })).status,
  400,
);
const invalid = new FormData();
invalid.set("file", new File(["bad"], "bad.txt"));
assert.equal(
  (
    await fetch(`${base}/api/upload`, {
      method: "POST",
      headers,
      body: invalid,
    })
  ).status,
  400,
);
const body = new FormData();
body.set("file", syntheticStatement());
const upload = await fetch(`${base}/api/upload`, {
  method: "POST",
  headers,
  body,
});
const uploadData = await upload.json();
assert.equal(upload.status, 200, JSON.stringify(uploadData));
assert.equal(uploadData.cutOffDate, "2026-09-28");
assert.equal(uploadData.expenses.length, 2);
const report = await fetch(`${base}/api/report?month=2026-09`, {
  headers,
}).then((response) => response.json());
assert.equal(report.details.length, 2);
assert.equal(
  report.details.reduce((total, row) => total + row.amount, 0),
  400,
);
assert.ok(report.details.some((row) => row.date === "2026-08-29"));
const august = await fetch(`${base}/api/report?month=2026-08`, {
  headers,
}).then((response) => response.json());
assert.equal(august.details.length, 0);
assert.equal(
  (await fetch(`${base}/api/upload`, { method: "POST", headers, body })).status,
  409,
);
const other = await fetch(`${base}/api/report?month=2026-09`, {
  headers: { "X-User-Name": `${username}-other` },
}).then((response) => response.json());
assert.equal(other.details.length, 0);
console.log(
  "PASS: invalid month/file, PDF parsing, persistence, statement-month grouping, duplicate conflict, profile isolation.",
);
console.log(
  `Synthetic records are confined to the development profile: ${username}`,
);
