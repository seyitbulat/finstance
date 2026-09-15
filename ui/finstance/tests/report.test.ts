import { test } from "node:test";
import assert from "node:assert/strict";
import {
  summarize,
  filterExpenses,
  shiftMonth,
  toCsv,
  demoReport,
  type Expense,
} from "../src/lib/report.ts";
const expense: Expense = {
  id: 1,
  date: "2026-08-29",
  amount: 150,
  locationId: 1,
  bankStatementId: 1,
  locationName: "İSTANBUL MARKET",
  category: "Market",
  cutOffDate: "2026-09-10",
};
test("spending excludes payments and refunds and preserves statement-period transactions", () => {
  const result = summarize([
    expense,
    { ...expense, id: 2, amount: -50 },
    { ...expense, id: 3, amount: 250, category: "Unknown", bankStatementId: 2 },
  ]);
  assert.equal(result.total, 400);
  assert.equal(result.refunds, 50);
  assert.equal(result.average, 200);
  assert.equal(result.statements, 2);
  assert.equal(
    result.groups.find((group) => group.key === "Diger")?.amount,
    250,
  );
});
test("empty report does not produce NaN", () => {
  assert.equal(summarize([]).average, 0);
  assert.equal(summarize([]).total, 0);
});
test("month navigation crosses calendar years", () => {
  assert.equal(shiftMonth("2026-01", -1), "2025-12");
  assert.equal(shiftMonth("2026-12", 1), "2027-01");
});
test("search respects Turkish casing and combines category filters", () => {
  assert.equal(filterExpenses([expense], "istanbul", "Market").length, 1);
  assert.equal(filterExpenses([expense], "istanbul", "Restoran").length, 0);
});
test("CSV quotes merchant values and neutralizes spreadsheet formulas", () => {
  const csv = toCsv([{ ...expense, locationName: '=HYPERLINK("bad")' }]);
  assert.ok(csv.startsWith("\uFEFF"));
  assert.ok(csv.includes('"\'=HYPERLINK(""bad"")"'));
  assert.ok(csv.includes("2026-08-29"));
});
test("demo data uses valid dates even in February and stays separate", () => {
  const result = demoReport("2026-02");
  assert.equal(result.details.length, 21);
  assert.ok(result.details.every((row) => row.date <= "2026-02-28"));
});
