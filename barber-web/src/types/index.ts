// Definindo "formatos" dos dados que vêm da API
// Esses tipos espelham exatamente os DTOs do backend

export interface AuthResponse {
    token: string
    name: string
    email: string
}

export interface Profile {
    id: string
    displayName: string
    businessName: string
    phone: string
    slug: string
    photoUrl: string | null
    primaryColor: string
}

export interface Service {
    id: string
    name: string
    durationMinutes: number
    price: number | null
    isActive: boolean
}

export interface WorkingHour {
    id: string
    dayOfWeek: number // 0=Domingo, 1=Segunda... 6=Sábado
    isOpen: boolean
    startTime: string // "09:00"
    endTime: string   // "18:00"
    hasLunchBreak: boolean
    lunchStart: string | null
    lunchEnd: string | null
}

export interface Block {
    id: string
    startDate: string // "2026-07-10"
    endDate: string
    reason: string | null
}

export interface Appointment {
    id: string
    clientName: string
    clientPhone: string
    serviceName: string
    serviceDuration: number
    servicePrice: number | null
    appointmentDate: string
    startTime: string
    endTime: string
    status: 'Scheduled' | 'Completed' | 'Cancelled'
}

export interface PublicBarber {
    id: string
    displayName: string
    businessName: string
    phone: string
    slug: string
    photoUrl: string | null
    primaryColor: string
}

export interface AvailabilityResponse {
    date: string
    slots: string[]
}

export interface PeriodSummary {
  revenue: number
  appointments: number
  previousRevenue: number | null
  previousAppointments: number | null
  revenueChangePercent: number | null
}

export interface DailyRevenue {
  date: string
  label: string
  revenue: number
  appointments: number
}

export interface ReportSummary {
  periodLabel: string
  today: PeriodSummary
  selectedPeriod: PeriodSummary
  mostPopularService: string | null
  bestDayOfWeek: string | null
  chartData: DailyRevenue[]
}