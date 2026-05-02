"use client";

import { useEffect, useRef, useState } from "react";
import Image from "next/image";
import toast from "react-hot-toast";
import { api } from "@/src/lib/api";
import { uploadPhoto } from "@/src/lib/upload";
import { Profile } from "@/src/types";
import { Button } from "@/src/components/ui/Button";
import { Input } from "@/src/components/ui/Input";
import { Card } from "@/src/components/ui/Card";

// Cores predefinidas para seleção rápida
const PRESET_COLORS = [
  { label: "Preto", value: "#18181b" },
  { label: "Azul", value: "#1d4ed8" },
  { label: "Verde", value: "#15803d" },
  { label: "Vermelho", value: "#b91c1c" },
  { label: "Roxo", value: "#7e22ce" },
  { label: "Laranja", value: "#c2410c" },
  { label: "Marrom", value: "#78350f" },
  { label: "Cinza", value: "#374151" },
];

export default function ProfilePage() {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploadingPhoto, setUploadingPhoto] = useState(false);
  const [profile, setProfile] = useState<Profile | null>(null);
  const [form, setForm] = useState({
    displayName: "",
    businessName: "",
    phone: "",
    slug: "",
    primaryColor: "#18181b",
  });

  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    async function load() {
      try {
        const data = await api.get<Profile>("/api/profile");
        setProfile(data);
        setForm({
          displayName: data.displayName,
          businessName: data.businessName,
          phone: data.phone,
          slug: data.slug,
          primaryColor: data.primaryColor,
        });
      } catch {
        // Perfil ainda não existe
      } finally {
        setLoading(false);
      }
    }
    load();
  }, []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    if (!form.displayName.trim()) {
      toast.error("Nome de exibição é obrigatório.");
      return;
    }
    if (!form.businessName.trim()) {
      toast.error("Nome do negócio é obrigatório.");
      return;
    }
    if (!form.slug.trim()) {
      toast.error("Slug é obrigatório.");
      return;
    }
    if (!/^[a-z0-9-]+$/.test(form.slug)) {
      toast.error(
        "Slug deve conter apenas letras minúsculas, números e hífens.",
      );
      return;
    }

    setSaving(true);
    try {
      const data = await api.put<Profile>("/api/profile", form);
      setProfile(data);
      toast.success("Perfil salvo!");
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Erro ao salvar.";
      toast.error(message);
    } finally {
      setSaving(false);
    }
  }

  async function handlePhotoChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    // Valida no frontend antes de enviar
    if (file.size > 5 * 1024 * 1024) {
      toast.error("Arquivo muito grande. Máximo 5MB.");
      return;
    }

    const allowed = ["image/jpeg", "image/png", "image/webp"];
    if (!allowed.includes(file.type)) {
      toast.error("Formato inválido. Use JPG, PNG ou WebP.");
      return;
    }

    setUploadingPhoto(true);
    try {
      const updated = await uploadPhoto(file);
      setProfile((prev) =>
        prev ? { ...prev, photoUrl: (updated as Profile).photoUrl } : null,
      );
      toast.success("Foto atualizada!");
    } catch (err: unknown) {
      const message =
        err instanceof Error ? err.message : "Erro ao enviar foto.";
      toast.error(message);
    } finally {
      setUploadingPhoto(false);
      // Limpa o input para permitir selecionar o mesmo arquivo novamente
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  }

  async function handleRemovePhoto() {
    if (!confirm("Remover foto de perfil?")) return;
    try {
      const data = await api.delete<Profile>("/api/profile/photo");
      setProfile(data);
      toast.success("Foto removida.");
    } catch {
      toast.error("Erro ao remover foto.");
    }
  }

  function handleSlugChange(value: string) {
    const slug = value
      .toLowerCase()
      .replace(/\s+/g, "-")
      .replace(/[^a-z0-9-]/g, "");
    setForm({ ...form, slug });
  }

  if (loading)
    return <p className="text-zinc-500 dark:text-zinc-400">Carregando...</p>;

  return (
    <div className="flex flex-col gap-4 md:gap-6">
      <div>
        <h1 className="text-xl md:text-2xl font-bold text-zinc-900 dark:text-zinc-100">
          Meu Perfil
        </h1>
        <p className="text-zinc-500 dark:text-zinc-400 text-xs md:text-sm mt-1">
          Configure as informações públicas da sua barbearia
        </p>
      </div>

      {/* ── Foto de perfil ──────────────────────────────────────────────── */}
      <Card>
        <h2 className="font-semibold text-zinc-900 dark:text-zinc-100 mb-4">
          Foto de perfil
        </h2>

        <div className="flex items-center gap-6">
          {/* Preview da foto */}
          <div
            className="w-20 h-20 rounded-full flex items-center justify-center text-white text-2xl shrink-0 overflow-hidden"
            style={{ backgroundColor: form.primaryColor }}
          >
            {profile?.photoUrl ? (
              <Image
                src={profile.photoUrl}
                alt="Foto de perfil"
                width={80}
                height={80}
                className="w-full h-full object-cover"
              />
            ) : (
              "✂️"
            )}
          </div>

          <div className="flex flex-col gap-2">
            <p className="text-sm text-zinc-500 dark:text-zinc-400">
              JPG, PNG ou WebP · Máximo 5MB
            </p>
            <div className="flex gap-2 flex-wrap">
              <Button
                variant="secondary"
                onClick={() => fileInputRef.current?.click()}
                loading={uploadingPhoto}
              >
                {profile?.photoUrl ? "Trocar foto" : "Adicionar foto"}
              </Button>

              {profile?.photoUrl && (
                <Button variant="danger" onClick={handleRemovePhoto}>
                  Remover
                </Button>
              )}
            </div>
          </div>

          {/* Input de arquivo oculto */}
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            onChange={handlePhotoChange}
            className="hidden"
          />
        </div>
      </Card>

      {/* ── Cor principal ───────────────────────────────────────────────── */}
      <Card>
        <h2 className="font-semibold text-zinc-900 dark:text-zinc-100 mb-1">
          Cor principal
        </h2>
        <p className="text-xs text-zinc-500 dark:text-zinc-400 mb-4">
          Aparece nos botões e destaques da sua página pública
        </p>

        {/* Preview da cor aplicada */}
        <div
          className="w-full h-10 rounded-lg mb-4 flex items-center justify-center text-white text-sm font-medium transition-colors"
          style={{ backgroundColor: form.primaryColor }}
        >
          Prévia da cor · {form.primaryColor}
        </div>

        {/* Cores predefinidas */}
        <div className="flex flex-wrap gap-2 mb-4">
          {PRESET_COLORS.map((color) => (
            <button
              key={color.value}
              title={color.label}
              onClick={() => setForm({ ...form, primaryColor: color.value })}
              className={`w-8 h-8 rounded-full border-2 transition-all ${
                form.primaryColor === color.value
                  ? "border-zinc-900 dark:border-zinc-100 scale-110"
                  : "border-transparent hover:scale-105"
              }`}
              style={{ backgroundColor: color.value }}
            />
          ))}
        </div>

        {/* Input de cor personalizada */}
        <div className="flex items-center gap-3">
          <input
            type="color"
            value={form.primaryColor}
            onChange={(e) => setForm({ ...form, primaryColor: e.target.value })}
            className="w-10 h-10 rounded-lg border border-zinc-300 dark:border-zinc-600 cursor-pointer bg-transparent"
          />
          <span className="text-sm text-zinc-500 dark:text-zinc-400">
            Ou escolha uma cor personalizada
          </span>
        </div>
      </Card>

      {/* ── Dados do perfil ─────────────────────────────────────────────── */}
      <Card>
        <h2 className="font-semibold text-zinc-900 dark:text-zinc-100 mb-4">
          Dados da barbearia
        </h2>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <Input
            label="Seu nome (exibido para clientes)"
            placeholder="Lucas Barber"
            value={form.displayName}
            onChange={(e) => setForm({ ...form, displayName: e.target.value })}
            required
          />
          <Input
            label="Nome da barbearia"
            placeholder="Barbearia do Lucas"
            value={form.businessName}
            onChange={(e) => setForm({ ...form, businessName: e.target.value })}
            required
          />
          <Input
            label="Telefone / WhatsApp (sem código do país)"
            placeholder="81999999999"
            type="tel"
            value={form.phone}
            onChange={(e) => setForm({ ...form, phone: e.target.value })}
          />
          <div>
            <Input
              label="Slug (seu link público)"
              placeholder="lucas-barber"
              value={form.slug}
              onChange={(e) => handleSlugChange(e.target.value)}
              required
            />
            {form.slug && (
              <p className="text-xs text-zinc-400 dark:text-zinc-500 mt-1">
                Seu link:{" "}
                <a
                  href={"/b/" + form.slug}
                  target="_blank"
                  rel="noreferrer"
                  className="text-zinc-700 dark:text-zinc-300 font-medium hover:underline"
                >
                  {typeof window !== "undefined" ? window.location.origin : ""}
                  /b/{form.slug}
                </a>
              </p>
            )}
          </div>

          <Button
            type="submit"
            loading={saving}
            className="mt-2 w-full sm:w-auto"
          >
            Salvar perfil
          </Button>
        </form>
      </Card>
    </div>
  );
}
