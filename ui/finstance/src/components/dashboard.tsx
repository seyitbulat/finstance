"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import {
  ArrowDownLeft,
  ArrowDownToLine,
  ArrowRight,
  ArrowUpRight,
  BarChart3,
  Check,
  ChevronLeft,
  ChevronRight,
  CircleHelp,
  CreditCard,
  FileText,
  LayoutDashboard,
  LoaderCircle,
  Plus,
  Search,
  ShieldCheck,
  SlidersHorizontal,
  Sparkles,
  Upload,
  Wallet,
  X,
} from "lucide-react";
import {
  categories,
  categoryInfo,
  demoReport,
  filterExpenses,
  money,
  monthLabel,
  shiftMonth,
  summarize,
  toCsv,
  type Expense,
  type Report,
} from "@/lib/report";

type View = "overview" | "transactions" | "statements";
type Loaded = {
  key: string;
  report?: Report;
  previous?: Report;
  error?: string;
};
const dateLabel = (value: string) =>
  new Date(`${value}T12:00:00`).toLocaleDateString("tr-TR", {
    day: "numeric",
    month: "short",
  });
async function readResponse(response: Response) {
  const body = await response.json().catch(() => null);
  if (!response.ok)
    throw new Error(
      typeof body === "string"
        ? body
        : body?.error || "İşlem tamamlanamadı. Lütfen tekrar deneyin.",
    );
  return body;
}

export default function Dashboard() {
  const [month, setMonth] = useState(() =>
    new Date().toISOString().slice(0, 7),
  );
  const [username, setUsername] = useState("guest");
  const [profileOpen, setProfileOpen] = useState(false);
  const [demo, setDemo] = useState(false);
  const [view, setView] = useState<View>("overview");
  const [query, setQuery] = useState("");
  const [category, setCategory] = useState("");
  const [sort, setSort] = useState("date");
  const [page, setPage] = useState(1);
  const [revision, setRevision] = useState(0);
  const [loaded, setLoaded] = useState<Loaded>({ key: "" });
  const [notice, setNotice] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [uploadError, setUploadError] = useState("");
  const [uploading, setUploading] = useState(false);
  const [dragging, setDragging] = useState(false);
  const dialog = useRef<HTMLDialogElement>(null);
  const fileInput = useRef<HTMLInputElement>(null);
  const key = `${month}:${username}:${revision}`;

  useEffect(() => {
    if (demo) return;
    const controller = new AbortController();
    const get = (selected: string) =>
      fetch(`/api/report?month=${selected}`, {
        headers: { "X-User-Name": username },
        signal: controller.signal,
      }).then(readResponse) as Promise<Report>;
    // The API lazily creates profiles, so avoid two concurrent first reads.
    get(month)
      .then(async (report) => {
        const previous = await get(shiftMonth(month, -1)).catch(
          () => undefined,
        );
        if (!controller.signal.aborted) setLoaded({ key, report, previous });
      })
      .catch((error: Error) => {
        if (!controller.signal.aborted)
          setLoaded({ key, error: error.message });
      });
    return () => controller.abort();
  }, [month, username, key, demo]);

  const loading = !demo && loaded.key !== key;
  const error = !demo && loaded.key === key ? loaded.error : undefined;
  const expenses = useMemo(
    () =>
      demo
        ? demoReport(month).details
        : loaded.key === key
          ? (loaded.report?.details ?? [])
          : [],
    [demo, month, loaded, key],
  );
  const summary = useMemo(() => summarize(expenses), [expenses]);
  const previousTotal =
    !demo && loaded.key === key && loaded.previous
      ? summarize(loaded.previous.details).total
      : 0;
  const change =
    previousTotal > 0
      ? ((summary.total - previousTotal) / previousTotal) * 100
      : null;
  const filtered = useMemo(
    () =>
      filterExpenses(expenses, query, category).sort((a, b) =>
        sort === "amount"
          ? b.amount - a.amount
          : b.date.localeCompare(a.date) || b.id - a.id,
      ),
    [expenses, query, category, sort],
  );
  const pageSize = view === "overview" ? 5 : 10;
  const maxPage = Math.max(1, Math.ceil(filtered.length / pageSize));
  const safePage = Math.min(page, maxPage);
  const rows = filtered.slice((safePage - 1) * pageSize, safePage * pageSize);
  const top = summary.groups[0];

  function selectMonth(value: string) {
    if (/^\d{4}-(0[1-9]|1[0-2])$/.test(value)) {
      setMonth(value);
      setPage(1);
      setNotice("");
    }
  }
  function openUpload() {
    setUploadError("");
    setFile(null);
    if (fileInput.current) fileInput.current.value = "";
    dialog.current?.showModal();
  }
  function selectFile(selected?: File) {
    setUploadError("");
    if (!selected) return;
    if (
      !selected.name.toLowerCase().endsWith(".pdf") ||
      !selected.size ||
      selected.size > 20 * 1024 * 1024
    ) {
      setFile(null);
      setUploadError("20 MB’dan küçük, boş olmayan bir PDF seçin.");
      return;
    }
    setFile(selected);
  }
  async function upload() {
    if (!file || uploading) return;
    setUploading(true);
    setUploadError("");
    try {
      const form = new FormData();
      form.set("file", file);
      const result = await fetch("/api/upload", {
        method: "POST",
        headers: { "X-User-Name": username },
        body: form,
      }).then(readResponse);
      setDemo(false);
      selectMonth(result.cutOffDate.slice(0, 7));
      setRevision((value) => value + 1);
      setNotice(
        `${result.bankType} ekstreniz işlendi. ${result.expenses.length} işlem eklendi.`,
      );
      dialog.current?.close();
    } catch (cause) {
      setUploadError(
        cause instanceof Error ? cause.message : "Ekstre yüklenemedi.",
      );
    } finally {
      setUploading(false);
    }
  }
  function exportCsv() {
    const url = URL.createObjectURL(
      new Blob([toCsv(filtered)], { type: "text/csv;charset=utf-8;" }),
    );
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = `finstance-${month}${demo ? "-ornek" : ""}.csv`;
    anchor.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main">
        İçeriğe geç
      </a>
      <aside className="sidebar">
        <Link className="brand" href="/" aria-label="Finstance ana sayfa">
          <span className="brand-mark">
            <BarChart3 size={23} strokeWidth={2.5} />
          </span>
          finstance<span className="brand-dot">.</span>
        </Link>
        <div className="workspace-label">KİŞİSEL FİNANS ALANIN</div>
        <nav aria-label="Ana menü">
          {(
            [
              { id: "overview", label: "Genel bakış", icon: LayoutDashboard },
              { id: "transactions", label: "Harcamalar", icon: Wallet },
              { id: "statements", label: "Ekstreler", icon: FileText },
            ] as const
          ).map((item) => (
            <button
              key={item.id}
              className={`nav-item ${view === item.id ? "active" : ""}`}
              aria-current={view === item.id ? "page" : undefined}
              onClick={() => {
                setView(item.id);
                setPage(1);
              }}
            >
              <item.icon size={19} />
              {item.label}
              {view === item.id && <span className="nav-dot" />}
            </button>
          ))}
        </nav>
        <div className="sidebar-note">
          <div className="mini-orbit">
            <Sparkles size={24} />
          </div>
          <h3>
            Biraz daha farkındalık.
            <br />
            Biraz daha kontrol.
          </h3>
          <p>Ekstreni yükle, harcamalarının bütününü gör.</p>
          <button className="text-button" onClick={openUpload}>
            İlk adımı at <ArrowRight size={16} />
          </button>
        </div>
        <div className="sidebar-bottom">
          <span className="local-badge">
            <span /> Kişisel çalışma alanı
          </span>
          <button
            className="profile"
            aria-expanded={profileOpen}
            onClick={() => setProfileOpen(!profileOpen)}
          >
            <span className="avatar">
              {username === "guest"
                ? "M"
                : username.slice(0, 2).toLocaleUpperCase("tr-TR")}
            </span>
            <span>
              <strong>{username === "guest" ? "Misafir" : username}</strong>
              <small>Yerel profil</small>
            </span>
            <SlidersHorizontal size={16} />
          </button>
        </div>
      </aside>

      <div className="main-shell">
        <header className="topbar">
          <div className="breadcrumb">
            Çalışma alanım <ChevronRight size={14} />
            <strong>
              {
                {
                  overview: "Genel bakış",
                  transactions: "Harcamalar",
                  statements: "Ekstreler",
                }[view]
              }
            </strong>
          </div>
          <div className="topbar-right">
            <span className="small-brand">finstance.</span>
            <button
              className={`demo-toggle ${demo ? "selected" : ""}`}
              onClick={() => {
                setDemo(!demo);
                setNotice("");
                setPage(1);
              }}
            >
              {demo ? <Check size={14} /> : <Sparkles size={14} />}
              {demo ? "Örnek veriler açık" : "Örnek verilerle keşfet"}
            </button>
            <button
              className="top-avatar"
              aria-label="Yerel profili düzenle"
              aria-expanded={profileOpen}
              onClick={() => setProfileOpen(!profileOpen)}
            >
              {username === "guest"
                ? "M"
                : username.slice(0, 1).toLocaleUpperCase("tr-TR")}
            </button>
          </div>
        </header>
        <main id="main">
          {profileOpen && (
            <form
              className="profile-panel"
              onSubmit={(event) => {
                event.preventDefault();
                const value = String(
                  new FormData(event.currentTarget).get("username") ?? "",
                ).trim();
                if (value) {
                  setUsername(value);
                  setProfileOpen(false);
                  setNotice("");
                }
              }}
            >
              <div>
                <h3>Yerel profil</h3>
                <p>
                  Ekstreler bu isim altında tutulur. Profil seçimi kimlik
                  doğrulama sağlamaz.
                </p>
              </div>
              <label>
                Profil adı
                <input
                  name="username"
                  defaultValue={username}
                  required
                  maxLength={64}
                  pattern="[A-Za-z0-9_.-]+"
                  title="Harf, rakam, nokta, tire veya alt çizgi kullanın."
                />
              </label>
              <button className="primary-button" type="submit">
                Uygula
              </button>
              <button
                className="icon-button"
                type="button"
                aria-label="Profili kapat"
                onClick={() => setProfileOpen(false)}
              >
                <X size={18} />
              </button>
            </form>
          )}
          <div className="page-heading">
            <div>
              <div className="eyebrow">PARANA DAHA YAKINDAN BAK</div>
              <h1>
                {
                  {
                    overview: "Harcamalarına bir bakış.",
                    transactions: "Her harcamanın bir hikâyesi var.",
                    statements: "Ekstrelerin, bir arada.",
                  }[view]
                }
              </h1>
              <p>
                {
                  {
                    overview: "Küçük detayları gör. Büyük resmi anla.",
                    transactions:
                      "İşlemlerini keşfet, ara ve kategorilerine göre incele.",
                    statements:
                      "Seçili dönemin ekstrelerini ve harcama özetlerini incele.",
                  }[view]
                }
              </p>
            </div>
            <button className="primary-button" onClick={openUpload}>
              <Plus size={18} /> Ekstre yükle
            </button>
          </div>
          {demo && (
            <div className="notice demo-notice">
              <Sparkles size={17} />
              <span>
                Örnek verileri inceliyorsun. Bu işlemler hesabına kaydedilmez.
              </span>
              <button onClick={() => setDemo(false)}>
                Gerçek verilerime dön <ArrowRight size={15} />
              </button>
            </div>
          )}
          {notice && (
            <div className="notice success" role="status">
              <Check size={17} />
              {notice}
              <button
                aria-label="Bildirimi kapat"
                onClick={() => setNotice("")}
              >
                <X size={16} />
              </button>
            </div>
          )}
          <section className="period-row" aria-label="Rapor dönemi">
            <div className="month-picker">
              <button
                className="icon-button"
                aria-label="Önceki ay"
                onClick={() => selectMonth(shiftMonth(month, -1))}
              >
                <ChevronLeft size={17} />
              </button>
              <label>
                <span className="sr-only">Ekstre ayı</span>
                <input
                  aria-label="Ekstre ayı"
                  type="month"
                  value={month}
                  onChange={(event) => selectMonth(event.target.value)}
                />
              </label>
              <button
                className="icon-button"
                aria-label="Sonraki ay"
                onClick={() => selectMonth(shiftMonth(month, 1))}
              >
                <ChevronRight size={17} />
              </button>
            </div>
            <span className="period-hint">
              <span className="status-dot" /> Ekstre kesim ayına göre{" "}
              <span
                className="hint-icon"
                title="İşlem tarihi farklı bir ayda olsa bile, harcama ekstre kesim ayının raporunda yer alır."
              >
                <CircleHelp size={14} />
              </span>
            </span>
            <button
              className="secondary-button export-button"
              onClick={exportCsv}
              disabled={!filtered.length || loading}
            >
              <ArrowDownToLine size={15} /> CSV indir
            </button>
          </section>
          {error ? (
            <div className="error-panel" role="alert">
              <CircleHelp size={28} />
              <h2>Raporu şu an getiremedik.</h2>
              <p>{error}</p>
              <button
                className="secondary-button"
                onClick={() => setRevision((value) => value + 1)}
              >
                Tekrar dene
              </button>
            </div>
          ) : (
            <>
              <section
                className="metrics"
                aria-label="Aylık özet"
                aria-busy={loading}
              >
                <div className="metric featured">
                  <div className="metric-label">
                    Toplam harcama <Wallet size={18} />
                  </div>
                  <div className="metric-value">
                    {loading ? "—" : money(summary.total)}
                  </div>
                  <div className="metric-foot">
                    {change !== null ? (
                      <>
                        <span
                          className={`change ${change <= 0 ? "down" : "up"}`}
                        >
                          {change <= 0 ? (
                            <ArrowDownLeft size={13} />
                          ) : (
                            <ArrowUpRight size={13} />
                          )}
                          {Math.abs(change).toFixed(1).replace(".", ",")}%
                        </span>{" "}
                        önceki ekstre ayına göre
                      </>
                    ) : (
                      <>
                        <span className="tiny-dot" /> {monthLabel(month)} dönemi
                      </>
                    )}
                  </div>
                </div>
                <div className="metric">
                  <div className="metric-label">
                    Harcama sayısı <CreditCard size={18} />
                  </div>
                  <div className="metric-value">
                    {loading ? "—" : summary.count}
                    <span>işlem</span>
                  </div>
                  <div className="metric-foot">
                    {summary.statements} ekstre üzerinden hesaplandı
                  </div>
                </div>
                <div className="metric">
                  <div className="metric-label">
                    İşlem başına ortalama <BarChart3 size={18} />
                  </div>
                  <div className="metric-value">
                    {loading ? "—" : money(summary.average)}
                  </div>
                  <div className="metric-foot">
                    Pozitif tutarlı harcamaların ortalaması
                  </div>
                </div>
                <div className="metric">
                  <div className="metric-label">
                    En çok harcanan <ArrowUpRight size={18} />
                  </div>
                  <div className="metric-value category-value">
                    {loading ? "—" : (top?.label ?? "Henüz veri yok")}
                  </div>
                  <div className="metric-foot">
                    {top ? (
                      <>
                        <span
                          className="category-dot"
                          style={{ background: top.color }}
                        />
                        Toplam harcamanın %
                        {Math.round((top.amount / summary.total) * 100)}’i
                      </>
                    ) : (
                      "İlk ekstrenle keşfet"
                    )}
                  </div>
                </div>
              </section>
              {loading ? (
                <div className="loading-panel" role="status">
                  <LoaderCircle className="spin" size={25} />
                  <p>Ekstrelerin hazırlanıyor…</p>
                </div>
              ) : (
                <>
                  {view === "overview" && (
                    <div className="chart-grid">
                      <section className="panel trend-panel">
                        <div className="panel-heading">
                          <div>
                            <h2>Harcama hareketleri</h2>
                            <p>İşlem tarihine göre günlük harcamaların</p>
                          </div>
                          <span className="chart-key">
                            <span />
                            Harcama
                          </span>
                        </div>
                        <SpendingChart expenses={expenses} />
                        <div className="chart-footer">
                          <span>
                            {monthLabel(month)} ekstresindeki işlemler
                          </span>
                          <strong>{summary.count} harcama</strong>
                        </div>
                      </section>
                      <section className="panel category-panel">
                        <div className="panel-heading">
                          <div>
                            <h2>Paran nereye gidiyor?</h2>
                            <p>Kategorilere göre harcama dağılımı</p>
                          </div>
                        </div>
                        <CategoryChart
                          groups={summary.groups}
                          total={summary.total}
                          onSelect={(selected) => {
                            setCategory(selected);
                            setView("transactions");
                            setPage(1);
                          }}
                        />
                      </section>
                    </div>
                  )}
                  {view === "statements" ? (
                    <section className="panel statement-panel">
                      <div className="panel-heading">
                        <div>
                          <h2>Dönem ekstreleri</h2>
                          <p>{monthLabel(month)} kesim tarihli ekstreler</p>
                        </div>
                        <FileText size={20} />
                      </div>
                      {summary.statements ? (
                        <div className="statement-grid">
                          {Array.from(
                            new Set(
                              expenses.map(
                                (expense) => expense.bankStatementId,
                              ),
                            ),
                          ).map((id) => {
                            const items = expenses.filter(
                              (expense) => expense.bankStatementId === id,
                            );
                            return (
                              <article className="statement-card" key={id}>
                                <span className="statement-icon">
                                  <FileText size={24} />
                                </span>
                                <h3>Ekstre #{id}</h3>
                                <p>
                                  Kesim tarihi:{" "}
                                  {new Date(
                                    `${items[0].cutOffDate}T12:00:00`,
                                  ).toLocaleDateString("tr-TR")}
                                </p>
                                <strong>{money(summarize(items).total)}</strong>
                                <small>{items.length} işlem</small>
                                <span className="processed">
                                  <Check size={13} /> İşlendi
                                </span>
                              </article>
                            );
                          })}
                        </div>
                      ) : (
                        <Empty onUpload={openUpload} />
                      )}
                    </section>
                  ) : (
                    <section className="panel transactions-panel">
                      <div className="panel-heading">
                        <div>
                          <h2>
                            {view === "overview"
                              ? "Son harcamalar"
                              : "Tüm harcamalar"}
                            <span className="count-badge">
                              {expenses.length}
                            </span>
                          </h2>
                          <p>Harcama detayların, tek bir yerde.</p>
                        </div>
                        {view === "overview" && (
                          <button
                            className="text-button"
                            onClick={() => {
                              setView("transactions");
                              setPage(1);
                            }}
                          >
                            Tümünü gör <ArrowRight size={16} />
                          </button>
                        )}
                      </div>
                      <div className="table-toolbar">
                        <label className="search-box">
                          <Search size={17} />
                          <input
                            aria-label="Harcama ara"
                            placeholder="İş yeri veya marka ara…"
                            value={query}
                            onChange={(event) => {
                              setQuery(event.target.value);
                              setPage(1);
                            }}
                          />
                        </label>
                        <label className="filter-select">
                          <SlidersHorizontal size={15} />
                          <span className="sr-only">Kategori</span>
                          <select
                            aria-label="Kategori"
                            value={category}
                            onChange={(event) => {
                              setCategory(event.target.value);
                              setPage(1);
                            }}
                          >
                            <option value="">Tüm kategoriler</option>
                            {Object.entries(categories).map(([id, info]) => (
                              <option key={id} value={id}>
                                {info.label}
                              </option>
                            ))}
                          </select>
                        </label>
                        <select
                          className="sort-select"
                          aria-label="Sıralama"
                          value={sort}
                          onChange={(event) => {
                            setSort(event.target.value);
                            setPage(1);
                          }}
                        >
                          <option value="date">En yeni önce</option>
                          <option value="amount">
                            Tutar: yüksekten düşüğe
                          </option>
                        </select>
                      </div>
                      {!expenses.length ? (
                        <Empty onUpload={openUpload} />
                      ) : !filtered.length ? (
                        <div className="empty-state">
                          <Search size={28} />
                          <h3>Eşleşen harcama bulunamadı.</h3>
                          <p>Başka bir iş yeri veya kategori deneyebilirsin.</p>
                          <button
                            className="text-button"
                            onClick={() => {
                              setQuery("");
                              setCategory("");
                            }}
                          >
                            Filtreleri temizle
                          </button>
                        </div>
                      ) : (
                        <>
                          <div className="table-scroll">
                            <table>
                              <thead>
                                <tr>
                                  <th scope="col">İŞ YERİ</th>
                                  <th scope="col">KATEGORİ</th>
                                  <th scope="col">İŞLEM TARİHİ</th>
                                  <th scope="col">TUTAR</th>
                                </tr>
                              </thead>
                              <tbody>
                                {rows.map((expense) => (
                                  <tr key={expense.id}>
                                    <td>
                                      <span className="merchant-cell">
                                        <span
                                          className="merchant-icon"
                                          style={{
                                            background: categoryInfo(
                                              expense.category,
                                            ).soft,
                                            color: categoryInfo(
                                              expense.category,
                                            ).color,
                                          }}
                                        >
                                          {expense.locationName.slice(0, 1)}
                                        </span>
                                        <span>
                                          <strong>
                                            {expense.locationName}
                                          </strong>
                                          <small className="desktop-detail">
                                            Ekstre #{expense.bankStatementId}
                                          </small>
                                          <small className="mobile-detail">
                                            {
                                              categoryInfo(expense.category)
                                                .label
                                            }{" "}
                                            · {dateLabel(expense.date)}
                                          </small>
                                        </span>
                                      </span>
                                    </td>
                                    <td>
                                      <span
                                        className="category-tag"
                                        style={{
                                          background: categoryInfo(
                                            expense.category,
                                          ).soft,
                                        }}
                                      >
                                        <span
                                          style={{
                                            background: categoryInfo(
                                              expense.category,
                                            ).color,
                                          }}
                                        />
                                        {categoryInfo(expense.category).label}
                                      </span>
                                    </td>
                                    <td className="date-cell">
                                      {dateLabel(expense.date)}
                                    </td>
                                    <td
                                      className={`amount-cell ${expense.amount < 0 ? "refund" : ""}`}
                                    >
                                      {money(expense.amount)}
                                      {expense.amount < 0 && (
                                        <small>İade / ödeme</small>
                                      )}
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          </div>
                          <div className="table-footer">
                            <span>
                              {filtered.length} işlemden{" "}
                              {(safePage - 1) * pageSize + 1}–
                              {Math.min(safePage * pageSize, filtered.length)}{" "}
                              arası
                            </span>
                            <div className="pagination">
                              <button
                                className="icon-button"
                                aria-label="Önceki sayfa"
                                disabled={safePage === 1}
                                onClick={() => setPage(safePage - 1)}
                              >
                                <ChevronLeft size={16} />
                              </button>
                              <span>
                                {safePage} / {maxPage}
                              </span>
                              <button
                                className="icon-button"
                                aria-label="Sonraki sayfa"
                                disabled={safePage === maxPage}
                                onClick={() => setPage(safePage + 1)}
                              >
                                <ChevronRight size={16} />
                              </button>
                            </div>
                          </div>
                        </>
                      )}
                    </section>
                  )}
                  {summary.refunds > 0 && (
                    <p className="refund-note">
                      Bu dönemde {money(summary.refunds)} iade / ödeme var.
                      Harcama özetlerine dahil edilmedi.
                    </p>
                  )}
                </>
              )}
            </>
          )}
          <footer className="page-footer">
            <span>
              <ShieldCheck size={15} /> Daha bilinçli harcamalar, daha net
              yarınlar.
            </span>
            <span>
              finstance <span className="footer-dot">·</span> Küçük detaylar,
              büyük resim.
            </span>
          </footer>
        </main>
      </div>
      <dialog
        ref={dialog}
        aria-labelledby="upload-title"
        className="upload-dialog"
        onCancel={(event) => {
          if (uploading) event.preventDefault();
        }}
      >
        <div className="dialog-heading">
          <span className="upload-icon">
            <Upload size={25} />
          </span>
          <button
            className="icon-button"
            aria-label="Yükleme penceresini kapat"
            disabled={uploading}
            onClick={() => dialog.current?.close()}
          >
            <X size={20} />
          </button>
        </div>
        <h2 id="upload-title">Ekstrenden başlayalım.</h2>
        <p>
          Kredi kartı ekstreni yükle. Harcamalarını senin için bir araya
          getirelim.
        </p>
        <div
          className={`dropzone ${dragging ? "dragging" : ""}`}
          onDragOver={(event) => {
            event.preventDefault();
            if (!uploading) setDragging(true);
          }}
          onDragLeave={() => setDragging(false)}
          onDrop={(event) => {
            event.preventDefault();
            setDragging(false);
            if (!uploading) selectFile(event.dataTransfer.files[0]);
          }}
        >
          <FileText size={32} />
          <strong>{file ? file.name : "PDF dosyanı buraya bırak"}</strong>
          <span>
            {file
              ? `${(file.size / 1024 / 1024).toFixed(2)} MB`
              : "veya bilgisayarından seç · En fazla 20 MB"}
          </span>
          <button
            className="secondary-button"
            disabled={uploading}
            onClick={() => fileInput.current?.click()}
          >
            {file ? "Dosyayı değiştir" : "Dosya seç"}
          </button>
          <input
            className="sr-only"
            ref={fileInput}
            type="file"
            accept=".pdf,application/pdf"
            aria-label="PDF ekstre dosyası"
            disabled={uploading}
            onChange={(event) => selectFile(event.target.files?.[0])}
          />
        </div>
        <div className="supported-banks">
          <span>DESTEKLENEN BANKALAR</span>
          <strong>Yapı Kredi</strong>
          <strong>QNB</strong>
        </div>
        {demo && (
          <p className="upload-info">
            Yüklediğin PDF, {username} profiline gerçek veri olarak kaydedilir;
            örnek modundan çıkılır.
          </p>
        )}
        {uploadError && (
          <p className="upload-error" role="alert">
            {uploadError}
          </p>
        )}
        <button
          className="primary-button upload-submit"
          disabled={!file || uploading}
          onClick={upload}
        >
          {uploading ? (
            <>
              <LoaderCircle className="spin" size={17} /> Ekstre işleniyor…
            </>
          ) : (
            <>
              <Upload size={17} /> Ekstreyi yükle
            </>
          )}
        </button>
        <small className="dialog-note">
          İşlemler ekstre kesim ayının raporuna eklenir.
        </small>
      </dialog>
    </div>
  );
}

function Empty({ onUpload }: { onUpload: () => void }) {
  return (
    <div className="empty-state">
      <span className="empty-art">
        <FileText size={30} />
      </span>
      <h3>Bu ayın hikâyesi henüz başlamadı.</h3>
      <p>İlk ekstreni yükle; harcamaların ve kategorilerin burada görünsün.</p>
      <button className="primary-button" onClick={onUpload}>
        <Plus size={17} /> Ekstre yükle
      </button>
    </div>
  );
}

function SpendingChart({ expenses }: { expenses: Expense[] }) {
  const dates = [...new Set(expenses.map((expense) => expense.date))].sort();
  const points = dates.map((date) => ({
    date,
    amount: expenses
      .filter((expense) => expense.date === date && expense.amount > 0)
      .reduce((sum, expense) => sum + expense.amount, 0),
  }));
  const max = Math.max(...points.map((point) => point.amount), 1000);
  const ceiling = Math.ceil(max / 1000) * 1000;
  if (!points.length)
    return (
      <div className="empty-chart">
        <BarChart3 size={32} />
        <p>İlk ekstrenle harcama hareketlerin burada görünecek.</p>
      </div>
    );
  return (
    <div className="spending-chart">
      <div className="chart-y-axis">
        {[1, 0.75, 0.5, 0.25, 0].map((ratio) => (
          <span key={ratio}>
            {new Intl.NumberFormat("tr-TR", { notation: "compact" }).format(
              ceiling * ratio,
            )}{" "}
            ₺
          </span>
        ))}
      </div>
      <div className="plot">
        <div className="grid-lines">
          {[0, 1, 2, 3, 4].map((line) => (
            <span key={line} />
          ))}
        </div>
        <div className="bars">
          {points.map((point, index) => (
            <div className="bar-slot" key={point.date}>
              <div
                tabIndex={0}
                aria-label={`${dateLabel(point.date)}: ${money(point.amount)}`}
                className={`bar ${index === points.length - 1 ? "last" : ""}`}
                style={{
                  height: `${Math.max((point.amount / ceiling) * 100, 1)}%`,
                }}
              >
                <span className="bar-tooltip">
                  {dateLabel(point.date)}
                  <strong>{money(point.amount)}</strong>
                </span>
              </div>
            </div>
          ))}
        </div>
        <div className="chart-x-axis">
          <span>{dateLabel(dates[0])}</span>
          <span>{dateLabel(dates[Math.floor(dates.length / 2)])}</span>
          <span>{dateLabel(dates[dates.length - 1])}</span>
        </div>
      </div>
    </div>
  );
}

function CategoryChart({
  groups,
  total,
  onSelect,
}: {
  groups: ReturnType<typeof summarize>["groups"];
  total: number;
  onSelect: (category: string) => void;
}) {
  const stops = groups.map((group, index) => {
    const start =
      (groups.slice(0, index).reduce((sum, item) => sum + item.amount, 0) /
        total) *
      100;
    const end = start + (group.amount / total) * 100;
    return `${group.color} ${start}% ${end}%`;
  });
  return (
    <div className="category-content">
      <div
        className="donut"
        role="img"
        aria-label={
          total
            ? `Kategori dağılımı: ${groups.map((group) => `${group.label} yüzde ${Math.round((group.amount / total) * 100)}`).join(", ")}`
            : "Henüz kategori verisi yok"
        }
        style={{
          background: stops.length
            ? `conic-gradient(${stops.join(", ")})`
            : "#edf0eb",
        }}
      >
        <div className="donut-hole">
          <span>{groups.length}</span>
          <small>kategori</small>
        </div>
      </div>
      <div className="category-legend">
        {groups.length ? (
          groups.slice(0, 4).map((group) => (
            <button key={group.key} onClick={() => onSelect(group.key)}>
              <span
                className="category-dot"
                style={{ background: group.color }}
              />
              <span>{group.label}</span>
              <strong>%{Math.round((group.amount / total) * 100)}</strong>
            </button>
          ))
        ) : (
          <p>Harcamaların otomatik olarak kategorilere ayrılır.</p>
        )}
        {groups.length > 4 && (
          <span className="other-categories">
            +{groups.length - 4} kategori daha
          </span>
        )}
      </div>
    </div>
  );
}
