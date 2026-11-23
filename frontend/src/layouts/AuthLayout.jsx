import React from 'react';
import { Outlet } from 'react-router-dom';
import { Store, ShieldCheck, Sparkles, TrendingUp } from 'lucide-react';

export function AuthLayout() {
  return (
    <div className="min-h-screen bg-slate-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div className="sm:mx-auto sm:w-full sm:max-w-md text-center">
        <div className="inline-flex items-center justify-center w-12 h-12 rounded-2xl bg-indigo-700 text-white shadow-lg shadow-indigo-600/30 mb-3">
          <Store className="w-7 h-7" />
        </div>
        <h2 className="text-2xl font-bold tracking-tight text-slate-900">
          SmartStock
        </h2>
        <p className="mt-1 text-sm text-slate-500">
          AI-Powered Kirana Inventory & Assistant
        </p>
      </div>

      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md px-4 sm:px-0">
        <div className="bg-white py-8 px-6 shadow-sm border border-slate-200/80 rounded-2xl sm:px-10">
          <Outlet />
        </div>

        {/* Value props badge list */}
        <div className="mt-6 grid grid-cols-3 gap-2 text-center text-[11px] text-slate-500 font-medium">
          <div className="flex flex-col items-center gap-1">
            <Sparkles className="w-4 h-4 text-indigo-600" />
            <span>AI Forecasting</span>
          </div>
          <div className="flex flex-col items-center gap-1">
            <TrendingUp className="w-4 h-4 text-emerald-600" />
            <span>Smart Reordering</span>
          </div>
          <div className="flex flex-col items-center gap-1">
            <ShieldCheck className="w-4 h-4 text-blue-600" />
            <span>Real-time POS</span>
          </div>
        </div>
      </div>
    </div>
  );
}

export default AuthLayout;

