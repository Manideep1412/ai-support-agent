'use client';

import { useState } from 'react';
import { ChevronDown } from 'lucide-react';
import type { Source } from '@/types';

const CATEGORY_COLORS: Record<string, string> = {
  Billing:   'bg-amber-100 text-amber-700',
  Technical: 'bg-blue-100 text-blue-700',
  Account:   'bg-green-100 text-green-700',
  General:   'bg-gray-100 text-gray-600',
};

export default function SourceCard({ source }: { source: Source }) {
  const [expanded, setExpanded] = useState(false);
  const color = CATEGORY_COLORS[source.category] ?? CATEGORY_COLORS.General;

  return (
    <div className="text-xs">
      <button
        onClick={() => setExpanded(x => !x)}
        className="flex items-center gap-2 px-3 py-1.5 bg-white border border-gray-200 rounded-lg
                   shadow-sm hover:border-indigo-300 hover:shadow-md transition-all cursor-pointer w-full text-left"
      >
        <span className={`px-1.5 py-0.5 rounded font-medium shrink-0 ${color}`}>
          {source.category}
        </span>
        <span className="text-gray-600 truncate flex-1">{source.title}</span>
        <ChevronDown
          size={12}
          className={`text-gray-400 shrink-0 transition-transform duration-200 ${expanded ? 'rotate-180' : ''}`}
        />
      </button>

      {expanded && source.content && (
        <div className="mt-1 px-3 py-2.5 bg-gray-50 border border-gray-200 rounded-lg
                        text-gray-600 leading-relaxed max-w-sm">
          {source.content}
        </div>
      )}
    </div>
  );
}
