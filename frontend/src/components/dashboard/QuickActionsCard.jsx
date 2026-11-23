import React from 'react';
import { Link } from 'react-router-dom';
import { PlusCircle, ShoppingCart, Truck, Bell } from 'lucide-react';

export function QuickActionsCard() {
  const actions = [
    {
      label: 'Record Sale',
      to: '/sales/new',
      icon: ShoppingCart,
      color: 'bg-emerald-600 hover:bg-emerald-700 text-white',
      desc: 'Point of sale billing',
    },
    {
      label: 'Add Product',
      to: '/products/new',
      icon: PlusCircle,
      color: 'bg-indigo-600 hover:bg-indigo-700 text-white',
      desc: 'New catalog SKU',
    },
    {
      label: 'Record Purchase',
      to: '/purchases/new',
      icon: Truck,
      color: 'bg-blue-600 hover:bg-blue-700 text-white',
      desc: 'Restock from supplier',
    },
    {
      label: 'View Alerts',
      to: '/notifications',
      icon: Bell,
      color: 'bg-slate-800 hover:bg-slate-900 text-white',
      desc: 'Critical inventory alerts',
    },
  ];

  return (
    <div className="bg-white p-5 rounded-xl border border-slate-200/80 shadow-sm">
      <h3 className="text-base font-bold text-slate-900 mb-3">Quick Actions</h3>
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        {actions.map((act) => {
          const Icon = act.icon;
          return (
            <Link
              key={act.label}
              to={act.to}
              className={`p-3.5 rounded-xl ${act.color} transition shadow-sm flex flex-col justify-between`}
            >
              <Icon className="w-5 h-5 mb-2" />
              <div>
                <p className="font-semibold text-sm leading-tight">{act.label}</p>
                <p className="text-[11px] opacity-80 mt-0.5">{act.desc}</p>
              </div>
            </Link>
          );
        })}
      </div>
    </div>
  );
}

export default QuickActionsCard;

