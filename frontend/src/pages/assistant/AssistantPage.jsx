import React, { useState, useRef, useEffect } from 'react';
import { Send, Sparkles, Trash2, Loader2, Bot, ArrowRight } from 'lucide-react';
import aiService from '../../services/aiService';
import PageHeader from '../../components/common/PageHeader';
import useAuth from '../../hooks/useAuth';
import useLanguage from '../../hooks/useLanguage';

export function AssistantPage() {
  const { user } = useAuth();
  const { t } = useLanguage();

  const [inputQuestion, setInputQuestion] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [suggestedFollowUps, setSuggestedFollowUps] = useState([
    'Which products should I reorder this week?',
    'What are my top-selling products?',
    'What is my total inventory valuation?',
    'Which products are running low?',
  ]);
  const messagesEndRef = useRef(null);

  const [messages, setMessages] = useState(() => {
    const saved = sessionStorage.getItem('smartstock_chat');
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch {
        // fallback
      }
    }
    return [
      {
        id: 'msg_welcome',
        sender: 'ai',
        text: `Namaste ${user?.fullName || 'Shopkeeper'}! I am your **SmartStock Kirana AI Assistant**.\n\nI monitor your store's sales, stock levels, and supply velocity in real time. Ask me anything about what to reorder, top-performing groceries, or revenue numbers.`,
        timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      },
    ];
  });

  useEffect(() => {
    sessionStorage.setItem('smartstock_chat', JSON.stringify(messages));
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isLoading]);

  const handleSend = async (questionText) => {
    const q = (questionText || inputQuestion).trim();
    if (!q || isLoading) return;

    const userMessage = {
      id: `usr_${Date.now()}`,
      sender: 'user',
      text: q,
      timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    };

    setMessages((prev) => [...prev, userMessage]);
    setInputQuestion('');
    setIsLoading(true);

    try {
      const response = await aiService.askAssistant(q);
      const aiMessage = {
        id: `ai_${Date.now()}`,
        sender: 'ai',
        text: response.answer || response.message || 'I reviewed your store ledger.',
        timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      };
      setMessages((prev) => [...prev, aiMessage]);
      if (response.suggestedFollowUps && Array.isArray(response.suggestedFollowUps)) {
        setSuggestedFollowUps(response.suggestedFollowUps);
      }
    } catch (err) {
      const errorMessage = {
        id: `err_${Date.now()}`,
        sender: 'ai',
        text: 'Sorry, I encountered an issue connecting to the AI assistant service. Please check your network and try again.',
        timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      };
      setMessages((prev) => [...prev, errorMessage]);
    } finally {
      setIsLoading(false);
    }
  };

  const clearChat = () => {
    const reset = [
      {
        id: 'msg_welcome',
        sender: 'ai',
        text: `Conversation cleared. What would you like to know about your store inventory?`,
        timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      },
    ];
    setMessages(reset);
    sessionStorage.setItem('smartstock_chat', JSON.stringify(reset));
  };

  const renderFormattedText = (text) => {
    return text.split('\n').map((line, idx) => {
      const parts = line.split(/(\*\*.*?\*\*)/g);
      return (
        <span key={idx} className="block leading-relaxed">
          {parts.map((p, pIdx) => {
            if (p.startsWith('**') && p.endsWith('**')) {
              return <strong key={pIdx} className="font-bold text-slate-900">{p.slice(2, -2)}</strong>;
            }
            return p;
          })}
        </span>
      );
    });
  };

  return (
    <div className="max-w-4xl mx-auto flex flex-col h-[calc(100vh-140px)] min-h-[500px]">
      <PageHeader
        title={t('assistant.title')}
        subtitle={t('assistant.subtitle')}
        breadcrumbs={[{ label: 'AI Assistant' }]}
        action={
          <button
            onClick={clearChat}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-600 bg-white hover:bg-slate-50 border border-slate-200 rounded-lg transition"
          >
            <Trash2 className="w-3.5 h-3.5" />
            <span>{t('assistant.clearChat')}</span>
          </button>
        }
      />

      {/* Main Chat Box */}
      <div className="flex-1 bg-white rounded-2xl border border-slate-200 shadow-xs flex flex-col overflow-hidden">
        {/* Messages scroll area */}
        <div className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4">
          {messages.map((msg) => {
            const isUser = msg.sender === 'user';
            return (
              <div
                key={msg.id}
                className={`flex gap-3 ${isUser ? 'justify-end' : 'justify-start'}`}
              >
                {!isUser && (
                  <div className="w-8 h-8 rounded-xl bg-indigo-600 text-white flex items-center justify-center flex-shrink-0 shadow-xs">
                    <Sparkles className="w-4 h-4 text-amber-300" />
                  </div>
                )}

                <div
                  className={`max-w-[85%] sm:max-w-[75%] rounded-2xl p-4 text-xs sm:text-sm ${
                    isUser
                      ? 'bg-indigo-600 text-white rounded-tr-none'
                      : 'bg-slate-50 border border-slate-200/80 text-slate-800 rounded-tl-none shadow-xs'
                  }`}
                >
                  {isUser ? (
                    <p className="whitespace-pre-wrap">{msg.text}</p>
                  ) : (
                    <div className="space-y-1">{renderFormattedText(msg.text)}</div>
                  )}
                  <span
                    className={`text-[10px] block mt-2 ${
                      isUser ? 'text-indigo-200 text-right' : 'text-slate-400'
                    }`}
                  >
                    {msg.timestamp}
                  </span>
                </div>

                {isUser && (
                  <div className="w-8 h-8 rounded-xl bg-slate-900 text-white flex items-center justify-center flex-shrink-0 shadow-xs font-bold text-xs uppercase">
                    {user?.fullName ? user.fullName[0] : 'U'}
                  </div>
                )}
              </div>
            );
          })}

          {isLoading && (
            <div className="flex gap-3 justify-start items-center">
              <div className="w-8 h-8 rounded-xl bg-indigo-600 text-white flex items-center justify-center flex-shrink-0 shadow-xs">
                <Sparkles className="w-4 h-4 text-amber-300" />
              </div>
              <div className="p-3 rounded-2xl rounded-tl-none bg-slate-50 border border-slate-200/80 text-slate-600 text-xs flex items-center gap-2">
                <Loader2 className="w-3.5 h-3.5 animate-spin text-indigo-600" />
                <span>{t('assistant.thinking')}</span>
              </div>
            </div>
          )}

          <div ref={messagesEndRef} />
        </div>

        {/* Dynamic Suggested follow-ups carousel */}
        <div className="px-4 py-2.5 bg-slate-50/70 border-t border-slate-200/60 flex items-center gap-2 overflow-x-auto">
          <span className="text-[11px] font-semibold text-slate-500 whitespace-nowrap">
            {t('assistant.suggested')}:
          </span>
          {suggestedFollowUps.map((q) => (
            <button
              key={q}
              onClick={() => handleSend(q)}
              disabled={isLoading}
              className="whitespace-nowrap px-2.5 py-1 text-xs bg-white hover:bg-indigo-50 hover:text-indigo-700 hover:border-indigo-200 text-slate-700 border border-slate-200 rounded-full transition shadow-xs disabled:opacity-50"
            >
              {q}
            </button>
          ))}
        </div>

        {/* Chat input box */}
        <div className="p-3 sm:p-4 bg-white border-t border-slate-200">
          <form
            onSubmit={(e) => {
              e.preventDefault();
              handleSend();
            }}
            className="flex items-center gap-2"
          >
            <input
              type="text"
              value={inputQuestion}
              onChange={(e) => setInputQuestion(e.target.value)}
              placeholder={t('assistant.placeholder')}
              disabled={isLoading}
              className="flex-1 px-4 py-2.5 text-sm bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 transition"
            />
            <button
              type="submit"
              disabled={isLoading || !inputQuestion.trim()}
              className="p-2.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl shadow-sm transition disabled:opacity-50 flex items-center justify-center"
            >
              {isLoading ? <Loader2 className="w-5 h-5 animate-spin" /> : <Send className="w-5 h-5" />}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}

export default AssistantPage;
