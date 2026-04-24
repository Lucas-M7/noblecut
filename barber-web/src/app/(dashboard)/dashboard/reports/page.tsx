'use client'

import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import { api } from '@/src/lib/api'
import { ReportSummary } from '@/src/types'
import { Card } from '@/src/components/ui/Card'

// Períodos disponíveis para seleção
const PERIODS = [
  { value: 'this-month',    label: 'Este mês' },
  { value: 'last-month',    label: 'Mês passado' },
  { value: 'last-3-months', label: 'Últimos 3 meses' },
  { value: 'this-year',     label: 'Este ano' },
]

function formatCurrency(value: number): string {
  if (value === 0) return '—'
  return 'R$ ' + value.toFixed(2).replace('.', ',')
}

// Exibe a variação percentual em relação ao período anterior
// ex: +12,5% ou -8,3%
function ChangeIndicator({ percent }: { percent: number | null }) {
  if (percent === null) return null

  const isPositive = percent >= 0
  const formatted = Math.abs(percent).toFixed(1).replace('.', ',') + '%'

  return (
    <span className={`text-xs font-medium px-1.5 py-0.5 rounded-full ${
      isPositive
        ? 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300'
        : 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300'
    }`}>
      {isPositive ? '▲' : '▼'} {formatted}
    </span>
  )
}

export default function ReportsPage() {
  const [period, setPeriod] = useState('this-month')
  const [report, setReport] = useState<ReportSummary | null>(null)
  const [loading, setLoading] = useState(true)

  // Recarrega sempre que o período muda
  useEffect(() => {
    async function load() {
      setLoading(true)
      try {
        const data = await api.get<ReportSummary>(`/api/reports/summary?period=${period}`)
        setReport(data)
      } catch {
        toast.error('Erro ao carregar relatório.')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [period])

  // Calcula o valor máximo para normalizar as barras do gráfico
  const maxRevenue = report
    ? Math.max(...report.chartData.map(d => d.revenue), 1)
    : 1

  return (
    <div className="flex flex-col gap-4 md:gap-6">

      {/* ── Cabeçalho + seletor de período ───────────────────────────── */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl md:text-2xl font-bold text-zinc-900 dark:text-zinc-100">
            Relatório
          </h1>
          <p className="text-zinc-500 dark:text-zinc-400 text-xs md:text-sm mt-1">
            {report?.periodLabel ?? 'Carregando...'}
          </p>
        </div>

        {/* Seletor de período */}
        <div className="flex flex-wrap gap-2">
          {PERIODS.map((p) => (
            <button
              key={p.value}
              onClick={() => setPeriod(p.value)}
              className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                period === p.value
                  ? 'bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900'
                  : 'bg-zinc-100 text-zinc-600 hover:bg-zinc-200 dark:bg-zinc-800 dark:text-zinc-400 dark:hover:bg-zinc-700'
              }`}
            >
              {p.label}
            </button>
          ))}
        </div>
      </div>

      {loading ? (
        <p className="text-zinc-500 dark:text-zinc-400 text-sm">Carregando...</p>
      ) : !report ? null : (
        <>
          {/* ── Cards de resumo ────────────────────────────────────────── */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 md:gap-4">

            {/* Hoje */}
            <Card>
              <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400">Hoje</p>
              <p className="text-2xl md:text-3xl font-bold text-emerald-600 dark:text-emerald-400 mt-1">
                {formatCurrency(report.today.revenue)}
              </p>
              <p className="text-xs text-zinc-400 dark:text-zinc-500 mt-1">
                {report.today.appointments} atendimento{report.today.appointments !== 1 ? 's' : ''}
              </p>
            </Card>

            {/* Período selecionado — faturamento */}
            <Card>
              <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400">
                Faturamento · {PERIODS.find(p => p.value === period)?.label}
              </p>
              <div className="flex items-end gap-2 mt-1">
                <p className="text-2xl md:text-3xl font-bold text-emerald-600 dark:text-emerald-400">
                  {formatCurrency(report.selectedPeriod.revenue)}
                </p>
                <div className="mb-1">
                  <ChangeIndicator percent={report.selectedPeriod.revenueChangePercent} />
                </div>
              </div>
              {report.selectedPeriod.previousRevenue !== null && (
                <p className="text-xs text-zinc-400 dark:text-zinc-500 mt-1">
                  Período anterior: {formatCurrency(report.selectedPeriod.previousRevenue)}
                </p>
              )}
            </Card>

            {/* Período selecionado — atendimentos */}
            <Card>
              <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400">
                Atendimentos · {PERIODS.find(p => p.value === period)?.label}
              </p>
              <div className="flex items-end gap-2 mt-1">
                <p className="text-2xl md:text-3xl font-bold text-zinc-900 dark:text-zinc-100">
                  {report.selectedPeriod.appointments}
                </p>
                {report.selectedPeriod.previousAppointments !== null && (
                  <div className="mb-1">
                    <ChangeIndicator
                      percent={
                        report.selectedPeriod.previousAppointments === 0
                          ? null
                          : Math.round(
                              (report.selectedPeriod.appointments -
                                report.selectedPeriod.previousAppointments) /
                                report.selectedPeriod.previousAppointments * 100 * 10
                            ) / 10
                      }
                    />
                  </div>
                )}
              </div>
              {report.selectedPeriod.previousAppointments !== null && (
                <p className="text-xs text-zinc-400 dark:text-zinc-500 mt-1">
                  Período anterior: {report.selectedPeriod.previousAppointments} atendimento{report.selectedPeriod.previousAppointments !== 1 ? 's' : ''}
                </p>
              )}
            </Card>
          </div>

          {/* ── Destaques do período ───────────────────────────────────── */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 md:gap-4">
            <Card>
              <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400 mb-2">
                Serviço mais popular · {PERIODS.find(p => p.value === period)?.label}
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
                  Nenhum atendimento concluído no período
                </p>
              )}
            </Card>

            <Card>
              <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400 mb-2">
                Melhor dia da semana · {PERIODS.find(p => p.value === period)?.label}
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
                  Nenhum atendimento concluído no período
                </p>
              )}
            </Card>
          </div>

          {/* ── Gráfico ───────────────────────────────────────────────── */}
          <Card>
            <p className="text-sm font-semibold text-zinc-900 dark:text-zinc-100 mb-6">
              Faturamento por {period === 'last-3-months' || period === 'this-year' ? 'mês' : 'dia'}
            </p>

            {report.chartData.every(d => d.revenue === 0) ? (
              <p className="text-sm text-zinc-400 dark:text-zinc-500">
                Nenhum atendimento concluído no período.
              </p>
            ) : (
              <>
                <div className="flex items-end gap-0.5 h-32 w-full">
                  {report.chartData.map((day) => {
                    const heightPercent = (day.revenue / maxRevenue) * 100

                    return (
                      <div
                        key={day.date}
                        className="flex-1 flex flex-col items-center justify-end group relative"
                      >
                        {/* Tooltip */}
                        {day.revenue > 0 && (
                          <div className="
                            absolute bottom-full mb-1 left-1/2 -translate-x-1/2
                            bg-zinc-900 dark:bg-zinc-100
                            text-white dark:text-zinc-900
                            text-xs rounded px-2 py-1
                            whitespace-nowrap
                            opacity-0 group-hover:opacity-100
                            transition-opacity pointer-events-none z-10
                          ">
                            {day.label}
                            <br />
                            {formatCurrency(day.revenue)}
                            <br />
                            {day.appointments} atend.
                          </div>
                        )}

                        {/* Barra */}
                        <div
                          className={`w-full rounded-t transition-all ${
                            day.revenue > 0
                              ? 'bg-emerald-500 dark:bg-emerald-400 group-hover:bg-emerald-400 dark:group-hover:bg-emerald-300'
                              : 'bg-zinc-100 dark:bg-zinc-800'
                          }`}
                          style={{
                            height: `${Math.max(heightPercent, day.revenue > 0 ? 4 : 2)}%`
                          }}
                        />
                      </div>
                    )
                  })}
                </div>

                {/* Eixo X: mostra labels distribuídas */}
                <div className="flex justify-between mt-2">
                  {(() => {
                    const total = report.chartData.length
                    // Mostra no máximo 5 labels no eixo X
                    const indices = total <= 5
                      ? report.chartData.map((_, i) => i)
                      : [0,
                          Math.floor(total * 0.25),
                          Math.floor(total * 0.5),
                          Math.floor(total * 0.75),
                          total - 1]

                    return indices.map(i => (
                      <span
                        key={i}
                        className="text-xs text-zinc-400 dark:text-zinc-500"
                      >
                        {report.chartData[i].label}
                      </span>
                    ))
                  })()}
                </div>
              </>
            )}
          </Card>
        </>
      )}
    </div>
  )
}