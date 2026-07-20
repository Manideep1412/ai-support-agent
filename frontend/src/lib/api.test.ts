import {
  getSessions,
  getSession,
  deleteSession,
  getArticles,
  createArticle,
  updateArticle,
  deleteArticle,
  embedArticle,
  embedAll,
} from './api';

// ── fetch mock ────────────────────────────────────────────────────────────────

const mockFetch = jest.fn();
global.fetch = mockFetch;

const ok = (body: unknown, status = 200) =>
  Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
    text: () => Promise.resolve(JSON.stringify(body)),
  } as Response);

const fail = (status: number) =>
  Promise.resolve({ ok: false, status, json: () => Promise.resolve({}) } as Response);

beforeEach(() => mockFetch.mockReset());

// ── Chat sessions ─────────────────────────────────────────────────────────────

describe('getSessions', () => {
  it('fetches /chat/sessions and returns the list', async () => {
    const data = [{ sessionId: 's1', preview: 'Hello', createdAt: '2024-01-01T00:00:00Z' }];
    mockFetch.mockReturnValueOnce(ok(data));

    const result = await getSessions();

    expect(mockFetch).toHaveBeenCalledWith(expect.stringContaining('/chat/sessions'));
    expect(result).toEqual(data);
  });

  it('throws when response is not ok', async () => {
    mockFetch.mockReturnValueOnce(fail(500));
    await expect(getSessions()).rejects.toThrow('Failed to fetch sessions');
  });
});

describe('getSession', () => {
  it('fetches /chat/sessions/:id and returns session', async () => {
    const data = { sessionId: 's1', messages: [], createdAt: '2024-01-01T00:00:00Z' };
    mockFetch.mockReturnValueOnce(ok(data));

    const result = await getSession('s1');

    expect(mockFetch).toHaveBeenCalledWith(expect.stringContaining('/chat/sessions/s1'));
    expect(result.sessionId).toBe('s1');
  });

  it('throws when response is not ok', async () => {
    mockFetch.mockReturnValueOnce(fail(404));
    await expect(getSession('nope')).rejects.toThrow('Failed to fetch session');
  });
});

describe('deleteSession', () => {
  it('sends DELETE and resolves on 204', async () => {
    mockFetch.mockReturnValueOnce(Promise.resolve({ ok: true, status: 204 } as Response));

    await expect(deleteSession('s1')).resolves.toBeUndefined();
    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/chat/sessions/s1'),
      expect.objectContaining({ method: 'DELETE' })
    );
  });

  it('resolves silently on 404 (idempotent)', async () => {
    mockFetch.mockReturnValueOnce(Promise.resolve({ ok: false, status: 404 } as Response));
    await expect(deleteSession('gone')).resolves.toBeUndefined();
  });

  it('throws on unexpected error', async () => {
    mockFetch.mockReturnValueOnce(fail(500));
    await expect(deleteSession('s1')).rejects.toThrow('Failed to delete session');
  });
});

// ── Knowledge base ────────────────────────────────────────────────────────────

describe('getArticles', () => {
  it('fetches /knowledge and returns article list', async () => {
    const data = [{ id: 'a1', title: 'Password Reset', content: '...', category: 'Account', hasEmbedding: true }];
    mockFetch.mockReturnValueOnce(ok(data));

    const result = await getArticles();

    expect(mockFetch).toHaveBeenCalledWith(expect.stringContaining('/knowledge'));
    expect(result).toEqual(data);
  });

  it('throws on failure', async () => {
    mockFetch.mockReturnValueOnce(fail(500));
    await expect(getArticles()).rejects.toThrow('Failed to fetch articles');
  });
});

describe('createArticle', () => {
  it('POSTs to /knowledge and returns created article', async () => {
    const payload = { title: 'T', content: 'C', category: 'General' };
    const created = { id: 'new', ...payload, hasEmbedding: false, createdAt: '', updatedAt: '' };
    mockFetch.mockReturnValueOnce(ok(created, 201));

    const result = await createArticle(payload);

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/knowledge'),
      expect.objectContaining({ method: 'POST', body: JSON.stringify(payload) })
    );
    expect(result.id).toBe('new');
  });

  it('throws on failure', async () => {
    mockFetch.mockReturnValueOnce(fail(400));
    await expect(createArticle({ title: '', content: '', category: '' })).rejects.toThrow(
      'Failed to create article'
    );
  });
});

describe('updateArticle', () => {
  it('PUTs to /knowledge/:id and returns updated article', async () => {
    const payload = { title: 'Updated', content: 'C', category: 'Billing' };
    const updated = { id: 'a1', ...payload, hasEmbedding: true, createdAt: '', updatedAt: '' };
    mockFetch.mockReturnValueOnce(ok(updated));

    const result = await updateArticle('a1', payload);

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/knowledge/a1'),
      expect.objectContaining({ method: 'PUT' })
    );
    expect(result.title).toBe('Updated');
  });

  it('throws on failure', async () => {
    mockFetch.mockReturnValueOnce(fail(404));
    await expect(updateArticle('bad', { title: '', content: '', category: '' })).rejects.toThrow(
      'Failed to update article'
    );
  });
});

describe('deleteArticle', () => {
  it('sends DELETE to /knowledge/:id', async () => {
    mockFetch.mockReturnValueOnce(Promise.resolve({ ok: true, status: 204 } as Response));

    await expect(deleteArticle('a1')).resolves.toBeUndefined();
    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/knowledge/a1'),
      expect.objectContaining({ method: 'DELETE' })
    );
  });

  it('throws on failure', async () => {
    mockFetch.mockReturnValueOnce(fail(500));
    await expect(deleteArticle('a1')).rejects.toThrow('Failed to delete article');
  });
});

describe('embedArticle', () => {
  it('POSTs to /knowledge/:id/embed and returns article with hasEmbedding true', async () => {
    const data = { id: 'a1', title: 'T', content: 'C', category: 'General', hasEmbedding: true, createdAt: '', updatedAt: '' };
    mockFetch.mockReturnValueOnce(ok(data));

    const result = await embedArticle('a1');

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/knowledge/a1/embed'),
      expect.objectContaining({ method: 'POST' })
    );
    expect(result.hasEmbedding).toBe(true);
  });

  it('throws on failure', async () => {
    mockFetch.mockReturnValueOnce(fail(404));
    await expect(embedArticle('bad')).rejects.toThrow('Failed to embed article');
  });
});

describe('embedAll', () => {
  it('POSTs to /knowledge/embed-all and returns counts', async () => {
    mockFetch.mockReturnValueOnce(ok({ embedded: 8, failed: 0 }));

    const result = await embedAll();

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/knowledge/embed-all'),
      expect.objectContaining({ method: 'POST' })
    );
    expect(result).toEqual({ embedded: 8, failed: 0 });
  });

  it('throws on failure', async () => {
    mockFetch.mockReturnValueOnce(fail(500));
    await expect(embedAll()).rejects.toThrow('Failed to embed articles');
  });
});
