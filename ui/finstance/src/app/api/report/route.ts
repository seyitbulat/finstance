import { forward } from "@/lib/api";
export async function GET(request: Request) {
  const month = new URL(request.url).searchParams.get("month") ?? "";
  if (!/^\d{4}-(0[1-9]|1[0-2])$/.test(month))
    return Response.json({ error: "Geçerli bir ay seçin." }, { status: 400 });
  return forward(request, `/getMonthlyReport?date=${month}-01`);
}
