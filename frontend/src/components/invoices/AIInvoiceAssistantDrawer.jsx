import React, { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import {
  BotMessageSquare,
  X,
  Send,
  Sparkles,
  TrendingUp,
  CopyCheck,
  Clock,
  Loader2,
  FileSpreadsheet
} from 'lucide-react';
import supplierInvoiceService from '../../services/supplierInvoiceService';

export function AIInvoiceAssistantDrawer({ isOpen, onClose }) {
  const [messages, setMessages] = useState([
    {
      id: 1,
      sender: 'assistant',
      text: "Hello! I am your AI Supplier Invoice Assistant. I can analyze price variance, detect duplicate scans, or find line-item changes across your wholesale invoices. What would you like to check?",
      suggestions: [
        'Which items had a price increase?',
        'Are there any duplicate invoices?',
        'How many invoices are pending review?',
      ],
    },
  ]);
  const [input, setInput] = useState('');

  const queryMutation = useMutation({
    mutationFn: (query) => supplierInvoiceService.queryAssistant(query),
    onSuccess: (data) => {
      setMessages((prev) => [
        ...prev,
        {
          id: Date.now(),
          sender: 'assistant',
          text: data?.answer || "I have analyzed your invoice records.",
          suggestions: data?.suggestedActions || [],
        },
      ]);
    },
    onError: (err) => {
      setMessages((prev) => [
        ...prev,
        {
          id: Date.now(),
          sender: 'assistant',
          text: `Sorry, an error occurred while querying invoice data: ${err.message}`,
        },
      ]);
    },
  });

  const handleSend = (textToSend) => {
    const query = (textToSend || input).trim();
    if (!query) return;

    setMessages((prev) => [
      ...prev,
      { id: Date.now(), sender: 'user', text: query },
    ]);
    setInput('');
    queryMutation.mutate(query);
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-hidden bg-slate-900/40 backdrop-blur-xs flex justify-end">
      <div className="w-full max-w-md bg-white h-full shadow-2xl flex flex-col border-l border-slate-200 animate-in slide-in-from-right duration-200">
        {/* Header */}
        <div className="p-4 border-b border-slate-100 flex items-center justify-between bg-slate-900 text-white">
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-lg bg-indigo-500/20 text-indigo-400 flex items-center justify-center border border-indigo-400/30">
              <Sparkles className="w-4 h-4" />
            </div>
            <div>
              <h3 className="font-semibold text-sm">Invoice Intelligence</h3>
              <p className="text-[11px] text-slate-400">SmartStock AI Assistant</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1 text-slate-400 hover:text-white rounded-lg transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Messages */}
        <div className="flex-1 p-4 overflow-y-auto space-y-4 text-sm bg-slate-50/50">
          {messages.map((m) => (
            <div
              key={m.id}
              className={`flex flex-col ${m.sender === 'user' ? 'items-end' : 'items-start'}`}
            >
              <div
                className={`max-w-[85%] rounded-2xl px-4 py-2.5 shadow-xs ${
                  m.sender === 'user'
                    ? 'bg-indigo-600 text-white rounded-br-xs'
                    : 'bg-white text-slate-800 border border-slate-200/80 rounded-bl-xs'
                }`}
              >
                <div className="whitespace-pre-wrap leading-relaxed">{m.text}</div>
              </div>

              {m.suggestions && m.suggestions.length > 0 && (
                <div className="mt-2 flex flex-wrap gap-1.5 max-w-[90%]">
                  {m.suggestions.map((s, idx) => (
                    <button
                      key={idx}
                      onClick={() => handleSend(s)}
                      className="text-xs bg-indigo-50/80 hover:bg-indigo-100 text-indigo-700 px-2.5 py-1 rounded-full border border-indigo-200/60 transition text-left"
                    >
                      {s}
                    </button>
                  ))}
                </div>
              )}
            </div>
          ))}

          {queryMutation.isPending && (
            <div className="flex items-center gap-2 text-slate-400 text-xs py-2">
              <Loader2 className="w-4 h-4 animate-spin text-indigo-600" />
              <span>Analyzing supplier invoices and price histories...</span>
            </div>
          )}
        </div>

        {/* Input */}
        <div className="p-3 border-t border-slate-200 bg-white">
          <form
            onSubmit={(e) => {
              e.preventDefault();
              handleSend();
            }}
            className="flex items-center gap-2"
          >
            <input
              type="text"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder="Ask about price changes, duplicates..."
              className="flex-1 px-3.5 py-2 text-sm bg-slate-50 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
            />
            <button
              type="submit"
              disabled={!input.trim() || queryMutation.isPending}
              className="p-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white rounded-lg transition"
            >
              <Send className="w-4 h-4" />
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}

export default AIInvoiceAssistantDrawer;
