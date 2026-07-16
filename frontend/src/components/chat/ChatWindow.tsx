'use client';

import { useEffect, useRef } from 'react';
import MessageBubble from './MessageBubble';
import type { Message } from '@/types';
import { Bot } from 'lucide-react';

interface Props {
  messages: Message[];
  onSend:   (question: string) => void;
}

export default function ChatWindow({ messages, onSend }: Props) {
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  if (messages.length === 0) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center gap-4 text-center p-8">
        <div className="w-16 h-16 bg-indigo-100 rounded-2xl flex items-center justify-center">
          <Bot size={32} className="text-indigo-600" />
        </div>
        <div>
          <h2 className="text-lg font-semibold text-gray-800">How can I help you today?</h2>
          <p className="text-sm text-gray-500 mt-1 max-w-sm">
            Ask me about billing, account settings, technical issues, or anything else.
            I'll search the knowledge base to find the best answer.
          </p>
        </div>
        <div className="grid grid-cols-2 gap-2 mt-2 max-w-md w-full">
          {SUGGESTED.map(q => (
            <button
              key={q}
              onClick={() => onSend(q)}
              className="text-left text-xs px-3 py-2.5 bg-white border border-gray-200 rounded-xl
                         text-gray-600 hover:border-indigo-300 hover:bg-indigo-50 transition-colors cursor-pointer"
            >
              {q}
            </button>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 overflow-y-auto px-4 py-6">
      <div className="max-w-3xl mx-auto space-y-5">
        {messages.map(msg => (
          <MessageBubble key={msg.id} message={msg} />
        ))}
        <div ref={bottomRef} />
      </div>
    </div>
  );
}

const SUGGESTED = [
  'How do I reset my password?',
  'What payment methods do you accept?',
  'How do I cancel my subscription?',
  'Is my data encrypted?',
];
