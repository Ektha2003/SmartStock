import React from 'react';
import { cn } from '../../utils/cn';
import Breadcrumbs from './Breadcrumbs';

export function PageHeader({
  title,
  subtitle,
  breadcrumbs = [],
  action,
  className,
}) {
  return (
    <div className={cn('mb-6', className)}>
      {breadcrumbs.length > 0 && <Breadcrumbs items={breadcrumbs} />}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">{title}</h1>
          {subtitle && <p className="text-sm text-slate-500 mt-1">{subtitle}</p>}
        </div>
        {action && <div className="flex items-center gap-3">{action}</div>}
      </div>
    </div>
  );
}

export default PageHeader;

