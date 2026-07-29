import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title:       'AI Support Agent',
  description: 'RAG-powered customer support assistant built with .NET 9 + Next.js + MongoDB Atlas',
  openGraph: {
    title: 'AI Support Agent',
    description:
      'Knowledge-base-driven AI support chat with document upload, vector search, and RAG pipeline. Built with Next.js 15 and OpenAI GPT-4o.',
    url: 'https://ai-support-agent-three-ebon.vercel.app',
    siteName: 'AI Support Agent',
    images: [
      {
        url: 'https://ai-support-agent-three-ebon.vercel.app/og.png',
        width: 1200,
        height: 627,
        alt: 'AI Support Agent',
      },
    ],
    type: 'website',
  },
  twitter: {
    card: 'summary_large_image',
    title: 'AI Support Agent',
    description:
      'RAG-powered knowledge base chat agent with admin panel. Built with Next.js 15 and GPT-4o.',
    images: ['https://ai-support-agent-three-ebon.vercel.app/og.png'],
  },
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
