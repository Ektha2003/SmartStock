import React from 'react';
import { Link } from 'react-router-dom';
import { Store, ArrowLeft } from 'lucide-react';

export function NotFoundPage() {
  return (
    <div className="min-h-screen bg-slate-50 flex flex-col items-center justify-center p-6 text-center">
      <div className="w-16 h-16 rounded-2xl bg-indigo-100 text-indigo-700 flex items-center justify-center mb-4">
        <Store className="w-8 h-8" />
      </div>
      <h1 className="text-4xl font-black text-slate-900">404</h1>
      <h2 className="text-lg font-bold text-slate-800 mt-2">Page Not Found</h2>
      <p className="text-sm text-slate-500 max-w-sm mt-1">
        The SmartStock page or record you are looking for does not exist or has been moved.
      </p>
      <Link
        to="/dashboard"
        className="mt-6 inline-flex items-center gap-2 px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm rounded-lg shadow-sm transition"
      >
        <ArrowLeft className="w-4 h-4" />
        <span>Return to Dashboard</span>
      </Link>
    </div>
  );
}

export default NotFoundPage;

