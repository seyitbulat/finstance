import type { Metadata } from "next";
import "./globals.css";
export const metadata: Metadata = {
  title: "Finstance · Harcamalarına bir bakış",
  description: "Kredi kartı ekstrelerini anlamlı bir harcama özetine dönüştür.",
};
export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="tr">
      <body>{children}</body>
    </html>
  );
}
