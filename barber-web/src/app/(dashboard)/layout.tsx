'use client'

import { useEffect, useState } from 'react'
import { useRouter, usePathname } from 'next/navigation'
import Link from 'next/link'
import { useAuth } from '@/src/contexts/AuthContext'
import { useTheme } from '@/src/contexts/ThemeContext'

const navItems = [
  { href: '/dashboard', label: 'Início', icon: '🏠' },
  { href: '/dashboard/appointments', label: 'Agendamentos', icon: '📅' },
  { href: '/dashboard/reports', label: 'Relatório', icon: '📊' },
  { href: '/dashboard/services', label: 'Serviços', icon: '✂️' },
  { href: '/dashboard/hours', label: 'Horários', icon: '🕐' },
  { href: '/dashboard/blocks', label: 'Bloqueios', icon: '🚫' },
  { href: '/dashboard/special-hours', label: 'Dias especiais', icon: '📆' },
  { href: '/dashboard/profile', label: 'Meu Perfil', icon: '👤' },
]

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, userName, logout } = useAuth()
  const { isDark, toggle } = useTheme()
  const router = useRouter()
  const pathname = usePathname()
  const [menuOpen, setMenuOpen] = useState(false)

  useEffect(() => {
    if (!isAuthenticated) {
      const token = localStorage.getItem('token')
      if (!token) router.push('/login')
    }
  }, [isAuthenticated, router])

  // Fecha o menu ao navegar
  useEffect(() => {
    setMenuOpen(false)
  }, [pathname])

  const NavLinks = () => (
    <>
      {navItems.map((item) => {
        const isActive = pathname === item.href
        return (
          <Link
            key={item.href}
            href={item.href}
            className={`
              flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors
              ${isActive
                ? 'bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900 font-medium'
                : 'text-zinc-600 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:bg-zinc-800'
              }
            `}
          >
            <span>{item.icon}</span>
            {item.label}
          </Link>
        )
      })}
    </>
  )

  return (
    <div className="flex h-screen bg-zinc-50 dark:bg-zinc-950 overflow-hidden">

      {/* Sidebar — visível apenas em desktop */}
      <aside className="hidden md:flex w-60 bg-white border-r border-zinc-200 dark:bg-zinc-900 dark:border-zinc-700 flex-col shrink-0">
        <div className="p-6 border-b border-zinc-200 dark:border-zinc-700">
          <h1 className="text-lg font-bold text-zinc-900 dark:text-zinc-100">✂️ Noblecut</h1>
          <p className="text-xs text-zinc-500 dark:text-zinc-400 mt-1 truncate">{userName}</p>
        </div>

        <nav className="flex-1 p-4 flex flex-col gap-1 overflow-y-auto">
          <NavLinks />
        </nav>

        <div className="p-4 border-t border-zinc-200 dark:border-zinc-700 flex flex-col gap-2">
          <button
            onClick={toggle}
            className="w-full text-left px-3 py-2 text-sm text-zinc-500 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:bg-zinc-800 rounded-lg transition-colors"
          >
            {isDark ? '☀️ Tema claro' : '🌙 Tema escuro'}
          </button>
          <button
            onClick={logout}
            className="w-full text-left px-3 py-2 text-sm text-zinc-500 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950 dark:hover:text-red-400 rounded-lg transition-colors"
          >
            🚪 Sair
          </button>
        </div>
      </aside>

      {/* Área principal */}
      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">

        {/* Header mobile */}
        <header className="md:hidden flex items-center justify-between px-4 py-3 bg-white dark:bg-zinc-900 border-b border-zinc-200 dark:border-zinc-700 shrink-0">
          <h1 className="text-base font-bold text-zinc-900 dark:text-zinc-100">✂️ Noblecut</h1>
          <button
            onClick={() => setMenuOpen(!menuOpen)}
            className="p-2 rounded-lg text-zinc-600 dark:text-zinc-400 hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors"
            aria-label="Abrir menu"
          >
            {menuOpen ? (
              // X
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            ) : (
              // Hambúrguer
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
              </svg>
            )}
          </button>
        </header>

        {/* Menu mobile (dropdown) */}
        {menuOpen && (
          <div className="md:hidden bg-white dark:bg-zinc-900 border-b border-zinc-200 dark:border-zinc-700 px-4 py-3 flex flex-col gap-1 shrink-0">
            <NavLinks />
            <div className="border-t border-zinc-100 dark:border-zinc-800 mt-2 pt-2 flex flex-col gap-1">
              <button
                onClick={toggle}
                className="w-full text-left px-3 py-2 text-sm text-zinc-500 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:bg-zinc-800 rounded-lg transition-colors"
              >
                {isDark ? '☀️ Tema claro' : '🌙 Tema escuro'}
              </button>
              <button
                onClick={logout}
                className="w-full text-left px-3 py-2 text-sm text-zinc-500 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950 dark:hover:text-red-400 rounded-lg transition-colors"
              >
                🚪 Sair
              </button>
            </div>
          </div>
        )}

        {/* Conteúdo */}
        <main className="flex-1 overflow-y-auto p-4 md:p-8">
          {children}
        </main>
      </div>
    </div>
  )
}