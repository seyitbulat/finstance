import { forward } from "@/lib/api";
export async function POST(request: Request) {
  if (Number(request.headers.get("Content-Length")) > 21 * 1024 * 1024)
    return Response.json(
      { error: "Dosya boyutu 20 MB’ı aşamaz." },
      { status: 413 },
    );
  let form: FormData;
  try {
    form = await request.formData();
  } catch {
    return Response.json({ error: "Dosya okunamadı." }, { status: 400 });
  }
  const file = form.get("file");
  if (
    !(file instanceof File) ||
    !file.size ||
    file.size > 20 * 1024 * 1024 ||
    !file.name.toLowerCase().endsWith(".pdf")
  ) {
    return Response.json(
      { error: "20 MB’dan küçük, boş olmayan bir PDF seçin." },
      { status: 400 },
    );
  }
  const body = new FormData();
  body.set("file", file);
  return forward(request, "/upload", { method: "POST", body });
}
