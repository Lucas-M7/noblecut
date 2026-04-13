"use client";

import { useEffect, useState, useCallback } from "react";
import Link from "next/link";
import toast from "react-hot-toast";
import { getLocalToday } from "@/src/lib/date";
import { api } from "@/src/lib/api";
import { Appointment, Profile } from "@/src/types";
import { Card } from "@/src/components/ui/Card";
import { Badge } from "@/src/components/ui/Badge";
import { Button } from "@/src/components/ui/Button";

export default function DashboardPage() {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [isEmailConfirmed, setIsEmailConfirmed] = useState(true);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const today = getLocalToday()
      const prof = await api.get<Profile>("/api/profile").catch(() => null);
      setProfile(prof);

      if (prof) {
        const appts = await api.get<Appointment[]>(
          `/api/appointments?date=${today}`,
        );
        setAppointments(appts);
      } else {
        setAppointments([]);
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

  async function handleResendConfirmation() {
    try {
      await api.post("/api/auth/resend-confirmation", {});
      toast.success("E-mail de confirmação reenviado!");
    } catch {
      toast.error("Erro ao reenviar.");
    }
  }

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

      {/* {!isEmailConfirmed && (
        <div className="bg-blue-50 border border-blue-200 rounded-xl p-4 text-sm text-blue-800 dark:bg-blue-950 dark:border-blue-700 dark:text-blue-300 flex items-start justify-between gap-4">
          <p>📧 Confirme seu e-mail para garantir o acesso à sua conta.</p>
          <button
            onClick={handleResendConfirmation}
            className="text-blue-700 dark:text-blue-300 font-medium underline shrink-0"
          >
            Reenviar
          </button>
        </div>
      )} */}

      {!profile && (
        <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 text-sm text-amber-800 dark:bg-amber-950 dark:border-amber-700 dark:text-amber-300">
          ⚠️ Seu perfil ainda não foi configurado.{" "}
          <Link href="/dashboard/profile" className="font-medium underline">
            Configure agora
          </Link>{" "}
          para gerar seu link público.
        </div>
      )}

      {/* Cards: 2 colunas no mobile, 4 no desktop */}
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
            <a
              href={"/b/" + profile.slug}
              target="_blank"
              rel="noreferrer"
              className="text-sm font-medium text-zinc-900 dark:text-zinc-100 hover:underline mt-1 block truncate"
            >
              {"/b/" + profile.slug}
            </a>
          ) : (
            <p className="text-sm text-zinc-400 mt-1">Não configurado</p>
          )}
        </Card>
      </div>

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
            {appointments.map((a) => (
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
