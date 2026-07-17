import type { Article, SessionDto, SessionSummary, SseEvent } from '@/types';

const BASE = `${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'}/api`;

// ── Chat ──────────────────────────────────────────────────────────────────────

export async function* streamChat(
  question: string,
  sessionId?: string
): AsyncGenerator<SseEvent> {
  const res = await fetch(`${BASE}/chat/ask`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ question, sessionId }),
  });

  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  if (!res.body) throw new Error('No response body');

  const reader  = res.body.getReader();
  const decoder = new TextDecoder();
  let   buffer  = '';
  let   currentEvent = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop() ?? '';

    for (const line of lines) {
      if (line.startsWith('event: ')) {
        currentEvent = line.slice(7).trim();
      } else if (line.startsWith('data: ') && currentEvent) {
        try {
          const data = JSON.parse(line.slice(6));
          yield { event: currentEvent, data } as SseEvent;
        } catch {
          // ignore malformed JSON
        }
        currentEvent = '';
      }
    }
  }
}

export async function getSessions(): Promise<SessionSummary[]> {
  const res = await fetch(`${BASE}/chat/sessions`);
  if (!res.ok) throw new Error('Failed to fetch sessions');
  return res.json();
}

export async function getSession(sessionId: string): Promise<SessionDto> {
  const res = await fetch(`${BASE}/chat/sessions/${sessionId}`);
  if (!res.ok) throw new Error('Failed to fetch session');
  return res.json();
}

export async function deleteSession(sessionId: string): Promise<void> {
  const res = await fetch(`${BASE}/chat/sessions/${sessionId}`, { method: 'DELETE' });
  if (!res.ok && res.status !== 404) throw new Error('Failed to delete session');
}

// ── Knowledge base ────────────────────────────────────────────────────────────

export async function getArticles(): Promise<Article[]> {
  const res = await fetch(`${BASE}/knowledge`);
  if (!res.ok) throw new Error('Failed to fetch articles');
  return res.json();
}

export async function createArticle(
  data: { title: string; content: string; category: string }
): Promise<Article> {
  const res = await fetch(`${BASE}/knowledge`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw new Error('Failed to create article');
  return res.json();
}

export async function updateArticle(
  id: string,
  data: { title: string; content: string; category: string }
): Promise<Article> {
  const res = await fetch(`${BASE}/knowledge/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw new Error('Failed to update article');
  return res.json();
}

export async function deleteArticle(id: string): Promise<void> {
  const res = await fetch(`${BASE}/knowledge/${id}`, { method: 'DELETE' });
  if (!res.ok) throw new Error('Failed to delete article');
}

export async function embedArticle(id: string): Promise<Article> {
  const res = await fetch(`${BASE}/knowledge/${id}/embed`, { method: 'POST' });
  if (!res.ok) throw new Error('Failed to embed article');
  return res.json();
}

export async function embedAll(): Promise<{ embedded: number; failed: number }> {
  const res = await fetch(`${BASE}/knowledge/embed-all`, { method: 'POST' });
  if (!res.ok) throw new Error('Failed to embed articles');
  return res.json();
}
