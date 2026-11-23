import React, { useState, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Bell,
  CheckCheck,
  Check,
  AlertTriangle,
  PackageX,
  ArrowRight,
  ShieldCheck,
  RefreshCw,
} from 'lucide-react';
import notificationService from '../../services/notificationService';
import PageHeader from '../../components/common/PageHeader';
import StatusBadge from '../../components/common/StatusBadge';
import { formatDateTime } from '../../utils/formatters';
import { toast } from 'sonner';

export function NotificationsPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [unreadOnly, setUnreadOnly] = useState(false);

  const { data: notificationsData, isLoading } = useQuery({
    queryKey: ['notifications', unreadOnly],
    queryFn: () => notificationService.getNotifications({ unreadOnly }),
  });

  const notifications = useMemo(() => {
    if (Array.isArray(notificationsData)) return notificationsData;
    if (notificationsData?.items && Array.isArray(notificationsData.items)) return notificationsData.items;
    return [];
  }, [notificationsData]);

  const markReadMutation = useMutation({
    mutationFn: (id) => notificationService.markAsRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });

  const markAllReadMutation = useMutation({
    mutationFn: () => notificationService.markAllAsRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      toast.success('All notifications marked as read');
    },
  });

  const checkStockMutation = useMutation({
    mutationFn: () => notificationService.checkStock(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['products'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      toast.success('Inventory re-scan completed. Fresh alerts updated!');
    },
  });

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  return (
    <div className="max-w-4xl mx-auto space-y-5">
      <PageHeader
        title="Stock Alert Center"
        subtitle="Active inventory shortage alerts, out-of-stock notifications, and restock warnings."
        breadcrumbs={[{ label: 'Stock Alerts' }]}
        action={
          <div className="flex items-center gap-2">
            <button
              onClick={() => checkStockMutation.mutate()}
              disabled={checkStockMutation.isPending}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white hover:bg-slate-50 border border-slate-200 rounded-lg transition"
              title="Trigger manual re-scan of stock levels"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${checkStockMutation.isPending ? 'animate-spin' : ''}`} />
              <span>Scan Stock Levels</span>
            </button>
            {unreadCount > 0 && (
              <button
                onClick={() => markAllReadMutation.mutate()}
                disabled={markAllReadMutation.isPending}
                className="inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-semibold text-indigo-700 bg-indigo-50 hover:bg-indigo-100 rounded-lg transition"
              >
                <CheckCheck className="w-4 h-4" />
                <span>Mark All Read ({unreadCount})</span>
              </button>
            )}
          </div>
        }
      />

      {/* Filter tab */}
      <div className="flex items-center justify-between pb-2 border-b border-slate-200 text-xs font-medium text-slate-600">
        <div className="flex items-center gap-2">
          <button
            onClick={() => setUnreadOnly(false)}
            className={`px-3 py-1.5 rounded-lg transition ${
              !unreadOnly ? 'bg-indigo-600 text-white font-bold' : 'hover:bg-slate-100'
            }`}
          >
            All Alerts ({notifications.length})
          </button>
          <button
            onClick={() => setUnreadOnly(true)}
            className={`px-3 py-1.5 rounded-lg transition ${
              unreadOnly ? 'bg-indigo-600 text-white font-bold' : 'hover:bg-slate-100'
            }`}
          >
            Unread Only ({unreadCount})
          </button>
        </div>
      </div>

      {isLoading ? (
        <div className="bg-white rounded-xl border border-slate-200 p-12 flex justify-center">
          <div className="w-8 h-8 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin" />
        </div>
      ) : notifications.length === 0 ? (
        <div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
          <ShieldCheck className="w-12 h-12 text-emerald-500 mx-auto mb-3" />
          <h3 className="text-base font-bold text-slate-900">All Stock Levels Healthy</h3>
          <p className="text-xs text-slate-500 max-w-sm mx-auto mt-1">
            There are currently no alerts matching your filter. Click &quot;Scan Stock Levels&quot; to re-verify shelf thresholds.
          </p>
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-slate-200 divide-y divide-slate-100 overflow-hidden shadow-xs">
          {notifications.map((item) => {
            const isCritical =
              item.severity === 'Critical' ||
              item.notificationType === 2 ||
              item.notificationTypeName === 'OutOfStock';
            return (
              <div
                key={item.id}
                className={`p-4 transition flex items-start justify-between gap-4 ${
                  !item.isRead ? 'bg-indigo-50/30' : 'hover:bg-slate-50/50'
                }`}
              >
                <div className="flex items-start gap-3.5">
                  <div
                    className={`p-2.5 rounded-xl flex-shrink-0 ${
                      isCritical ? 'bg-rose-100 text-rose-700' : 'bg-amber-100 text-amber-700'
                    }`}
                  >
                    {isCritical ? <PackageX className="w-5 h-5" /> : <AlertTriangle className="w-5 h-5" />}
                  </div>

                  <div>
                    <div className="flex items-center gap-2">
                      <h4 className="text-sm font-bold text-slate-900">{item.title}</h4>
                      <StatusBadge status={isCritical ? 'Critical' : 'Low Stock'} size="sm" />
                      {!item.isRead && <span className="w-2 h-2 rounded-full bg-indigo-600" />}
                    </div>
                    <p className="text-xs text-slate-600 mt-1">{item.message}</p>
                    <span className="text-[11px] text-slate-400 mt-2 block">
                      {formatDateTime(item.date || item.createdAt)}
                    </span>
                  </div>
                </div>

                <div className="flex items-center gap-2 flex-shrink-0">
                  {item.productId && (
                    <button
                      onClick={() => navigate(`/products/${item.productId}`)}
                      className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:text-indigo-600 bg-white border border-slate-200 hover:border-indigo-300 rounded-lg transition"
                    >
                      <span>View SKU</span>
                      <ArrowRight className="w-3.5 h-3.5" />
                    </button>
                  )}
                  {!item.isRead && (
                    <button
                      onClick={() => markReadMutation.mutate(item.id)}
                      className="p-1.5 text-slate-400 hover:text-indigo-600 rounded-lg hover:bg-slate-100 transition"
                      title="Mark as read"
                    >
                      <Check className="w-4 h-4" />
                    </button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

export default NotificationsPage;
