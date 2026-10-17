import type { Metadata } from 'next';
import Link from 'next/link';
import 'bootstrap/dist/css/bootstrap.min.css';
import './globals.css';

export const metadata: Metadata = {
  title: 'Krizovka · velín',
  description: 'Krizová hlášení a metodiky organizace',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="cs">
      <body>
        <a className="visually-hidden-focusable" href="#content">Přejít na obsah</a>
        <header className="app-header">
          <Link className="brand" href="/">Krizovka <span>velín</span></Link>
          <nav aria-label="Hlavní navigace">
            <Link href="/">Hlášení</Link>
            <Link href="/metodiky">Metodiky</Link>
          </nav>
        </header>
        <div className="development-notice" role="status">
          Vývojový základ. Příjem hlášení a přihlášení ještě nejsou aktivní.
        </div>
        <main id="content">{children}</main>
      </body>
    </html>
  );
}
