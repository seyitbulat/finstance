export type Expense = {
  id: number;
  date: string;
  amount: number;
  locationId: number;
  bankStatementId: number;
  locationName: string;
  category: string;
  cutOffDate: string;
};
export type Report = { requestDate: string; details: Expense[] };
export const categories: Record<
  string,
  { label: string; color: string; soft: string }
> = {
  Market: { label: "Market", color: "#47795b", soft: "#edf4eb" },
  Restoran: { label: "Yeme & içme", color: "#b47c43", soft: "#faf0e3" },
  Ulasim: { label: "Ulaşım", color: "#66899a", soft: "#eaf1f5" },
  Giyim: { label: "Giyim", color: "#9184aa", soft: "#f1edf7" },
  Eglence: { label: "Eğlence", color: "#bd8190", soft: "#f9edf1" },
  Saglik: { label: "Sağlık", color: "#64988d", soft: "#e9f5f1" },
  Fatura: { label: "Faturalar", color: "#b69d51", soft: "#f7f3e4" },
  Diger: { label: "Diğer", color: "#7c8580", soft: "#eff1ee" },
};
export const categoryInfo = (key: string) =>
  categories[key] ?? categories.Diger;
export const money = (amount: number) =>
  new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: "TRY",
    maximumFractionDigits: 2,
  }).format(amount);
export function monthLabel(month: string) {
  return new Date(`${month}-01T12:00:00`).toLocaleDateString("tr-TR", {
    month: "long",
    year: "numeric",
  });
}
export function shiftMonth(month: string, offset: number) {
  const [year, number] = month.split("-").map(Number);
  const date = new Date(Date.UTC(year, number - 1 + offset, 1));
  return date.toISOString().slice(0, 7);
}
export function summarize(expenses: Expense[]) {
  // Negative transactions are refunds/payments, not spending categories.
  const spending = expenses.filter((expense) => expense.amount > 0);
  const total = spending.reduce((sum, expense) => sum + expense.amount, 0);
  const refunds = expenses
    .filter((expense) => expense.amount < 0)
    .reduce((sum, expense) => sum - expense.amount, 0);
  const groups = Object.entries(categories)
    .map(([key, category]) => ({
      key,
      ...category,
      amount: spending
        .filter(
          (expense) =>
            (categories[expense.category] ? expense.category : "Diger") === key,
        )
        .reduce((sum, expense) => sum + expense.amount, 0),
    }))
    .filter((group) => group.amount > 0)
    .sort((a, b) => b.amount - a.amount);
  return {
    total,
    refunds,
    count: spending.length,
    average: spending.length ? total / spending.length : 0,
    groups,
    statements: new Set(expenses.map((expense) => expense.bankStatementId))
      .size,
  };
}
export function filterExpenses(
  expenses: Expense[],
  query: string,
  category: string,
) {
  const term = query.trim().toLocaleLowerCase("tr-TR");
  return expenses.filter(
    (expense) =>
      (!category || expense.category === category) &&
      expense.locationName.toLocaleLowerCase("tr-TR").includes(term),
  );
}
export function toCsv(expenses: Expense[]) {
  const escape = (value: unknown) => {
    let text = String(value);
    if (/^[=+\-@\t\r]/.test(text)) text = `'${text}`;
    return `"${text.replaceAll('"', '""')}"`;
  };
  return (
    "\uFEFF" +
    [
      ["Tarih", "İş yeri", "Kategori", "Tutar (TRY)", "Ekstre kesim tarihi"],
      ...expenses.map((expense) => [
        expense.date,
        expense.locationName,
        categoryInfo(expense.category).label,
        expense.amount.toFixed(2),
        expense.cutOffDate,
      ]),
    ]
      .map((row) => row.map(escape).join(";"))
      .join("\r\n")
  );
}
export function demoReport(month: string): Report {
  const rows: [string, string, number, number][] = [
    ["MİGROS", "Market", 1486.75, 3],
    ["KRONOTROP", "Restoran", 245, 4],
    ["İSTANBULKART", "Ulasim", 500, 5],
    ["ZARA", "Giyim", 2190, 7],
    ["CARREFOURSA", "Market", 876.5, 9],
    ["NETFLIX", "Eglence", 299.99, 10],
    ["TURKCELL", "Fatura", 480, 11],
    ["MİGROS", "Market", 1234.8, 13],
    ["ESPRESSOLAB", "Restoran", 320, 14],
    ["İDO", "Ulasim", 680, 15],
    ["ECZANE", "Saglik", 425, 16],
    ["YEMEKSEPETİ", "Restoran", 560, 17],
    ["MACRO CENTER", "Market", 1645.25, 19],
    ["D&R", "Diger", 389.9, 20],
    ["MUBI", "Eglence", 169, 21],
    ["MİGROS", "Market", 1120.45, 23],
    ["İSKİ", "Fatura", 385.6, 24],
    ["BIG CHEFS", "Restoran", 1280, 25],
    ["SHELL", "Ulasim", 1750, 26],
    ["UNIQLO", "Giyim", 1590, 27],
    ["CARREFOURSA", "Market", 945.5, 28],
  ];
  return {
    requestDate: `${month}-01`,
    details: rows.map(([locationName, category, amount, day], index) => ({
      id: index + 1,
      locationId: index + 1,
      bankStatementId: (index % 2) + 1,
      date: `${month}-${String(day).padStart(2, "0")}`,
      amount,
      locationName,
      category,
      cutOffDate: `${month}-28`,
    })),
  };
}
