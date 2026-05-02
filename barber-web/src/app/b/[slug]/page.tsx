"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Image from "next/image";
import toast from "react-hot-toast";
import { api } from "@/src/lib/api";
import { PublicBarber, Service, AvailabilityResponse } from "@/src/types";
import { Input } from "@/src/components/ui/Input";
import { Card } from "@/src/components/ui/Card";

type Step = "service" | "date" | "slot" | "confirm";

function getContrastColor(hex: string): string {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance > 0.5 ? "#000000" : "#ffffff";
}

function formatDate(d: string): string {
  const [, m, day] = d.split("-");
  return `${day}/${m}`;
}

function formatDateFull(d: string): string {
  const [y, m, day] = d.split("-");
  return `${day}/${m}/${y}`;
}

export default function PublicPage() {
  const { slug } = useParams<{ slug: string }>();

  const [barber, setBarber] = useState<PublicBarber | null>(null);
  const [services, setServices] = useState<Service[]>([]);
  const [notFound, setNotFound] = useState(false);

  const [step, setStep] = useState<Step>("service");
  const [selectedService, setSelectedService] = useState<Service | null>(null);
  const [selectedDate, setSelectedDate] = useState("");
  const [slots, setSlots] = useState<string[]>([]);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [selectedSlot, setSelectedSlot] = useState("");
  const [form, setForm] = useState({ clientName: "", clientPhone: "" });
  const [saving, setSaving] = useState(false);
  const [success, setSuccess] = useState(false);

  const today = new Date().toISOString().split("T")[0];

  useEffect(() => {
    async function load() {
      try {
        const [barberData, servicesData] = await Promise.all([
          api.get<PublicBarber>(`/api/public/${slug}`),
          api.get<Service[]>(`/api/public/${slug}/services`),
        ]);
        setBarber(barberData);
        setServices(servicesData);
      } catch {
        setNotFound(true);
      }
    }
    load();
  }, [slug]);

  async function loadSlots(date: string) {
    if (!selectedService || !date) return;
    setLoadingSlots(true);
    setSlots([]);
    setSelectedSlot("");
    try {
      const data = await api.get<AvailabilityResponse>(
        `/api/public/${slug}/availability?serviceId=${selectedService.id}&date=${date}`,
      );
      setSlots(data.slots);
      if (data.slots.length === 0) {
        toast("Nenhum horário disponível nessa data.", { icon: "📅" });
      }
    } catch {
      toast.error("Erro ao buscar horários.");
    } finally {
      setLoadingSlots(false);
    }
  }

  async function handleConfirm() {
    if (!form.clientName.trim() || !form.clientPhone.trim()) {
      toast.error("Preencha seu nome e WhatsApp.");
      return;
    }
    setSaving(true);
    try {
      await api.post(`/api/public/${slug}/appointments`, {
        serviceId: selectedService!.id,
        clientName: form.clientName,
        clientPhone: form.clientPhone,
        appointmentDate: selectedDate,
        startTime: selectedSlot,
      });
      setSuccess(true);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Erro ao agendar.";
      toast.error(message);
    } finally {
      setSaving(false);
    }
  }

  function resetFlow() {
    setSuccess(false);
    setStep("service");
    setSelectedService(null);
    setSelectedDate("");
    setSelectedSlot("");
    setSlots([]);
    setForm({ clientName: "", clientPhone: "" });
  }

  const primaryColor = barber?.primaryColor ?? "#18181b";
  const contrastColor = getContrastColor(primaryColor);

  // ── Tela: barbeiro não encontrado ─────────────────────────────────────────
  if (notFound) {
    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950 flex items-center justify-center p-4">
        <div className="text-center">
          <p className="text-4xl mb-4">✂️</p>
          <h1 className="text-xl font-bold text-zinc-900 dark:text-zinc-100">
            Barbeiro não encontrado
          </h1>
          <p className="text-zinc-500 dark:text-zinc-400 text-sm mt-2">
            Verifique o link e tente novamente.
          </p>
        </div>
      </div>
    );
  }

  // ── Tela: agendamento confirmado ──────────────────────────────────────────
  if (success) {
    const phone = barber?.phone.replace(/\D/g, "");
    const mensagem = [
      `Olá! Acabei de agendar pelo seu sistema. 😊`,
      ``,
      `*Serviço:* ${selectedService?.name}`,
      `*Data:* ${formatDateFull(selectedDate)}`,
      `*Horário:* ${selectedSlot}`,
      `*Nome:* ${form.clientName}`,
    ].join("\n");
    const whatsappUrl = `https://wa.me/55${phone}?text=${encodeURIComponent(mensagem)}`;

    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950 flex items-center justify-center p-4">
        <Card className="max-w-md w-full text-center">
          <div className="text-5xl mb-4">🎉</div>
          <h1 className="text-xl font-bold text-zinc-900 dark:text-zinc-100">
            Agendamento confirmado!
          </h1>

          <div className="text-sm text-zinc-500 dark:text-zinc-400 mt-4 flex flex-col gap-1 text-left bg-zinc-50 dark:bg-zinc-800 rounded-xl p-4">
            <p>
              <strong className="text-zinc-700 dark:text-zinc-300">
                Serviço:
              </strong>{" "}
              {selectedService?.name}
            </p>
            <p>
              <strong className="text-zinc-700 dark:text-zinc-300">
                Data:
              </strong>{" "}
              {formatDateFull(selectedDate)}
            </p>
            <p>
              <strong className="text-zinc-700 dark:text-zinc-300">
                Horário:
              </strong>{" "}
              {selectedSlot}
            </p>
            <p>
              <strong className="text-zinc-700 dark:text-zinc-300">
                Local:
              </strong>{" "}
              {barber?.businessName}
            </p>
          </div>

          <div className="flex flex-col gap-3 mt-6">
            {phone && (
              <a
                href={whatsappUrl}
                target="_blank"
                rel="noreferrer"
                className="flex items-center justify-center gap-2 w-full bg-green-500 hover:bg-green-600 text-white font-medium py-2 px-4 rounded-lg transition-colors text-sm"
              >
                <svg viewBox="0 0 24 24" className="w-5 h-5 fill-current">
                  <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z" />
                </svg>
                Avisar barbeiro pelo WhatsApp
              </a>
            )}

            <button
              onClick={resetFlow}
              className="w-full border border-zinc-200 dark:border-zinc-700 text-zinc-700 dark:text-zinc-300 font-medium py-2 px-4 rounded-lg transition-colors text-sm hover:bg-zinc-50 dark:hover:bg-zinc-800"
            >
              Fazer outro agendamento
            </button>
          </div>
        </Card>
      </div>
    );
  }

  // ── Tela principal: fluxo de agendamento ──────────────────────────────────
  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950 pb-10">
      <div className="max-w-lg mx-auto px-4">
        {/* ── Cabeçalho personalizado ──────────────────────────────────── */}
        <div
          className="rounded-b-2xl px-6 pt-10 pb-8 text-center mb-6"
          style={{ backgroundColor: primaryColor }}
        >
          <div
            className="w-20 h-20 rounded-full mx-auto mb-4 overflow-hidden border-4 flex items-center justify-center text-3xl"
            style={{ borderColor: `${contrastColor}33` }}
          >
            {barber?.photoUrl ? (
              <Image
                src={barber.photoUrl}
                alt={barber?.displayName ?? "Barbeiro"}
                width={80}
                height={80}
                className="w-full h-full object-cover"
              />
            ) : (
              <span style={{ color: contrastColor }}>✂️</span>
            )}
          </div>

          <h1 className="text-xl font-bold" style={{ color: contrastColor }}>
            {barber?.displayName ?? "..."}
          </h1>
          <p
            className="text-sm mt-1"
            style={{ color: contrastColor, opacity: 0.8 }}
          >
            {barber?.businessName}
          </p>
        </div>

        {/* ── Indicador de etapas ───────────────────────────────────────── */}
        <div className="flex items-center justify-center gap-2 mb-6">
          {(["service", "date", "slot", "confirm"] as Step[]).map((s, i) => {
            const steps = ["service", "date", "slot", "confirm"] as Step[];
            const currentIndex = steps.indexOf(step);
            const isActive = step === s;
            const isDone = currentIndex > i;

            return (
              <div key={s} className="flex items-center gap-2">
                <div
                  className="w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold transition-all"
                  style={
                    isActive
                      ? { backgroundColor: primaryColor, color: contrastColor }
                      : isDone
                        ? { backgroundColor: "#22c55e", color: "#ffffff" }
                        : { backgroundColor: "#e4e4e7", color: "#71717a" }
                  }
                >
                  {isDone ? "✓" : i + 1}
                </div>
                {i < 3 && (
                  <div className="w-8 h-px bg-zinc-300 dark:bg-zinc-700" />
                )}
              </div>
            );
          })}
        </div>

        {/* ── ETAPA 1: Escolher serviço ─────────────────────────────────── */}
        {step === "service" && (
          <Card>
            <h2 className="font-semibold text-zinc-900 dark:text-zinc-100 mb-4">
              Escolha o serviço
            </h2>

            {services.length === 0 ? (
              <p className="text-sm text-zinc-400 dark:text-zinc-500">
                Nenhum serviço disponível no momento.
              </p>
            ) : (
              <div className="flex flex-col gap-3">
                {services.map((s) => (
                  <button
                    key={s.id}
                    onClick={() => {
                      setSelectedService(s);
                      setStep("date");
                    }}
                    className="flex items-center justify-between p-4 border-2 border-zinc-200 dark:border-zinc-700 rounded-xl transition-all text-left hover:shadow-sm"
                  >
                    <div>
                      <p className="font-medium text-zinc-900 dark:text-zinc-100">
                        {s.name}
                      </p>
                      <p className="text-sm text-zinc-500 dark:text-zinc-400">
                        {s.durationMinutes} min
                      </p>
                    </div>
                    {s.price != null && (
                      <p className="font-semibold text-zinc-900 dark:text-zinc-100 shrink-0">
                        R$ {s.price.toFixed(2).replace(".", ",")}
                      </p>
                    )}
                  </button>
                ))}
              </div>
            )}
          </Card>
        )}

        {/* ── ETAPA 2: Escolher data ────────────────────────────────────── */}
        {step === "date" && (
          <Card>
            <h2 className="font-semibold text-zinc-900 dark:text-zinc-100 mb-1">
              Escolha a data
            </h2>
            <p className="text-sm text-zinc-500 dark:text-zinc-400 mb-4">
              Serviço:{" "}
              <strong className="text-zinc-700 dark:text-zinc-300">
                {selectedService?.name}
              </strong>
            </p>

            <Input
              label="Data"
              type="date"
              min={today}
              value={selectedDate}
              onChange={(e) => setSelectedDate(e.target.value)}
            />

            <div className="flex gap-3 mt-6">
              <button
                onClick={() => setStep("service")}
                className="flex-1 border border-zinc-200 dark:border-zinc-700 text-zinc-700 dark:text-zinc-300 py-2 px-4 rounded-lg text-sm font-medium hover:bg-zinc-50 dark:hover:bg-zinc-800 transition-colors"
              >
                Voltar
              </button>
              <button
                disabled={!selectedDate}
                onClick={() => {
                  setStep("slot");
                  loadSlots(selectedDate);
                }}
                className="flex-1 py-2 px-4 rounded-lg text-sm font-medium transition-all disabled:opacity-50"
                style={{ backgroundColor: primaryColor, color: contrastColor }}
              >
                Ver horários
              </button>
            </div>
          </Card>
        )}

        {/* ── ETAPA 3: Escolher horário ─────────────────────────────────── */}
        {step === "slot" && (
          <Card>
            <h2 className="font-semibold text-zinc-900 dark:text-zinc-100 mb-1">
              Escolha o horário
            </h2>
            <p className="text-sm text-zinc-500 dark:text-zinc-400 mb-4">
              {selectedService?.name} · {formatDateFull(selectedDate)}
            </p>

            {loadingSlots ? (
              <p className="text-sm text-zinc-400 dark:text-zinc-500">
                Buscando horários...
              </p>
            ) : slots.length === 0 ? (
              <p className="text-sm text-zinc-400 dark:text-zinc-500">
                Nenhum horário disponível. Tente outra data.
              </p>
            ) : (
              <div className="grid grid-cols-3 sm:grid-cols-4 gap-2">
                {slots.map((slot) => (
                  <button
                    key={slot}
                    onClick={() => setSelectedSlot(slot)}
                    className="py-2 px-3 rounded-lg border text-sm font-medium transition-all"
                    style={
                      selectedSlot === slot
                        ? {
                            backgroundColor: primaryColor,
                            color: contrastColor,
                            borderColor: primaryColor,
                          }
                        : {
                            borderColor: "#e4e4e7",
                            color: "#3f3f46",
                          }
                    }
                  >
                    {slot}
                  </button>
                ))}
              </div>
            )}

            <div className="flex gap-3 mt-6">
              <button
                onClick={() => setStep("date")}
                className="flex-1 border border-zinc-200 dark:border-zinc-700 text-zinc-700 dark:text-zinc-300 py-2 px-4 rounded-lg text-sm font-medium hover:bg-zinc-50 dark:hover:bg-zinc-800 transition-colors"
              >
                Voltar
              </button>
              <button
                disabled={!selectedSlot}
                onClick={() => setStep("confirm")}
                className="flex-1 py-2 px-4 rounded-lg text-sm font-medium transition-all disabled:opacity-50"
                style={{ backgroundColor: primaryColor, color: contrastColor }}
              >
                Continuar
              </button>
            </div>
          </Card>
        )}

        {/* ── ETAPA 4: Confirmar ────────────────────────────────────────── */}
        {step === "confirm" && (
          <Card>
            <h2 className="font-semibold text-zinc-900 dark:text-zinc-100 mb-4">
              Confirme seus dados
            </h2>

            <div className="bg-zinc-50 dark:bg-zinc-800 rounded-xl p-4 mb-6 flex flex-col gap-1">
              <p className="text-sm text-zinc-600 dark:text-zinc-300">
                ✂️ <strong>{selectedService?.name}</strong>
              </p>
              <p className="text-sm text-zinc-600 dark:text-zinc-300">
                📅 {formatDateFull(selectedDate)} às{" "}
                <strong>{selectedSlot}</strong>
              </p>
            </div>

            <div className="flex flex-col gap-4">
              <Input
                label="Seu nome"
                placeholder="João Silva"
                value={form.clientName}
                onChange={(e) =>
                  setForm({ ...form, clientName: e.target.value })
                }
                required
              />
              <Input
                label="WhatsApp (somente números)"
                placeholder="81999999999"
                type="tel"
                value={form.clientPhone}
                onChange={(e) =>
                  setForm({ ...form, clientPhone: e.target.value })
                }
                required
              />
            </div>

            <div className="flex gap-3 mt-6">
              <button
                onClick={() => setStep("slot")}
                className="flex-1 border border-zinc-200 dark:border-zinc-700 text-zinc-700 dark:text-zinc-300 py-2 px-4 rounded-lg text-sm font-medium hover:bg-zinc-50 dark:hover:bg-zinc-800 transition-colors"
              >
                Voltar
              </button>
              <button
                disabled={saving}
                onClick={handleConfirm}
                className="flex-1 py-2 px-4 rounded-lg text-sm font-medium transition-all disabled:opacity-50"
                style={{ backgroundColor: primaryColor, color: contrastColor }}
              >
                {saving ? "Confirmando..." : "Confirmar agendamento"}
              </button>
            </div>
          </Card>
        )}
      </div>
    </div>
  );
}
