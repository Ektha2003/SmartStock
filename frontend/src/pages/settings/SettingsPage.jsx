import React, { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Store, User, ShieldCheck, LogOut, Loader2, Server } from 'lucide-react';
import businessService from '../../services/businessService';
import authService from '../../services/authService';
import useAuth from '../../hooks/useAuth';
import PageHeader from '../../components/common/PageHeader';
import { toast } from 'sonner';

export function SettingsPage() {
  const { user, logout } = useAuth();
  const queryClient = useQueryClient();

  const { data: business, isLoading } = useQuery({
    queryKey: ['business'],
    queryFn: () => businessService.getBusiness(),
  });

  const [businessName, setBusinessName] = useState('');
  const [ownerName, setOwnerName] = useState('');
  const [phone, setPhone] = useState('');
  const [address, setAddress] = useState('');
  const [businessType, setBusinessType] = useState('Kirana / Grocery');

  useEffect(() => {
    if (business) {
      setBusinessName(business.businessName || '');
      setOwnerName(business.ownerName || user?.fullName || '');
      setPhone(business.phone || '');
      setAddress(business.address || '');
      setBusinessType(business.businessType || 'Kirana / Grocery');
    }
  }, [business, user]);

  const updateMutation = useMutation({
    mutationFn: (data) => businessService.updateBusiness(data),
    onSuccess: (updated) => {
      queryClient.invalidateQueries({ queryKey: ['business'] });
      toast.success('Store & Business profile updated successfully!');
    },
    onError: (err) => {
      toast.error(err.message || 'Failed to update store settings');
    },
  });

  const handleSave = (e) => {
    e.preventDefault();
    updateMutation.mutate({
      businessName,
      ownerName,
      phone,
      address,
      businessType,
    });
  };

  const isMock = import.meta.env.VITE_USE_MOCK === 'true';

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <PageHeader
        title="Store Settings & Configuration"
        subtitle="Manage business identity, store location, and API connectivity."
        breadcrumbs={[{ label: 'Settings' }]}
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left col: Business & User profile form */}
        <div className="lg:col-span-2 space-y-6">
          <form onSubmit={handleSave} className="bg-white rounded-2xl border border-slate-200 p-6 shadow-xs space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-slate-100">
              <div className="flex items-center gap-2">
                <Store className="w-5 h-5 text-indigo-600" />
                <h3 className="font-bold text-base text-slate-900">Kirana Store & Business Profile</h3>
              </div>
              <span className="text-xs font-mono text-slate-400">ID: {business?.id ? String(business.id).slice(0, 8) + '...' : 'active-store'}</span>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                Store / Enterprise Name *
              </label>
              <input
                type="text"
                value={businessName}
                onChange={(e) => setBusinessName(e.target.value)}
                placeholder="e.g. Sharma Kirana & General Store"
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                required
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                  Store Owner Name *
                </label>
                <input
                  type="text"
                  value={ownerName}
                  onChange={(e) => setOwnerName(e.target.value)}
                  placeholder="e.g. Ramesh Sharma"
                  className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                  Contact Phone *
                </label>
                <input
                  type="text"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  placeholder="+91 9810123456"
                  className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                  required
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                Business Type
              </label>
              <select
                value={businessType}
                onChange={(e) => setBusinessType(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              >
                <option value="Kirana / Grocery">Kirana / Grocery Store</option>
                <option value="Supermarket">Supermarket</option>
                <option value="Provision & Staples">Provision & Staples</option>
                <option value="FMCG Retail">FMCG Retail</option>
                <option value="Wholesale & Retail">Wholesale & Retail</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                Store Location / Mandi Address *
              </label>
              <textarea
                rows={2}
                value={address}
                onChange={(e) => setAddress(e.target.value)}
                placeholder="Shop No., Market / Mandi, City, Pincode"
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                required
              />
            </div>

            <div className="pt-3 flex justify-end">
              <button
                type="submit"
                disabled={updateMutation.isPending}
                className="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold rounded-lg shadow-sm transition flex items-center gap-2"
              >
                {updateMutation.isPending && <Loader2 className="w-4 h-4 animate-spin" />}
                Save Store Settings
              </button>
            </div>
          </form>
        </div>

        {/* Right col: System status & Account */}
        <div className="space-y-5">
          {/* Backend Connectivity Status */}
          <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-xs space-y-3">
            <div className="flex items-center gap-2">
              <Server className="w-4 h-4 text-slate-700" />
              <h4 className="font-bold text-sm text-slate-900">API Connection Status</h4>
            </div>

            <div className="p-3 rounded-xl bg-slate-50 border border-slate-200 text-xs space-y-2">
              <div className="flex justify-between items-center">
                <span className="text-slate-500">Service Mode:</span>
                <span
                  className={`font-bold px-2 py-0.5 rounded text-[11px] ${
                    isMock ? 'bg-amber-100 text-amber-800' : 'bg-emerald-100 text-emerald-800'
                  }`}
                >
                  {isMock ? 'Dynamic Store (Active)' : 'Live REST API'}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Base URL:</span>
                <span className="font-mono text-slate-700 truncate max-w-[150px]">
                  {import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api/v1'}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Tenant Store:</span>
                <span className="font-medium text-slate-900">{businessName || 'Kirana Store'}</span>
              </div>
            </div>

            <p className="text-[11px] text-slate-400">
              When the ASP.NET Core backend is active at <code className="bg-slate-100 px-1 rounded">http://localhost:5000</code>, set <code className="bg-slate-100 px-1 rounded">VITE_USE_MOCK=false</code> in <code className="bg-slate-100 px-1 rounded">.env</code>.
            </p>
          </div>

          {/* Account Security & Logout */}
          <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-xs space-y-3">
            <div className="flex items-center gap-2">
              <ShieldCheck className="w-4 h-4 text-slate-700" />
              <h4 className="font-bold text-sm text-slate-900">Account Session</h4>
            </div>
            <p className="text-xs text-slate-500">
              Logged in as <strong className="text-slate-800">{user?.email || 'demo@smartstock.in'}</strong> ({user?.role === 1 ? 'Owner' : 'Staff'}).
            </p>
            <button
              onClick={() => {
                logout();
                window.location.href = '/login';
              }}
              className="w-full py-2 bg-rose-50 hover:bg-rose-100 text-rose-700 font-semibold text-xs rounded-lg transition flex items-center justify-center gap-1.5 border border-rose-200"
            >
              <LogOut className="w-3.5 h-3.5" />
              <span>Log Out of SmartStock</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

export default SettingsPage;
