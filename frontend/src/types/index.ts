export interface Source {
  id:       string;
  title:    string;
  category: string;
  content:  string;
}

export interface Message {
  id:          string;
  role:        'user' | 'assistant';
  content:     string;
  sources:     Source[];
  createdAt:   Date;
  isStreaming?: boolean;
}

export interface Article {
  id:           string;
  title:        string;
  content:      string;
  category:     string;
  hasEmbedding: boolean;
  createdAt:    string;
  updatedAt:    string;
}

export interface SessionSummary {
  sessionId: string;
  preview:   string;
  createdAt: string;
}

export interface SessionDto {
  sessionId: string;
  messages: {
    role:      'user' | 'assistant';
    content:   string;
    sources:   Source[];
    createdAt: string;
  }[];
  createdAt: string;
}

export type SseEvent =
  | { event: 'sources'; data: { sources: Source[]; sessionId: string } }
  | { event: 'chunk';   data: { text: string } }
  | { event: 'done';    data: Record<string, never> };
