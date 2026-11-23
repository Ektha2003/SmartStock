import React from 'react';
import { Loader2 } from 'lucide-react';
import { cn } from '../../utils/cn';

export function LoadingSpinner({ size = 'md', text, className }) {
  const sizes = {
    sm: 'w-4 h-4',
    md: 'w-6 h-6',
    lg: 'w-8 h-8',
    xl: 'w-10 h-10',
  };

  return (
    <div className={cn('flex flex-col items-center justify-center gap-2', className)}>
      <Loader2 className={cn('animate-spin text-indigo-600', sizes[size] || sizes.md)} />
      {text && <p className="text-xs font-medium text-slate-500">{text}</p>}
    </div>
  );
}

export default LoadingSpinner;

