'use client';

import { Copy, Check } from 'lucide-react';
import { useState } from 'react';
import SourceCard from './SourceCard';
import type { Message } from '@/types';

// ── Inline content renderer (handles ```blocks``` and `inline`) ───────────────

function CodeBlock({ lang, code }: { lang: string; code: string }) {
  const [copied, setCopied] = useState(false);

  function handleCopy() {
    navigator.clipboard.writeText(code);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <div className="my-2 rounded-lg overflow-hidden bg-gray-900 text-gray-100 text-xs">
      <div className="flex items-center justify-between px-3 py-1.5 bg-gray-800 border-b border-gray-700">
        <span className="text-gray-400 font-mono text-[10px]">{lang || 'code'}</span>
        <button
          onClick={handleCopy}
          className="flex items-center gap-1 text-gray-400 hover:text-white transition-colors"
        >
          {copied ? <Check size={11} /> : <Copy size={11} />}
          <span className="text-[10px]">{copied ? 'Copied' : 'Copy'}</span>
        </button>
      </div>
      <pre className="p-3 overflow-x-auto leading-relaxed">
        <code>{code.trimEnd()}</code>
      </pre>
    </div>
  );
}

function renderContent(text: string) {
  // Split on fenced code blocks: ```lang\ncode```
  const parts = text.split(/(```[\w]*\n[\s\S]*?```)/g);

  return parts.map((part, i) => {
    const blockMatch = part.match(/^```([\w]*)\n([\s\S]*?)```$/);
    if (blockMatch) {
      return <CodeBlock key={i} lang={blockMatch[1]} code={blockMatch[2]} />;
    }

    // Handle inline code within normal text
    const inlineParts = part.split(/(`[^`\n]+`)/g);
    return (
      <span key={i} className="whitespace-pre-wrap">
        {inlineParts.map((ip, j) =>
          ip.startsWith('`') && ip.endsWith('`') ? (
            <code
              key={j}
              className="px-1 py-0.5 bg-gray-100 text-gray-800 rounded text-[11px] font-mono"
            >
              {ip.slice(1, -1)}
            </code>
          ) : (
            <span key={j}>{ip}</span>
          )
        )}
      </span>
    );
  });
}

// ── MessageBubble ─────────────────────────────────────────────────────────────

export default function MessageBubble({ message }: { message: Message }) {
  const isUser = message.role === 'user';

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div className={`max-w-[80%] ${isUser ? 'order-2' : 'order-1'}`}>

        {/* Avatar */}
        {!isUser && (
          <div className="flex items-center gap-2 mb-1">
            <div className="w-6 h-6 rounded-full bg-indigo-600 flex items-center justify-center">
              <span className="text-white text-xs font-bold">AI</span>
            </div>
            <span className="text-xs text-gray-500 font-medium">Support Agent</span>
          </div>
        )}

        {/* Bubble */}
        <div
          className={`px-4 py-3 rounded-2xl text-sm leading-relaxed break-words ${
            isUser
              ? 'bg-indigo-600 text-white rounded-tr-sm whitespace-pre-wrap'
              : 'bg-white border border-gray-200 text-gray-800 rounded-tl-sm shadow-sm'
          } ${message.isStreaming ? 'streaming-cursor' : ''}`}
        >
          {isUser
            ? (message.content || (message.isStreaming ? '' : '…'))
            : (message.content
                ? renderContent(message.content)
                : (message.isStreaming ? '' : '…')
              )
          }
        </div>

        {/* Source citations */}
        {!isUser && message.sources.length > 0 && !message.isStreaming && (
          <div className="mt-2 flex flex-wrap gap-1.5">
            <span className="text-xs text-gray-400 self-center">Sources:</span>
            {message.sources.map(s => (
              <SourceCard key={s.id} source={s} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
