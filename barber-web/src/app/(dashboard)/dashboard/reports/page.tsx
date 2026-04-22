'use client'

import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import { api } from '@/src/lib/api'
import { ReportSummary } from '@/src/types'
import { Card } from '@/src/components/ui/Card'

function formatCurrency(value: number): string {
  if (value === 0) return '—'
  return 'R$ ' + value.toFixed(2).replace('.', ',')
}

function formatDate(d: string): string {
  const [, m, day] = d.split('-')
  return `${day}/${m}`
}

export default function ReportsPage() {
  const [report, setReport] = useState<ReportSummary | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function load() {
      try {
        const data = await api.get<ReportSummary>('/api/reports/summary')
        setReport(data)
      } catch {
        toast.error('Erro ao carregar relatório.')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  if (loading) return <p className="text-zinc-500 dark:text-zinc-400">Carregando...</p>
  if (!report) return null

  // Calcula o valor máximo para normalizar a altura das barras do gráfico
  const maxRevenue = Math.max(...report.last30Days.map(d => d.revenue), 1)

  return (
    <div className="flex flex-col gap-4 md:gap-6">
      <div>
        <h1 className="text-xl md:text-2xl font-bold text-zinc-900 dark:text-zinc-100">
          Relatório
        </h1>
        <p className="text-zinc-500 dark:text-zinc-400 text-xs md:text-sm mt-1">
          Resumo do seu desempenho financeiro
        </p>
      </div>

      {/* ── Cards de período ──────────────────────────────────────────── */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 md:gap-4">
        <Card>
          <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400">Hoje</p>
          <p className="text-2xl md:text-3xl font-bold text-emerald-600 dark:text-emerald-400 mt-1">
            {formatCurrency(report.today.revenue)}
          </p>
          <p className="text-xs text-zinc-400 dark:text-zinc-500 mt-1">
            {report.today.appointments} atendimento{report.today.appointments !== 1 ? 's' : ''}
          </p>
        </Card>

        <Card>
          <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400">Esta semana</p>
          <p className="text-2xl md:text-3xl font-bold text-emerald-600 dark:text-emerald-400 mt-1">
            {formatCurrency(report.thisWeek.revenue)}
          </p>
          <p className="text-xs text-zinc-400 dark:text-zinc-500 mt-1">
            {report.thisWeek.appointments} atendimento{report.thisWeek.appointments !== 1 ? 's' : ''}
          </p>
        </Card>

        <Card>
          <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400">Este mês</p>
          <p className="text-2xl md:text-3xl font-bold text-emerald-600 dark:text-emerald-400 mt-1">
            {formatCurrency(report.thisMonth.revenue)}
          </p>
          <p className="text-xs text-zinc-400 dark:text-zinc-500 mt-1">
            {report.thisMonth.appointments} atendimento{report.thisMonth.appointments !== 1 ? 's' : ''}
          </p>
        </Card>
      </div>

      {/* ── Destaques do mês ──────────────────────────────────────────── */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 md:gap-4">
        <Card>
          <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400 mb-2">
            Serviço mais popular este mês
          </p>
          {report.mostPopularService ? (
            <div className="flex items-center gap-2">
              <span className="text-2xl">✂️</span>
              <p className="font-semibold text-zinc-900 dark:text-zinc-100">
                {report.mostPopularService}
              </p>
            </div>
          ) : (
            <p className="text-sm text-zinc-400 dark:text-zinc-500">
              Nenhum atendimento concluído ainda
            </p>
          )}
        </Card>

        <Card>
          <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400 mb-2">
            Melhor dia da semana este mês
          </p>
          {report.bestDayOfWeek ? (
            <div className="flex items-center gap-2">
              <span className="text-2xl">📅</span>
              <p className="font-semibold text-zinc-900 dark:text-zinc-100">
                {report.bestDayOfWeek}
              </p>
            </div>
          ) : (
            <p className="text-sm text-zinc-400 dark:text-zinc-500">
              Nenhum atendimento concluído ainda
            </p>
          )}
        </Card>
      </div>

      {/* ── Gráfico: últimos 30 dias ──────────────────────────────────── */}
      <Card>
        <p className="text-sm font-semibold text-zinc-900 dark:text-zinc-100 mb-6">
          Faturamento — últimos 30 dias
        </p>

        {report.last30Days.every(d => d.revenue === 0) ? (
          <p className="text-sm text-zinc-400 dark:text-zinc-500">
            Nenhum atendimento concluído nos últimos 30 dias.
          </p>
        ) : (
          <>
            {/* Barras do gráfico */}
            <div className="flex items-end gap-0.5 h-32 w-full">
              {report.last30Days.map((day) => {
                // Altura proporcional ao valor máximo do período
                const heightPercent = maxRevenue > 0
                  ? (day.revenue / maxRevenue) * 100
                  : 0

                return (
                  <div
                    key={day.date}
                    className="flex-1 flex flex-col items-center justify-end group relative"
                  >
                    {/* Tooltip ao passar o mouse */}
                    {day.revenue > 0 && (
                      <div className="
                        absolute bottom-full mb-1 left-1/2 -translate-x-1/2
                        bg-zinc-900 dark:bg-zinc-100
                        text-white dark:text-zinc-900
                        text-xs rounded px-2 py-1
                        whitespace-nowrap
                        opacity-0 group-hover:opacity-100
                        transition-opacity pointer-events-none
                        z-10
                      ">
                        {formatDate(day.date)}
                        <br />
                        {formatCurrency(day.revenue)}
                        <br />
                        {day.appointments} atend.
                      </div>
                    )}

                    {/* Barra */}
                    <div
                      className={`
                        w-full rounded-t transition-all
                        ${day.revenue > 0
                          ? 'bg-emerald-500 dark:bg-emerald-400 group-hover:bg-emerald-400 dark:group-hover:bg-emerald-300'
                          : 'bg-zinc-100 dark:bg-zinc-800'
                        }
                      `}
                      style={{ height: `${Math.max(heightPercent, day.revenue > 0 ? 4 : 2)}%` }}
                    />
                  </div>
                )
              })}
            </div>

            {/* Eixo X: mostra só algumas datas para não poluir */}
            <div className="flex justify-between mt-2">
              {[0, 9, 19, 29].map(i => (
                <span
                  key={i}
                  className="text-xs text-zinc-400 dark:text-zinc-500"
                >
                  {formatDate(report.last30Days[i].date)}
                </span>
              ))}
            </div>
          </>
        )}
      </Card>
    </div>
  )
}