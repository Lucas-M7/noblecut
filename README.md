<div align="center">

# ✂️ NobleCut

### Agenda online para barbeiros autônomos

**NobleCut** é um SaaS completo que permite ao barbeiro autônomo gerenciar sua agenda online, configurar serviços, definir horários e receber agendamentos através de um link público personalizado, sem complicação.

[![Next.js](https://img.shields.io/badge/Next.js-16-black?style=flat-square&logo=next.js)](https://nextjs.org)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-10-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Neon-336791?style=flat-square&logo=postgresql)](https://neon.tech)
[![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?style=flat-square&logo=typescript)](https://typescriptlang.org)

[🔗 Demo ao vivo](https://noblecut-staging.vercel.app) · [📋 Reportar Bug](https://github.com/Lucas-M7/noblecut/issues) · [💡 Sugerir Feature](https://github.com/Lucas-M7/noblecut/issues)

</div>

---

## 📸 Screenshots

### Painel do Barbeiro

> **[PRINT 1]** —

> **[PRINT 2]** —

> **[PRINT 3]** —

### Página Pública (visão do cliente)

> **[PRINT 4]** —

> **[PRINT 5]** —

> **[PRINT 6]** —

### Personalização

> **[PRINT 7]** —

---

## ✨ Funcionalidades

### Para o barbeiro
- **Dashboard completo** com resumo do dia, faturamento e indicador de horário de trabalho
- **Relatório financeiro** com filtros por período (hoje, semana, mês, ano) e comparativo com período anterior
- **Gerenciamento de serviços** — criar, editar, ativar e desativar
- **Configuração de horários semanais** com suporte a pausa para almoço
- **Bloqueios de agenda** — bloquear dias específicos ou intervalos com motivo
- **Dias especiais** — configurar horários diferentes para datas específicas
- **Página pública personalizável** — foto de perfil, cor principal e link único
- **Notificação via WhatsApp** ao cancelar agendamento, com mensagem pré-preenchida para o cliente
- **Tema claro e escuro** com persistência de preferência

### Para o cliente
- Acesso via link público sem necessidade de cadastro
- Visualização dos serviços ativos com preço e duração
- Seleção de data e horário disponível em tempo real
- Confirmação instantânea do agendamento

### Técnicas de segurança
- **Rate limiting** em todos os **endpoints críticos**
- **Validação de entrada** com Data Annotations nos DTOs
- **Headers de segurança HTTP** (X-Content-Type-Options, X-Frame-Options, Referrer-Policy)
- **Proteção contra enumeração de usuários** no login
- **Proteção contra race condition** no agendamento simultâneo
- **Logging estruturado** com Serilog
- **Suporte a múltiplos ambientes** (Development, Staging, Production)

---

## 🛠️ Stack

| Camada | Tecnologia |
|---|---|
| **Frontend** | Next.js 16, TypeScript, Tailwind CSS |
| **Backend** | ASP.NET Core 10, C# |
| **Banco de dados** | PostgreSQL (Neon) |
| **ORM** | Entity Framework Core |
| **Autenticação** | JWT |
| **Upload de imagens** | Cloudinary |
| **Email** | Futuramente
| **Logging** | Serilog |
| **Deploy Frontend** | Vercel |
| **Deploy Backend** | Render |

---

## 🏗️ Arquitetura

O sistema é dividido em dois projetos independentes que se comunicam via HTTP/JSON.

```
noblecut/
├── src/                          # Backend ASP.NET Core
│   ├── BarberShop.API/           # Controllers, middlewares, configuração
│   ├── BarberShop.Application/   # Services, DTOs, lógica de negócio
│   ├── BarberShop.Domain/        # Entidades, enums
│   └── BarberShop.Infrastructure/ # DbContext, migrations, configurações EF
│
└── barber-web/                   # Frontend Next.js
    └── src/
        ├── app/                  # Páginas (App Router)
        │   ├── (auth)/           # Login, cadastro, recuperação de senha
        │   ├── (dashboard)/      # Painel autenticado do barbeiro
        │   └── b/[slug]/         # Página pública do barbeiro
        ├── components/           # Componentes reutilizáveis
        ├── lib/                  # Cliente HTTP, utilitários
        ├── contexts/             # AuthContext, ThemeContext
        └── types/                # Tipos TypeScript
```

### Fluxo de dados

```
Cliente (browser)
       │
       ▼
  Next.js (Vercel)
  ├── /b/[slug]  ──► GET /api/public/{slug}  (sem auth)
  └── /dashboard ──► Bearer Token JWT         (autenticado)
                              │
                              ▼
                    ASP.NET Core API (Render)
                              │
                              ▼
                       PostgreSQL (Neon)
```
---

## 🌍 Deploy em produção

O sistema utiliza serviços gratuitos para o MVP:

| Serviço | Plataforma | Plano |
|---|---|---|
| Frontend | [Vercel](https://vercel.com) | Free |
| Backend | [Render](https://render.com) | Free |
| Banco de dados | [Neon](https://neon.tech) | Free |
| Imagens | [Cloudinary](https://cloudinary.com) | Free |

---

## 📋 Regras de negócio

1. Se o dia estiver configurado como fechado no horário semanal, não mostrar horários
2. Se houver bloqueio para a data, não mostrar horários
3. Bloqueios têm prioridade sobre o horário semanal
4. Horários especiais têm prioridade sobre o horário semanal
5. Serviços inativos não aparecem para o cliente
6. Não é permitido agendar no passado
7. Não é permitido conflito de horário
8. Os horários respeitam a duração do serviço e a pausa para almoço
9. O último slot possível é `EndTime - DurationMinutes`
10. Cancelar um agendamento libera o horário automaticamente
11. O slug público do barbeiro é único em todo o sistema

---

## 🔒 Segurança

- **Rate limiting** por IP nos endpoints de login (5/min), cadastro (3/hora) e agendamento público (10/hora)
- **Senhas** com hash BCrypt
- **JWT** com expiração de 1 dia
- **Validação de entrada** com Data Annotations em todos os DTOs
- **Headers de segurança** HTTP em todas as respostas
- **Anti-enumeração** de usuários no login (mesma mensagem e tempo para email/senha errados)
- **Proteção contra race condition** no agendamento simultâneo
- **Logs** nunca expõem stack trace ao cliente em erros 500
- **Upload de imagens** limitado a 5MB, apenas JPG/PNG/WebP

---

## 🗂️ Ambientes

| Ambiente | Backend | Frontend | Banco |
|---|---|---|---|
| **Development** | `localhost:5000` | `localhost:3000` | PostgreSQL local |
| **Staging** | Render (staging) | Vercel (staging) | Neon branch staging |
| **Production** | Render (prod) | Vercel (prod) | Neon branch main |

---

## 🧱 Padrões e boas práticas

- **Thin Controllers** — controllers apenas recebem e delegam, sem lógica de negócio
- **Single Responsibility** — cada classe e método tem uma responsabilidade
- **DRY** — `BarberProfileResolver` elimina lookup duplicado em todos os services
- **Fail Fast** — validações no topo dos métodos, antes de ir ao banco
- **Early Return** — casos especiais tratados imediatamente, sem aninhamento
- **Projeção** — queries buscam apenas os campos necessários
- **Records imutáveis** para todos os DTOs
- **Logging estruturado** com contexto em operações críticas

---

<div align="center">

Desenvolvido por [Lucas M7](https://github.com/Lucas-M7)

</div>
