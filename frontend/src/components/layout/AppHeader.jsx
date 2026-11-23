import React, { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { Menu, Bell, Languages, BotMessageSquare, Sparkles } from 'lucide-react';
import useAuth from '../../hooks/useAuth';
import useLanguage from '../../hooks/useLanguage';
import { useQuery } from '@tanstack/react-query';
import notificationService from '../../services/notificationService';
import businessService from '../../services/businessService';

export function AppHeader({ onMenuClick }) {
  const { user } = useAuth();
  const { language, toggleLanguage, t } = useLanguage();

  const { data: business } = useQuery({
    queryKey: ['business'],
    queryFn: () => businessService.getBusiness(),
  });

  const { data: notificationsData } = useQuery({
    queryKey: ['notifications'],
    queryFn: () => notificationService.getNotifications(),
    refetchInterval: 30000,
  });

  const notifications = useMemo(() => {
    if (Array.isArray(notificationsData)) return notificationsData;
    if (notificationsData?.items && Array.isArray(notificationsData.items)) return notificationsData.items;
    return [];
  }, [notificationsData]);

  const unreadCount = notifications.filter((n) => !n.isRead).length;
  const storeName = business?.businessName || user?.businessName || 'Sharma Kirana Store';

  return (
    <header className="sticky top-0 z-30 h-16 bg-white border-b border-slate-200/80 px-4 sm:px-6 flex items-center justify-between shadow-xs">
      {/* Left section: mobile hamburger & shop name */}
      <div className="flex items-center gap-3">
        <button
          onClick={onMenuClick}
          className="lg:hidden p-2 rounded-lg text-slate-600 hover:bg-slate-100 transition"
        >
          <Menu className="w-5 h-5" />
        </button>
        <div>
          <span className="hidden sm:inline-block text-xs font-semibold uppercase tracking-wider text-indigo-700 bg-indigo-50 px-2 py-0.5 rounded">
            {business?.businessType || 'Kirana / Grocery'}
          </span>
          <h2 className="text-sm sm:text-base font-bold text-slate-900 leading-tight truncate max-w-[240px]">
            {storeName}
          </h2>
        </div>
      </div>

      {/* Right section: AI shortcuts, Language toggle, notifications, profile */}
      <div className="flex items-center gap-2 sm:gap-3">
        {/* Quick link to AI Assistant */}
        <Link
          to="/assistant"
          className="hidden md:flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold text-indigo-700 bg-indigo-50 hover:bg-indigo-100 transition border border-indigo-200/60"
        >
          <BotMessageSquare className="w-3.5 h-3.5" />
          <span>Ask AI</span>
        </Link>

        {/* AI Insights button */}
        <Link
          to="/insights"
          className="hidden md:flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold text-slate-700 hover:bg-slate-100 transition border border-slate-200"
        >
          <Sparkles className="w-3.5 h-3.5 text-amber-500" />
          <span>Restock Forecast</span>
        </Link>

        {/* Language switcher button */}
        <button
          onClick={toggleLanguage}
          className="flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-xs font-medium text-slate-700 bg-slate-100 hover:bg-slate-200 transition"
          title="Switch Language / भाषा बदलें"
        >
          <Languages className="w-4 h-4 text-slate-500" />
          <span className="font-semibold">{language === 'en' ? 'हिन्दी' : 'English'}</span>
        </button>

        {/* Notifications button */}
        <Link
          to="/notifications"
          className="relative p-2 rounded-lg text-slate-600 hover:bg-slate-100 transition"
          title="Stock Alerts"
        >
          <Bell className="w-5 h-5" />
          {unreadCount > 0 && (
            <span className="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-rose-600 ring-2 ring-white animate-pulse" />
          )}
        </Link>

        {/* User initials bubble */}
        <Link
          to="/settings"
          className="flex items-center gap-2 pl-2 border-l border-slate-200 text-left"
        >
          <div className="w-8 h-8 rounded-full bg-indigo-600 text-white flex items-center justify-center font-semibold text-xs shadow-xs">
            {user?.fullName ? user.fullName[0] : 'R'}
          </div>
        </Link>
      </div>
    </header>
  );
}

export default AppHeader;
