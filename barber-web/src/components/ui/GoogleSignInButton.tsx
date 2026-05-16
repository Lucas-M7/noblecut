'use client'

import { GoogleLogin, type CredentialResponse } from '@react-oauth/google'
import { useState } from 'react'
import { useAuth } from '@/src/contexts/AuthContext'
import { AuthResponse } from '@/src/types'
import { api } from '@/src/lib/api'
import toast from 'react-hot-toast'

interface Props {
  mode?: 'signin' | 'signup'
}

export function GoogleSignInButton({ mode = 'signin' }: Props) {
  const { login } = useAuth()
  const [loading, setLoading] = useState(false)

  async function handleCredential(credentialResponse: CredentialResponse) {
    if (!credentialResponse.credential) {
      toast.error('Não foi possível obter as credenciais do Google.')
      return
    }

    setLoading(true)
    try {
      const data = await api.post<AuthResponse>('/api/auth/google', {
        idToken: credentialResponse.credential,
      })
      login(data.token, data.name)
      toast.success(`Bem-vindo, ${data.name}!`)
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Erro ao entrar com Google.'
      toast.error(message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className={`w-full transition-opacity ${loading ? 'opacity-50 pointer-events-none' : ''}`}>
      <GoogleLogin
        onSuccess={handleCredential}
        onError={() => toast.error('Falha no login com Google. Tente novamente.')}
        text={mode === 'signin' ? 'signin_with' : 'signup_with'}
        shape="rectangular"
        theme="outline"
        size="large"
        width="400"
        useOneTap={false}
      />
    </div>
  )
}