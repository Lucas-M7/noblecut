'use client'

import { useState } from 'react'
import Link from 'next/link'
import toast from 'react-hot-toast'
import { useAuth } from '@/src/contexts/AuthContext'
import { api } from '@/src/lib/api'
import { AuthResponse } from '@/src/types'
import { Button } from '@/src/components/ui/Button'
import { Input } from '@/src/components/ui/Input'
import { Card } from '@/src/components/ui/Card'
import { GoogleSignInButton } from '@/src/components/ui/GoogleSignInButton'
import { OrDivider } from '@/src/components/ui/OrDivider'

export default function RegisterPage() {
  const { login } = useAuth()
  const [loading, setLoading] = useState(false)
  const [form, setForm] = useState({ name: '', email: '', password: '' })

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()

    if (form.password.length < 6) {
      toast.error('A senha deve ter pelo menos 6 caracteres.')
      return
    }

    setLoading(true)
    try {
      const data = await api.post<AuthResponse>('/api/auth/register', form)
      login(data.token, data.name)

      if (!data.isEmailConfirmed) {
        toast.success('Conta criada! Verifique seu e-mail para confirmar o cadastro.', {
          duration: 6000,
        })
      } else {
        toast.success('Conta criada com sucesso!')
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Erro ao criar conta.'
      toast.error(message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-zinc-900 dark:text-zinc-100">✂️ Noblecut</h1>
          <p className="text-zinc-500 dark:text-zinc-400 mt-2">Crie sua conta</p>
        </div>

        <Card>
          <GoogleSignInButton mode="signup" />

          <OrDivider />

          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <Input
              label="Seu nome"
              placeholder="Lucas Silva"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              required
            />
            <Input
              label="E-mail"
              type="email"
              placeholder="seu@email.com"
              value={form.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
              required
            />
            <Input
              label="Senha"
              type="password"
              placeholder="Mínimo 6 caracteres"
              value={form.password}
              onChange={(e) => setForm({ ...form, password: e.target.value })}
              required
            />
            <Button type="submit" loading={loading} className="w-full mt-2">
              Criar conta
            </Button>
          </form>

          <p className="text-center text-sm text-zinc-500 dark:text-zinc-400 mt-4">
            Já tem conta?{' '}
            <Link href="/login" className="text-zinc-900 dark:text-zinc-100 font-medium hover:underline">
              Entrar
            </Link>
          </p>
        </Card>
      </div>
    </div>
  )
}