import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Eye, EyeOff, User, Store, Mail, Phone, MapPin, Lock, Loader2, AlertCircle } from 'lucide-react';
import { registerSchema } from '../../validations/authSchemas';
import useAuth from '../../hooks/useAuth';
import { toast } from 'sonner';

export function RegisterPage() {
  const [showPassword, setShowPassword] = useState(false);
  const [apiError, setApiError] = useState('');
  const { register: registerAuth } = useAuth();
  const navigate = useNavigate();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      fullName: '',
      businessName: '',
      email: '',
      phone: '',
      address: '',
      password: '',
      confirmPassword: '',
    },
  });

  const onSubmit = async (data) => {
    try {
      setApiError('');
      await registerAuth(data);
      toast.success('Store registered successfully! Welcome aboard.');
      navigate('/dashboard', { replace: true });
    } catch (err) {
      const errorMsg = err.message || 'Registration failed. Please try again.';
      setApiError(errorMsg);
      toast.error(errorMsg);
    }
  };

  return (
    <div>
      <div className="mb-5">
        <h3 className="text-xl font-bold text-slate-900">Register your Kirana Store</h3>
        <p className="text-xs text-slate-500 mt-1">
          Set up smart inventory and AI assistant for your retail business.
        </p>
      </div>

      {apiError && (
        <div
          role="alert"
          className="mb-5 p-3.5 rounded-lg bg-rose-50 border border-rose-200 text-rose-800 text-xs flex items-start gap-2.5 shadow-sm animate-in fade-in duration-200"
        >
          <AlertCircle className="w-4 h-4 text-rose-600 shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="font-semibold text-rose-900">Registration Failed</p>
            <p className="mt-0.5 text-rose-700 leading-relaxed">{apiError}</p>
          </div>
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3.5">
        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
            Store Owner Full Name *
          </label>
          <div className="relative">
            <User className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              {...register('fullName')}
              placeholder="e.g. Ramesh Kumar"
              className="w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-200 rounded-lg placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 transition"
            />
          </div>
          {errors.fullName && <p className="text-xs text-rose-600 mt-1">{errors.fullName.message}</p>}
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
            Business / Store Name *
          </label>
          <div className="relative">
            <Store className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              {...register('businessName')}
              placeholder="e.g. Kumar Provision & Kirana Store"
              className="w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-200 rounded-lg placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 transition"
            />
          </div>
          {errors.businessName && <p className="text-xs text-rose-600 mt-1">{errors.businessName.message}</p>}
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Email *
            </label>
            <div className="relative">
              <Mail className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type="email"
                {...register('email')}
                placeholder="store@gmail.com"
                className="w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-200 rounded-lg placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 transition"
              />
            </div>
            {errors.email && <p className="text-xs text-rose-600 mt-1">{errors.email.message}</p>}
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Phone Number *
            </label>
            <div className="relative">
              <Phone className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                {...register('phone')}
                placeholder="9876543210"
                className="w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-200 rounded-lg placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 transition"
              />
            </div>
            {errors.phone && <p className="text-xs text-rose-600 mt-1">{errors.phone.message}</p>}
          </div>
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
            Store Address / Location *
          </label>
          <div className="relative">
            <MapPin className="w-4 h-4 text-slate-400 absolute left-3 top-3" />
            <textarea
              rows={2}
              {...register('address')}
              placeholder="Shop No., Market, City, Pincode"
              className="w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-200 rounded-lg placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 transition"
            />
          </div>
          {errors.address && <p className="text-xs text-rose-600 mt-1">{errors.address.message}</p>}
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Password *
            </label>
            <div className="relative">
              <Lock className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type={showPassword ? 'text' : 'password'}
                {...register('password')}
                placeholder="••••••"
                className="w-full pl-9 pr-8 py-2 text-sm bg-white border border-slate-200 rounded-lg placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 transition"
              />
            </div>
            {errors.password && <p className="text-xs text-rose-600 mt-1">{errors.password.message}</p>}
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Confirm Password *
            </label>
            <div className="relative">
              <Lock className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type={showPassword ? 'text' : 'password'}
                {...register('confirmPassword')}
                placeholder="••••••"
                className="w-full pl-9 pr-8 py-2 text-sm bg-white border border-slate-200 rounded-lg placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 transition"
              />
            </div>
            {errors.confirmPassword && <p className="text-xs text-rose-600 mt-1">{errors.confirmPassword.message}</p>}
          </div>
        </div>

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full mt-2 py-2.5 px-4 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold rounded-lg shadow-sm transition flex items-center justify-center gap-2 disabled:opacity-50"
        >
          {isSubmitting ? (
            <>
              <Loader2 className="w-4 h-4 animate-spin" /> Registering Store...
            </>
          ) : (
            'Complete Registration'
          )}
        </button>
      </form>

      <div className="mt-5 text-center text-xs text-slate-500">
        Already have an account?{' '}
        <Link to="/login" className="font-semibold text-indigo-600 hover:underline">
          Sign In
        </Link>
      </div>
    </div>
  );
}

export default RegisterPage;

