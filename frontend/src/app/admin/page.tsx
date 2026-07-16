'use client';

import { useState, useEffect, useCallback } from 'react';
import { Plus, Pencil, Trash2, Zap, ZapOff, ArrowLeft, Loader2 } from 'lucide-react';
import Link from 'next/link';
import { getArticles, createArticle, updateArticle, deleteArticle, embedArticle, embedAll } from '@/lib/api';
import type { Article } from '@/types';

const CATEGORIES = ['General', 'Billing', 'Technical', 'Account'];

interface FormState { title: string; content: string; category: string; }
const EMPTY: FormState = { title: '', content: '', category: 'General' };

export default function AdminPage() {
  const [articles, setArticles] = useState<Article[]>([]);
  const [loading,  setLoading]  = useState(true);
  const [modal,    setModal]    = useState<{ open: boolean; article?: Article }>({ open: false });
  const [form,     setForm]     = useState<FormState>(EMPTY);
  const [saving,   setSaving]   = useState(false);
  const [embedding, setEmbedding] = useState<string | null>(null);
  const [embedAllLoading, setEmbedAllLoading] = useState(false);
  const [toast,    setToast]    = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try { setArticles(await getArticles()); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  function openCreate() { setForm(EMPTY); setModal({ open: true }); }
  function openEdit(a: Article) {
    setForm({ title: a.title, content: a.content, category: a.category });
    setModal({ open: true, article: a });
  }
  function closeModal() { setModal({ open: false }); }

  function showToast(msg: string) {
    setToast(msg);
    setTimeout(() => setToast(''), 3000);
  }

  async function handleSave() {
    if (!form.title.trim() || !form.content.trim()) return;
    setSaving(true);
    try {
      if (modal.article) {
        const updated = await updateArticle(modal.article.id, form);
        setArticles(prev => prev.map(a => a.id === updated.id ? updated : a));
        showToast('Article updated');
      } else {
        const created = await createArticle(form);
        setArticles(prev => [created, ...prev]);
        showToast('Article created — run "Embed All" to enable semantic search');
      }
      closeModal();
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this article?')) return;
    await deleteArticle(id);
    setArticles(prev => prev.filter(a => a.id !== id));
    showToast('Article deleted');
  }

  async function handleEmbed(id: string) {
    setEmbedding(id);
    try {
      const updated = await embedArticle(id);
      setArticles(prev => prev.map(a => a.id === updated.id ? updated : a));
      showToast('Embedding generated');
    } finally { setEmbedding(null); }
  }

  async function handleEmbedAll() {
    setEmbedAllLoading(true);
    try {
      const result = await embedAll();
      await load();
      showToast(`Embedded ${result.embedded} articles${result.failed > 0 ? `, ${result.failed} failed` : ''}`);
    } finally { setEmbedAllLoading(false); }
  }

  const embeddedCount = articles.filter(a => a.hasEmbedding).length;

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Toast */}
      {toast && (
        <div className="fixed top-4 right-4 z-50 bg-gray-900 text-white text-sm px-4 py-2.5 rounded-xl shadow-lg">
          {toast}
        </div>
      )}

      {/* Header */}
      <header className="bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Link href="/" className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors">
            <ArrowLeft size={18} className="text-gray-500" />
          </Link>
          <div>
            <h1 className="font-semibold text-gray-900">Knowledge Base</h1>
            <p className="text-xs text-gray-400">
              {articles.length} articles · {embeddedCount} embedded
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={handleEmbedAll}
            disabled={embedAllLoading}
            className="flex items-center gap-1.5 text-sm px-3 py-1.5 border border-gray-300
                       rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50"
          >
            {embedAllLoading
              ? <Loader2 size={15} className="animate-spin" />
              : <Zap size={15} className="text-amber-500" />}
            Embed All
          </button>
          <button
            onClick={openCreate}
            className="flex items-center gap-1.5 text-sm bg-indigo-600 text-white px-3 py-1.5 rounded-lg hover:bg-indigo-700 transition-colors"
          >
            <Plus size={15} />
            New Article
          </button>
        </div>
      </header>

      {/* Articles */}
      <main className="max-w-4xl mx-auto px-6 py-6">
        {loading ? (
          <div className="flex justify-center py-20">
            <Loader2 className="animate-spin text-gray-400" size={32} />
          </div>
        ) : articles.length === 0 ? (
          <div className="text-center py-20 text-gray-400">
            No articles yet. Click "New Article" to add one.
          </div>
        ) : (
          <div className="space-y-3">
            {articles.map(a => (
              <div key={a.id} className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
                <div className="flex items-start justify-between gap-3">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <span className="text-xs font-medium px-2 py-0.5 bg-indigo-50 text-indigo-600 rounded-full">
                        {a.category}
                      </span>
                      {a.hasEmbedding
                        ? <span className="text-xs text-green-600 flex items-center gap-1"><Zap size={11} />embedded</span>
                        : <span className="text-xs text-gray-400 flex items-center gap-1"><ZapOff size={11} />not embedded</span>
                      }
                    </div>
                    <h3 className="font-medium text-gray-900 truncate">{a.title}</h3>
                    <p className="text-sm text-gray-500 mt-1 line-clamp-2">{a.content}</p>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <button
                      onClick={() => handleEmbed(a.id)}
                      disabled={embedding === a.id}
                      title="Generate embedding"
                      className="p-1.5 hover:bg-amber-50 rounded-lg transition-colors disabled:opacity-50"
                    >
                      {embedding === a.id
                        ? <Loader2 size={15} className="animate-spin text-amber-500" />
                        : <Zap size={15} className="text-amber-500" />}
                    </button>
                    <button onClick={() => openEdit(a)} className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors">
                      <Pencil size={15} className="text-gray-500" />
                    </button>
                    <button onClick={() => handleDelete(a.id)} className="p-1.5 hover:bg-red-50 rounded-lg transition-colors">
                      <Trash2 size={15} className="text-red-500" />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </main>

      {/* Modal */}
      {modal.open && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg">
            <div className="px-6 py-4 border-b border-gray-100">
              <h2 className="font-semibold text-gray-900">
                {modal.article ? 'Edit Article' : 'New Article'}
              </h2>
            </div>
            <div className="px-6 py-4 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Title *</label>
                <input
                  value={form.title}
                  onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
                  className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
                  placeholder="e.g. How to reset your password"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Category</label>
                <select
                  value={form.category}
                  onChange={e => setForm(f => ({ ...f, category: e.target.value }))}
                  className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg outline-none focus:border-indigo-500"
                >
                  {CATEGORIES.map(c => <option key={c}>{c}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Content *</label>
                <textarea
                  value={form.content}
                  onChange={e => setForm(f => ({ ...f, content: e.target.value }))}
                  rows={6}
                  className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 resize-none"
                  placeholder="Write the knowledge base article content…"
                />
              </div>
            </div>
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-2">
              <button onClick={closeModal} className="px-4 py-2 text-sm text-gray-600 hover:bg-gray-100 rounded-lg transition-colors">
                Cancel
              </button>
              <button
                onClick={handleSave}
                disabled={saving || !form.title.trim() || !form.content.trim()}
                className="px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors disabled:opacity-50"
              >
                {saving ? 'Saving…' : 'Save'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
