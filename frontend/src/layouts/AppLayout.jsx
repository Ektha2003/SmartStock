import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import AppSidebar from '../components/layout/AppSidebar';
import AppHeader from '../components/layout/AppHeader';

export function AppLayout() {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  return (
    <div className="min-h-screen bg-[#F8FAFC] flex">
      {/* Sidebar */}
      <AppSidebar isOpen={mobileMenuOpen} onClose={() => setMobileMenuOpen(false)} />

      {/* Main content container */}
      <div className="flex-1 flex flex-col min-w-0 min-h-screen">
        <AppHeader onMenuClick={() => setMobileMenuOpen(true)} />

        <main className="flex-1 p-4 sm:p-6 lg:p-8 max-w-7xl w-full mx-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

export default AppLayout;

