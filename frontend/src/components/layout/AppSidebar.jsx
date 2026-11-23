import React, { useMemo } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  Package,
  Boxes,
  ArrowUpDown,
  ShoppingCart,
  Truck,
  Users,
  Bell,
  Sparkles,
  BotMessageSquare,
  Settings,
  LogOut,
  FolderTree,
  Store,
  X,
} from 'lucide-react';
import { cn } from '../../utils/cn';
import useAuth from '../../hooks/useAuth';
import useLanguage from '../../hooks/useLanguage';
import { useQuery } from '@tanstack/react-query';
import notificationService from '../../services/notificationService';
import businessService from '../../services/businessService';

export function AppSidebar({ isOpen, onClose }) {
  const { user, logout } = useAuth();
  const { t } = useLanguage();
  const navigate = useNavigate();

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

  const unreadAlerts = notifications.filter((n) => !n.isRead).length;
  const storeName = business?.businessName || user?.businessName || 'Sharma Kirana Store';

  const navItems = [
    { label: t('nav.dashboard'), to: '/dashboard', icon: LayoutDashboard },
    { label: t('nav.products'), to: '/products', icon: Package },
    { label: t('nav.categories'), to: '/categories', icon: FolderTree },
    { label: t('nav.inventory'), to: '/inventory', icon: Boxes },
    { label: t('nav.transactions'), to: '/inventory/transactions', icon: ArrowUpDown },
    { label: t('nav.sales'), to: '/sales', icon: ShoppingCart },
    { label: t('nav.purchases'), to: '/purchases', icon: Truck },
    { label: t('nav.suppliers'), to: '/suppliers', icon: Users },
    { label: t('nav.notifications'), to: '/notifications', icon: Bell, badge: unreadAlerts },
    { label: t('nav.insights'), to: '/insights', icon: Sparkles, highlight: true },
    { label: t('nav.assistant'), to: '/assistant', icon: BotMessageSquare, highlight: true },
    { label: t('nav.settings'), to: '/settings', icon: Settings },
  ];

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <>
      {/* Mobile backdrop */}
      {isOpen && (
        <div
          onClick={onClose}
          className="fixed inset-0 z-40 bg-slate-900/40 backdrop-blur-sm lg:hidden"
        />
      )}

      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-50 w-64 bg-slate-900 text-slate-300 flex flex-col transition-transform duration-200 ease-in-out border-r border-slate-800 lg:static lg:translate-x-0',
          isOpen ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        {/* Brand header */}
        <div className="h-16 flex items-center justify-between px-5 border-b border-slate-800/80">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-indigo-600 flex items-center justify-center text-white shadow-md shadow-indigo-600/30">
              <Store className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center gap-1.5">
                <span className="font-bold text-base text-white tracking-tight">SmartStock</span>
                <span className="text-[10px] font-semibold bg-indigo-500/30 text-indigo-300 px-1.5 py-0.5 rounded">
                  PRO
                </span>
              </div>
              <p className="text-[11px] text-slate-400 truncate max-w-[130px]" title={storeName}>
                {storeName}
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="lg:hidden text-slate-400 hover:text-white p-1 rounded-lg"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Navigation list */}
        <nav className="flex-1 overflow-y-auto px-3 py-4 space-y-1">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.to}
                to={item.to}
                onClick={() => onClose && onClose()}
                className={({ isActive }) =>
                  cn(
                    'flex items-center justify-between px-3.5 py-2.5 rounded-lg text-sm font-medium transition-colors',
                    isActive
                      ? 'bg-indigo-600 text-white shadow-sm'
                      : 'text-slate-300 hover:bg-slate-800/80 hover:text-white',
                    item.highlight && !isActive && 'text-indigo-300 hover:text-indigo-200'
                  )
                }
              >
                <div className="flex items-center gap-3">
                  <Icon className={cn('w-4 h-4', item.highlight ? 'text-indigo-400' : '')} />
                  <span>{item.label}</span>
                </div>
                {item.badge > 0 && (
                  <span className="bg-rose-500 text-white text-[10px] font-bold px-1.5 py-0.5 rounded-full">
                    {item.badge}
                  </span>
                )}
              </NavLink>
            );
          })}
        </nav>

        {/* User profile & logout bottom bar */}
        <div className="p-3 border-t border-slate-800/80 bg-slate-950/40">
          <div className="flex items-center justify-between p-2 rounded-lg bg-slate-900/60">
            <div className="flex items-center gap-2.5 overflow-hidden">
              <div className="w-8 h-8 rounded-full bg-indigo-700 text-white flex items-center justify-center font-bold text-xs uppercase flex-shrink-0">
                {user?.fullName ? user.fullName[0] : 'R'}
              </div>
              <div className="truncate">
                <p className="text-xs font-medium text-white truncate">{user?.fullName || 'Ramesh Sharma'}</p>
                <p className="text-[11px] text-slate-400 truncate">{user?.email || 'demo@smartstock.in'}</p>
              </div>
            </div>
            <button
              onClick={handleLogout}
              title={t('nav.logout')}
              className="text-slate-400 hover:text-rose-400 p-1.5 rounded-lg hover:bg-slate-800 transition"
            >
              <LogOut className="w-4 h-4" />
            </button>
          </div>
        </div>
      </aside>
    </>
  );
}

export default AppSidebar;
