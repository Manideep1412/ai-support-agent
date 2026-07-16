import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title:       'AI Support Agent',
  description: 'RAG-powered customer support assistant built with .NET 9 + Next.js + MongoDB Atlas',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
