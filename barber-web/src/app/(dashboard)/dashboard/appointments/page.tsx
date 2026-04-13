'use client'

import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import { api } from '@/src/lib/api'
import { Appointment } from '@/src/types'
import { Button } from '@/src/components/ui/Button'
import { Card } from '@/src/components/ui/Card'
import { Badge } from '@/src/components/ui/Badge'
import { getLocalToday } from '@/src/lib/date'

export default function AppointmentsPage() {
  const [appointments, setAppointments] = useState<Appointment[]>([])
  const [loading, setLoading] = useState(true)
  const [filterDate, setFilterDate] = useState(getLocalToday())

  useEffect(() => { loadAppointments() }, [filterDate])

  async function loadAppointments() {
    setLoading(true)
    try {
      const data = await api.get<Appointment[]>(
        `/api/appointments${filterDate ? `?date=${filterDate}` : ''}`
      )
      setAppointments(data)
    } catch {
      toast.error('Erro ao carregar agendamentos.')
    } finally {
      setLoading(false)
    }
  }

  async function handleCancel(id: string) {
    if (!confirm('Cancelar este agendamento?')) return
    try {
      await api.patch(`/api/appointments/${id}/cancel`)
      toast.success('Agendamento cancelado.')
      loadAppointments()
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Erro ao cancelar.'
      toast.error(message)
    }
  }

  async function handleComplete(id: string) {
    try {
      await api.patch(`/api/appointments/${id}/complete`)
      toast.success('Agendamento concluído!')
      loadAppointments()
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Erro ao concluir.'
      toast.error(message)
    }
  }

  function formatDate(d: string) {
    const [y, m, day] = d.split('-')
    return `${day}/${m}/${y}`
  }

  return (
    <div className="flex flex-col gap-4 md:gap-6">
      <div>
        <h1 className="text-xl md:text-2xl font-bold text-zinc-900 dark:text-zinc-100">Agendamentos</h1>
        <p className="text-zinc-500 dark:text-zinc-400 text-xs md:text-sm mt-1">Visualize e gerencie sua agenda</p>
      </div>

      <Card>
        <div className="flex flex-col sm:flex-row sm:items-center gap-3">
          <label className="text-sm font-medium text-zinc-700 dark:text-zinc-300 shrink-0">
            Filtrar por data:
          </label>
          <input
            type="date"
            value={filterDate}
            onChange={(e) => setFilterDate(e.target.value)}
            className="border border-zinc-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-zinc-900 dark:bg-zinc-800 dark:border-zinc-600 dark:text-zinc-100 dark:focus:ring-zinc-400 w-full sm:w-auto"
          />
          <Button variant="secondary" onClick={() => setFilterDate('')} className="w-full sm:w-auto">
            Ver todos
          </Button>
        </div>
      </Card>

      {loading ? (
        <p className="text-zinc-500 dark:text-zinc-400 text-sm">Carregando...</p>
      ) : appointments.length === 0 ? (
        <Card>
          <p className="text-sm text-zinc-400 dark:text-zinc-500">Nenhum agendamento encontrado.</p>
        </Card>
      ) : (
        <div className="flex flex-col gap-3">
          {appointments.map((a) => (
            <Card key={a.id} className="p-4 md:p-6">
              <div className="flex flex-col gap-3">
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-center gap-2 flex-wrap">
                    <p className="font-medium text-zinc-900 dark:text-zinc-100">{a.clientName}</p>
                    <Badge status={a.status} />
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-1">
                  <p className="text-sm text-zinc-500 dark:text-zinc-400">📱 {a.clientPhone}</p>
                  <p className="text-sm text-zinc-500 dark:text-zinc-400">
                    ✂️ {a.serviceName} · {a.serviceDuration} min
                    {a.servicePrice ? ` · R$ ${a.servicePrice.toFixed(2).replace('.', ',')}` : ''}
                  </p>
                  <p className="text-sm text-zinc-500 dark:text-zinc-400">
                    📅 {formatDate(a.appointmentDate)} · {a.startTime} - {a.endTime}
                  </p>
                </div>

                {a.status === 'Scheduled' && (
                  <div className="flex flex-col sm:flex-row gap-2 pt-1">
                    <Button onClick={() => handleComplete(a.id)} className="w-full sm:w-auto">
                      Concluir
                    </Button>
                    <Button variant="danger" onClick={() => handleCancel(a.id)} className="w-full sm:w-auto">
                      Cancelar
                    </Button>
                  </div>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}