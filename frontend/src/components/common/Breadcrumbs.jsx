import React from 'react';
import { Link } from 'react-router-dom';
import { ChevronRight, Home } from 'lucide-react';
import { cn } from '../../utils/cn';

export function Breadcrumbs({ items = [], className }) {
  if (!items || items.length === 0) return null;

  return (
    <nav className={cn('flex items-center text-xs text-slate-500 mb-3', className)}>
      <Link to="/dashboard" className="flex items-center hover:text-indigo-600 transition">
        <Home className="w-3.5 h-3.5 mr-1" />
        <span>Home</span>
      </Link>
      {items.map((item, idx) => (
        <React.Fragment key={idx}>
          <ChevronRight className="w-3.5 h-3.5 mx-1.5 text-slate-400" />
          {item.to ? (
            <Link to={item.to} className="hover:text-indigo-600 transition">
              {item.label}
            </Link>
          ) : (
            <span className="text-slate-800 font-medium">{item.label}</span>
          )}
        </React.Fragment>
      ))}
    </nav>
  );
}

export default Breadcrumbs;

