"use client";

import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { api } from "@/src/lib/api";
import { Appointment } from "@/src/types";
import { Button } from "@/src/components/ui/Button";
import { Card } from "@/src/components/ui/Card";
import { Badge } from "@/src/components/ui/Badge";

export default function AppointmentsPage() {
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [loading, setLoading] = useState(true);
  const [filterDate, setFilterDate] = useState(
    new Date().toISOString().split("T")[0],
  );

  // Guarda o id do agendamento que acabou de ser cancelado
  // para exibir o botão de WhatsApp
  const [justCancelledId, setJustCancelledId] = useState<string | null>(null);

  useEffect(() => {
    loadAppointments();
  }, [filterDate]);

  async function loadAppointments() {
    setLoading(true);
    try {
      const data = await api.get<Appointment[]>(
        `/api/appointments${filterDate ? `?date=${filterDate}` : ""}`,
      );
      setAppointments(data);
    } catch {
      toast.error("Erro ao carregar agendamentos.");
    } finally {
      setLoading(false);
    }
  }

  async function handleCancel(appointment: Appointment) {
    if (!confirm(`Cancelar agendamento de ${appointment.clientName}?`)) return;

    try {
      await api.patch(`/api/appointments/${appointment.id}/cancel`);
      toast.success("Agendamento cancelado.");

      // Marca este agendamento como "recém cancelado"
      // para exibir o botão de WhatsApp
      setJustCancelledId(appointment.id);

      loadAppointments();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Erro ao cancelar.";
      toast.error(message);
    }
  }

  async function handleComplete(id: string) {
    try {
      await api.patch(`/api/appointments/${id}/complete`);
      toast.success("Agendamento concluído!");
      loadAppointments();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Erro ao concluir.";
      toast.error(message);
    }
  }

  function buildWhatsAppUrl(appointment: Appointment): string {
    // Remove tudo que não é número do telefone
    const phone = appointment.clientPhone.replace(/\D/g, "");

    // Formata a data de YYYY-MM-DD para DD/MM/YYYY
    const [y, m, day] = appointment.appointmentDate.split("-");
    const formattedDate = `${day}/${m}/${y}`;

    const message = [
      `Olá, ${appointment.clientName}! 👋`,
      ``,
      `Precisei cancelar nosso agendamento:`,
      `📅 ${formattedDate} às ${appointment.startTime}`,
      `✂️ ${appointment.serviceName}`,
      ``,
      `Fique à vontade para remarcar pelo link abaixo!`,
    ].join("\n");

    return `https://wa.me/55${phone}?text=${encodeURIComponent(message)}`;
  }

  function formatDate(d: string) {
    const [y, m, day] = d.split("-");
    return `${day}/${m}/${y}`;
  }

  return (
    <div className="flex flex-col gap-4 md:gap-6">
      <div>
        <h1 className="text-xl md:text-2xl font-bold text-zinc-900 dark:text-zinc-100">
          Agendamentos
        </h1>
        <p className="text-zinc-500 dark:text-zinc-400 text-xs md:text-sm mt-1">
          Visualize e gerencie sua agenda
        </p>
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
          <Button
            variant="secondary"
            onClick={() => setFilterDate("")}
            className="w-full sm:w-auto"
          >
            Ver todos
          </Button>
        </div>
      </Card>

      {loading ? (
        <p className="text-zinc-500 dark:text-zinc-400 text-sm">
          Carregando...
        </p>
      ) : appointments.length === 0 ? (
        <Card>
          <p className="text-sm text-zinc-400 dark:text-zinc-500">
            Nenhum agendamento encontrado.
          </p>
        </Card>
      ) : (
        <div className="flex flex-col gap-3">
          {appointments.map((a) => (
            <Card key={a.id} className="p-4 md:p-6">
              <div className="flex flex-col gap-3">
                {/* Cabeçalho: nome + badge */}
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-center gap-2 flex-wrap">
                    <p className="font-medium text-zinc-900 dark:text-zinc-100">
                      {a.clientName}
                    </p>
                    <Badge status={a.status} />
                  </div>
                </div>

                {/* Detalhes */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-1">
                  <p className="text-sm text-zinc-500 dark:text-zinc-400">
                    📱 {a.clientPhone}
                  </p>
                  <p className="text-sm text-zinc-500 dark:text-zinc-400">
                    ✂️ {a.serviceName} · {a.serviceDuration} min
                    {a.servicePrice
                      ? ` · R$ ${a.servicePrice.toFixed(2).replace(".", ",")}`
                      : ""}
                  </p>
                  <p className="text-sm text-zinc-500 dark:text-zinc-400">
                    📅 {formatDate(a.appointmentDate)} · {a.startTime} -{" "}
                    {a.endTime}
                  </p>
                </div>

                {/* Ações para agendamentos ainda agendados */}
                {a.status === "Scheduled" && (
                  <div className="flex flex-col sm:flex-row gap-2 pt-1">
                    <Button
                      onClick={() => handleComplete(a.id)}
                      className="w-full sm:w-auto"
                    >
                      Concluir
                    </Button>
                    <Button
                      variant="danger"
                      onClick={() => handleCancel(a)}
                      className="w-full sm:w-auto"
                    >
                      Cancelar
                    </Button>
                  </div>
                )}

                {/* Botão WhatsApp: aparece apenas para o agendamento
                    que acabou de ser cancelado nesta sessão */}
                {a.status === "Cancelled" && justCancelledId === a.id && (
                  <a href={buildWhatsAppUrl(a)}
                    target="_blank"
                    rel="noreferrer"
                    className="flex items-center justify-center gap-2 w-full sm:w-auto bg-green-500 
                    hover:bg-green-600 text-white font-medium py-2 px-4 rounded-lg transition-colors text-sm">
                    <svg
                      viewBox="0 0 24 24"
                      className="w-4 h-4 fill-current shrink-0"
                    >
                      <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z" />
                    </svg>
                    Avisar {a.clientName} pelo WhatsApp
                  </a>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
