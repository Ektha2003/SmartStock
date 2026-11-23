import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Eye, EyeOff, Lock, Mail, Loader2, Sparkles, AlertCircle } from 'lucide-react';
import { loginSchema } from '../../validations/authSchemas';
import useAuth from '../../hooks/useAuth';
import { toast } from 'sonner';

export function LoginPage() {
  const [showPassword, setShowPassword] = useState(false);
  const [apiError, setApiError] = useState('');
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const from = location.state?.from?.pathname || '/dashboard';

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
      rememberMe: true,
    },
  });

  const onSubmit = async (data) => {
    try {
      setApiError('');
      await login(data);
      toast.success('Welcome back to SmartStock!');
      navigate(from, { replace: true });
    } catch (err) {
      const errorMsg = err.message || 'Login failed. Please verify your credentials.';
      setApiError(errorMsg);
      toast.error(errorMsg);
    }
  };

  const fillDemoCredentials = () => {
    setValue('email', 'demo@smartstock.in');
    setValue('password', 'demo1234');
    setApiError('');
  };

  return (
    <div>
      <div className="mb-6">
        <h3 className="text-xl font-bold text-slate-900">Sign in to your store</h3>
        <p className="text-xs text-slate-500 mt-1">
          Manage inventory, sales, and AI stock forecasting.
        </p>
      </div>

      {apiError && (
        <div
          role="alert"
          className="mb-5 p-3.5 rounded-lg bg-rose-50 border border-rose-200 text-rose-800 text-xs flex items-start gap-2.5 shadow-sm animate-in fade-in duration-200"
        >
          <AlertCircle className="w-4 h-4 text-rose-600 shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="font-semibold text-rose-900">Unable to Sign In</p>
            <p className="mt-0.5 text-rose-700 leading-relaxed">{apiError}</p>
          </div>
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
            Email address
          </label>
          <div className="relative">
            <Mail className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="email"
              {...register('email')}
              placeholder="sharma.kirana@gmail.com"
              className="w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-200 rounded-lg placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 transition"
            />
          </div>
          {errors.email && (
            <p className="text-xs text-rose-600 mt-1">{errors.email.message}</p>
          )}
        </div>

        <div>
          <div className="flex items-center justify-between mb-1.5">
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider">
              Password
            </label>
          </div>
          <div className="relative">
            <Lock className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type={showPassword ? 'text' : 'password'}
              {...register('password')}
              placeholder="••••••••"
              className="w-full pl-9 pr-10 py-2 text-sm bg-white border border-slate-200 rounded-lg placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 transition"
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
            >
              {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
            </button>
          </div>
          {errors.password && (
            <p className="text-xs text-rose-600 mt-1">{errors.password.message}</p>
          )}
        </div>

        <div className="flex items-center justify-between pt-1">
          <label className="flex items-center gap-2 cursor-pointer text-xs text-slate-600">
            <input
              type="checkbox"
              {...register('rememberMe')}
              className="rounded border-slate-300 text-indigo-600 focus:ring-indigo-500"
            />
            <span>Remember me</span>
          </label>
        </div>

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full py-2.5 px-4 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold rounded-lg shadow-sm transition flex items-center justify-center gap-2 disabled:opacity-50"
        >
          {isSubmitting ? (
            <>
              <Loader2 className="w-4 h-4 animate-spin" /> Signing in...
            </>
          ) : (
            'Sign In'
          )}
        </button>

        {/* Quick Demo Credentials shortcut */}
        <div className="pt-2">
          <button
            type="button"
            onClick={fillDemoCredentials}
            className="w-full py-2 px-3 border border-indigo-200 bg-indigo-50/70 hover:bg-indigo-100/70 text-indigo-700 text-xs font-semibold rounded-lg transition flex items-center justify-center gap-1.5"
          >
            <Sparkles className="w-3.5 h-3.5" />
            <span>Use Demo Kirana Credentials (1-Click)</span>
          </button>
        </div>
      </form>

      <div className="mt-6 text-center text-xs text-slate-500">
        Don&apos;t have a SmartStock account?{' '}
        <Link to="/register" className="font-semibold text-indigo-600 hover:underline">
          Register Store
        </Link>
      </div>
    </div>
  );
}

export default LoginPage;

