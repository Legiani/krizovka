import type { Metadata } from 'next';
import { AppShell } from '@/shared/components/AppShell';
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
        <AppShell>{children}</AppShell>
      </body>
    </html>
  );
}
