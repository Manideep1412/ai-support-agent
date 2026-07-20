'use client';

import { useState, useRef, useEffect, type MouseEvent } from 'react';
import { Send, Plus, BookOpen, History, X, Trash2 } from 'lucide-react';
import Link from 'next/link';
import ChatWindow from '@/components/chat/ChatWindow';
import { streamChat, getSessions, getSession, deleteSession } from '@/lib/api';
import type { Message, SessionSummary } from '@/types';

export default function ChatPage() {
  const [messages,       setMessages]       = useState<Message[]>([]);
  const [sessionId,      setSessionId]      = useState<string | undefined>();
  const [input,          setInput]          = useState('');
  const [loading,        setLoading]        = useState(false);

  // History panel
  const [showHistory,    setShowHistory]    = useState(false);
  const [sessions,       setSessions]       = useState<SessionSummary[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);
  const historyRef = useRef<HTMLDivElement>(null);

  // Close history panel on outside click
  useEffect(() => {
    function onClickOutside(e: globalThis.MouseEvent) {
      if (historyRef.current && !historyRef.current.contains(e.target as Node))
        setShowHistory(false);
    }
    if (showHistory) document.addEventListener('mousedown', onClickOutside);
    return () => document.removeEventListener('mousedown', onClickOutside);
  }, [showHistory]);

  async function openHistory() {
    setShowHistory(true);
    setHistoryLoading(true);
    try {
      setSessions(await getSessions());
    } catch {
      setSessions([]);
    } finally {
      setHistoryLoading(false);
    }
  }

  async function handleDeleteSession(e: MouseEvent, sid: string) {
    e.stopPropagation();
    try {
      await deleteSession(sid);
      setSessions(prev => prev.filter(s => s.sessionId !== sid));
      if (sid === sessionId) handleNewChat();
    } catch { /* ignore */ }
  }

  async function loadSession(sid: string) {
    setShowHistory(false);
    try {
      const dto = await getSession(sid);
      const msgs: Message[] = dto.messages.map(m => ({
        id:        crypto.randomUUID(),
        role:      m.role,
        content:   m.content,
        sources:   m.sources,
        createdAt: new Date(m.createdAt),
      }));
      setMessages(msgs);
      setSessionId(sid);
    } catch {
      /* ignore */
    }
  }

  async function handleSend(question: string) {
    if (!question.trim() || loading) return;
    setInput('');
    setLoading(true);

    const userMsg: Message = {
      id: crypto.randomUUID(), role: 'user', content: question,
      sources: [], createdAt: new Date(),
    };
    const aiId = crypto.randomUUID();
    const aiMsg: Message = {
      id: aiId, role: 'assistant', content: '',
      sources: [], createdAt: new Date(), isStreaming: true,
    };

    setMessages(prev => [...prev, userMsg, aiMsg]);

    try {
      for await (const event of streamChat(question, sessionId)) {
        if (event.event === 'sources') {
          setSessionId(event.data.sessionId);
          setMessages(prev => prev.map(m =>
            m.id === aiId ? { ...m, sources: event.data.sources } : m
          ));
        } else if (event.event === 'chunk') {
          setMessages(prev => prev.map(m =>
            m.id === aiId ? { ...m, content: m.content + event.data.text } : m
          ));
        } else if (event.event === 'done') {
          setMessages(prev => prev.map(m =>
            m.id === aiId ? { ...m, isStreaming: false } : m
          ));
        }
      }
    } catch {
      setMessages(prev => prev.map(m =>
        m.id === aiId
          ? { ...m, content: 'Sorry, something went wrong. Please try again.', isStreaming: false }
          : m
      ));
    } finally {
      setLoading(false);
    }
  }

  function handleNewChat() {
    setMessages([]);
    setSessionId(undefined);
  }

  function formatTime(iso: string) {
    const d = new Date(iso);
    const now = new Date();
    const diffH = (now.getTime() - d.getTime()) / 3_600_000;
    if (diffH < 24) return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    return d.toLocaleDateString([], { month: 'short', day: 'numeric' });
  }

  return (
    <div className="flex flex-col h-screen bg-slate-50">
      {/* ── Header ── */}
      <header className="bg-white border-b border-gray-200 px-4 sm:px-6 py-3 flex items-center justify-between shrink-0 relative z-20">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 bg-indigo-600 rounded-xl flex items-center justify-center shadow-sm">
            <span className="text-white text-sm font-bold">AI</span>
          </div>
          <div>
            <h1 className="font-semibold text-gray-900 leading-none">AI Support Agent</h1>
            <p className="text-[11px] text-gray-400 mt-0.5">GPT-4o mini · RAG · MongoDB Atlas</p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Link
            href="/admin"
            className="flex items-center gap-1.5 text-sm text-gray-500 px-2 sm:px-3 py-1.5 rounded-lg hover:bg-gray-100 transition-colors"
          >
            <BookOpen size={15} />
            <span className="hidden sm:inline">Knowledge Base</span>
          </Link>

          {/* History button + dropdown */}
          <div className="relative" ref={historyRef}>
            <button
              onClick={showHistory ? () => setShowHistory(false) : openHistory}
              className="flex items-center gap-1.5 text-sm text-gray-500 px-2 sm:px-3 py-1.5 rounded-lg hover:bg-gray-100 transition-colors"
            >
              <History size={15} />
              <span className="hidden sm:inline">History</span>
            </button>

            {showHistory && (
              <div className="absolute right-0 top-full mt-2 w-[min(320px,calc(100vw-2rem))] bg-white border border-gray-200 rounded-xl shadow-lg overflow-hidden">
                <div className="flex items-center justify-between px-4 py-3 border-b border-gray-100">
                  <span className="text-sm font-semibold text-gray-700">Recent conversations</span>
                  <button onClick={() => setShowHistory(false)} className="text-gray-400 hover:text-gray-600">
                    <X size={14} />
                  </button>
                </div>

                <div className="max-h-72 overflow-y-auto">
                  {historyLoading ? (
                    <div className="px-4 py-6 text-center text-sm text-gray-400">Loading…</div>
                  ) : sessions.length === 0 ? (
                    <div className="px-4 py-6 text-center text-sm text-gray-400">No past conversations</div>
                  ) : (
                    sessions.map(s => (
                      <div
                        key={s.sessionId}
                        className={`group flex items-start gap-2 px-4 py-3 border-b border-gray-50
                          hover:bg-indigo-50 transition-colors
                          ${s.sessionId === sessionId ? 'bg-indigo-50' : ''}`}
                      >
                        <button
                          onClick={() => loadSession(s.sessionId)}
                          className="flex-1 text-left min-w-0"
                        >
                          <p className="text-sm text-gray-700 line-clamp-2 leading-snug">{s.preview}</p>
                          <p className="text-[11px] text-gray-400 mt-1">{formatTime(s.createdAt)}</p>
                        </button>
                        <button
                          onClick={e => handleDeleteSession(e, s.sessionId)}
                          className="shrink-0 mt-0.5 p-1 rounded text-gray-300 hover:text-red-500
                                     hover:bg-red-50 transition-colors opacity-0 group-hover:opacity-100"
                          title="Delete conversation"
                        >
                          <Trash2 size={13} />
                        </button>
                      </div>
                    ))
                  )}
                </div>
              </div>
            )}
          </div>

          <button
            onClick={handleNewChat}
            className="flex items-center gap-1.5 text-sm bg-indigo-600 text-white px-2 sm:px-3 py-1.5 rounded-lg hover:bg-indigo-700 transition-colors"
          >
            <Plus size={15} />
            <span className="hidden sm:inline">New chat</span>
          </button>
        </div>
      </header>

      {/* ── Messages ── */}
      <ChatWindow messages={messages} onSend={handleSend} />

      {/* ── Input ── */}
      <div className="bg-white border-t border-gray-200 px-4 py-4 shrink-0">
        <form
          onSubmit={e => { e.preventDefault(); handleSend(input); }}
          className="max-w-3xl mx-auto flex gap-2"
        >
          <input
            value={input}
            onChange={e => setInput(e.target.value)}
            placeholder="Ask a question…"
            disabled={loading}
            className="flex-1 px-4 py-2.5 text-sm border border-gray-300 rounded-xl outline-none
                       focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition disabled:opacity-50"
          />
          <button
            type="submit"
            disabled={loading || !input.trim()}
            className="px-4 py-2.5 bg-indigo-600 text-white rounded-xl hover:bg-indigo-700
                       transition disabled:opacity-40 flex items-center gap-1.5 text-sm font-medium"
          >
            <Send size={15} />
            Send
          </button>
        </form>
        <p className="text-center text-[11px] text-gray-400 mt-2">
          Answers are grounded in the knowledge base · Powered by OpenAI
        </p>
      </div>
    </div>
  );
}
