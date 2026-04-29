"use client";

import { useEffect, useState, useCallback } from "react";
import Link from "next/link";
import toast from "react-hot-toast";
import { api } from "@/src/lib/api";
import { Appointment, Profile, WorkingHour } from "@/src/types";
import { Card } from "@/src/components/ui/Card";
import { Badge } from "@/src/components/ui/Badge";
import { Button } from "@/src/components/ui/Button";
import { getLocalToday } from "@/src/lib/date";

// Converte "HH:mm" para minutos desde meia-noite
// ex: "09:30" → 570
function timeToMinutes(time: string): number {
  const [h, m] = time.split(":").map(Number);
  return h * 60 + m;
}

// Converte minutos desde meia-noite para "HH:mm"
// ex: 570 → "09:30"
function minutesToTime(minutes: number): string {
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`;
}

export default function DashboardPage() {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [todayHours, setTodayHours] = useState<WorkingHour | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const today = getLocalToday();

      const [prof, me] = await Promise.all([
        api.get<Profile>("/api/profile").catch(() => null),
        api.get<{ isEmailConfirmed: boolean }>("/api/auth/me"),
      ]);

      setProfile(prof);

      if (prof) {
        // Busca agendamentos e horários em paralelo
        const [appts, hours] = await Promise.all([
          api.get<Appointment[]>(`/api/appointments?date=${today}`),
          api.get<WorkingHour[]>("/api/workinghours"),
        ]);

        setAppointments(appts);

        // Pega o horário do dia da semana atual
        // 0=Domingo, 1=Segunda... 6=Sábado
        const todayDayOfWeek = new Date().getDay();
        const todayHour =
          hours.find((h) => h.dayOfWeek === todayDayOfWeek) ?? null;
        setTodayHours(todayHour);
      } else {
        setAppointments([]);
        setTodayHours(null);
      }
    } catch {
      toast.error("Erro inesperado ao carregar dados.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
    const handleFocus = () => load();
    window.addEventListener("focus", handleFocus);
    return () => window.removeEventListener("focus", handleFocus);
  }, [load]);

  if (loading)
    return <p className="text-zinc-500 dark:text-zinc-400">Carregando...</p>;

  const scheduled = appointments.filter((a) => a.status === "Scheduled").length;
  const completed = appointments.filter((a) => a.status === "Completed").length;
  const revenue = appointments
    .filter((a) => a.status === "Completed")
    .reduce((sum, a) => sum + (a.servicePrice ?? 0), 0);

  const todayFormatted = new Date().toLocaleDateString("pt-BR", {
    weekday: "long",
    day: "2-digit",
    month: "long",
  });

  // ── Cálculo do indicador de horário ────────────────────────────────────────

  // Horário em que o último agendamento do dia termina
  const activeAppointments = appointments.filter(
    (a) => a.status !== "Cancelled",
  );
  const lastAppointmentEnd =
    activeAppointments.length > 0
      ? activeAppointments.reduce(
          (latest, a) =>
            timeToMinutes(a.endTime) > timeToMinutes(latest)
              ? a.endTime
              : latest,
          "00:00",
        )
      : null;

  // Progresso do dia: quanto do expediente já foi consumido por agendamentos
  // ex: expediente 09:00-18:00 = 540 min, agendamentos ocupam 180 min = 33%
  let dayProgressPercent = 0;
  if (todayHours?.isOpen && lastAppointmentEnd) {
    const expedienteTotal =
      timeToMinutes(todayHours.endTime) - timeToMinutes(todayHours.startTime);
    const ocupado =
      timeToMinutes(lastAppointmentEnd) - timeToMinutes(todayHours.startTime);
    dayProgressPercent = Math.min(
      Math.round((ocupado / expedienteTotal) * 100),
      100,
    );
  }

  // Verifica se o barbeiro vai trabalhar além do horário previsto
  const workingOvertime =
    todayHours?.isOpen &&
    lastAppointmentEnd &&
    timeToMinutes(lastAppointmentEnd) > timeToMinutes(todayHours.endTime);

  return (
    <div className="flex flex-col gap-4 md:gap-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-xl md:text-2xl font-bold text-zinc-900 dark:text-zinc-100">
            Início
          </h1>
          <p className="text-zinc-500 dark:text-zinc-400 text-xs md:text-sm mt-1 capitalize">
            {todayFormatted}
          </p>
        </div>
        <Button variant="secondary" onClick={load} className="shrink-0">
          🔄 Atualizar
        </Button>
      </div>

      {!profile && (
        <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 text-sm text-amber-800 dark:bg-amber-950 dark:border-amber-700 dark:text-amber-300">
          ⚠️ Seu perfil ainda não foi configurado.{" "}
          <Link href="/dashboard/profile" className="font-medium underline">
            Configure agora
          </Link>{" "}
          para gerar seu link público.
        </div>
      )}

      {/* ── Indicador de horário de trabalho ─────────────────────────────── */}
      {todayHours && (
        <Card>
          {!todayHours.isOpen ? (
            // Dia fechado
            <div className="flex items-center gap-3">
              <span className="text-2xl">🚫</span>
              <div>
                <p className="font-medium text-zinc-900 dark:text-zinc-100">
                  Hoje é dia de folga
                </p>
                <p className="text-xs text-zinc-500 dark:text-zinc-400 mt-0.5">
                  Nenhum atendimento configurado para hoje
                </p>
              </div>
            </div>
          ) : (
            <div className="flex flex-col gap-3">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="font-medium text-zinc-900 dark:text-zinc-100">
                    Expediente de hoje
                  </p>
                  <p className="text-xs text-zinc-500 dark:text-zinc-400 mt-0.5">
                    {todayHours.startTime} até {todayHours.endTime}
                    {todayHours.hasLunchBreak &&
                    todayHours.lunchStart &&
                    todayHours.lunchEnd
                      ? ` · Almoço ${todayHours.lunchStart} - ${todayHours.lunchEnd}`
                      : ""}
                  </p>
                </div>

                <div className="text-right shrink-0">
                  {activeAppointments.length === 0 ? (
                    <p className="text-sm text-zinc-400 dark:text-zinc-500">
                      Sem agendamentos
                    </p>
                  ) : (
                    <>
                      <p className="text-sm font-medium text-zinc-900 dark:text-zinc-100">
                        {activeAppointments.length} atendimento
                        {activeAppointments.length !== 1 ? "s" : ""}
                      </p>
                      {lastAppointmentEnd && (
                        <p
                          className={`text-xs mt-0.5 ${
                            workingOvertime
                              ? "text-red-600 dark:text-red-400 font-medium"
                              : "text-zinc-500 dark:text-zinc-400"
                          }`}
                        >
                          {workingOvertime ? "⚠️ " : ""}
                          Termina às {lastAppointmentEnd}
                          {workingOvertime ? " (além do expediente)" : ""}
                        </p>
                      )}
                    </>
                  )}
                </div>
              </div>

              {/* Barra de progresso do dia */}
              {activeAppointments.length > 0 && (
                <div>
                  <div className="h-2 bg-zinc-100 dark:bg-zinc-800 rounded-full overflow-hidden">
                    <div
                      className={`h-full rounded-full transition-all ${
                        workingOvertime
                          ? "bg-red-500 dark:bg-red-400"
                          : dayProgressPercent >= 80
                            ? "bg-amber-500 dark:bg-amber-400"
                            : "bg-emerald-500 dark:bg-emerald-400"
                      }`}
                      style={{ width: `${dayProgressPercent}%` }}
                    />
                  </div>
                  <div className="flex justify-between mt-1">
                    <span className="text-xs text-zinc-400 dark:text-zinc-500">
                      {todayHours.startTime}
                    </span>
                    <span className="text-xs text-zinc-400 dark:text-zinc-500">
                      {dayProgressPercent}% ocupado
                    </span>
                    <span className="text-xs text-zinc-400 dark:text-zinc-500">
                      {todayHours.endTime}
                    </span>
                  </div>
                </div>
              )}
            </div>
          )}
        </Card>
      )}

      {/* ── Cards de resumo ───────────────────────────────────────────────── */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 md:gap-4">
        <Card className="p-4 md:p-6">
          <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400">
            Agendados hoje
          </p>
          <p className="text-2xl md:text-3xl font-bold text-zinc-900 dark:text-zinc-100 mt-1">
            {scheduled}
          </p>
        </Card>
        <Card className="p-4 md:p-6">
          <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400">
            Concluídos hoje
          </p>
          <p className="text-2xl md:text-3xl font-bold text-green-600 dark:text-green-400 mt-1">
            {completed}
          </p>
        </Card>
        <Card className="p-4 md:p-6">
          <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400">
            Faturamento hoje
          </p>
          <p className="text-2xl md:text-3xl font-bold text-emerald-600 dark:text-emerald-400 mt-1">
            {revenue > 0 ? "R$ " + revenue.toFixed(2).replace(".", ",") : "—"}
          </p>
        </Card>
        <Card className="p-4 md:p-6 col-span-2 md:col-span-1">
          <p className="text-xs md:text-sm text-zinc-500 dark:text-zinc-400">
            Seu link público
          </p>
          {profile ? (
            <a href={"/b/" + profile.slug}
              target="_blank"
              rel="noreferrer"
              className="text-sm font-medium text-zinc-900 dark:text-zinc-100 hover:underline mt-1 block truncate">
              {"/b/" + profile.slug}
            </a>
          ) : (
            <p className="text-sm text-zinc-400 mt-1">Não configurado</p>
          )}
        </Card>
      </div>

      {/* ── Agenda de hoje ────────────────────────────────────────────────── */}
      <Card>
        <h2 className="font-semibold text-zinc-900 dark:text-zinc-100 mb-4 text-sm md:text-base">
          Agenda de hoje
        </h2>
        {appointments.length === 0 ? (
          <p className="text-sm text-zinc-400 dark:text-zinc-500">
            Nenhum agendamento para hoje.
          </p>
        ) : (
          <div className="flex flex-col gap-3">
            {appointments
              .sort(
                (a, b) =>
                  timeToMinutes(a.startTime) - timeToMinutes(b.startTime),
              )
              .map((a) => (
                <div
                  key={a.id}
                  className="flex items-center justify-between py-3 border-b border-zinc-100 dark:border-zinc-800 last:border-0 gap-3"
                >
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-zinc-900 dark:text-zinc-100 truncate">
                      {a.clientName}
                    </p>
                    <p className="text-xs text-zinc-500 dark:text-zinc-400 truncate">
                      {a.serviceName} · {a.startTime} - {a.endTime}
                      {a.servicePrice
                        ? ` · R$ ${a.servicePrice.toFixed(2).replace(".", ",")}`
                        : ""}
                    </p>
                  </div>
                  <div className="shrink-0">
                    <Badge status={a.status} />
                  </div>
                </div>
              ))}
          </div>
        )}
      </Card>
    </div>
  );
}
